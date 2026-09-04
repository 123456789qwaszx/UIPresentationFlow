using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class AdaptiveDemoUIRoot : UIRoot<AdaptiveDemoUIRoot.Refs>
{
    public event Action LeftActionClicked;
    public event Action RightActionClicked;

    public enum Refs
    {
        Background,
        Header,
        PrimaryContent,
        SideInfo,
        BottomControls,
        LeftAction,
        RightAction,
    }

    private RectTransform _background;
    private RectTransform _header;
    private RectTransform _primaryContent;
    private RectTransform _sideInfo;
    private RectTransform _bottomControls;

    private Button _leftActionButton;
    private Button _rightActionButton;

    protected override void OnInitialize()
    {
        _background = View.Rect(Refs.Background);
        _header = View.Rect(Refs.Header);
        _primaryContent = View.Rect(Refs.PrimaryContent);
        _sideInfo = View.Rect(Refs.SideInfo);
        _bottomControls = View.Rect(Refs.BottomControls);

        _leftActionButton = View.Button(Refs.LeftAction);
        _rightActionButton = View.Button(Refs.RightAction);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ValidateRefs();
#endif

        BindEvent(_leftActionButton, HandleLeftActionClicked);
        BindEvent(_rightActionButton, HandleRightActionClicked);
    }

    private void HandleLeftActionClicked(PointerEventData _)
    {
        LeftActionClicked?.Invoke();
    }

    private void HandleRightActionClicked(PointerEventData _)
    {
        RightActionClicked?.Invoke();
    }

    private void ValidateRefs()
    {
        if (_background == null)
            Debug.LogWarning($"[AdaptiveDemoUIRoot] Missing ref: {Refs.Background}", this);

        if (_header == null)
            Debug.LogWarning($"[AdaptiveDemoUIRoot] Missing ref: {Refs.Header}", this);

        if (_primaryContent == null)
            Debug.LogWarning($"[AdaptiveDemoUIRoot] Missing ref: {Refs.PrimaryContent}", this);

        if (_sideInfo == null)
            Debug.LogWarning($"[AdaptiveDemoUIRoot] Missing ref: {Refs.SideInfo}", this);

        if (_bottomControls == null)
            Debug.LogWarning($"[AdaptiveDemoUIRoot] Missing ref: {Refs.BottomControls}", this);

        if (_leftActionButton == null)
            Debug.LogWarning($"[AdaptiveDemoUIRoot] Missing ref: {Refs.LeftAction}", this);

        if (_rightActionButton == null)
            Debug.LogWarning($"[AdaptiveDemoUIRoot] Missing ref: {Refs.RightAction}", this);
    }
}