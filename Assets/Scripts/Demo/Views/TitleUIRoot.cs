using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class TitleUIRoot : UIRoot<TitleUIRoot.Refs>
{
    public event Action StartClicked;
    public event Action LoadClicked;
    public event Action AlbumClicked;

    public enum Refs
    {
        TitleBG_Image,

        StartBtn_Button,

        [UIRefTextRole(UITextRole.Body)]
        StartBtn_Text,

        LoadBtn_Button,

        [UIRefTextRole(UITextRole.Body)]
        LoadBtn_Text,

        AlbumBtn_Button,

        [UIRefTextRole(UITextRole.Body)]
        AlbumBtn_Text,
    }

    private Image _titleBgImage;

    private Button _startButton;
    private TMP_Text _startBtnText;

    private Button _loadButton;
    private TMP_Text _loadBtnText;

    private Button _albumButton;
    private TMP_Text _albumBtnText;

    protected override void OnInitialize()
    {
        _titleBgImage = View.Image(Refs.TitleBG_Image);

        _startButton = View.Button(Refs.StartBtn_Button);
        _startBtnText = View.Text(Refs.StartBtn_Text);

        _loadButton = View.Button(Refs.LoadBtn_Button);
        _loadBtnText = View.Text(Refs.LoadBtn_Text);

        _albumButton = View.Button(Refs.AlbumBtn_Button);
        _albumBtnText = View.Text(Refs.AlbumBtn_Text);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ValidateRefs();
#endif

        BindEvent(_startButton, HandleStartClicked);
        BindEvent(_loadButton, HandleLoadClicked);
        BindEvent(_albumButton, HandleAlbumClicked);
    }

    private void HandleStartClicked(PointerEventData _)
    {
        StartClicked?.Invoke();
    }

    private void HandleLoadClicked(PointerEventData _)
    {
        LoadClicked?.Invoke();
    }

    private void HandleAlbumClicked(PointerEventData _)
    {
        AlbumClicked?.Invoke();
    }

    private void ValidateRefs()
    {
        if (_titleBgImage == null)
            Debug.LogWarning($"[TitleUIRoot] Missing ref: {Refs.TitleBG_Image}", this);

        if (_startButton == null)
            Debug.LogWarning($"[TitleUIRoot] Missing ref: {Refs.StartBtn_Button}", this);

        if (_startBtnText == null)
            Debug.LogWarning($"[TitleUIRoot] Missing ref: {Refs.StartBtn_Text}", this);

        if (_loadButton == null)
            Debug.LogWarning($"[TitleUIRoot] Missing ref: {Refs.LoadBtn_Button}", this);

        if (_loadBtnText == null)
            Debug.LogWarning($"[TitleUIRoot] Missing ref: {Refs.LoadBtn_Text}", this);

        if (_albumButton == null)
            Debug.LogWarning($"[TitleUIRoot] Missing ref: {Refs.AlbumBtn_Button}", this);

        if (_albumBtnText == null)
            Debug.LogWarning($"[TitleUIRoot] Missing ref: {Refs.AlbumBtn_Text}", this);
    }
}
