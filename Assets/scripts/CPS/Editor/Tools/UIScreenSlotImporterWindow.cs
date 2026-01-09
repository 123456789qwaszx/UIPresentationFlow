#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class UIScreenSlotImporterWindow : EditorWindow
{
    [SerializeField] private UIScreenSpecAsset _source;
    [SerializeField] private UIScreenSpecAsset _target;

    private enum ImportMode
    {
        Append,
        MergeByName
    }

    [SerializeField] private ImportMode _mode = ImportMode.MergeByName;
    [SerializeField] private bool _includeRoot = false;            // 기본: Root 제외
    [SerializeField] private bool _overwriteOnMerge = true;        // Merge 시 덮어쓰기
    [SerializeField] private bool _ensureUniqueNamesOnAppend = true;

    private string _search = string.Empty;

    // source slot index -> selected
    private readonly Dictionary<int, bool> _selected = new();
    private Vector2 _scroll;

    [MenuItem("Tools/UI/Slot Importer")]
    public static void Open()
    {
        var w = GetWindow<UIScreenSlotImporterWindow>();
        w.titleContent = new GUIContent("Slot Importer");
        w.minSize = new Vector2(520, 420);
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);

        DrawAssetFields();

        EditorGUILayout.Space(6);
        DrawOptions();

        EditorGUILayout.Space(6);
        DrawSourceSlotsList();

        EditorGUILayout.Space(8);
        DrawBottomBar();
    }

    private void DrawAssetFields()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _source = (UIScreenSpecAsset)EditorGUILayout.ObjectField("Source", _source, typeof(UIScreenSpecAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                ResetSelectionCache();
            }

            _target = (UIScreenSpecAsset)EditorGUILayout.ObjectField("Target", _target, typeof(UIScreenSpecAsset), false);

            if (_source != null && _target != null && ReferenceEquals(_source, _target))
            {
                EditorGUILayout.HelpBox("Source and Target are the same asset.", MessageType.Warning);
            }
        }
    }

    private void DrawOptions()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Import Options", EditorStyles.boldLabel);

            _mode = (ImportMode)EditorGUILayout.EnumPopup("Mode", _mode);
            _includeRoot = EditorGUILayout.ToggleLeft("Include Root Slot (index 0)", _includeRoot);

            if (_mode == ImportMode.MergeByName)
                _overwriteOnMerge = EditorGUILayout.ToggleLeft("Overwrite when same Slot Name (Merge)", _overwriteOnMerge);

            if (_mode == ImportMode.Append)
                _ensureUniqueNamesOnAppend = EditorGUILayout.ToggleLeft("Ensure Unique Names on Append", _ensureUniqueNamesOnAppend);

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Search", GUILayout.Width(50));
                _search = EditorGUILayout.TextField(_search);
                if (GUILayout.Button("Clear", GUILayout.Width(60)))
                    _search = string.Empty;
            }
        }
    }

    private void DrawSourceSlotsList()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Source Slots (check what you want)", EditorStyles.boldLabel);

            if (_source == null || _source.spec == null || _source.spec.slots == null)
            {
                EditorGUILayout.HelpBox("Select a valid Source UIScreenSpecAsset.", MessageType.Info);
                return;
            }

            var slots = _source.spec.slots;
            if (slots.Count == 0)
            {
                EditorGUILayout.HelpBox("Source has no slots.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select All", GUILayout.Width(90)))
                    SetAllVisibleSelection(true);

                if (GUILayout.Button("Select None", GUILayout.Width(90)))
                    SetAllVisibleSelection(false);

                GUILayout.FlexibleSpace();

                int selCount = CountSelectedVisible();
                EditorGUILayout.LabelField($"Selected: {selCount}", GUILayout.Width(120));
            }

            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < slots.Count; i++)
            {
                if (!_includeRoot && i == 0) continue;

                SlotSpec s = slots[i];
                string name = (s != null ? s.slotName : string.Empty) ?? string.Empty;
                name = name.Trim();
                if (string.IsNullOrEmpty(name)) name = $"Slot {i}";

                if (!PassSearch(name, _search))
                    continue;

                if (!_selected.TryGetValue(i, out bool on))
                    on = false;

                int widgetCount = (s != null && s.widgets != null) ? s.widgets.Count : 0;

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool newOn = EditorGUILayout.Toggle(on, GUILayout.Width(18));
                    if (newOn != on) _selected[i] = newOn;

                    GUILayout.Label($"[{i}] {name}", GUILayout.MinWidth(220));

                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"widgets: {widgetCount}", EditorStyles.miniLabel, GUILayout.Width(80));
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawBottomBar()
    {
        bool canImport =
            _source != null && _source.spec != null && _source.spec.slots != null &&
            _target != null &&
            !ReferenceEquals(_source, _target);

        using (new EditorGUI.DisabledScope(!canImport))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Import Selected → Target", GUILayout.Height(28), GUILayout.Width(220)))
                {
                    ImportSelected();
                }
            }
        }
    }

    private void ImportSelected()
    {
        if (_source == null || _target == null) return;
        if (ReferenceEquals(_source, _target)) return;

        var srcSlots = _source.spec?.slots;
        if (srcSlots == null) return;

        // collect selected indices (visible 기준이 아니라, 선택된 전체)
        var indices = new List<int>();
        foreach (var kv in _selected)
        {
            if (!kv.Value) continue;
            indices.Add(kv.Key);
        }

        indices.Sort();

        // root 제외 옵션이면 혹시 선택되어도 제거
        if (!_includeRoot)
            indices.RemoveAll(i => i == 0);

        if (indices.Count == 0)
        {
            EditorUtility.DisplayDialog("Import Slots", "No slots selected.", "OK");
            return;
        }

        // target spec/slots ensure
        EnsureTargetSpecAndSlots(_target);

        // Undo
        Undo.RecordObject(_target, "Import Slots");
        EditorUtility.SetDirty(_target);

        var dstSlots = _target.spec.slots;

        // Root ensure (너의 정책과 동일)
        EnsureRootExists(dstSlots);

        int imported = 0;
        int skipped = 0;
        int overwritten = 0;

        // build map for merge
        var nameToIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        if (_mode == ImportMode.MergeByName)
        {
            for (int i = 0; i < dstSlots.Count; i++)
            {
                var nm = (dstSlots[i]?.slotName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(nm)) continue;
                if (!nameToIndex.ContainsKey(nm)) nameToIndex.Add(nm, i);
            }
        }

        foreach (int idx in indices)
        {
            if (idx < 0 || idx >= srcSlots.Count) continue;
            var src = srcSlots[idx];
            if (src == null) continue;

            var clone = CloneSlotSpec(src);
            NormalizeSlotNameForImport(clone, idx);

            // Root 포함 옵션이라도 target의 canonical root(0)를 덮는 건 위험할 수 있음.
            // => 사용자가 includeRoot 켰다면 "name 기준"으로 동작하게 두고,
            //    Append에서는 그냥 추가/유니크 처리, Merge에서는 slotName이 같으면 merge 규칙으로 처리.
            string slotName = (clone.slotName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(slotName))
            {
                skipped++;
                continue;
            }

            if (_mode == ImportMode.Append)
            {
                if (_ensureUniqueNamesOnAppend)
                    clone.slotName = MakeUniqueSlotName(dstSlots, slotName);

                dstSlots.Add(clone);
                imported++;
            }
            else // MergeByName
            {
                if (nameToIndex.TryGetValue(slotName, out int existing))
                {
                    if (_overwriteOnMerge)
                    {
                        dstSlots[existing] = clone;
                        overwritten++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                else
                {
                    dstSlots.Add(clone);
                    nameToIndex[slotName] = dstSlots.Count - 1;
                    imported++;
                }
            }
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Import Slots",
            $"Done.\n\n" +
            $"- Imported: {imported}\n" +
            $"- Overwritten: {overwritten}\n" +
            $"- Skipped: {skipped}",
            "OK");
    }

    // ─────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────

    private void ResetSelectionCache()
    {
        _selected.Clear();

        // source 바뀌면 기본 선택 상태는 false로 두되,
        // includeRoot에 따라 선택 가능 목록이 달라질 수 있으니 그냥 초기화만.
        Repaint();
    }

    private bool PassSearch(string name, string search)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        return name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void SetAllVisibleSelection(bool on)
    {
        if (_source?.spec?.slots == null) return;

        var slots = _source.spec.slots;
        for (int i = 0; i < slots.Count; i++)
        {
            if (!_includeRoot && i == 0) continue;

            var s = slots[i];
            string name = (s != null ? s.slotName : string.Empty) ?? string.Empty;
            name = name.Trim();
            if (string.IsNullOrEmpty(name)) name = $"Slot {i}";

            if (!PassSearch(name, _search))
                continue;

            _selected[i] = on;
        }

        Repaint();
    }

    private int CountSelectedVisible()
    {
        if (_source?.spec?.slots == null) return 0;

        int count = 0;
        var slots = _source.spec.slots;
        for (int i = 0; i < slots.Count; i++)
        {
            if (!_includeRoot && i == 0) continue;

            var s = slots[i];
            string name = (s != null ? s.slotName : string.Empty) ?? string.Empty;
            name = name.Trim();
            if (string.IsNullOrEmpty(name)) name = $"Slot {i}";

            if (!PassSearch(name, _search))
                continue;

            if (_selected.TryGetValue(i, out bool on) && on)
                count++;
        }

        return count;
    }

    private static void EnsureTargetSpecAndSlots(UIScreenSpecAsset target)
    {
        if (target.spec == null)
            target.spec = new UIScreenSpec();

        if (target.spec.slots == null)
            target.spec.slots = new List<SlotSpec>();
    }

    private static void EnsureRootExists(List<SlotSpec> slots)
    {
        if (slots == null) return;

        if (slots.Count == 0)
        {
            slots.Add(new SlotSpec { slotName = "Root", widgets = new List<WidgetSpec>() });
            return;
        }

        if (slots[0] == null)
            slots[0] = new SlotSpec { slotName = "Root", widgets = new List<WidgetSpec>() };

        if (string.IsNullOrWhiteSpace(slots[0].slotName))
            slots[0].slotName = "Root";

        if (slots[0].widgets == null)
            slots[0].widgets = new List<WidgetSpec>();
    }

    private static string MakeUniqueSlotName(List<SlotSpec> dst, string baseName)
    {
        baseName = (baseName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(baseName)) baseName = "Slot";

        bool Exists(string n)
        {
            for (int i = 0; i < dst.Count; i++)
            {
                var s = dst[i];
                string name = (s?.slotName ?? string.Empty).Trim();
                if (string.Equals(name, n, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        if (!Exists(baseName))
            return baseName;

        int k = 1;
        while (true)
        {
            string cand = $"{baseName}_{k}";
            if (!Exists(cand))
                return cand;
            k++;
        }
    }

    private static SlotSpec CloneSlotSpec(SlotSpec src)
    {
        var dst = new SlotSpec
        {
            slotName = src.slotName,
            widgets = new List<WidgetSpec>()
        };

        if (src.widgets != null)
        {
            foreach (var w in src.widgets)
            {
                if (w == null) continue;
                dst.widgets.Add(CloneWidgetSpec(w));
            }
        }

        return dst;
    }

    private static WidgetSpec CloneWidgetSpec(WidgetSpec w)
    {
        return new WidgetSpec
        {
            widgetType = w.widgetType,
            nameTag = w.nameTag,
            text = w.text,
            onClickRoute = w.onClickRoute,
            prefabOverride = w.prefabOverride,

            rectMode = w.rectMode,
            disabled = w.disabled,

            anchorMin = w.anchorMin,
            anchorMax = w.anchorMax,
            pivot = w.pivot,
            anchoredPosition = w.anchoredPosition,
            sizeDelta = w.sizeDelta,

            imageSprite = w.imageSprite,
            imageColor = w.imageColor,
            imageSetNativeSize = w.imageSetNativeSize,

            toggleInitialValue = w.toggleInitialValue,
            toggleInteractable = w.toggleInteractable,

            sliderMin = w.sliderMin,
            sliderMax = w.sliderMax,
            sliderInitialValue = w.sliderInitialValue,
            sliderWholeNumbers = w.sliderWholeNumbers,

            slotId = w.slotId
        };
    }

    private static void NormalizeSlotNameForImport(SlotSpec s, int fallbackIndex)
    {
        if (s == null) return;
        s.slotName = (s.slotName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(s.slotName))
            s.slotName = $"Slot {fallbackIndex}";
        if (s.widgets == null)
            s.widgets = new List<WidgetSpec>();
    }
}
#endif
