#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public sealed class UIScreenSpecEditorWindow : EditorWindow
{
    private UIScreenSpecAsset _asset;
    private SerializedObject _so;

    private SerializedProperty _specProp;
    private SerializedProperty _slotsProp;

    private ReorderableList _slotsList;
    private ReorderableList _widgetsList;

    private Vector2 _slotsScroll;
    private Vector2 _widgetsScroll;

    private int _selectedSlotIndex = -1;

    // Current slot index path, representing the navigation depth.
    // ex) [0] -> [0, 2] -> [0, 2, 5]
    private readonly List<int> _slotPath = new();

    // Per-widget foldout state cache
    private readonly Dictionary<string, bool> _widgetFoldoutStates = new();

    // Widget preset catalog
    [SerializeField] private WidgetPresetCatalog _presetCatalog;
    private readonly Dictionary<string, int> _widgetPresetSelection = new();

    [MenuItem("Tools/UI/UIScreen Spec Editor")]
    public static void Open()
    {
        var w = GetWindow<UIScreenSpecEditorWindow>();
        w.titleContent = new GUIContent("UIScreen Spec Editor");
        w.Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(530, 380);
        Selection.selectionChanged += TryAutoBindFromSelection;
        TryAutoBindFromSelection();

        _slotsScroll = Vector2.zero;
        _widgetsScroll = Vector2.zero;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= TryAutoBindFromSelection;
    }

    private void TryAutoBindFromSelection()
    {
        var sel = Selection.activeObject as UIScreenSpecAsset;
        if (sel == null) return;

        Bind(sel);
        Repaint();
    }

    private void Bind(UIScreenSpecAsset asset)
    {
        _asset = asset;
        _so = new SerializedObject(_asset);

        _specProp = _so.FindProperty("spec");
        if (_specProp == null)
        {
            Debug.LogError("[UIScreenSpecEditor] 'spec' property not found on UIScreenSpecAsset.");
            return;
        }

        _slotsProp = _specProp.FindPropertyRelative("slots");

        // Ensure at least one root slot exists
        EnsureRootSlotExists();

        BuildSlotsList();

        _slotPath.Clear();

        if (_slotsProp != null && _slotsProp.arraySize > 0)
        {
            _selectedSlotIndex = Mathf.Clamp(_selectedSlotIndex, 0, _slotsProp.arraySize - 1);
            SetRootSlot(_selectedSlotIndex);
        }
        else
        {
            _selectedSlotIndex = -1;
            _widgetsList = null;
        }
    }

    private void EnsureRootSlotExists()
    {
        if (_slotsProp == null) return;

        if (_slotsProp.arraySize == 0)
        {
            _slotsProp.InsertArrayElementAtIndex(0);
            var root = _slotsProp.GetArrayElementAtIndex(0);

            var nameProp    = root.FindPropertyRelative("slotName");
            var widgetsProp = root.FindPropertyRelative("widgets");

            // Initial root slot id can be customized later to match the UISlot.id on the template.
            if (nameProp != null)
                nameProp.stringValue = "Root";

            if (widgetsProp != null)
                widgetsProp.ClearArray();

            _so.ApplyModifiedProperties();
        }
    }

    // ─────────────────────────────────────────────
    // Slots list
    // ─────────────────────────────────────────────
    private void BuildSlotsList()
    {
        // Read-only list in terms of add/remove/drag (we manage order manually).
        _slotsList = new ReorderableList(_so, _slotsProp,
            draggable: false,
            displayHeader: true,
            displayAddButton: false,
            displayRemoveButton: false);

        _slotsList.drawHeaderCallback = rect =>
            EditorGUI.LabelField(rect, "Slots");

        _slotsList.onSelectCallback = list =>
        {
            _selectedSlotIndex = list.index;
            RebuildSlotPathForSelected(_selectedSlotIndex);
        };

        _slotsList.onAddCallback = list =>
        {
            int i = _slotsProp.arraySize;
            _slotsProp.InsertArrayElementAtIndex(i);
            var slot = _slotsProp.GetArrayElementAtIndex(i);

            var nameProp    = slot.FindPropertyRelative("slotName");
            var widgetsProp = slot.FindPropertyRelative("widgets");

            if (nameProp != null)
                nameProp.stringValue = $"Slot {i}";

            if (widgetsProp != null)
                widgetsProp.ClearArray();

            _so.ApplyModifiedProperties();

            // Treat the newly created slot as a new root context for editing.
            SetRootSlot(i);
        };

        _slotsList.onRemoveCallback = list =>
        {
            if (list.index < 0 || list.index >= _slotsProp.arraySize)
                return;

            int removeIndex = list.index;

            _slotsProp.DeleteArrayElementAtIndex(removeIndex);
            _so.ApplyModifiedProperties();

            if (_slotsProp.arraySize == 0)
            {
                _selectedSlotIndex = -1;
                _slotPath.Clear();
                _widgetsList = null;
                return;
            }

            int newIndex = Mathf.Clamp(removeIndex, 0, _slotsProp.arraySize - 1);
            _selectedSlotIndex = newIndex;
            RebuildSlotPathForSelected(newIndex);
            Repaint();
        };

        _slotsList.elementHeightCallback = index =>
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            float vGap  = 2f;

            // Root (index 0) uses 2 lines (main + Root Slot Id field), others use 1 line.
            int lines = (index == 0) ? 2 : 1;

            return lines * (lineH + vGap) + 4f;
        };

        _slotsList.drawElementCallback = DrawSlotElement;
    }

    private void DrawSlotElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        float lineH = EditorGUIUtility.singleLineHeight;
        float vGap  = 2f;

        rect.y += 2f;

        const float horizontalPadding = 4f;
        rect.x     += horizontalPadding;
        rect.width -= horizontalPadding * 2f;
        rect.height = lineH;

        var slot        = _slotsProp.GetArrayElementAtIndex(index);
        var nameProp    = slot.FindPropertyRelative("slotName");
        var widgetsProp = slot.FindPropertyRelative("widgets");
        int widgetCount = widgetsProp != null ? widgetsProp.arraySize : 0;

        int    depth;
        string pathLabel = GetSlotDisplayPath(index, out depth);

        // Check if this slot is unlinked (no parent) – index 0 is always treated as the root.
        bool isUnlinked = false;
        if (index > 0)
        {
            var hasParent = BuildHasParentFlags();
            if (index < hasParent.Length)
                isUnlinked = !hasParent[index];
        }

        string labelText;
        if (index == 0)
        {
            // Root slot label (first line)
            labelText = $"[Root] {pathLabel} ({widgetCount})";
        }
        else if (isUnlinked)
        {
            // Slot that has no parent reference in the current graph
            labelText = $"(!) [unlinked] {pathLabel} ({widgetCount})";
        }
        else
        {
            // Normal slot with a valid parent chain
            labelText = $"[depth{depth}] {pathLabel} ({widgetCount})";
        }

        // ──────────────────────
        // First line: label + (↑↓ controls for non-root slots)
        // ──────────────────────
        const float btnWidth = 18f;
        const float btnGap   = 2f;

        float labelWidth = rect.width;
        if (index > 0)
            labelWidth -= (btnWidth * 2f + btnGap * 2f);

        var labelRect = new Rect(rect.x, rect.y, labelWidth, lineH);
        EditorGUI.LabelField(labelRect, labelText);

        // Move buttons for non-root slots
        if (index > 0)
        {
            var upRect   = new Rect(labelRect.xMax + btnGap, rect.y, btnWidth, lineH);
            var downRect = new Rect(upRect.xMax + btnGap, rect.y, btnWidth, lineH);

            // Up: only from index >= 2 (index 1 cannot move into root position)
            using (new EditorGUI.DisabledScope(index <= 1))
            {
                if (GUI.Button(upRect, "↑"))
                {
                    MoveSlot(index, index - 1);
                }
            }

            // Down: any index that is not the last one
            using (new EditorGUI.DisabledScope(index >= _slotsProp.arraySize - 1))
            {
                if (GUI.Button(downRect, "↓"))
                {
                    MoveSlot(index, index + 1);
                }
            }
        }

        // ──────────────────────
        // Second line: Root Slot Id field (root only)
        // ──────────────────────
        if (index == 0)
        {
            var idRect = new Rect(rect.x, rect.y + lineH + vGap, rect.width, lineH);

            string currentName = nameProp != null ? nameProp.stringValue : string.Empty;

            EditorGUI.BeginChangeCheck();
            string newName = EditorGUI.TextField(idRect, "Root Slot Id", currentName);
            if (EditorGUI.EndChangeCheck() && nameProp != null)
            {
                newName = (newName ?? string.Empty).Trim();

                if (string.IsNullOrEmpty(newName))
                {
                    EditorUtility.DisplayDialog(
                        "Invalid Root Slot Id",
                        "Root Slot Id cannot be empty.\nPlease enter a unique id.",
                        "OK"
                    );
                }
                else if (IsSlotNameUsedByOtherSlots(0, newName))
                {
                    // 다른 슬롯에서 이미 사용 중인 이름이면 Root 로 쓸 수 없음
                    EditorUtility.DisplayDialog(
                        "Duplicate Root Slot Id",
                        $"The id '{newName}' is already used by another slot.\n\n" +
                        "Root Slot Id must be unique.\n" +
                        "Please choose a different id or rename the other slot first.",
                        "OK"
                    );
                }
                else
                {
                    // 유효하고, 중복도 아니면 반영
                    nameProp.stringValue = newName;
                    _so.ApplyModifiedProperties();
                }
            }
        }
    }

    private void MoveSlot(int from, int to)
    {
        if (_slotsProp == null) return;

        int size = _slotsProp.arraySize;
        if (from < 0 || from >= size) return;
        if (to   < 0 || to   >= size) return;

        // Slot 0 is the canonical root and cannot be moved into.
        if (to == 0) return;

        _slotsProp.MoveArrayElement(from, to);
        _so.ApplyModifiedProperties();

        // Update selection and rebuild path
        _slotsList.index  = to;
        _selectedSlotIndex = to;
        RebuildSlotPathForSelected(to);

        Repaint();
    }

    // ─────────────────────────────────────────────
    // Slot path & widget list for the current slot
    // ─────────────────────────────────────────────
    private void SetRootSlot(int slotIndex)
    {
        if (_slotsProp == null)
        {
            _slotPath.Clear();
            _widgetsList = null;
            _selectedSlotIndex = -1;
            return;
        }

        if (slotIndex < 0 || slotIndex >= _slotsProp.arraySize)
        {
            _slotPath.Clear();
            _widgetsList = null;
            _selectedSlotIndex = -1;
            return;
        }

        _selectedSlotIndex = slotIndex;

        _slotPath.Clear();
        _slotPath.Add(slotIndex);

        BuildWidgetsListForCurrentSlot();
    }

    /// <summary>
    /// When a slot is selected in the left list, reconstruct the slot path
    /// (root → ... → selected) based on Slot widgets (slotId) connections.
    /// </summary>
    private void RebuildSlotPathForSelected(int targetIndex)
    {
        if (_slotsProp == null || _slotsProp.arraySize == 0)
        {
            SetRootSlot(targetIndex);
            return;
        }

        int slotCount = _slotsProp.arraySize;
        if (targetIndex < 0 || targetIndex >= slotCount)
        {
            SetRootSlot(targetIndex);
            return;
        }

        // 1) Build name -> index map
        var nameToIndex = new Dictionary<string, int>();
        for (int i = 0; i < slotCount; i++)
        {
            var slot     = _slotsProp.GetArrayElementAtIndex(i);
            var nameProp = slot.FindPropertyRelative("slotName");
            string name  = (nameProp != null ? nameProp.stringValue : string.Empty)?.Trim();
            if (!string.IsNullOrEmpty(name) && !nameToIndex.ContainsKey(name))
                nameToIndex.Add(name, i);
        }

        // 2) Build parent -> children graph using Slot widgets (slotId)
        var children  = new List<int>[slotCount];
        var hasParent = new bool[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            children[i] = new List<int>();

            var slot        = _slotsProp.GetArrayElementAtIndex(i);
            var widgetsProp = slot.FindPropertyRelative("widgets");
            if (widgetsProp == null) continue;

            for (int wi = 0; wi < widgetsProp.arraySize; wi++)
            {
                var widget     = widgetsProp.GetArrayElementAtIndex(wi);
                var typeProp   = widget.FindPropertyRelative("widgetType");
                var slotIdProp = widget.FindPropertyRelative("slotId");

                if (typeProp == null) continue;
                var widgetType = (WidgetType)typeProp.enumValueIndex;
                if (widgetType != WidgetType.Slot) continue;

                string id = (slotIdProp != null ? slotIdProp.stringValue : string.Empty)?.Trim();
                if (string.IsNullOrEmpty(id)) continue;

                if (nameToIndex.TryGetValue(id, out int childIndex))
                {
                    children[i].Add(childIndex);
                    hasParent[childIndex] = true;
                }
            }
        }

        // 3) Collect root candidates (slots with no parent)
        var roots = new List<int>();
        for (int i = 0; i < slotCount; i++)
        {
            if (!hasParent[i])
                roots.Add(i);
        }

        // 4) DFS from each root to find a path to targetIndex
        var path     = new List<int>();
        var visiting = new HashSet<int>();

        bool TryDfs(int current)
        {
            if (visiting.Contains(current))
                return false; // prevent cycles

            visiting.Add(current);
            path.Add(current);

            if (current == targetIndex)
                return true;

            foreach (int child in children[current])
            {
                if (TryDfs(child))
                    return true;
            }

            // backtrack on failure
            path.RemoveAt(path.Count - 1);
            visiting.Remove(current);
            return false;
        }

        bool found = false;
        foreach (int root in roots)
        {
            path.Clear();
            visiting.Clear();
            if (TryDfs(root))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            // If the graph traversal cannot find a path, treat this slot as an independent root.
            SetRootSlot(targetIndex);
            return;
        }

        _slotPath.Clear();
        _slotPath.AddRange(path);
        _selectedSlotIndex = targetIndex;
        BuildWidgetsListForCurrentSlot();
    }

    private void BuildWidgetsListForCurrentSlot()
    {
        _widgetsList = null;

        if (_slotsProp == null || _slotPath.Count == 0)
            return;

        int slotIndex = _slotPath[_slotPath.Count - 1];
        if (slotIndex < 0 || slotIndex >= _slotsProp.arraySize)
            return;

        var slot        = _slotsProp.GetArrayElementAtIndex(slotIndex);
        var widgetsProp = slot.FindPropertyRelative("widgets");

        _widgetsList = new ReorderableList(_so, widgetsProp, true, true, true, true);

        _widgetsList.drawElementBackgroundCallback = (rect, index, isActive, isFocused) =>
        {
            const float padding = 2f;

            Rect bgRect = new Rect(
                rect.x + padding,
                rect.y + padding,
                rect.width - padding * 2f,
                rect.height - padding * 2f
            );

            Color normalBg   = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            Color selectedBg = new Color(0f, 0f, 0f, 0.24f);

            EditorGUI.DrawRect(bgRect, isActive ? selectedBg : normalBg);
        };

        _widgetsList.drawHeaderCallback = rect =>
        {
            if (_slotsProp == null || slotIndex < 0 || slotIndex >= _slotsProp.arraySize)
            {
                EditorGUI.LabelField(rect, "Widgets");
                return;
            }

            var slotProp = _slotsProp.GetArrayElementAtIndex(slotIndex);
            var nameProp = slotProp.FindPropertyRelative("slotName");
            EditorGUI.LabelField(rect, $"Widgets (Slot: {nameProp.stringValue})");
        };

        _widgetsList.onRemoveCallback = list =>
        {
            if (widgetsProp == null) return;
            if (list.index < 0 || list.index >= widgetsProp.arraySize) return;

            widgetsProp.DeleteArrayElementAtIndex(list.index);
            _so.ApplyModifiedProperties();
            BuildWidgetsListForCurrentSlot();
            Repaint();
        };

        _widgetsList.elementHeightCallback = index => CalcWidgetElementHeight(widgetsProp, index);

        _widgetsList.onAddCallback = list =>
        {
            if (widgetsProp == null) return;

            int insertIndex = widgetsProp.arraySize;
            widgetsProp.InsertArrayElementAtIndex(insertIndex);

            var newElem = widgetsProp.GetArrayElementAtIndex(insertIndex);
            ResetWidgetSpecDefaults(newElem, insertIndex);

            _so.ApplyModifiedProperties();
            BuildWidgetsListForCurrentSlot();
            if (_widgetsList != null)
                _widgetsList.index = insertIndex;

            Repaint();
        };

        _widgetsList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            DrawWidgetElement(rect, index, isActive, isFocused, widgetsProp);
        };
    }

    // ─────────────────────────────────────────────
    // Per-widget element height
    // ─────────────────────────────────────────────
    private float CalcWidgetElementHeight(SerializedProperty widgetsProp, int index)
    {
        float lineH         = EditorGUIUtility.singleLineHeight;
        float vGap          = 2f;
        float borderPadding = 2f;

        if (widgetsProp == null || index < 0 || index >= widgetsProp.arraySize)
            return lineH + 2f * borderPadding;

        var w = widgetsProp.GetArrayElementAtIndex(index);

        string foldKey = w.propertyPath;
        bool expanded  = true;
        _widgetFoldoutStates.TryGetValue(foldKey, out expanded);

        if (!expanded)
        {
            int   collapsedLines  = 1;
            float collapsedHeight = collapsedLines * (lineH + vGap) + vGap;
            return collapsedHeight + borderPadding * 2f + 4f;
        }

        int lines = 0;

        // 1 line: Name + Type
        lines += 1;
        // Preset dropdown
        lines += 1;
        // Text 2 lines
        lines += 2;

        var typeProp   = w.FindPropertyRelative("widgetType");
        var widgetType = (WidgetType)typeProp.enumValueIndex;

        // Route + Prefab
        lines += (widgetType == WidgetType.Button) ? 2 : 1;

        // Layout Mode
        lines += 1;

        var rectModeProp = w.FindPropertyRelative("rectMode");
        var rectMode     = (WidgetRectMode)rectModeProp.enumValueIndex;
        if (rectMode == WidgetRectMode.OverrideInSlot)
        {
            // AnchorMin, AnchorMax, Pivot, Size, Position
            lines += 5;
        }

        switch (widgetType)
        {
            case WidgetType.Button:
                lines += 1;
                break;
            case WidgetType.Image:
                lines += 4;
                break;
            case WidgetType.Toggle:
                lines += 3;
                break;
            case WidgetType.Slider:
                lines += 5;
                break;
            case WidgetType.Slot:
                // [Slot Options] + Slot Id
                lines += 2;
                break;
        }

        float contentHeight = lines * (lineH + vGap) + vGap;
        return contentHeight + borderPadding * 2f + 4f;
    }

    // ─────────────────────────────────────────────
    // Widget element rendering
    // ─────────────────────────────────────────────
    private void DrawWidgetElement(
        Rect rect,
        int index,
        bool isActive,
        bool isFocused,
        SerializedProperty widgetsProp
    )
    {
        var e = Event.current;

        const float borderPadding = 2f;
        var borderRect = new Rect(
            rect.x + borderPadding,
            rect.y + borderPadding,
            rect.width - borderPadding * 2f,
            rect.height - borderPadding * 2f
        );

        EditorGUI.DrawRect(borderRect, new Color(0.25f, 0.25f, 0.25f, 0.3f));

        float vGap = 2f;
        const float horizontalPadding = 6f;

        rect = borderRect;
        rect.y     += vGap;
        rect.x     += horizontalPadding;
        rect.width -= horizontalPadding * 2f;

        float lineH = EditorGUIUtility.singleLineHeight;
        float y     = rect.y;

        var w = widgetsProp.GetArrayElementAtIndex(index);
        var typeProp       = w.FindPropertyRelative("widgetType");
        var nameProp       = w.FindPropertyRelative("nameTag");
        var textProp       = w.FindPropertyRelative("text");
        var routeProp      = w.FindPropertyRelative("onClickRoute");
        var prefabProp     = w.FindPropertyRelative("prefabOverride");
        var rectModeProp   = w.FindPropertyRelative("rectMode");
        var anchorMinProp  = w.FindPropertyRelative("anchorMin");
        var anchorMaxProp  = w.FindPropertyRelative("anchorMax");
        var pivotProp      = w.FindPropertyRelative("pivot");
        var anchoredPosProp = w.FindPropertyRelative("anchoredPosition");
        var sizeDeltaProp  = w.FindPropertyRelative("sizeDelta");

        var imageSpriteProp = w.FindPropertyRelative("imageSprite");
        var imageColorProp  = w.FindPropertyRelative("imageColor");
        var imageNativeProp = w.FindPropertyRelative("imageSetNativeSize");

        var toggleInitialProp = w.FindPropertyRelative("toggleInitialValue");
        var toggleInteractProp = w.FindPropertyRelative("toggleInteractable");

        var sliderMinProp   = w.FindPropertyRelative("sliderMin");
        var sliderMaxProp   = w.FindPropertyRelative("sliderMax");
        var sliderInitProp  = w.FindPropertyRelative("sliderInitialValue");
        var sliderWholeProp = w.FindPropertyRelative("sliderWholeNumbers");
        var disabledProp    = w.FindPropertyRelative("disabled");

        var slotIdProp = w.FindPropertyRelative("slotId");

        // Context menu (Add / Delete widget)
        if (e.type == EventType.ContextClick && borderRect.Contains(e.mousePosition))
        {
            var menu          = new GenericMenu();
            int capturedIndex = index;

            menu.AddItem(new GUIContent("Add Widget Below"), false, () =>
            {
                if (widgetsProp == null) return;

                int insertIndex = Mathf.Clamp(capturedIndex + 1, 0, widgetsProp.arraySize);
                widgetsProp.InsertArrayElementAtIndex(insertIndex);

                var newElem = widgetsProp.GetArrayElementAtIndex(insertIndex);
                ResetWidgetSpecDefaults(newElem, insertIndex);

                _so.ApplyModifiedProperties();
                BuildWidgetsListForCurrentSlot();
                if (_widgetsList != null)
                    _widgetsList.index = insertIndex;
                Repaint();
            });

            menu.AddItem(new GUIContent("Delete Widget"), false, () =>
            {
                if (widgetsProp == null) return;
                if (capturedIndex < 0 || capturedIndex >= widgetsProp.arraySize) return;

                widgetsProp.DeleteArrayElementAtIndex(capturedIndex);
                _so.ApplyModifiedProperties();
                BuildWidgetsListForCurrentSlot();
                Repaint();
            });

            menu.ShowAsContext();
            e.Use();
        }

        // Header: Foldout + Enabled toggle + Name + Type
        string foldKey = w.propertyPath;
        bool   expanded = true;
        _widgetFoldoutStates.TryGetValue(foldKey, out expanded);

        var foldoutRect = new Rect(rect.x, y, 14f, lineH);
        expanded = EditorGUI.Foldout(foldoutRect, expanded, GUIContent.none);
        _widgetFoldoutStates[foldKey] = expanded;

        float x = foldoutRect.xMax + 2f;

        var toggleRect = new Rect(x, y, 18f, lineH);
        bool enabled = disabledProp != null ? !disabledProp.boolValue : true;
        enabled = EditorGUI.Toggle(toggleRect, enabled);
        if (disabledProp != null)
            disabledProp.boolValue = !enabled;

        x = toggleRect.xMax + 4f;

        const float typeWidth = 70f;
        const float gap       = 4f;

        float typeX    = rect.x + rect.width - typeWidth;
        var   typeRect = new Rect(typeX, y, typeWidth, lineH);

        float nameWidth = typeX - x - gap;
        if (nameWidth < 60f) nameWidth = 60f;
        var nameFieldRect = new Rect(x, y, nameWidth, lineH);

        nameProp.stringValue = EditorGUI.TextField(nameFieldRect, nameProp.stringValue);
        EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);

        y += lineH + vGap;

        if (!expanded)
            return;

        var widgetType = (WidgetType)typeProp.enumValueIndex;

        // Preset selection
        {
            string[] labels;
            bool hasPresetCatalog =
                _presetCatalog != null &&
                _presetCatalog.presets != null &&
                _presetCatalog.presets.Count > 0;

            if (hasPresetCatalog)
            {
                var presets     = _presetCatalog.presets;
                int presetCount = presets.Count;

                labels      = new string[presetCount + 1];
                labels[0]   = "Select Preset";
                for (int pi = 0; pi < presetCount; pi++)
                {
                    var p = presets[pi];
                    labels[pi + 1] = string.IsNullOrEmpty(p.id) ? $"Preset {pi}" : p.id;
                }
            }
            else
            {
                labels = new[] { "(No presets configured)" };
            }

            var presetRect = new Rect(rect.x, y, rect.width, lineH);

            string presetKey = w.propertyPath;
            if (!_widgetPresetSelection.TryGetValue(presetKey, out int currentIndex))
                currentIndex = 0;

            if (currentIndex < 0 || currentIndex >= labels.Length)
                currentIndex = 0;

            EditorGUI.BeginDisabledGroup(!hasPresetCatalog);
            int newIndex = EditorGUI.Popup(presetRect, currentIndex, labels);
            EditorGUI.EndDisabledGroup();

            if (hasPresetCatalog && newIndex != currentIndex)
            {
                _widgetPresetSelection[presetKey] = newIndex;

                if (newIndex > 0)
                {
                    var presets = _presetCatalog.presets;
                    var chosen  = presets[newIndex - 1];
                    ApplyPresetToWidget(chosen, w);
                    _so.ApplyModifiedProperties();
                }
            }

            y += lineH + vGap;
        }

        // Layout Mode
        var layoutModeRect = new Rect(rect.x, y, rect.width, lineH);
        EditorGUI.PropertyField(layoutModeRect, rectModeProp, new GUIContent("Layout Mode"));
        y += lineH + vGap;

        var rectMode = (WidgetRectMode)rectModeProp.enumValueIndex;

        if (rectMode == WidgetRectMode.OverrideInSlot)
        {
            float labelWidth = 90f;
            float fieldGap   = 4f;
            float rowHeight  = lineH;

            Rect MakeRowRect() => new Rect(rect.x, y, rect.width, rowHeight);

            // Anchor Min
            var rowRect   = MakeRowRect();
            var labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowHeight);
            var valueRect = new Rect(
                rowRect.x + labelWidth + fieldGap,
                rowRect.y,
                rowRect.width - labelWidth - fieldGap,
                rowHeight
            );

            EditorGUI.LabelField(labelRect, "Anchor Min");
            var anchorMinValue = anchorMinProp.vector2Value;
            anchorMinValue     = EditorGUI.Vector2Field(valueRect, GUIContent.none, anchorMinValue);
            anchorMinProp.vector2Value = anchorMinValue;
            y += rowHeight + vGap;

            // Anchor Max
            rowRect   = MakeRowRect();
            labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowHeight);
            valueRect = new Rect(
                rowRect.x + labelWidth + fieldGap,
                rowRect.y,
                rowRect.width - labelWidth - fieldGap,
                rowHeight
            );

            EditorGUI.LabelField(labelRect, "Anchor Max");
            var anchorMaxValue  = anchorMaxProp.vector2Value;
            anchorMaxValue      = EditorGUI.Vector2Field(valueRect, GUIContent.none, anchorMaxValue);
            anchorMaxProp.vector2Value = anchorMaxValue;
            y += rowHeight + vGap;

            // Pivot
            rowRect   = MakeRowRect();
            labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowHeight);
            valueRect = new Rect(
                rowRect.x + labelWidth + fieldGap,
                rowRect.y,
                rowRect.width - labelWidth - fieldGap,
                rowHeight
            );

            EditorGUI.LabelField(labelRect, "Pivot");
            var pivotValue   = pivotProp.vector2Value;
            pivotValue       = EditorGUI.Vector2Field(valueRect, GUIContent.none, pivotValue);
            pivotProp.vector2Value = pivotValue;
            y += rowHeight + vGap;

            // Size
            rowRect   = MakeRowRect();
            labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowHeight);
            valueRect = new Rect(
                rowRect.x + labelWidth + fieldGap,
                rowRect.y,
                rowRect.width - labelWidth - fieldGap,
                rowHeight
            );

            EditorGUI.LabelField(labelRect, "Size");
            var sizeValue   = sizeDeltaProp.vector2Value;
            sizeValue       = EditorGUI.Vector2Field(valueRect, GUIContent.none, sizeValue);
            sizeDeltaProp.vector2Value = sizeValue;
            y += rowHeight + vGap;

            // Position
            rowRect   = MakeRowRect();
            labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowHeight);
            valueRect = new Rect(
                rowRect.x + labelWidth + fieldGap,
                rowRect.y,
                rowRect.width - labelWidth - fieldGap,
                rowHeight
            );

            EditorGUI.LabelField(labelRect, "Position");
            var posValue   = anchoredPosProp.vector2Value;
            posValue       = EditorGUI.Vector2Field(valueRect, GUIContent.none, posValue);
            anchoredPosProp.vector2Value = posValue;
            y += rowHeight + vGap;

            // Type-specific options
            switch (widgetType)
            {
                case WidgetType.Button:
                {
                    var headerRect = new Rect(rect.x, y, rect.width, lineH);
                    EditorGUI.LabelField(headerRect, "[Button Options]", EditorStyles.miniBoldLabel);
                    y += lineH + vGap;

                    var routeRect = new Rect(rect.x, y, rect.width, lineH);
                    routeProp.stringValue =
                        EditorGUI.TextField(routeRect, "OnClick Route", routeProp.stringValue);
                    y += lineH + vGap;
                    break;
                }
                case WidgetType.Image:
                {
                    var headerRect = new Rect(rect.x, y, rect.width, lineH);
                    EditorGUI.LabelField(headerRect, "[Image Options]", EditorStyles.miniBoldLabel);
                    y += lineH + vGap;

                    var spriteRect = new Rect(rect.x, y, rect.width, lineH);
                    EditorGUI.PropertyField(spriteRect, imageSpriteProp, new GUIContent("Sprite"));
                    y += lineH + vGap;

                    var colorRect = new Rect(rect.x, y, rect.width, lineH);
                    EditorGUI.PropertyField(colorRect, imageColorProp, new GUIContent("Color"));
                    y += lineH + vGap;

                    var nativeRect = new Rect(rect.x, y, rect.width, lineH);
                    EditorGUI.PropertyField(nativeRect, imageNativeProp, new GUIContent("Set Native Size"));
                    y += lineH + vGap;
                    break;
                }
                case WidgetType.Toggle:
                {
                    var headerRect = new Rect(rect.x, y, rect.width, lineH);
                    EditorGUI.LabelField(headerRect, "[Toggle Options]", EditorStyles.miniBoldLabel);
                    y += lineH + vGap;

                    var initRect = new Rect(rect.x, y, rect.width, lineH);
                    EditorGUI.PropertyField(initRect, toggleInitialProp, new GUIContent("Initial Value"));
                    y += lineH + vGap;

                    var interactRect = new Rect(rect.x, y, rect.width, lineH);
                    EditorGUI.PropertyField(interactRect, toggleInteractProp, new GUIContent("Interactable"));
                    y += lineH + vGap;
                    break;
                }
                case WidgetType.Slider:
                {
                    var headerRect = new Rect(rect.x, y, rect.width, lineH);
                    EditorGUI.LabelField(headerRect, "[Slider Options]", EditorStyles.miniBoldLabel);
                    y += lineH + vGap;

                    var minRect = new Rect(rect.x, y, rect.width, lineH);
                    EditorGUI.PropertyField(minRect, sliderMinProp, new GUIContent("Min"));
                    y += lineH + vGap;

                    var maxRect = new Rect(rect.x, y, rect.width, lineH);
                    EditorGUI.PropertyField(maxRect, sliderMaxProp, new GUIContent("Max"));
                    y += lineH + vGap;

                    var initRect = new Rect(rect.x, y, rect.width, lineH);
                    EditorGUI.PropertyField(initRect, sliderInitProp, new GUIContent("Initial Value"));
                    y += lineH + vGap;

                    var wholeRect = new Rect(rect.x, y, rect.width, lineH);
                    EditorGUI.PropertyField(wholeRect, sliderWholeProp, new GUIContent("Whole Numbers"));
                    y += lineH + vGap;
                    break;
                }
                case WidgetType.Slot:
                {
                    var headerRect = new Rect(rect.x, y, rect.width, lineH);
                    EditorGUI.LabelField(headerRect, "[Slot Options]", EditorStyles.miniBoldLabel);
                    y += lineH + vGap;

                    var idRect = new Rect(rect.x, y, rect.width - 120f, lineH);
                    slotIdProp.stringValue =
                        EditorGUI.TextField(idRect, "Slot Id", slotIdProp.stringValue);

                    var buttonRect = new Rect(idRect.xMax + 4f, y, 110f, lineH);
                    using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(slotIdProp.stringValue)))
                    {
                        if (GUI.Button(buttonRect, "Open Child Slot"))
                        {
                            string targetName = (slotIdProp.stringValue ?? string.Empty).Trim();
                            if (!string.IsNullOrEmpty(targetName))
                            {
                                OpenChildSlot(targetName);
                            }
                        }
                    }

                    y += lineH + vGap;
                    break;
                }
            }
        }

        // Text
        {
            int   textLines   = 2;
            float textHeight  = (lineH + 2f) * textLines;

            var textRect = new Rect(rect.x, y, rect.width, textHeight);
            textProp.stringValue =
                EditorGUI.TextArea(textRect, textProp.stringValue, EditorStyles.textArea);
            y += textHeight + vGap;
        }

        // Prefab Override
        {
            var prefabRect = new Rect(rect.x, y, rect.width, lineH);
            EditorGUI.PropertyField(prefabRect, prefabProp, new GUIContent("Prefab Override"));
            y += lineH + vGap;
        }
    }

    // Open a child slot by Slot widget's Slot Id, and push it into the current slot path.
    private void OpenChildSlot(string slotName)
    {
        if (_slotsProp == null) return;

        slotName = (slotName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(slotName)) return;

        // Current parent slot index (last in the breadcrumb path)
        int currentParentIndex = -1;
        if (_slotPath != null && _slotPath.Count > 0)
            currentParentIndex = _slotPath[_slotPath.Count - 1];

        // 1) Prevent simple cycles: linking to any ancestor in the current path
        if (WouldCreateCycleFromCurrentPath(slotName))
        {
            EditorUtility.DisplayDialog(
                "Invalid Slot Link",
                $"The slot '{slotName}' matches an ancestor slot name in the current path.\n" +
                "This may create a recursive structure.\n\n" +
                "A pattern like 'Root → ... → " + slotName + " → ... → " + slotName + "' is not allowed.",
                "OK"
            );
            return;
        }

        // 2) Warn if this slot name is already used as a child of another parent
        if (currentParentIndex >= 0 &&
            HasOtherParentForSlotName(slotName, currentParentIndex, out int otherParentIndex))
        {
            string currentParentName = GetSlotNameByIndex(currentParentIndex);
            string otherParentName   = GetSlotNameByIndex(otherParentIndex);

            EditorUtility.DisplayDialog(
                "Ambiguous Slot Graph",
                $"The slot '{slotName}' is already used as a child under another parent slot.\n\n" +
                $"- Existing parent: '{otherParentName}'\n" +
                $"- Current parent:  '{currentParentName}'\n\n" +
                "Sharing the same slot under multiple parents can make the slot path\n" +
                "more complex or harder to reason about.",
                "OK"
            );
            // Only warn and continue. If you want strict single-parent rules,
            // you can 'return;' here instead.
        }

        // 3) Find or create the child slot with the given name
        int childIndex = -1;
        for (int i = 0; i < _slotsProp.arraySize; i++)
        {
            var slot     = _slotsProp.GetArrayElementAtIndex(i);
            var nameProp = slot.FindPropertyRelative("slotName");
            string name  = (nameProp != null ? nameProp.stringValue : string.Empty)?.Trim();
            if (!string.IsNullOrEmpty(name) &&
                string.Equals(name, slotName, StringComparison.Ordinal))
            {
                childIndex = i;
                break;
            }
        }

        if (childIndex < 0)
        {
            childIndex = _slotsProp.arraySize;
            _slotsProp.InsertArrayElementAtIndex(childIndex);

            var newSlot     = _slotsProp.GetArrayElementAtIndex(childIndex);
            var nameProp    = newSlot.FindPropertyRelative("slotName");
            var widgetsProp = newSlot.FindPropertyRelative("widgets");

            if (nameProp != null)
                nameProp.stringValue = slotName;
            if (widgetsProp != null)
                widgetsProp.ClearArray();

            _so.ApplyModifiedProperties();
        }

        // 4) Push into the path and refresh widget list
        _slotPath.Add(childIndex);
        _selectedSlotIndex = childIndex;
        BuildWidgetsListForCurrentSlot();
        Repaint();
    }

    // ─────────────────────────────────────────────
    // OnGUI
    // ─────────────────────────────────────────────
    private void OnGUI()
    {
        EditorGUILayout.Space(6);

        var newAsset =
            (UIScreenSpecAsset)EditorGUILayout.ObjectField("Spec Asset", _asset, typeof(UIScreenSpecAsset), false);

        if (newAsset != _asset)
        {
            if (newAsset == null)
            {
                _asset       = null;
                _so          = null;
                _slotsList   = null;
                _widgetsList = null;
                _slotPath.Clear();
                return;
            }

            Bind(newAsset);
        }

        _presetCatalog = (WidgetPresetCatalog)EditorGUILayout.ObjectField(
            "Widget Presets",
            _presetCatalog,
            typeof(WidgetPresetCatalog),
            false);

        if (_asset != null && _so == null)
        {
            Bind(_asset);
        }

        if (_asset == null || _so == null)
        {
            EditorGUILayout.HelpBox(
                "Select or drag a UIScreenSpecAsset to begin editing.\n" +
                "(Click a Spec Asset in the Project window to auto-bind.)",
                MessageType.Info);
            return;
        }

        _so.Update();

        var prefabProp = _specProp.FindPropertyRelative("templatePrefab");

        EditorGUILayout.LabelField("Template", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(prefabProp, new GUIContent("Template Prefab"));

        EditorGUILayout.Space(8);

        using (new EditorGUILayout.HorizontalScope())
        {
            // Left: Slot list
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.4f)))
            {
                _slotsScroll = EditorGUILayout.BeginScrollView(_slotsScroll);
                _slotsList?.DoLayoutList();
                EditorGUILayout.EndScrollView();
            }

            GUILayout.Space(4f);

            // Right: Slot path + Widgets
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                DrawSlotPathBreadcrumb();

                _widgetsScroll = EditorGUILayout.BeginScrollView(_widgetsScroll);

                if (_widgetsList == null)
                {
                    EditorGUILayout.HelpBox(
                        "Select a Slot on the left, or enter a Slot Id in a Slot widget\n" +
                        "and press 'Open Child Slot' to drill down into nested slots.",
                        MessageType.None);
                }
                else
                {
                    _widgetsList.DoLayoutList();
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(4f);

                bool hasSlotSelected =
                    _slotsProp != null &&
                    _slotsProp.arraySize > 0 &&
                    _slotPath.Count > 0;

                bool hasAnySlot =
                    _slotsProp != null &&
                    _slotsProp.arraySize > 0;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    // Clean unlinked slots button
                    EditorGUI.BeginDisabledGroup(!hasAnySlot || _asset == null);
                    if (GUILayout.Button("Clean Unlinked Slots", GUILayout.Width(180f)))
                    {
                        CleanupUnlinkedSlots();
                    }
                    EditorGUI.EndDisabledGroup();

                    GUILayout.Space(4f);

                    // Enable all widgets button
                    EditorGUI.BeginDisabledGroup(!hasSlotSelected || _asset == null);
                    if (GUILayout.Button("Enable All Widgets", GUILayout.Width(180f)))
                    {
                        EnableAllDisabledWidgets(_asset.spec);
                        _so.Update();
                        EditorUtility.SetDirty(_asset);
                        BuildWidgetsListForCurrentSlot();
                        Repaint();
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }
        }

        _so.ApplyModifiedProperties();
    }

    private void DrawSlotPathBreadcrumb()
    {
        if (_slotsProp == null || _slotPath.Count == 0)
        {
            EditorGUILayout.LabelField("Slot Path: (none)");
            EditorGUILayout.Space(2f);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Slot Path:", GUILayout.Width(70f));

            for (int i = 0; i < _slotPath.Count; i++)
            {
                int slotIndex = _slotPath[i];
                string name   = $"Slot {slotIndex}";

                if (slotIndex >= 0 && slotIndex < _slotsProp.arraySize)
                {
                    var slotProp = _slotsProp.GetArrayElementAtIndex(slotIndex);
                    var nameProp = slotProp.FindPropertyRelative("slotName");
                    if (nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue))
                        name = nameProp.stringValue;
                }

                bool isLast = (i == _slotPath.Count - 1);

                if (GUILayout.Button(name, isLast ? EditorStyles.boldLabel : EditorStyles.miniButton))
                {
                    int keepCount = i + 1;
                    if (_slotPath.Count > keepCount)
                        _slotPath.RemoveRange(keepCount, _slotPath.Count - keepCount);

                    _selectedSlotIndex = _slotPath[_slotPath.Count - 1];
                    BuildWidgetsListForCurrentSlot();
                }

                if (!isLast)
                    GUILayout.Label(">", GUILayout.Width(12f));
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(_slotPath.Count <= 1))
            {
                if (GUILayout.Button("Back", GUILayout.Width(60f)))
                {
                    if (_slotPath.Count > 1)
                    {
                        _slotPath.RemoveAt(_slotPath.Count - 1);
                        _selectedSlotIndex = _slotPath[_slotPath.Count - 1];
                        BuildWidgetsListForCurrentSlot();
                    }
                }
            }
        }

        EditorGUILayout.Space(4f);
    }
    private bool IsSlotNameUsedByOtherSlots(int selfIndex, string name)
    {
        if (_slotsProp == null)
            return false;

        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
            return false;

        int slotCount = _slotsProp.arraySize;
        for (int i = 0; i < slotCount; i++)
        {
            if (i == selfIndex)
                continue;

            var slot     = _slotsProp.GetArrayElementAtIndex(i);
            var nameProp = slot.FindPropertyRelative("slotName");
            string other = (nameProp != null ? nameProp.stringValue : string.Empty)?.Trim();

            if (string.IsNullOrEmpty(other))
                continue;

            if (string.Equals(other, name, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private string GetSlotDisplayPath(int slotIndex, out int depth)
    {
        depth = 0;

        if (_slotsProp == null || slotIndex < 0 || slotIndex >= _slotsProp.arraySize)
            return "(invalid)";

        // Walk child -> parent -> grandparent ...
        var chain   = new List<int>();
        var visited = new HashSet<int>();

        int current = slotIndex;
        while (current >= 0 && current < _slotsProp.arraySize && !visited.Contains(current))
        {
            visited.Add(current);
            chain.Add(current);

            int parent = FindParentSlotIndex(_slotsProp, current);
            if (parent < 0)
                break;

            current = parent;
        }

        // Reverse to get root -> ... -> child
        chain.Reverse();
        depth = chain.Count - 1;

        var names = new List<string>();
        foreach (int idx in chain)
        {
            var slot     = _slotsProp.GetArrayElementAtIndex(idx);
            var nameProp = slot.FindPropertyRelative("slotName");
            string rawName = nameProp != null ? nameProp.stringValue : string.Empty;

            string label;

            if (idx == 0)
            {
                // Root slot: use the user-entered root Slot Id directly.
                // If empty, show "(root)".
                label = string.IsNullOrWhiteSpace(rawName)
                    ? "(root)"
                    : rawName.Trim();
            }
            else
            {
                // Non-root slots keep the normalized label behavior.
                label = NormalizeSlotLabel(rawName);
            }

            names.Add(label);
        }

        return string.Join(" > ", names);
    }

    private static int FindParentSlotIndex(SerializedProperty slotsProp, int childIndex)
    {
        if (slotsProp == null || childIndex < 0 || childIndex >= slotsProp.arraySize)
            return -1;

        var childSlot     = slotsProp.GetArrayElementAtIndex(childIndex);
        var childNameProp = childSlot.FindPropertyRelative("slotName");
        string childName  = (childNameProp != null ? childNameProp.stringValue : string.Empty)?.Trim();
        if (string.IsNullOrEmpty(childName))
            return -1;

        // Find any Slot widget whose slotId matches this slot's name → that slot is its parent.
        for (int i = 0; i < slotsProp.arraySize; i++)
        {
            if (i == childIndex) continue;

            var slot        = slotsProp.GetArrayElementAtIndex(i);
            var widgetsProp = slot.FindPropertyRelative("widgets");
            if (widgetsProp == null) continue;

            for (int w = 0; w < widgetsProp.arraySize; w++)
            {
                var widget     = widgetsProp.GetArrayElementAtIndex(w);
                var typeProp   = widget.FindPropertyRelative("widgetType");
                var slotIdProp = widget.FindPropertyRelative("slotId");

                if (typeProp == null || slotIdProp == null)
                    continue;

                var widgetType = (WidgetType)typeProp.enumValueIndex;
                if (widgetType != WidgetType.Slot)
                    continue;

                string id = (slotIdProp.stringValue ?? string.Empty).Trim();
                if (id == childName)
                    return i;
            }
        }

        return -1;
    }

    private static string NormalizeSlotLabel(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return "(unnamed)";

        string trimmed = rawName.Trim();

        // Hide auto-generated names like "Slot 0", "Slot 1" etc. behind "(unnamed)".
        if (trimmed.StartsWith("Slot "))
        {
            bool allDigits = true;
            for (int i = 5; i < trimmed.Length; i++)
            {
                if (!char.IsDigit(trimmed[i]))
                {
                    allDigits = false;
                    break;
                }
            }

            if (allDigits)
                return "(unnamed)";
        }

        return trimmed;
    }

    private bool WouldCreateCycleFromCurrentPath(string targetSlotName)
    {
        if (_slotsProp == null) return false;

        targetSlotName = (targetSlotName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(targetSlotName))
            return false;

        if (_slotPath == null || _slotPath.Count == 0)
            return false;

        // Check all slot names in the current breadcrumb path
        foreach (int slotIndex in _slotPath)
        {
            if (slotIndex < 0 || slotIndex >= _slotsProp.arraySize)
                continue;

            var slotProp = _slotsProp.GetArrayElementAtIndex(slotIndex);
            var nameProp = slotProp.FindPropertyRelative("slotName");
            string name  = (nameProp != null ? nameProp.stringValue : string.Empty)?.Trim();

            if (string.IsNullOrEmpty(name))
                continue;

            if (string.Equals(name, targetSlotName, StringComparison.Ordinal))
                return true; // linking back to an ancestor → potential cycle
        }

        return false;
    }

    private bool HasOtherParentForSlotName(string targetSlotName, int currentParentIndex, out int otherParentIndex)
    {
        otherParentIndex = -1;

        if (_slotsProp == null)
            return false;

        targetSlotName = (targetSlotName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(targetSlotName))
            return false;

        for (int i = 0; i < _slotsProp.arraySize; i++)
        {
            // Skip the parent we're currently editing from
            if (i == currentParentIndex)
                continue;

            var slot        = _slotsProp.GetArrayElementAtIndex(i);
            var widgetsProp = slot.FindPropertyRelative("widgets");
            if (widgetsProp == null)
                continue;

            for (int w = 0; w < widgetsProp.arraySize; w++)
            {
                var widget     = widgetsProp.GetArrayElementAtIndex(w);
                var typeProp   = widget.FindPropertyRelative("widgetType");
                var slotIdProp = widget.FindPropertyRelative("slotId");

                if (typeProp == null || slotIdProp == null)
                    continue;

                var widgetType = (WidgetType)typeProp.enumValueIndex;
                if (widgetType != WidgetType.Slot)
                    continue;

                string id = (slotIdProp.stringValue ?? string.Empty).Trim();
                if (string.Equals(id, targetSlotName, StringComparison.Ordinal))
                {
                    otherParentIndex = i;
                    return true;
                }
            }
        }

        return false;
    }

    private string GetSlotNameByIndex(int index)
    {
        if (_slotsProp == null || index < 0 || index >= _slotsProp.arraySize)
            return $"Slot {index}";

        var slot     = _slotsProp.GetArrayElementAtIndex(index);
        var nameProp = slot.FindPropertyRelative("slotName");
        string name  = nameProp != null ? nameProp.stringValue : null;

        if (string.IsNullOrWhiteSpace(name))
            return $"Slot {index}";

        return name.Trim();
    }

    // ─────────────────────────────────────────────
    // Utility
    // ─────────────────────────────────────────────
    private void ApplyPresetToWidget(WidgetPreset preset, SerializedProperty widgetProp)
    {
        if (widgetProp == null) return;

        var rectModeProp      = widgetProp.FindPropertyRelative("rectMode");
        var anchorMinProp     = widgetProp.FindPropertyRelative("anchorMin");
        var anchorMaxProp     = widgetProp.FindPropertyRelative("anchorMax");
        var pivotProp         = widgetProp.FindPropertyRelative("pivot");
        var anchoredPosProp   = widgetProp.FindPropertyRelative("anchoredPosition");
        var sizeDeltaProp     = widgetProp.FindPropertyRelative("sizeDelta");

        rectModeProp.enumValueIndex = (int)preset.rectMode;
        anchorMinProp.vector2Value  = preset.anchorMin;
        anchorMaxProp.vector2Value  = preset.anchorMax;
        pivotProp.vector2Value      = preset.pivot;
        anchoredPosProp.vector2Value = preset.anchoredPosition;
        sizeDeltaProp.vector2Value  = preset.sizeDelta;
    }

    private void ResetWidgetSpecDefaults(SerializedProperty widgetProp, int index)
    {
        if (widgetProp == null) return;

        var typeProp           = widgetProp.FindPropertyRelative("widgetType");
        var nameTagProp        = widgetProp.FindPropertyRelative("nameTag");
        var textProp           = widgetProp.FindPropertyRelative("text");
        var routeProp          = widgetProp.FindPropertyRelative("onClickRoute");
        var prefabOverrideProp = widgetProp.FindPropertyRelative("prefabOverride");

        var rectModeProp    = widgetProp.FindPropertyRelative("rectMode");
        var anchorMinProp   = widgetProp.FindPropertyRelative("anchorMin");
        var anchorMaxProp   = widgetProp.FindPropertyRelative("anchorMax");
        var pivotProp       = widgetProp.FindPropertyRelative("pivot");
        var anchoredPosProp = widgetProp.FindPropertyRelative("anchoredPosition");
        var sizeDeltaProp   = widgetProp.FindPropertyRelative("sizeDelta");

        var disabledProp = widgetProp.FindPropertyRelative("disabled");

        typeProp.enumValueIndex  = (int)WidgetType.Text;
        nameTagProp.stringValue  = $"Widget {index}";
        textProp.stringValue     = string.Empty;
        routeProp.stringValue    = string.Empty;
        prefabOverrideProp.objectReferenceValue = null;

        rectModeProp.enumValueIndex = (int)WidgetRectMode.UseSlotLayout;

        anchorMinProp.vector2Value   = new Vector2(0.5f, 0.5f);
        anchorMaxProp.vector2Value   = new Vector2(0.5f, 0.5f);
        pivotProp.vector2Value       = new Vector2(0.5f, 0.5f);
        anchoredPosProp.vector2Value = Vector2.zero;
        sizeDeltaProp.vector2Value   = new Vector2(300f, 80f);

        if (disabledProp != null)
            disabledProp.boolValue = false;

        var imageColorProp    = widgetProp.FindPropertyRelative("imageColor");
        var imageNativeProp   = widgetProp.FindPropertyRelative("imageSetNativeSize");
        var toggleInitialProp = widgetProp.FindPropertyRelative("toggleInitialValue");
        var toggleInteractProp = widgetProp.FindPropertyRelative("toggleInteractable");
        var sliderMinProp     = widgetProp.FindPropertyRelative("sliderMin");
        var sliderMaxProp     = widgetProp.FindPropertyRelative("sliderMax");
        var sliderInitProp    = widgetProp.FindPropertyRelative("sliderInitialValue");
        var sliderWholeProp   = widgetProp.FindPropertyRelative("sliderWholeNumbers");

        var imageSpriteProp = widgetProp.FindPropertyRelative("imageSprite");
        if (imageSpriteProp != null) imageSpriteProp.objectReferenceValue = null;
        if (imageColorProp  != null) imageColorProp.colorValue           = Color.white;
        if (imageNativeProp != null) imageNativeProp.boolValue           = false;

        if (toggleInitialProp != null) toggleInitialProp.boolValue   = false;
        if (toggleInteractProp != null) toggleInteractProp.boolValue = true;

        if (sliderMinProp   != null) sliderMinProp.floatValue   = 0f;
        if (sliderMaxProp   != null) sliderMaxProp.floatValue   = 1f;
        if (sliderInitProp  != null) sliderInitProp.floatValue  = 0.5f;
        if (sliderWholeProp != null) sliderWholeProp.boolValue  = false;
    }

    private static void EnableAllDisabledWidgets(UIScreenSpec s)
    {
        if (s == null || s.slots == null)
            return;

        foreach (var slot in s.slots)
        {
            if (slot == null || slot.widgets == null)
                continue;

            foreach (var w in slot.widgets)
            {
                if (w == null) continue;
                if (w.disabled)
                    w.disabled = false;
            }
        }
    }

    private bool[] BuildHasParentFlags()
    {
        if (_slotsProp == null)
            return Array.Empty<bool>();

        int  slotCount = _slotsProp.arraySize;
        var  hasParent = new bool[slotCount];

        // Fill parent information via Slot widgets (slotId → slotName).
        for (int i = 0; i < slotCount; i++)
        {
            var slot        = _slotsProp.GetArrayElementAtIndex(i);
            var widgetsProp = slot.FindPropertyRelative("widgets");
            if (widgetsProp == null) continue;

            for (int w = 0; w < widgetsProp.arraySize; w++)
            {
                var widget     = widgetsProp.GetArrayElementAtIndex(w);
                var typeProp   = widget.FindPropertyRelative("widgetType");
                var slotIdProp = widget.FindPropertyRelative("slotId");

                if (typeProp == null || slotIdProp == null) continue;

                var widgetType = (WidgetType)typeProp.enumValueIndex;
                if (widgetType != WidgetType.Slot) continue;

                string id = (slotIdProp.stringValue ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(id)) continue;

                // If any slotName matches this id, that slot has a parent.
                for (int j = 0; j < slotCount; j++)
                {
                    var childSlot     = _slotsProp.GetArrayElementAtIndex(j);
                    var childNameProp = childSlot.FindPropertyRelative("slotName");
                    string childName  = (childNameProp != null ? childNameProp.stringValue : string.Empty).Trim();
                    if (string.IsNullOrEmpty(childName)) continue;

                    if (string.Equals(childName, id, StringComparison.Ordinal))
                    {
                        hasParent[j] = true;
                    }
                }
            }
        }

        return hasParent;
    }

    private void CleanupUnlinkedSlots()
    {
        if (_slotsProp == null || _slotsProp.arraySize == 0)
        {
            EditorUtility.DisplayDialog(
                "Clean Unlinked Slots",
                "There are no slots to clean.",
                "OK");
            return;
        }

        int  slotCount = _slotsProp.arraySize;
        var  hasParent = BuildHasParentFlags();
        var  toDelete  = new List<int>();

        // Slot index 0 is always the canonical root and is never removed.
        for (int i = 1; i < slotCount; i++)
        {
            // Slots that are not referenced by any parent are considered "unlinked".
            if (!hasParent[i])
                toDelete.Add(i);
        }

        if (toDelete.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Clean Unlinked Slots",
                "No unlinked slots were found.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Clean Unlinked Slots",
                $"This will remove {toDelete.Count} unlinked slots.\n\n" +
                "These slots are not referenced by any parent slot.\n" +
                "This action cannot be undone. Continue?",
                "Delete",
                "Cancel"))
        {
            return;
        }

        // Delete from the end to avoid index shift issues
        toDelete.Sort();
        for (int idx = toDelete.Count - 1; idx >= 0; idx--)
        {
            int slotIndex = toDelete[idx];
            _slotsProp.DeleteArrayElementAtIndex(slotIndex);
        }

        _so.ApplyModifiedProperties();

        // Root is always slot index 0 after cleanup (or re-created if needed).
        _slotPath.Clear();

        if (_slotsProp.arraySize > 0)
        {
            _selectedSlotIndex = 0;
            SetRootSlot(0);
        }
        else
        {
            _selectedSlotIndex = -1;
            _widgetsList       = null;

            // Very defensive fallback: recreate a root slot if nothing remains.
            EnsureRootSlotExists();
            _selectedSlotIndex = 0;
            SetRootSlot(0);
        }

        BuildSlotsList();
        Repaint();
    }
}
#endif