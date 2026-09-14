using System;
using System.Collections.Generic;

public sealed partial class VNScreenBindings : IDisposable
{
    private readonly UIManager _ui;
    private readonly UIPresentationSpec _titlePresentation;
    private readonly UIPresentationSpec _demoPageAPresentation;
    private readonly UIPresentationSpec _demoPageBPresentation;
    private readonly UIPresentationSpec _demoMenuPresentation;
    private readonly UIPresentationSpec _demoConfirmPresentation;

    private UIManager UI => _ui;

    public VNScreenBindings(
        UIManager uiManager,
        UIPresentationSpec titlePresentation,
        UIPresentationSpec demoPageAPresentation, 
        UIPresentationSpec demoPageBPresentation,
        UIPresentationSpec demoMenuPresentation,
        UIPresentationSpec demoConfirmPresentation)
    {
        _ui = uiManager;
        _titlePresentation = titlePresentation;
        _demoPageAPresentation = demoPageAPresentation;
        _demoPageBPresentation = demoPageBPresentation;
        _demoMenuPresentation = demoMenuPresentation;
        _demoConfirmPresentation = demoConfirmPresentation;
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
    
    private void BindView<T>(T owner, Action<T> apply)
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