using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class UIBinder
{
    private readonly bool _includeInactive;

    public UIBinder(bool includeInactive = true)
    {
        _includeInactive = includeInactive;
    }

    /// <summary>
    /// 1) UISlot 마커 기반으로 바인딩 시도
    /// 2) 마커가 하나도 없으면 이름 기반(root.Find)으로 폴백
    /// </summary>
    public Dictionary<string, RectTransform> BuildSlots(Transform root, IEnumerable<string> requiredSlotIds, bool strict = true)
    {
        var map = new Dictionary<string, RectTransform>(StringComparer.Ordinal);

        // 1) Marker-based
        UISlot[] markers = root.GetComponentsInChildren<UISlot>(_includeInactive);
        if (markers != null && markers.Length > 0)
        {
            for (int i = 0; i < markers.Length; i++)
            {
                UISlot marker = markers[i];
                if (marker == null) continue;

                string id = (marker.id ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(id))
                {
                    if (strict) throw new InvalidOperationException($"[UIBinder] Empty UISlot.id under '{root.name}'.");
                    Debug.LogWarning($"[UIBinder] Empty UISlot.id under '{root.name}'.", marker);
                    continue;
                }

                RectTransform rect = marker.target != null ? marker.target : marker.GetComponent<RectTransform>();
                if (rect == null)
                {
                    if (strict) throw new InvalidOperationException($"[UIBinder] UISlot '{id}' has no RectTransform (root='{root.name}').");
                    Debug.LogWarning($"[UIBinder] UISlot '{id}' has no RectTransform (root='{root.name}').", marker);
                    continue;
                }

                if (map.ContainsKey(id))
                {
                    if (strict) throw new InvalidOperationException($"[UIBinder] Duplicate slot id '{id}' under '{root.name}'.");
                    Debug.LogWarning($"[UIBinder] Duplicate slot id '{id}' under '{root.name}'. Using first.", marker);
                    continue;
                }

                map.Add(id, rect);
            }

            ValidateRequired(root, map, requiredSlotIds, strict);
            return map;
        }

        foreach (string raw in requiredSlotIds)
        {
            string id = (raw ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(id)) continue;

            Transform transform = root.Find(id);
            RectTransform rect = transform as RectTransform;

            if (rect == null)
            {
                if (strict) throw new KeyNotFoundException($"[UIBinder] Missing required slot '{id}' under '{root.name}'. (name-based fallback)");
                Debug.LogWarning($"[UIBinder] Missing required slot '{id}' under '{root.name}'.", root);
                continue;
            }

            map[id] = rect;
        }

        return map;
    }

    private void ValidateRequired(Transform root, Dictionary<string, RectTransform> map, IEnumerable<string> requiredSlotIds, bool strict)
    {
        if (requiredSlotIds == null) return;

        foreach (string raw in requiredSlotIds)
        {
            string id = (raw ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(id)) continue;

            if (!map.ContainsKey(id) || map[id] == null)
            {
                if (strict) throw new KeyNotFoundException($"[UIBinder] Missing required slot '{id}' under '{root.name}'.");
                Debug.LogWarning($"[UIBinder] Missing required slot '{id}' under '{root.name}'.", root);
            }
        }
    }
}
