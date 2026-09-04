using System;
using System.Collections.Generic;
using UnityEngine;

// Temporary R9 migration bridge.
//
// ScreenKey remains only as a route lookup key while UIManager migration is in
// progress. Presentation data itself no longer owns a route or a prefab.
[CreateAssetMenu(menuName = "UI/Screen Catalog", fileName = "UIScreenCatalog")]
public class UIScreenCatalog : ScriptableObject
{
    [Serializable]
    public class ScreenEntry
    {
        public ScreenKey screenKey;
        public GameObject templatePrefab;
        public UIPresentationSpec presentation;
    }

    public List<ScreenEntry> entries = new();

    private Dictionary<ScreenKey, ScreenEntry> _screenMap;

    public bool IsInitialized => _screenMap != null;

    public void Init()
    {
        _screenMap = new Dictionary<ScreenKey, ScreenEntry>();

        foreach (ScreenEntry entry in entries)
        {
            if (entry == null)
                continue;

            _screenMap[entry.screenKey] = entry;
        }
    }

    public bool TryGetScreenEntry(ScreenKey key, out ScreenEntry entry)
    {
        if (_screenMap == null)
        {
            entry = null;
            return false;
        }

        return _screenMap.TryGetValue(key, out entry);
    }

    public List<string> Validate()
    {
        var problems = new List<string>();
        var seenKeys = new HashSet<ScreenKey>();

        for (int i = 0; i < entries.Count; i++)
        {
            ScreenEntry entry = entries[i];
            string at = $"entries[{i}]";

            if (entry == null)
            {
                problems.Add($"{at}: null entry");
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.screenKey.Value))
                problems.Add($"{at}: screenKey is empty");
            else if (!seenKeys.Add(entry.screenKey))
                problems.Add($"{at}: duplicate screenKey '{entry.screenKey}'");

            ValidatePrefab(entry, at, problems);

            if (entry.presentation == null)
            {
                problems.Add($"{at} '{entry.screenKey}': presentation is null");
                continue;
            }

            problems.AddRange(
                UIPresentationSpecValidator.Validate(
                    entry.presentation,
                    $"{at} '{entry.screenKey}'"));
        }

        return problems;
    }

    private static void ValidatePrefab(
        ScreenEntry entry,
        string at,
        List<string> problems)
    {
        if (entry.templatePrefab == null)
        {
            problems.Add($"{at} '{entry.screenKey}': templatePrefab is null");
            return;
        }

        UIBase root = entry.templatePrefab.GetComponent<UIBase>();
        if (root == null)
        {
            problems.Add(
                $"{at} '{entry.screenKey}': templatePrefab '{entry.templatePrefab.name}' " +
                "must have a concrete UIBase<TRefs> component on its root");
            return;
        }

        if (root is not IUIPresentationRefProvider)
        {
            problems.Add(
                $"{at} '{entry.screenKey}': templatePrefab root '{root.GetType().Name}' " +
                "must expose presentation refs; use UIBase<TRefs>");
        }
    }
}