using System;
using System.Collections.Generic;
using UnityEngine;

// The live screen: a prefab instance plus a lookup from nameTag to the
// widgets that patches may touch. Holds no layout logic of its own.
public class UIScreen : MonoBehaviour
{
    private Dictionary<string, WidgetHandle> _widgetsByNameTag;

    // Null when the tag is unknown. Callers report the miss with their own
    // context (see LayoutSpecPatch) so a missing target is logged once.
    public WidgetHandle GetWidgetHandle(string nameTag)
    {
        if (string.IsNullOrWhiteSpace(nameTag) || _widgetsByNameTag == null)
            return null;

        return _widgetsByNameTag.TryGetValue(nameTag.Trim(), out WidgetHandle handle) ? handle : null;
    }

    public IEnumerable<WidgetHandle> GetAllWidgets()
    {
        if (_widgetsByNameTag == null)
            yield break;

        foreach (WidgetHandle handle in _widgetsByNameTag.Values)
        {
            if (handle != null)
                yield return handle;
        }
    }

    // Every UIWidgetTag under this screen becomes a WidgetHandle.
    // Called by UIScreenFactory right after instantiation, before patches.
    public void RegisterAuthoredWidgets()
    {
        AddWidgets(UIWidgetTag.CollectHandles(transform));
    }

    internal void AddWidgets(IEnumerable<WidgetHandle> handles)
    {
        _widgetsByNameTag ??= new Dictionary<string, WidgetHandle>(StringComparer.Ordinal);

        foreach (WidgetHandle handle in handles)
        {
            if (handle == null || string.IsNullOrEmpty(handle.NameTag))
                continue;

            if (!_widgetsByNameTag.TryAdd(handle.NameTag, handle))
                Debug.LogWarning($"[UIScreen] Duplicate widget nameTag='{handle.NameTag}' — first one kept.", this);
        }
    }
}
