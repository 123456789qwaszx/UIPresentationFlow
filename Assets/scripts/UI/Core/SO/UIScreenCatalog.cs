using System;
using System.Collections.Generic;
using UnityEngine;

// Registry: ScreenKey -> UIScreenSpec. Call Init() once before resolving.
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

    public bool IsInitialized => _screenMap != null;

    public void Init()
    {
        _screenMap = new Dictionary<ScreenKey, UIScreenSpec>();

        foreach (ScreenEntry e in entries)
        {
            if (e?.specAsset == null)
                continue;

            _screenMap[e.screenKey] = e.specAsset.spec;   // last duplicate wins; Validate() reports duplicates
        }
    }

    public bool TryGetScreenSpec(ScreenKey key, out UIScreenSpec spec)
    {
        if (_screenMap == null)
        {
            spec = null;
            return false;
        }

        return _screenMap.TryGetValue(key, out spec);
    }

    // Authoring-time integrity check. Pure: returns messages, logs nothing,
    // so the Editor button and tests share one implementation.
    public List<string> Validate()
    {
        var problems = new List<string>();
        var seenKeys = new HashSet<ScreenKey>();

        for (int i = 0; i < entries.Count; i++)
        {
            ScreenEntry e = entries[i];
            string at = $"entries[{i}]";

            if (e == null)
            {
                problems.Add($"{at}: null entry");
                continue;
            }

            if (string.IsNullOrWhiteSpace(e.screenKey.Value))
                problems.Add($"{at}: screenKey is empty");
            else if (!seenKeys.Add(e.screenKey))
                problems.Add($"{at}: duplicate screenKey '{e.screenKey}'");

            if (e.specAsset == null)
            {
                problems.Add($"{at} '{e.screenKey}': specAsset is null");
                continue;
            }

            if (!e.screenKey.Equals(e.specAsset.spec.screenKey))
                problems.Add($"{at} '{e.screenKey}': specAsset.spec.screenKey is '{e.specAsset.spec.screenKey}' (mismatch)");

            problems.AddRange(UIScreenSpecValidator.Validate(e.specAsset.spec, $"{at} '{e.screenKey}'"));
        }

        return problems;
    }
}
