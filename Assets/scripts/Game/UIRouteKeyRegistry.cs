using System;
using System.Collections.Generic;
using UnityEngine;

public static class UIRouteKeyRegistry
{
    static Dictionary<string, ScreenKey> _map;

    public static void Init(UIScreenCatalog catalog)
    {
        _map = new Dictionary<string, ScreenKey>(StringComparer.OrdinalIgnoreCase);

        if (catalog == null)
        {
            Debug.LogWarning("[UIRouteKeyRegistry] Init called with null catalog.");
            return;
        }

        // 1) UIRouteEntry 기반 route → ScreenKey 매핑 (우선순위 최고)
        if (catalog.routes != null)
        {
            foreach (var entry in catalog.routes)
            {
                if (entry.route == null)
                    continue;

                string route = entry.route.Trim();
                if (string.IsNullOrWhiteSpace(route))
                    continue;

                if (_map.TryGetValue(route, out var existing))
                {
                    if (!existing.Equals(entry.key))
                    {
                        Debug.LogWarning(
                            $"[UIRouteKeyRegistry] Duplicate route '{route}' " +
                            $"with different ScreenKey detected: {existing} -> {entry.key}. " +
                            "Keeping the first mapped value.");
                    }

                    // 이미 같은 key로 등록되어 있으면 그냥 무시
                    continue;
                }

                _map[route] = entry.key;
            }
        }
        
#if UNITY_EDITOR
        Debug.Log($"[UIRouteKeyRegistry] Initialized. Routes count = {_map.Count}");
#endif
    }

    public static bool TryGetRouteKey(string raw, out ScreenKey key)
    {
        key = default;

        if (_map == null)
        {
            Debug.LogError("[UIRouteKeyRegistry] Not initialized. Call Init(catalog) first.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        raw = raw.Trim();
        return _map.TryGetValue(raw, out key);
    }
}
