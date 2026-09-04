using System;
using System.Collections.Generic;

public sealed partial class VNScreenBindings : IDisposable
{
    private readonly UIManager _ui;
    private readonly UIPresentationSpec _titlePresentation;
    private UIManager UI => _ui;

    public VNScreenBindings(
        UIManager uiManager,
        UIPresentationSpec titlePresentation)
    {
        _ui = uiManager;
        _titlePresentation = titlePresentation;
    }
    
    public void OpenTitleMenu() => GoToTitle();
    
    /// <summary>
    /// Closes the top panel and releases its VNScreenBindings cleanup entries.
    /// </summary>
    private void ClosePanel()
    {
        UI.PopPanel(Unbind);
    }

    private void CloseAllPanels()
    {
        UI.PopAllPanels(Unbind);
    }
    
    #region Bindings
    
    private readonly Dictionary<UIBase, List<Action>> _cleanupByOwner = new();
    
    private UIBase _boundMain;
    
    private void BindMain<T>(T owner, Action<T> apply)
        where T : UIBase
    {
        if (_boundMain != null && _boundMain != owner)
            Unbind(_boundMain);

        Unbind(owner);

        _boundMain = owner;
        apply(owner);
    }

    private void BindPanel<T>(T owner, Action<T> apply)
        where T : UIBase
    {
        Unbind(owner);
        apply(owner);
    }

    private void AddBinding<T>(T owner, Action<T> attach, Action<T> detach)
        where T : UIBase
    {
        attach(owner);
        AddCleanup(owner, () => detach(owner));
    }

    private void AddCleanup(UIBase owner, Action cleanup)
    {
        if (!_cleanupByOwner.TryGetValue(owner, out List<Action> cleanups))
        {
            cleanups = new List<Action>();
            _cleanupByOwner[owner] = cleanups;
        }

        cleanups.Add(cleanup);
    }

    private void Unbind(UIBase owner)
    {
        if (!_cleanupByOwner.TryGetValue(owner, out List<Action> cleanups))
            return;

        RunCleanups(cleanups);
        _cleanupByOwner.Remove(owner);
    }

    private void UnbindAll()
    {
        foreach (var kv in _cleanupByOwner)
            RunCleanups(kv.Value);

        _cleanupByOwner.Clear();
        _boundMain = null;
    }

    private static void RunCleanups(List<Action> cleanups)
    {
        for (int i = cleanups.Count - 1; i >= 0; i--)
            cleanups[i]?.Invoke();
    }

    public void Dispose()
    {
        UnbindAll();
    }
    
    #endregion
}