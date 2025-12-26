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
        if (!_slots.TryGetValue(slotName, out RectTransform slot))
        {
            Debug.LogWarning($"[UIScreen] Slot '{slotName}' not found.", this);
            return null;
        }

        return slot;
    }
    
    #region 동적 재구성/재배치 용
    
    private Dictionary<string, WidgetHandle> _widgetsByNameTag = new();

    internal void SetWidgets(Dictionary<string, WidgetHandle> map)
        => _widgetsByNameTag = map;

    // WidgetHandle 자체를 얻고 싶을 때 사용.
    public WidgetHandle GetWidgetHandle(string nameTag)
    {
        if (string.IsNullOrWhiteSpace(nameTag))
            return null;

        if (!_widgetsByNameTag.TryGetValue(nameTag, out var handle) || handle == null)
        {
            Debug.LogWarning($"[UIScreen] WidgetHandle not found for nameTag='{nameTag}'", this);
            return null;
        }

        return handle;
    }

    // 특정 Component(TMP_Text, Image 등)를 바로 얻고 싶을 때 사용.
    // 예: var text = screen.GetWidget<TMP_Text>("ScoreText");
    public T GetWidget<T>(string nameTag) where T : Component
    {
        WidgetHandle handle = GetWidgetHandle(nameTag);
        if (handle == null)
            return null;

        var component = handle.GameObject.GetComponentInChildren<T>(includeInactive: true);
        if (component != null)
            return component;

        Debug.LogWarning(
            $"[UIScreen] Widget '{nameTag}' (GameObject='{handle.GameObject.name}') " +
            $"does not contain component of type {typeof(T).Name}", this);
        return null;
    }
    
    #endregion

    public void Open() => gameObject.SetActive(true);
    public void Close() => Destroy(gameObject);
}
