using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SystemMenuPanel
    : UIPanel<SystemMenuPanel.Refs>, IUIPageOwner
{
    public event Action SaveClicked;
    public event Action LoadClicked;
    public event Action CloseClicked;

    public RectTransform PageRoot => _pageRoot;

    public enum Refs
    {
        PageRoot,
        SaveButton_Button,
        LoadButton_Button,
        CloseButton_Button,
    }

    private RectTransform _pageRoot;
    private Button _saveButton;
    private Button _loadButton;
    private Button _closeButton;

    protected override void OnInitialize()
    {
        _pageRoot = View.Rect(Refs.PageRoot);
        _saveButton = View.Button(Refs.SaveButton_Button);
        _loadButton = View.Button(Refs.LoadButton_Button);
        _closeButton = View.Button(Refs.CloseButton_Button);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ValidateRefs();
#endif

        BindEvent(_saveButton, HandleSaveClicked);
        BindEvent(_loadButton, HandleLoadClicked);
        BindEvent(_closeButton, HandleCloseClicked);
    }

    private void HandleSaveClicked(PointerEventData _)
    {
        SaveClicked?.Invoke();
    }

    private void HandleLoadClicked(PointerEventData _)
    {
        LoadClicked?.Invoke();
    }

    private void HandleCloseClicked(PointerEventData _)
    {
        CloseClicked?.Invoke();
    }

    private void ValidateRefs()
    {
        if (_pageRoot == null)
            Debug.LogWarning($"[SystemMenuPanel] Missing ref: {Refs.PageRoot}", this);

        if (_saveButton == null)
            Debug.LogWarning($"[SystemMenuPanel] Missing ref: {Refs.SaveButton_Button}", this);

        if (_loadButton == null)
            Debug.LogWarning($"[SystemMenuPanel] Missing ref: {Refs.LoadButton_Button}", this);

        if (_closeButton == null)
            Debug.LogWarning($"[SystemMenuPanel] Missing ref: {Refs.CloseButton_Button}", this);
    }
}
