using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct UIRouteEntry
{
    public string route;
    public ScreenKey key;
}

[CreateAssetMenu(menuName = "UI/Screen Catalog", fileName = "UIScreenCatalog")]
public class UIScreenCatalog : ScriptableObject
{
    [Serializable]
    public class ScreenEntry
    {
        public ScreenKey key;
        public UIScreenSpecAsset specAsset;
    }

    public List<ScreenEntry> entries = new();
    
    //ActionKey 등록용
    public List<UIRouteEntry> routes = new();
    

    private Dictionary<ScreenKey, UIScreenSpec> _map;

    public void BuildCache()
    {
        _map = new Dictionary<ScreenKey, UIScreenSpec>();

        foreach (var e in entries)
        {
            if (e?.specAsset == null) continue;
            _map[e.key] = e.specAsset.spec;
        }
    }

    public UIScreenSpec GetScreenSpec(ScreenKey key)
    {
        if (_map == null)
        {
            Debug.LogError("[UIScreenCatalog] Cache not initialized.");
            return null;
        }

        return _map.TryGetValue(key, out var spec) ? spec : null;
    }
}
