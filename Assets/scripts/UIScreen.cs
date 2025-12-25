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
    
    #region 동적 재구성/재배치 용
    
    private Dictionary<string, MonoBehaviour> _widgetsByNameTag = new();

    internal void SetWidgets(Dictionary<string, MonoBehaviour> map)
        => _widgetsByNameTag = map;

    public T GetWidget<T>(string nameTag) where T : MonoBehaviour
    {
        if (!_widgetsByNameTag.TryGetValue(nameTag, out MonoBehaviour widget))
            return null;
        
        if (widget is T t)
            return t;
        
        Debug.LogWarning(
            $"[UIScreen] Widget '{nameTag}' is {widget.GetType().Name}, not {typeof(T).Name}", this);
        return null;
    }
    
    #endregion

    public void Open() => gameObject.SetActive(true);
    public void Close() => Destroy(gameObject);
}
