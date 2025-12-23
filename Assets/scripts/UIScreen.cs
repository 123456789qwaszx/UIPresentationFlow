using System;
using System.Collections.Generic;
using UnityEngine;

public class UIScreen : MonoBehaviour
{
    private Dictionary<string, RectTransform> _slots;

    public void Build(UIBinder binder, UIScreenSpec spec)
    {
        List<string> required = spec.slots.ConvertAll(s => s.slotName);
        _slots = binder.BuildSlots(transform, required);
    }

    public RectTransform GetSlot(string slotName)
    {
        return _slots[slotName];
    }

    public void Open() => gameObject.SetActive(true);
    public void Close() => Destroy(gameObject);
}
