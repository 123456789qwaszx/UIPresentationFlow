using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DemoPageB : UIPage<DemoPageB.Refs>
{
    public event Action ConfirmClicked;

    public enum Refs
    {
        ConfirmButton,
    }

    private Button _confirmButton;

    protected override void OnInitialize()
    {
        _confirmButton = View.Button(Refs.ConfirmButton);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_confirmButton == null)
            Debug.LogWarning($"[DemoPageB] Missing ref: {Refs.ConfirmButton}", this);
#endif

        BindEvent(_confirmButton, HandleConfirmClicked);
    }

    private void HandleConfirmClicked(PointerEventData _)
    {
        ConfirmClicked?.Invoke();
    }
}
