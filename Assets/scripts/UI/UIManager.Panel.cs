using System;
using System.Collections.Generic;

public sealed partial class UIManager
{
    private readonly Dictionary<UIBase, UIPresentationSpec>
        _panelPresentations = new();

    public T PushPanel<T>(
        UIPresentationSpec presentation, 
        Action<T> afterPresented = null)
        where T : UIBase, IUIPanel
    {
        if (presentation == null)
            throw new ArgumentNullException(nameof(presentation));

        T panel = Require<T>();

        if (_panelStack.Count > 0 && _panelStack.Peek() != panel)
            SetVisible(_panelStack.Peek(), false);

        RemovePanelIfPresent(panel);
        _panelStack.Push(panel);
        _panelPresentations[panel] = presentation;

        Mount(panel, _panelLayer);
        ApplyPresentation(panel, presentation, UnityDisplayContextProvider.GetCurrent());
        SetVisible(panel, true);

        afterPresented?.Invoke(panel);
        return panel;
    }

    public UIBase PopPanel(Action<UIBase> afterPopped = null)
    {
        if (_panelStack.Count == 0)
            return null;

        UIBase popped = _panelStack.Pop();
        SetVisible(popped, false);
        afterPopped?.Invoke(popped);

        if (_panelStack.Count > 0)
        {
            UIBase previous = _panelStack.Peek();
            if (_panelPresentations.TryGetValue(previous, out UIPresentationSpec presentation))
            {
                ApplyPresentation(previous, presentation, UnityDisplayContextProvider.GetCurrent());
            }

            SetVisible(previous, true);
        }

        return popped;
    }

    public void PopAllPanels(Action<UIBase> afterPopped = null)
    {
        while (_panelStack.Count > 0)
            PopPanel(afterPopped);
    }

    public void ReapplyVisible(in DisplayContext display)
    {
        ReapplyCurrentRoot(display);

        if (_panelStack.Count == 0)
            return;

        UIBase panel = _panelStack.Peek();
        if (_panelPresentations.TryGetValue(panel, out UIPresentationSpec presentation))
            ApplyPresentation(panel, presentation, display);
    }

    private void RemovePanelIfPresent(UIBase target)
    {
        if (!_panelStack.Contains(target))
            return;
        
        var temp = new Stack<UIBase>();
        while (_panelStack.Count > 0)
        {
            UIBase panel = _panelStack.Pop();
            if (panel != target)
                temp.Push(panel);
        }

        while (temp.Count > 0)
            _panelStack.Push(temp.Pop());
    }
}