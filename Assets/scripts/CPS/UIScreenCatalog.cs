using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct UIRouteEntry
{
    [UIRouteKey]
    public string route;
    public ScreenKey screenKey;
}

[CreateAssetMenu(menuName = "UI/Screen Catalog", fileName = "UIScreenCatalog")]
public class UIScreenCatalog : ScriptableObject
{
    [Serializable]
    public class ScreenEntry
    {
        public ScreenKey screenKey;
        public UIScreenSpecAsset specAsset;
    }
    public List<ScreenEntry> entries = new();
    private Dictionary<ScreenKey, UIScreenSpec> _screenMap;
    
    public List<UIRouteEntry> routes = new();
    private Dictionary<string, ScreenKey> _routeMap;
    
    #region Init
    
    public void Init()
    {
        BuildScreenCache();
        BuildRouteCache();
    }

    private void BuildScreenCache()
    {
        _screenMap = new Dictionary<ScreenKey, UIScreenSpec>();

        foreach (ScreenEntry e in entries)
        {
            if (e?.specAsset == null)
                continue;
            _screenMap[e.screenKey] = e.specAsset.spec;
        }
    }

    private void BuildRouteCache()
    {
        _routeMap = new Dictionary<string, ScreenKey>(StringComparer.OrdinalIgnoreCase);
        
        foreach (UIRouteEntry r in routes)
        {
            if (!_routeMap.TryAdd(r.route, r.screenKey))
            {
                Debug.LogWarning($"[UIScreenCatalog] Duplicate route detected: '{r.route}' in catalog '{name}'.");
            }
        }
    }
    
    #endregion
    
    #region Getter
    
    public bool TryGetScreenSpec(ScreenKey key, out UIScreenSpec spec)
    {
        if (_screenMap == null)
        {
            spec = null;
            return false;
        }

        return _screenMap.TryGetValue(key, out spec);
    }
    
    public bool TryGetRouteScreenKey(string route, out ScreenKey key)
    {
        if (_routeMap == null)
        {
            key = default;
            return false;
        }

        return _routeMap.TryGetValue(route, out key);
    }
    
    #endregion
    
    #region Validate
    
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
                    $"[UIScreenCatalog] ScreenEntry '{e.screenKey.Value}' has no UIScreenSpecAsset",
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
                definedKeys.Add(e.screenKey);
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
            if (!definedKeys.Contains(r.screenKey))
            {
                Debug.LogWarning(
                    $"[UIScreenCatalog] Route '{r.route}' references ScreenKey '{r.screenKey.Value}' which is not defined in entries",
                    this);
                warningCount++;
            }
        }

        return warningCount;
    }
#endif
    
    #endregion
}