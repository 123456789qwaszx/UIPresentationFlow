using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class AlbumUIRoot : UIRoot<AlbumUIRoot.Refs>
{
    public event Action BackClicked;

    public enum Refs
    {
        AlbumTitle_Text,
        AlbumEntry1_Text,
        AlbumEntry2_Text,
        AlbumEntry3_Text,
        BackButton_Button,
    }

    private TMP_Text _titleText;
    private TMP_Text _entry1Text;
    private TMP_Text _entry2Text;
    private TMP_Text _entry3Text;
    private Button _backButton;

    protected override void OnInitialize()
    {
        _titleText = View.Text(Refs.AlbumTitle_Text);
        _entry1Text = View.Text(Refs.AlbumEntry1_Text);
        _entry2Text = View.Text(Refs.AlbumEntry2_Text);
        _entry3Text = View.Text(Refs.AlbumEntry3_Text);
        _backButton = View.Button(Refs.BackButton_Button);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ValidateRefs();
#endif

        PresentDemoEntries();
        BindEvent(_backButton, HandleBackClicked);
    }

    private void PresentDemoEntries()
    {
        if (_titleText != null)
            _titleText.text = "Album";

        if (_entry1Text != null)
            _entry1Text.text = "CG 01 — Rooftop";

        if (_entry2Text != null)
            _entry2Text.text = "CG 02 — Sunset";

        if (_entry3Text != null)
            _entry3Text.text = "CG 03 — Ending";
    }

    private void HandleBackClicked(PointerEventData _)
    {
        BackClicked?.Invoke();
    }

    private void ValidateRefs()
    {
        if (_titleText == null)
            Debug.LogWarning($"[AlbumUIRoot] Missing ref: {Refs.AlbumTitle_Text}", this);

        if (_entry1Text == null)
            Debug.LogWarning($"[AlbumUIRoot] Missing ref: {Refs.AlbumEntry1_Text}", this);

        if (_entry2Text == null)
            Debug.LogWarning($"[AlbumUIRoot] Missing ref: {Refs.AlbumEntry2_Text}", this);

        if (_entry3Text == null)
            Debug.LogWarning($"[AlbumUIRoot] Missing ref: {Refs.AlbumEntry3_Text}", this);

        if (_backButton == null)
            Debug.LogWarning($"[AlbumUIRoot] Missing ref: {Refs.BackButton_Button}", this);
    }
}
