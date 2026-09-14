using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DemoConfirmPanel : UIPanel<DemoConfirmPanel.Refs>
{
    public event Action CloseClicked;

    public enum Refs
    {
        CloseButton,
    }

    private Button _closeButton;

    protected override void OnInitialize()
    {
        _closeButton = View.Button(Refs.CloseButton);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_closeButton == null)
            Debug.LogWarning($"[DemoConfirmPanel] Missing ref: {Refs.CloseButton}", this);
#endif

        BindEvent(_closeButton, HandleCloseClicked);
    }

    private void HandleCloseClicked(PointerEventData _)
    {
        CloseClicked?.Invoke();
    }
}
