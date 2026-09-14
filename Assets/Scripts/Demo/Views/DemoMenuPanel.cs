using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DemoMenuPanel
    : UIPanel<DemoMenuPanel.Refs>, IUIPageOwner
{
    public event Action CloseClicked;

    public RectTransform PageRoot => _pageRoot;

    public enum Refs
    {
        PageRoot,
        CloseButton,
    }

    private RectTransform _pageRoot;
    private Button _closeButton;

    protected override void OnInitialize()
    {
        _pageRoot = View.Rect(Refs.PageRoot);
        _closeButton = View.Button(Refs.CloseButton);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_pageRoot == null)
            Debug.LogWarning($"[DemoMenuPanel] Missing ref: {Refs.PageRoot}", this);

        if (_closeButton == null)
            Debug.LogWarning($"[DemoMenuPanel] Missing ref: {Refs.CloseButton}", this);
#endif

        BindEvent(_closeButton, HandleCloseClicked);
    }

    private void HandleCloseClicked(PointerEventData _)
    {
        CloseClicked?.Invoke();
    }
}