using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DemoPageA : UIPage<DemoPageA.Refs>
{
    public event Action OpenConfirmPanelClicked;
    public event Action ConfirmClicked;

    public enum Refs
    {
        OpenConfirmPanelButton,
        ConfirmButton,
    }

    private Button _openConfirmPanelButton;
    private Button _confirmButton;

    protected override void OnInitialize()
    {
        _openConfirmPanelButton = View.Button(Refs.OpenConfirmPanelButton);
        _confirmButton = View.Button(Refs.ConfirmButton);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_openConfirmPanelButton == null)
            Debug.LogWarning($"[DemoPageA] Missing ref: {Refs.OpenConfirmPanelButton}", this);

        if (_confirmButton == null)
            Debug.LogWarning($"[DemoPageA] Missing ref: {Refs.ConfirmButton}", this);
#endif

        BindEvent(_openConfirmPanelButton, HandleOpenConfirmPanelClicked);
        BindEvent(_confirmButton, HandleConfirmClicked);
    }

    private void HandleOpenConfirmPanelClicked(PointerEventData _)
    {
        OpenConfirmPanelClicked?.Invoke();
    }

    private void HandleConfirmClicked(PointerEventData _)
    {
        ConfirmClicked?.Invoke();
    }
}