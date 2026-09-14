using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class SaveSlotPage : UIPage<SaveSlotPage.Refs>
{
    public event Action<int> SlotClicked;

    protected abstract bool IsSaveMode { get; }

    public enum Refs
    {
        PageTitle_Text,

        Slot1_Button,
        Slot1_Text,

        Slot2_Button,
        Slot2_Text,

        Slot3_Button,
        Slot3_Text,
    }

    private TMP_Text _pageTitle;

    private Button _slot1Button;
    private TMP_Text _slot1Text;

    private Button _slot2Button;
    private TMP_Text _slot2Text;

    private Button _slot3Button;
    private TMP_Text _slot3Text;

    protected override void OnInitialize()
    {
        _pageTitle = View.Text(Refs.PageTitle_Text);

        _slot1Button = View.Button(Refs.Slot1_Button);
        _slot1Text = View.Text(Refs.Slot1_Text);

        _slot2Button = View.Button(Refs.Slot2_Button);
        _slot2Text = View.Text(Refs.Slot2_Text);

        _slot3Button = View.Button(Refs.Slot3_Button);
        _slot3Text = View.Text(Refs.Slot3_Text);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        ValidateRefs();
#endif

        PresentDemoSlots();

        BindEvent(_slot1Button, _ => SlotClicked?.Invoke(1));
        BindEvent(_slot2Button, _ => SlotClicked?.Invoke(2));
        BindEvent(_slot3Button, _ => SlotClicked?.Invoke(3));
    }

    private void PresentDemoSlots()
    {
        if (_pageTitle != null)
            _pageTitle.text = IsSaveMode ? "Save" : "Load";

        if (_slot1Text != null)
            _slot1Text.text = "Slot 1 — Day 2 / Scene 4";

        if (_slot2Text != null)
            _slot2Text.text = IsSaveMode
                ? "Slot 2 — New Save"
                : "Slot 2 — Empty";

        if (_slot3Text != null)
            _slot3Text.text = "Slot 3 — Day 5 / Scene 1";
    }

    private void ValidateRefs()
    {
        if (_pageTitle == null)
            Debug.LogWarning($"[{GetType().Name}] Missing ref: {Refs.PageTitle_Text}", this);

        if (_slot1Button == null)
            Debug.LogWarning($"[{GetType().Name}] Missing ref: {Refs.Slot1_Button}", this);

        if (_slot1Text == null)
            Debug.LogWarning($"[{GetType().Name}] Missing ref: {Refs.Slot1_Text}", this);

        if (_slot2Button == null)
            Debug.LogWarning($"[{GetType().Name}] Missing ref: {Refs.Slot2_Button}", this);

        if (_slot2Text == null)
            Debug.LogWarning($"[{GetType().Name}] Missing ref: {Refs.Slot2_Text}", this);

        if (_slot3Button == null)
            Debug.LogWarning($"[{GetType().Name}] Missing ref: {Refs.Slot3_Button}", this);

        if (_slot3Text == null)
            Debug.LogWarning($"[{GetType().Name}] Missing ref: {Refs.Slot3_Text}", this);
    }
}
