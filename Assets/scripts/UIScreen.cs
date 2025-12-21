using System;
using System.Collections.Generic;
using UnityEngine;

public class UIScreen : MonoBehaviour
{
    private Dictionary<string, RectTransform> _slots;

    public void Build(UIBinder binder, IEnumerable<string> requiredSlots = null)
    {
        _slots = binder.BuildSlots(transform, requiredSlots);
    }

    public RectTransform GetSlot(string slotName)
    {
        return _slots[slotName];
    }

    public void Open() => gameObject.SetActive(true);
    public void Close() => Destroy(gameObject);
}
