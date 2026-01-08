using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class UISlotBinder
{
    private readonly bool _includeInactive;

    public UISlotBinder(bool includeInactive = true)
    {
        _includeInactive = includeInactive;
    }

    public Dictionary<string, RectTransform> BindSlots(
        Transform root,
        IEnumerable<string> requiredSlotIds,
        bool strict = true)
    {
        var map = new Dictionary<string, RectTransform>(StringComparer.Ordinal);

        // null 방어
        if (requiredSlotIds == null)
            requiredSlotIds = Array.Empty<string>();

        // 1) Marker-based (UISlot 컴포넌트 기준)
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
                    if (strict)
                        throw new InvalidOperationException($"[UIBinder] Empty UISlot.id under '{root.name}'.");
                    Debug.LogWarning($"[UIBinder] Empty UISlot.id under '{root.name}'.", marker);
                    continue;
                }

                RectTransform rect = marker.target != null
                    ? marker.target
                    : marker.GetComponent<RectTransform>();

                if (rect == null)
                {
                    if (strict)
                        throw new InvalidOperationException(
                            $"[UIBinder] UISlot '{id}' has no RectTransform (root='{root.name}').");
                    Debug.LogWarning($"[UIBinder] UISlot '{id}' has no RectTransform (root='{root.name}').", marker);
                    continue;
                }

                if (map.ContainsKey(id))
                {
                    if (strict)
                        throw new InvalidOperationException(
                            $"[UIBinder] Duplicate slot id '{id}' under '{root.name}'.");
                    Debug.LogWarning($"[UIBinder] Duplicate slot id '{id}' under '{root.name}'. Using first.", marker);
                    continue;
                }

                map.Add(id, rect);
            }

            // 🔹 여기서 "필요한 slotId가 실제로 존재하는지" 검증하되, 예외는 절대 던지지 않게 한다.
            ValidateRequired(root, map, requiredSlotIds, strict: false);
            return map;
        }

        // 2) Marker 가 하나도 없을 때: 이름 기반 fallback
        foreach (string raw in requiredSlotIds)
        {
            string id = (raw ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(id)) continue;

            Transform transform = root.Find(id);
            RectTransform rect = transform as RectTransform;

            if (rect == null)
            {
                // 🔹 여기서도 이제 절대 throw 안 하고 Warn만 찍는다.
                Debug.LogWarning(
                    $"[UIBinder] Missing required slot '{id}' under '{root.name}'. (name-based fallback)",
                    root);
                continue;
            }

            map[id] = rect;
        }

        return map;
    }

    private void ValidateRequired(Transform root, Dictionary<string, RectTransform> map,
        IEnumerable<string> requiredSlotIds, bool strict)
    {
        if (requiredSlotIds == null) return;

        foreach (string raw in requiredSlotIds)
        {
            string id = (raw ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(id)) continue;

            if (!map.ContainsKey(id) || map[id] == null)
            {
                if (strict)
                    throw new KeyNotFoundException($"[UIBinder] Missing required slot '{id}' under '{root.name}'.");

                Debug.LogWarning($"[UIBinder] Missing required slot '{id}' under '{root.name}'.", root);
            }
        }
    }
}