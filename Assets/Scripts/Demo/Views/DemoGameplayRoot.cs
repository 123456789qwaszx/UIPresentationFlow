using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DemoGameplayRoot : UIRoot<DemoGameplayRoot.Refs>
{
    public event Action SaveClicked;
    public event Action LoadClicked;
    public event Action TitleClicked;

    public enum Refs
    {
        SaveButton_Button,
        LoadButton_Button,
        TitleButton_Button,
    }

    private Button _saveButton;
    private Button _loadButton;
    private Button _titleButton;

    protected override void OnInitialize()
    {
        _saveButton = View.Button(Refs.SaveButton_Button);
        _loadButton = View.Button(Refs.LoadButton_Button);
        _titleButton = View.Button(Refs.TitleButton_Button);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ValidateRefs();
#endif

        BindEvent(_saveButton, HandleSaveClicked);
        BindEvent(_loadButton, HandleLoadClicked);
        BindEvent(_titleButton, HandleTitleClicked);
    }

    private void HandleSaveClicked(PointerEventData _)
    {
        SaveClicked?.Invoke();
    }

    private void HandleLoadClicked(PointerEventData _)
    {
        LoadClicked?.Invoke();
    }

    private void HandleTitleClicked(PointerEventData _)
    {
        TitleClicked?.Invoke();
    }

    private void ValidateRefs()
    {
        if (_saveButton == null)
            Debug.LogWarning($"[DemoGameplayRoot] Missing ref: {Refs.SaveButton_Button}", this);

        if (_loadButton == null)
            Debug.LogWarning($"[DemoGameplayRoot] Missing ref: {Refs.LoadButton_Button}", this);

        if (_titleButton == null)
            Debug.LogWarning($"[DemoGameplayRoot] Missing ref: {Refs.TitleButton_Button}", this);
    }
}
