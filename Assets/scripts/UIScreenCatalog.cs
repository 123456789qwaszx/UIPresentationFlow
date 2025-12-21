using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct UIRouteEntry
{
    public string route;
    public ScreenKey key;
}

public enum ScreenKey { Home, Shop }

[CreateAssetMenu(menuName = "UI/Screen Catalog", fileName = "UIScreenCatalog")]
public class UIScreenCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ScreenKey key;
        public UIScreenSpecAsset specAsset;
    }

    [Header("Registered UIScreenSpecAsset")]
    public List<Entry> entries = new();

    // 런타임용 캐시 (에셋에 저장되지 않음)
    private Dictionary<ScreenKey, UIScreenSpec> _map;

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        BuildCache();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 에디터에서 값 바뀔 때도 다시 빌드
        BuildCache();
    }
#endif

    private void BuildCache()
    {
        _map = new Dictionary<ScreenKey, UIScreenSpec>();

        foreach (Entry e in entries)
        {
            if (e == null || e.specAsset == null) continue;
            
            _map[e.key] = e.specAsset.spec;
        }
    }

    public UIScreenSpec GetScreenSpec(ScreenKey key)
    {
        if (_map == null) BuildCache();

        _map.TryGetValue(key, out var spec);
        return spec;
    }
}
