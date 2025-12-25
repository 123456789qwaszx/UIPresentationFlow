using System;
using System.Collections.Generic;

public static class UIRouteKeyRegistry
{
    static Dictionary<string, ScreenKey> _map;

    public static void Init(UIScreenCatalog catalog)
    {
        _map = new(StringComparer.OrdinalIgnoreCase);

        foreach (UIScreenCatalog.ScreenEntry entry in catalog.entries)
        {
            _map[entry.key.ToString()] = entry.key;
        }
    }

    public static bool TryGetRouteKey(string raw, out ScreenKey key)
        => _map.TryGetValue(raw, out key);
}