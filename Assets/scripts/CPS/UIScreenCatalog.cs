using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct UIRouteEntry
{
    [UIRouteKey]
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

    public void ValidateAll()
    {
#if UNITY_EDITOR
        int warnings =
            ValidateEntries() +
            ValidateRoutes();

        if (warnings == 0)
        {
            Debug.Log(
                "[UIScreenCatalog] Route–Entry consistency check PASSED",
                this);
        }
#endif
    }
    
#if UNITY_EDITOR
    
    // Checks basic integrity of screen definitions (entries).
    private int ValidateEntries()
    {
        int warningCount = 0;

        foreach (var e in entries)
        {
            if (e == null)
                continue;

            if (e.specAsset == null)
            {
                Debug.LogWarning(
                    $"[UIScreenCatalog] ScreenEntry '{e.key.Value}' has no UIScreenSpecAsset",
                    this);
                warningCount++;
            }
        }

        return warningCount;
    }

    // Checks route-to-screen mapping consistency.
    private int ValidateRoutes()
    {
        int warningCount = 0;

        HashSet<string> routeSet = new();
        HashSet<ScreenKey> definedKeys = new();

        // 정의된 ScreenKey 수집
        foreach (var e in entries)
        {
            if (e != null)
                definedKeys.Add(e.key);
        }

        foreach (var r in routes)
        {
            // 1. 빈 route
            if (string.IsNullOrEmpty(r.route))
            {
                Debug.LogWarning(
                    "[UIScreenCatalog] Route is empty",
                    this);
                warningCount++;
                continue;
            }

            // 2. route 중복
            if (!routeSet.Add(r.route))
            {
                Debug.LogWarning(
                    $"[UIScreenCatalog] Duplicate route '{r.route}'",
                    this);
                warningCount++;
            }

            // 3. 정의되지 않은 ScreenKey 참조
            if (!definedKeys.Contains(r.key))
            {
                Debug.LogWarning(
                    $"[UIScreenCatalog] Route '{r.route}' references ScreenKey '{r.key.Value}' which is not defined in entries",
                    this);
                warningCount++;
            }
        }

        return warningCount;
    }
#endif
}