using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class UIManager
{
    private readonly Dictionary<UIBase, UIPresentationSpec>
        _panelPresentations = new();

    // If the panel already exists in the stack, pop back to it.
    // A -> B -> C, then Push(B) becomes A -> B.
    //
    // afterPopped handles cleanup for panels removed during the pop.
    public T PushPanel<T>(
        UIPresentationSpec presentation,
        Action<T> afterPresented = null,
        Action<UIBase> afterClosed = null)
        where T : UIBase, IUIPanel
    {
        if (presentation == null)
            throw new ArgumentNullException(nameof(presentation));

        T panel = Require<T>();

        // 되돌아가기면 그 Panel 안의 선택(바로 위 Panel을 쌓던 순간)을, 새로 쌓으면 지금 선택을 기억한다.
        GameObject returnSelection = null;
        bool returning = _panelStack.Contains(panel);

        if (returning)
        {
            returnSelection = PopUntil(panel, afterClosed);
        }
        else
        {
            RememberSelection(panel);
            _panelStack.Push(panel);
        }

        _panelPresentations[panel] = presentation;

        Mount(panel, _panelLayer);

        DisplayContext display = UnityDisplayContextProvider.GetCurrent();
        ApplyPresentation(panel, presentation, display);

        ApplyPanelStackState();
        ApplyRootInputState();

        if (returning)
            RestoreSelection(returnSelection);

        afterPresented?.Invoke(panel);
        return panel;
    }

    public UIBase PopPanel(Action<UIBase> afterClosed = null)
    {
        UIBase popped = PopTopPanel(afterClosed);
        if (popped == null)
            return null;

        ApplyRootInputState();
        RestoreSelection(TakeSelection(popped));

        return popped;
    }

    // 전부 꺼내면 맨 처음 Panel을 쌓기 전의 선택으로 돌아간다(중간 선택을 거치지 않는다).
    public void PopAllPanels(Action<UIBase> afterClosed = null)
    {
        if (_panelStack.Count == 0)
            return;

        GameObject firstSelection = null;

        while (_panelStack.Count > 0)
        {
            UIBase popped = PopTopPanel(afterClosed);
            firstSelection = TakeSelection(popped);
        }

        ApplyRootInputState();
        RestoreSelection(firstSelection);
    }

    private UIBase PopTopPanel(Action<UIBase> afterClosed)
    {
        if (_panelStack.Count == 0)
            return null;

        UIBase popped = _panelStack.Pop();
        ClosePage(popped);

        _panelPresentations.Remove(popped);
        ApplyPanelState(
            popped,
            active: false,
            interactable: false,
            blocksRaycasts: false,
            alpha: 0f);

        afterClosed?.Invoke(popped);

        if (_panelStack.Count > 0)
        {
            DisplayContext display = UnityDisplayContextProvider.GetCurrent();

            ReapplyPanelStack(display);
            ApplyPanelStackState();
        }

        return popped;
    }

    public void ReapplyVisible(in DisplayContext display)
    {
        ReapplyCurrentRoot(display);

        if (_panelStack.Count > 0)
        {
            ReapplyPanelStack(display);
            ApplyPanelStackState();
        }

        ReapplyOverlays(display);
    }

    // 돌려주는 것: target 바로 위 Panel을 쌓던 순간의 선택(target 안의 것). 꺼낸 것이 없으면 null.
    private GameObject PopUntil(
        UIBase target,
        Action<UIBase> afterClosed = null)
    {
        GameObject selectionInTarget = null;

        while (_panelStack.Count > 0 &&
               _panelStack.Peek() != target)
        {
            UIBase popped = _panelStack.Pop();
            selectionInTarget = TakeSelection(popped);

            ClosePage(popped);

            _panelPresentations.Remove(popped);
            ApplyPanelState(
                popped,
                active: false,
                interactable: false,
                blocksRaycasts: false,
                alpha: 0f);

            afterClosed?.Invoke(popped);
        }

        return selectionInTarget;
    }

    // Keep live panels in sync with the current display.
    private void ReapplyPanelStack(in DisplayContext display)
    {
        int keep = Math.Max(1, _panelKeepAliveDepth);
        var livePanels = new List<UIBase>(keep);

        foreach (UIBase panel in _panelStack)
        {
            if (livePanels.Count >= keep)
                break;

            livePanels.Add(panel);
        }

        // Apply bottom-up so the top panel remains the last resolved result.
        for (int i = livePanels.Count - 1; i >= 0; i--)
        {
            UIBase panel = livePanels[i];

            if (_panelPresentations.TryGetValue(
                    panel,
                    out UIPresentationSpec presentation))
            {
                ApplyPresentation(panel, presentation, display);
                ReapplyCurrentPage(panel, display);
            }
        }
    }

    private void ApplyPanelStackState()
    {
        if (_panelStack.Count == 0)
            return;

        int keep = Math.Max(1, _panelKeepAliveDepth);
        int index = 0;

        foreach (UIBase panel in _panelStack)
        {
            bool keepAlive = index < keep;

            if (!keepAlive)
            {
                ApplyPanelState(
                    panel,
                    active: false,
                    interactable: false,
                    blocksRaycasts: false,
                    alpha: 0f);

                index++;
                continue;
            }

            if (index == 0)
            {
                panel.transform.SetAsLastSibling();

                ApplyPanelState(
                    panel,
                    active: true,
                    interactable: true,
                    blocksRaycasts: true,
                    alpha: 1f);
            }
            else
            {
                ApplyPanelState(
                    panel,
                    active: true,
                    interactable: false,
                    blocksRaycasts: false,
                    alpha: _coveredPanelAlpha);
            }

            index++;
        }
    }
}