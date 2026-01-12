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

    // ─────────────────────────────────────────────
// Slot graph caches (performance)
// ─────────────────────────────────────────────
    private bool _slotGraphDirty = true;
    private bool[] _hasParentCache;
    private int[] _parentIndexCache; // child -> parent slot index (first found), -1 if none
    private int[] _depthCache;
    private string[] _pathLabelCache;

// ─────────────────────────────────────────────
// Preset labels cache (avoid per-widget allocations)
// ─────────────────────────────────────────────
    private string[] _presetLabelsCache;
    private int _presetLabelsCountCache = -1;
    private bool _presetLabelsHasCatalogCache = false;
    
    
    const float CenterMinWidth = 3000f;
    const float CenterMaxWidth = 380f;
    const float RightMinWidth  = 120f;
    const float RightMaxWidth  = 130f;


    private readonly Dictionary<string, bool> _widgetSectionFoldoutStates = new();

    private const string WidgetClipboardPrefix = "CPS_WIDGETSPEC_CLIP:";

    // ✅ 오른쪽 "All Widgets" 패널용 스크롤
    private Vector2 _allWidgetsScroll;

    private void MarkSlotGraphDirty() => _slotGraphDirty = true;

    [MenuItem("Tools/UI/UIScreen Spec Editor")]
    public static void Open()
    {
        var w = GetWindow<UIScreenSpecEditorWindow>();
        w.titleContent = new GUIContent("UIScreen Spec Editor");
        w.Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(250, 40);
        Selection.selectionChanged += TryAutoBindFromSelection;
        Undo.undoRedoPerformed += OnUndoRedo;
        TryAutoBindFromSelection();

        _slotsScroll = Vector2.zero;
        _widgetsScroll = Vector2.zero;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= TryAutoBindFromSelection;
        Undo.undoRedoPerformed -= OnUndoRedo;
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

            var nameProp = root.FindPropertyRelative("slotName");
            var widgetsProp = root.FindPropertyRelative("widgets");

            // Initial root slot id can be customized later to match the UISlot.id on the template.
            if (nameProp != null)
                nameProp.stringValue = "Root";

            if (widgetsProp != null)
                widgetsProp.ClearArray();

            _so.ApplyModifiedProperties();
            MarkSlotGraphDirty();
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

            var nameProp = slot.FindPropertyRelative("slotName");
            var widgetsProp = slot.FindPropertyRelative("widgets");

            if (nameProp != null)
                nameProp.stringValue = $"Slot {i}";

            if (widgetsProp != null)
                widgetsProp.ClearArray();

            _so.ApplyModifiedProperties();
            MarkSlotGraphDirty();

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
            MarkSlotGraphDirty();

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
            float vGap = 2f;

            int lines = (index == 0) ? 2 : 1;

            return lines * (lineH + vGap) + 4f;
        };

        _slotsList.drawElementCallback = DrawSlotElement;
    }

    private void DrawSlotElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        float lineH = EditorGUIUtility.singleLineHeight;
        float vGap = 2f;

        rect.y += 2f;

        const float horizontalPadding = 4f;
        rect.x += horizontalPadding;
        rect.width -= horizontalPadding * 2f;
        rect.height = lineH;

        var slot = _slotsProp.GetArrayElementAtIndex(index);
        var nameProp = slot.FindPropertyRelative("slotName");
        var widgetsProp = slot.FindPropertyRelative("widgets");
        int widgetCount = widgetsProp != null ? widgetsProp.arraySize : 0;

        // Use cached path/depth/parent flags
        int depth = 0;
        if (_depthCache != null && index >= 0 && index < _depthCache.Length)
            depth = _depthCache[index];

        string pathLabel = "(...)";
        if (_pathLabelCache != null && index >= 0 && index < _pathLabelCache.Length)
            pathLabel = _pathLabelCache[index];

        bool isUnlinked = false;
        if (index > 0 && _hasParentCache != null && index < _hasParentCache.Length)
            isUnlinked = !_hasParentCache[index];

        string labelText;
        string countLabel = widgetCount == 1 ? "1 widget" : $"{widgetCount} widgets";

        if (index == 0)
            labelText = $"[Root] {pathLabel} ({countLabel})";
        else if (isUnlinked)
            labelText = $"(!) [unlinked] {pathLabel} ({countLabel})";
        else
            labelText = $"[depth{depth}] {pathLabel} ({countLabel})";

        // ──────────────────────
        // First line: label + (↑↓ controls for non-root slots)
        // ──────────────────────
        const float btnWidth = 18f;
        const float btnGap = 2f;

        float labelWidth = rect.width;
        if (index > 0)
            labelWidth -= (btnWidth * 2f + btnGap * 2f);

        var labelRect = new Rect(rect.x, rect.y, labelWidth, lineH);
        EditorGUI.LabelField(labelRect, labelText);

        if (index > 0)
        {
            var upRect = new Rect(labelRect.xMax + btnGap, rect.y, btnWidth, lineH);
            var downRect = new Rect(upRect.xMax + btnGap, rect.y, btnWidth, lineH);

            using (new EditorGUI.DisabledScope(index <= 1))
            {
                if (GUI.Button(upRect, "↑"))
                    MoveSlot(index, index - 1);
            }

            using (new EditorGUI.DisabledScope(index >= _slotsProp.arraySize - 1))
            {
                if (GUI.Button(downRect, "↓"))
                    MoveSlot(index, index + 1);
            }
        }

        // ──────────────────────
        // Second line: Root Slot Id field (root only)
        // ──────────────────────
        if (index == 0)
        {
            var idRect = new Rect(rect.x, rect.y + lineH + vGap, rect.width, lineH);

            string currentName = nameProp != null ? nameProp.stringValue : string.Empty;

            // Root면 라벨만 "Root Slot Id"로 다르게
            string label = (index == 0) ? "Root Slot Id" : "Slot Id";

            EditorGUI.BeginChangeCheck();
            string newName = EditorGUI.TextField(idRect, label, currentName);
            if (EditorGUI.EndChangeCheck() && nameProp != null)
            {
                newName = (newName ?? string.Empty).Trim();

                if (string.IsNullOrEmpty(newName))
                {
                    EditorUtility.DisplayDialog(
                        "Invalid Slot Id",
                        "Slot Id cannot be empty.\nPlease enter a unique id.",
                        "OK"
                    );
                }
                else if (IsSlotNameUsedByOtherSlots(index, newName))
                {
                    EditorUtility.DisplayDialog(
                        "Duplicate Slot Id",
                        $"The id '{newName}' is already used by another slot.\n\n" +
                        "Slot Id must be unique.\n" +
                        "Please choose a different id or rename the other slot first.",
                        "OK"
                    );
                }
                else
                {
                    nameProp.stringValue = newName;
                    _so.ApplyModifiedProperties();
                    MarkSlotGraphDirty(); // 이름이 바뀌면 그래프/경로 캐시 모두 영향
                }
            }
        }
    }


    private void MoveSlot(int from, int to)
    {
        if (_slotsProp == null) return;

        int size = _slotsProp.arraySize;
        if (from < 0 || from >= size) return;
        if (to < 0 || to >= size) return;

        // Slot 0 is the canonical root and cannot be moved into.
        if (to == 0) return;

        _slotsProp.MoveArrayElement(from, to);
        _so.ApplyModifiedProperties();
        MarkSlotGraphDirty();

        // Update selection and rebuild path
        _slotsList.index = to;
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
            var slot = _slotsProp.GetArrayElementAtIndex(i);
            var nameProp = slot.FindPropertyRelative("slotName");
            string name = (nameProp != null ? nameProp.stringValue : string.Empty)?.Trim();
            if (!string.IsNullOrEmpty(name) && !nameToIndex.ContainsKey(name))
                nameToIndex.Add(name, i);
        }

        // 2) Build parent -> children graph using Slot widgets (slotId)
        var children = new List<int>[slotCount];
        var hasParent = new bool[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            children[i] = new List<int>();

            var slot = _slotsProp.GetArrayElementAtIndex(i);
            var widgetsProp = slot.FindPropertyRelative("widgets");
            if (widgetsProp == null) continue;

            for (int wi = 0; wi < widgetsProp.arraySize; wi++)
            {
                var widget = widgetsProp.GetArrayElementAtIndex(wi);
                var typeProp = widget.FindPropertyRelative("widgetType");
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
        var path = new List<int>();
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

        var slot = _slotsProp.GetArrayElementAtIndex(slotIndex);
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

            Color normalBg = new Color(0.3f, 0.3f, 0.3f, 0.5f);
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
            MarkSlotGraphDirty();
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
            MarkSlotGraphDirty();
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


    #region DrawVec

    // ─────────────────────────────────────────────
    // Per-widget element height
    // ─────────────────────────────────────────────
    private float CalcWidgetElementHeight(SerializedProperty widgetsProp, int index)
    {
        float lineH = EditorGUIUtility.singleLineHeight;
        float vGap = 2f;
        const float borderPadding = 2f;
        const float innerTopGap = 2f; // DrawWidgetElement에서 rect.y += vGap 했던 것과 유사
        const float extra = 4f; // 기존 리턴에 붙이던 여유

        if (widgetsProp == null || index < 0 || index >= widgetsProp.arraySize)
            return lineH + borderPadding * 2f + extra;

        var w = widgetsProp.GetArrayElementAtIndex(index);
        if (w == null)
            return lineH + borderPadding * 2f + extra;

        string foldKey = w.propertyPath;
        bool expanded = _widgetFoldoutStates.TryGetValue(foldKey, out var v) ? v : true;

        float h = 0f;

        // borderRect 내부에서 시작할 컨텐츠 높이 누적
        h += innerTopGap; // 첫 y 보정

        // Header 1줄: Foldout + Enabled + Name + Type
        h += lineH + vGap;

        if (!expanded)
            return h + borderPadding * 2f + extra;

        // Preset 1줄
        h += lineH + vGap;


        // Layout Mode 1줄
        h += lineH + vGap;

        var rectModeProp = w.FindPropertyRelative("rectMode");
        var rectMode = rectModeProp != null
            ? (WidgetRectMode)rectModeProp.enumValueIndex
            : WidgetRectMode.OverrideInSlot;

        if (rectMode == WidgetRectMode.OverrideInSlot)
        {
            // Position / Size / AnchorMin / AnchorMax / Pivot : 5줄
            h += 5f * (lineH + vGap);
        }

        var typeProp = w.FindPropertyRelative("widgetType");
        var widgetType = typeProp != null ? (WidgetType)typeProp.enumValueIndex : WidgetType.Text;

        if (WidgetTypeSupportsTextRole(widgetType))
        {
            h += lineH + vGap;
        }

        // ---- Type Options foldout header (Text도 foldout 쓰는 구조라면 포함) ----
        bool hasOptionsSection =
            widgetType == WidgetType.Text ||
            widgetType == WidgetType.Button ||
            widgetType == WidgetType.Image ||
            widgetType == WidgetType.Toggle ||
            widgetType == WidgetType.Slider ||
            widgetType == WidgetType.Slot ||
            widgetType == WidgetType.GameObject; // enum에 있다면

        if (hasOptionsSection)
        {
            h += lineH + vGap; // Options foldout header

            // options open?
            string optionsKey = $"{w.propertyPath}/{widgetType}/" + widgetType switch
            {
                WidgetType.Text => "TextOptions",
                WidgetType.Button => "ButtonOptions",
                WidgetType.Image => "ImageOptions",
                WidgetType.Toggle => "ToggleOptions",
                WidgetType.Slider => "SliderOptions",
                WidgetType.Slot => "SlotOptions",
                WidgetType.GameObject => "GameObjectOptions",
                _ => "Options"
            };

            bool defaultOpen = (widgetType == WidgetType.Slot); // Slot만 기본 펼침
            bool optionsOpen = GetSectionFoldout(optionsKey, defaultOpen);

            if (optionsOpen)
            {
                // 각 타입별 옵션 라인 수 + (TextArea가 있으면 “실제 높이”로 더함)
                float InlineTextAreaHeight(int lines)
                {
                    float textHeight = (lineH + 2f) * lines;
                    // 라벨 1줄 + vGap + 텍스트영역 + vGap
                    return (lineH + vGap) + (textHeight + vGap);
                }

                switch (widgetType)
                {
                    case WidgetType.Text:
                        h += InlineTextAreaHeight(2);
                        break;

                    case WidgetType.Button:
                        h += (lineH + vGap); // OnClick Route
                        h += InlineTextAreaHeight(2);
                        break;

                    case WidgetType.Image:
                        h += 3f * (lineH + vGap); // Sprite/Color/Native
                        h += InlineTextAreaHeight(2);
                        break;

                    case WidgetType.Toggle:
                        h += 2f * (lineH + vGap); // Initial/Interactable
                        h += InlineTextAreaHeight(2);
                        break;

                    case WidgetType.Slider:
                        h += 4f * (lineH + vGap); // Min/Max/Init/Whole
                        break;

                    case WidgetType.Slot:
                        h += (lineH + vGap); // Slot Id row
                        break;

                    case WidgetType.GameObject:
                        // 옵션이 있다면 여기에 줄 추가
                        break;
                }
            }
        }

        // ---- Prefab Override foldout ----
        string prefabKey = $"{w.propertyPath}/{widgetType}/PrefabOverride";
        bool prefabOpen = GetSectionFoldout(prefabKey, defaultValue: false);

        h += lineH + vGap; // Prefab Override header
        if (prefabOpen)
            h += lineH + vGap; // prefab field

        return h + borderPadding * 2f + extra;
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
        rect.y += vGap;
        rect.x += horizontalPadding;
        rect.width -= horizontalPadding * 2f;

        float lineH = EditorGUIUtility.singleLineHeight;
        float y = rect.y;

        var w = widgetsProp.GetArrayElementAtIndex(index);
        var typeProp = w.FindPropertyRelative("widgetType");
        var nameProp = w.FindPropertyRelative("nameTag");
        var textProp = w.FindPropertyRelative("text");
        var routeProp = w.FindPropertyRelative("onClickRoute");
        var prefabProp = w.FindPropertyRelative("prefabOverride");
        var rectModeProp = w.FindPropertyRelative("rectMode");
        var textRoleProp = w.FindPropertyRelative("textRole");
        var anchorMinProp = w.FindPropertyRelative("anchorMin");
        var anchorMaxProp = w.FindPropertyRelative("anchorMax");
        var pivotProp = w.FindPropertyRelative("pivot");
        var anchoredPosProp = w.FindPropertyRelative("anchoredPosition");
        var sizeDeltaProp = w.FindPropertyRelative("sizeDelta");

        var imageSpriteProp = w.FindPropertyRelative("imageSprite");
        var imageColorProp = w.FindPropertyRelative("imageColor");
        var imageNativeProp = w.FindPropertyRelative("imageSetNativeSize");

        var toggleInitialProp = w.FindPropertyRelative("toggleInitialValue");
        var toggleInteractProp = w.FindPropertyRelative("toggleInteractable");

        var sliderMinProp = w.FindPropertyRelative("sliderMin");
        var sliderMaxProp = w.FindPropertyRelative("sliderMax");
        var sliderInitProp = w.FindPropertyRelative("sliderInitialValue");
        var sliderWholeProp = w.FindPropertyRelative("sliderWholeNumbers");
        var disabledProp = w.FindPropertyRelative("disabled");

        var slotIdProp = w.FindPropertyRelative("slotId");

        // Context menu (Add / Delete widget)
        if (e.type == EventType.ContextClick && borderRect.Contains(e.mousePosition))
        {
            var menu = new GenericMenu();
            int capturedIndex = index;

            // ─────────────
            // 위젯 추가 / 복사 계열 (같은 그룹)
            // ─────────────
            menu.AddItem(new GUIContent("Add Widget Below"), false, () =>
            {
                if (widgetsProp == null) return;

                int insertIndex = Mathf.Clamp(capturedIndex + 1, 0, widgetsProp.arraySize);
                widgetsProp.InsertArrayElementAtIndex(insertIndex);

                var newElem = widgetsProp.GetArrayElementAtIndex(insertIndex);
                ResetWidgetSpecDefaults(newElem, insertIndex);

                _so.ApplyModifiedProperties();
                MarkSlotGraphDirty();
                BuildWidgetsListForCurrentSlot();
                if (_widgetsList != null)
                    _widgetsList.index = insertIndex;
                Repaint();
            });

            // Copy / Paste / Duplicate
            menu.AddItem(new GUIContent("Copy Widget"), false, () =>
            {
                if (_widgetsList != null) _widgetsList.index = capturedIndex;
                CopySelectedWidget();
            });

            bool canPaste = TryReadClipboardPayload(out _);
            if (canPaste)
            {
                menu.AddItem(new GUIContent("Paste Widget Below"), false, () =>
                {
                    if (_widgetsList != null) _widgetsList.index = capturedIndex;
                    PasteWidgetBelowSelected();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste Widget Below"), true);
            }

            menu.AddItem(new GUIContent("Duplicate Widget"), false, () =>
            {
                if (_widgetsList != null) _widgetsList.index = capturedIndex;
                DuplicateSelectedWidget();
            });

            // ─────────────
            // 여기서 Delete 앞에 선 하나
            // ─────────────
            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("Delete Widget"), false, () =>
            {
                if (widgetsProp == null) return;
                if (capturedIndex < 0 || capturedIndex >= widgetsProp.arraySize) return;

                widgetsProp.DeleteArrayElementAtIndex(capturedIndex);
                _so.ApplyModifiedProperties();
                MarkSlotGraphDirty();
                BuildWidgetsListForCurrentSlot();
                Repaint();
            });

            menu.ShowAsContext();
            e.Use();
        }

        // Header: Foldout + Enabled toggle + Name + Type
        string foldKey = w.propertyPath;
        if (!_widgetFoldoutStates.TryGetValue(foldKey, out bool expanded))
            expanded = true;

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
        const float gap = 4f;

        float typeX = rect.x + rect.width - typeWidth;
        var typeRect = new Rect(typeX, y, typeWidth, lineH);

        float nameWidth = typeX - x - gap;
        if (nameWidth < 60f) nameWidth = 60f;
        var nameFieldRect = new Rect(x, y, nameWidth, lineH);

        nameProp.stringValue = EditorGUI.TextField(nameFieldRect, nameProp.stringValue);

// ✅ 여기: 타입 변경이 Slot ↔ 다른 타입이면 slot graph에 영향
        var oldType = (WidgetType)typeProp.enumValueIndex;
        EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);
        var newType = (WidgetType)typeProp.enumValueIndex;

        if (oldType != newType && (oldType == WidgetType.Slot || newType == WidgetType.Slot))
            MarkSlotGraphDirty();

        y += lineH + vGap;

        if (!expanded)
            return;

        var widgetType = (WidgetType)typeProp.enumValueIndex;

        // Preset selection
        {
            RefreshPresetLabelsIfNeeded();

            bool hasPresetCatalog =
                _presetCatalog != null &&
                _presetCatalog.presets != null &&
                _presetCatalog.presets.Count > 0;

            var labels = _presetLabelsCache ?? new[] { "(No presets configured)" };

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
                    var chosen = _presetCatalog.presets[newIndex - 1];
                    ApplyPresetToWidget(chosen, w);
                    _so.ApplyModifiedProperties();
                    // preset changes don't affect slot graph (unless preset touches slotId; currently it doesn't)
                }
            }

            y += lineH + vGap;
        }
        if (WidgetTypeSupportsTextRole(widgetType) && textRoleProp != null)
        {
            var roleRect = new Rect(rect.x, y, rect.width, lineH);
            EditorGUI.PropertyField(roleRect, textRoleProp, new GUIContent("Text Role"));
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
            float fieldGap = 4f;
            float rowHeight = lineH;

            Rect MakeRowRect() => new Rect(rect.x, y, rect.width, rowHeight);

            void DrawVec2Row(string label, SerializedProperty prop)
            {
                var rowRect = MakeRowRect();
                var labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowHeight);
                var valueRect = new Rect(
                    rowRect.x + labelWidth + fieldGap,
                    rowRect.y,
                    rowRect.width - labelWidth - fieldGap,
                    rowHeight
                );

                EditorGUI.LabelField(labelRect, label);

                Vector2 v = prop.vector2Value;
                v = EditorGUI.Vector2Field(valueRect, GUIContent.none, v);
                prop.vector2Value = v;

                y += rowHeight + vGap;
            }

            // ✅ 원하는 순서로 배치
            DrawVec2Row("Position", anchoredPosProp);
            DrawVec2Row("Size", sizeDeltaProp);
            DrawVec2Row("Anchor Min", anchorMinProp);
            DrawVec2Row("Anchor Max", anchorMaxProp);
            DrawVec2Row("Pivot", pivotProp);


            // Type-specific options
            switch (widgetType)
            {
                case WidgetType.Text:
                {
                    string sectionKey = $"{w.propertyPath}/{widgetType}/TextOptions";
                    bool open = GetSectionFoldout(sectionKey, defaultValue: false); // 기본 접힘

                    var headerRect = new Rect(rect.x, y, rect.width, lineH);
                    open = EditorGUI.Foldout(headerRect, open, "[Text Options]", true);
                    SetSectionFoldout(sectionKey, open);
                    y += lineH + vGap;

                    if (open)
                    {
                        // ✅ Text 위젯: 옵션 안에서 TextArea 표시
                        DrawInlineTextArea(rect, ref y, lineH, vGap, textProp, lines: 2, label: "Text");
                    }

                    break;
                }

                case WidgetType.Button:
                {
                    string sectionKey = $"{w.propertyPath}/{widgetType}/ButtonOptions";
                    bool open = GetSectionFoldout(sectionKey, defaultValue: false); // 기본 접힘

                    var headerRect = new Rect(rect.x, y, rect.width, lineH);
                    open = EditorGUI.Foldout(headerRect, open, "[Button Options]", true);
                    SetSectionFoldout(sectionKey, open);
                    y += lineH + vGap;

                    if (open)
                    {
                        var routeRect = new Rect(rect.x, y, rect.width, lineH);
                        routeProp.stringValue = EditorGUI.TextField(routeRect, "OnClick Route", routeProp.stringValue);
                        y += lineH + vGap;

                        // ✅ Button 위젯: 옵션 안에서 TextArea 표시
                        DrawInlineTextArea(rect, ref y, lineH, vGap, textProp, lines: 2, label: "Text");
                    }

                    break;
                }

                case WidgetType.Image:
                {
                    string sectionKey = $"{w.propertyPath}/{widgetType}/ImageOptions";
                    bool open = GetSectionFoldout(sectionKey, defaultValue: false); // 기본 접힘

                    var headerRect = new Rect(rect.x, y, rect.width, lineH);
                    open = EditorGUI.Foldout(headerRect, open, "[Image Options]", true);
                    SetSectionFoldout(sectionKey, open);
                    y += lineH + vGap;

                    if (open)
                    {
                        var spriteRect = new Rect(rect.x, y, rect.width, lineH);
                        EditorGUI.PropertyField(spriteRect, imageSpriteProp, new GUIContent("Sprite"));
                        y += lineH + vGap;

                        var colorRect = new Rect(rect.x, y, rect.width, lineH);
                        EditorGUI.PropertyField(colorRect, imageColorProp, new GUIContent("Color"));
                        y += lineH + vGap;

                        var nativeRect = new Rect(rect.x, y, rect.width, lineH);
                        EditorGUI.PropertyField(nativeRect, imageNativeProp, new GUIContent("Set Native Size"));
                        y += lineH + vGap;

                        // ✅ Image 위젯: 옵션 안에서 TextArea 표시
                        DrawInlineTextArea(rect, ref y, lineH, vGap, textProp, lines: 2, label: "Text");
                    }

                    break;
                }

                case WidgetType.Toggle:
                {
                    string sectionKey = $"{w.propertyPath}/{widgetType}/ToggleOptions";
                    bool open = GetSectionFoldout(sectionKey, defaultValue: false); // 기본 접힘

                    var headerRect = new Rect(rect.x, y, rect.width, lineH);
                    open = EditorGUI.Foldout(headerRect, open, "[Toggle Options]", true);
                    SetSectionFoldout(sectionKey, open);
                    y += lineH + vGap;

                    if (open)
                    {
                        var initRect = new Rect(rect.x, y, rect.width, lineH);
                        EditorGUI.PropertyField(initRect, toggleInitialProp, new GUIContent("Initial Value"));
                        y += lineH + vGap;

                        var interactRect = new Rect(rect.x, y, rect.width, lineH);
                        EditorGUI.PropertyField(interactRect, toggleInteractProp, new GUIContent("Interactable"));
                        y += lineH + vGap;

                        // ✅ Toggle 위젯: 옵션 안에서 TextArea 표시
                        DrawInlineTextArea(rect, ref y, lineH, vGap, textProp, lines: 2, label: "Text");
                    }

                    break;
                }

                case WidgetType.Slider:
                {
                    string sectionKey = $"{w.propertyPath}/{widgetType}/SliderOptions";
                    bool open = GetSectionFoldout(sectionKey, defaultValue: false); // 기본 접힘

                    var headerRect = new Rect(rect.x, y, rect.width, lineH);
                    open = EditorGUI.Foldout(headerRect, open, "[Slider Options]", true);
                    SetSectionFoldout(sectionKey, open);
                    y += lineH + vGap;

                    if (open)
                    {
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

                        // ❌ Slider는 TextArea 표시 안 함
                    }

                    break;
                }

                // enum에 GameObject가 실제로 있다면:
                case WidgetType.GameObject:
                {
                    string sectionKey = $"{w.propertyPath}/{widgetType}/GameObjectOptions";
                    bool open = GetSectionFoldout(sectionKey, defaultValue: false); // 기본 접힘

                    var headerRect = new Rect(rect.x, y, rect.width, lineH);
                    open = EditorGUI.Foldout(headerRect, open, "[GameObject Options]", true);
                    SetSectionFoldout(sectionKey, open);
                    y += lineH + vGap;

                    if (open)
                    {
                        // 여기서 GameObject 전용 옵션이 있으면 그리면 됨.
                        // (없으면 그냥 비워둬도 OK)
                        // GameObject는 TextArea 표시 안 함
                    }

                    break;
                }

                case WidgetType.Slot:
                {
                    string sectionKey = $"{w.propertyPath}/{widgetType}/SlotOptions";
                    bool open = GetSectionFoldout(sectionKey, defaultValue: true); // Slot만 기본 펼침

                    var headerRect = new Rect(rect.x, y, rect.width, lineH);
                    open = EditorGUI.Foldout(headerRect, open, "[Slot Options]", true);
                    SetSectionFoldout(sectionKey, open);
                    y += lineH + vGap;

                    if (open)
                    {
                        const float buttonWidth   = 110f;
                        const float buttonGap     = 4f;
                        const float minTextWidth  = 80f; // Slot Id 텍스트 최소 폭 (원하는 값으로 조절)

                        float availableWidth = rect.width - buttonWidth - buttonGap;
                        float textWidth      = Mathf.Max(minTextWidth, availableWidth);

                        var idRect = new Rect(rect.x, y, textWidth, lineH);
                        var buttonRect = new Rect(idRect.xMax + buttonGap, y, buttonWidth, lineH);

                        // (선택) 라벨 폭도 줄여서 텍스트 영역 확보
                        float oldLabelWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 60f; // 기본보다 살짝 줄이기

                        EditorGUI.BeginChangeCheck();
                        string newId = EditorGUI.TextField(idRect, "Slot Id", slotIdProp.stringValue);
                        if (EditorGUI.EndChangeCheck())
                        {
                            newId = (newId ?? string.Empty).Trim();
                            slotIdProp.stringValue = newId;
                            MarkSlotGraphDirty();
                        }

                        EditorGUIUtility.labelWidth = oldLabelWidth; // 원복

                        using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(slotIdProp.stringValue)))
                        {
                            if (GUI.Button(buttonRect, "Create Child Slot"))
                            {
                                string targetName = (slotIdProp.stringValue ?? string.Empty).Trim();
                                if (!string.IsNullOrEmpty(targetName))
                                    OpenChildSlot(targetName);
                            }
                        }

                        y += lineH + vGap;
                    }

                    break;
                }
            }
        }

        // Prefab Override
        {
            string sectionKey = $"{w.propertyPath}/{widgetType}/PrefabOverride";
            bool open = GetSectionFoldout(sectionKey, defaultValue: false); // ✅ 기본 접힘

            var headerRect = new Rect(rect.x, y, rect.width, lineH);
            open = EditorGUI.Foldout(headerRect, open, "Prefab Override", true);
            SetSectionFoldout(sectionKey, open);
            y += lineH + vGap;

            if (open)
            {
                var prefabRect = new Rect(rect.x, y, rect.width, lineH);
                EditorGUI.PropertyField(prefabRect, prefabProp, GUIContent.none);
                y += lineH + vGap;
            }
        }
    }

    #endregion

    private static void DrawInlineTextArea(
        Rect rect,
        ref float y,
        float lineH,
        float vGap,
        SerializedProperty textProp,
        int lines = 2,
        string label = "Text"
    )
    {
        if (textProp == null) return;

        // 라벨 1줄 + 텍스트 영역
        var labelRect = new Rect(rect.x, y, rect.width, lineH);
        EditorGUI.LabelField(labelRect, label, EditorStyles.miniBoldLabel);
        y += lineH + vGap;

        float textHeight = (lineH + 2f) * lines;
        var textRect = new Rect(rect.x, y, rect.width, textHeight);
        textProp.stringValue = EditorGUI.TextArea(textRect, textProp.stringValue, EditorStyles.textArea);
        y += textHeight + vGap;
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
            string otherParentName = GetSlotNameByIndex(otherParentIndex);

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
            var slot = _slotsProp.GetArrayElementAtIndex(i);
            var nameProp = slot.FindPropertyRelative("slotName");
            string name = (nameProp != null ? nameProp.stringValue : string.Empty)?.Trim();
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

            var newSlot = _slotsProp.GetArrayElementAtIndex(childIndex);
            var nameProp = newSlot.FindPropertyRelative("slotName");
            var widgetsProp = newSlot.FindPropertyRelative("widgets");

            if (nameProp != null)
                nameProp.stringValue = slotName;
            if (widgetsProp != null)
                widgetsProp.ClearArray();

            _so.ApplyModifiedProperties();
            MarkSlotGraphDirty();
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
                _asset = null;
                _so = null;
                _slotsList = null;
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
        var e = Event.current;
        if (e.type == EventType.KeyDown && !EditorGUIUtility.editingTextField)
        {
            bool action = e.control || e.command; // Win: Ctrl, Mac: Cmd

            if (action && e.keyCode == KeyCode.C)
            {
                CopySelectedWidget();
                e.Use();
            }
            else if (action && e.keyCode == KeyCode.V)
            {
                PasteWidgetBelowSelected();
                e.Use();
            }
            else if (action && e.keyCode == KeyCode.D)
            {
                DuplicateSelectedWidget();
                e.Use();
            }
        }

        if (_slotGraphDirty && Event.current.type == EventType.Layout)
        {
            RebuildSlotGraphCache();
            _slotGraphDirty = false;
        }

        var prefabProp = _specProp.FindPropertyRelative("templatePrefab");

        EditorGUILayout.LabelField("Template", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(prefabProp, new GUIContent("Template Prefab"));

        EditorGUILayout.Space(8);

        using (new EditorGUILayout.HorizontalScope())
        {
            // 1) Left: Slots
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.42f)))
            {
                _slotsScroll = EditorGUILayout.BeginScrollView(_slotsScroll);
                _slotsList?.DoLayoutList();
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(4f);

                // Slots 패널 아래쪽, 오른쪽 정렬된 Clean 버튼
                bool hasAnySlot =
                    _slotsProp != null &&
                    _slotsProp.arraySize > 0;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace(); // 오른쪽으로 밀기

                    EditorGUI.BeginDisabledGroup(!hasAnySlot || _asset == null);
                    if (GUILayout.Button("Clean Unlinked Slots", GUILayout.Width(160f)))
                    {
                        CleanupUnlinkedSlots();
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }

            GUILayout.Space(4f);

            // 2) Center: 현재 Slot의 Widgets (기존 그대로)
            using (new EditorGUILayout.VerticalScope(
                       GUILayout.MinWidth(CenterMinWidth),
                       GUILayout.MaxWidth(CenterMaxWidth),
                       GUILayout.ExpandWidth(false)))
            {
                DrawSlotPathBreadcrumb();
                DrawCurrentSlotHeader();

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

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    
                    // Enable all widgets
                    EditorGUI.BeginDisabledGroup(!hasSlotSelected || _asset == null);
                    if (GUILayout.Button("Enable All Widgets", GUILayout.Width(140f)))
                    {
                        EnableAllDisabledWidgets(_asset.spec);
                        _so.Update();
                        EditorUtility.SetDirty(_asset);
                        BuildWidgetsListForCurrentSlot();
                        Repaint();
                    }

                    EditorGUI.EndDisabledGroup();

                    GUILayout.Space(4f);

                    // Expand / Collapse All (current slot)
                    EditorGUI.BeginDisabledGroup(!hasSlotSelected || _asset == null);
                    if (GUILayout.Button("Expand All", GUILayout.Width(90f)))
                    {
                        _so.Update();
                        SetAllWidgetFoldoutsInCurrentSlot(true);
                    }

                    if (GUILayout.Button("Collapse All", GUILayout.Width(90f)))
                    {
                        _so.Update();
                        SetAllWidgetFoldoutsInCurrentSlot(false);
                    }

                    EditorGUI.EndDisabledGroup();
                }
            }

            GUILayout.Space(4f);

            // 3) Right: All Widgets (새 패널)
            using (new EditorGUILayout.VerticalScope(
                       GUILayout.MinWidth(RightMinWidth),
                       GUILayout.MaxWidth(RightMaxWidth),
                       GUILayout.ExpandWidth(true)))
            {
                DrawAllWidgetsPanel();
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
                string name = $"Slot {slotIndex}";

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

            var slot = _slotsProp.GetArrayElementAtIndex(i);
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
        var chain = new List<int>();
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
            var slot = _slotsProp.GetArrayElementAtIndex(idx);
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

        var childSlot = slotsProp.GetArrayElementAtIndex(childIndex);
        var childNameProp = childSlot.FindPropertyRelative("slotName");
        string childName = (childNameProp != null ? childNameProp.stringValue : string.Empty)?.Trim();
        if (string.IsNullOrEmpty(childName))
            return -1;

        // Find any Slot widget whose slotId matches this slot's name → that slot is its parent.
        for (int i = 0; i < slotsProp.arraySize; i++)
        {
            if (i == childIndex) continue;

            var slot = slotsProp.GetArrayElementAtIndex(i);
            var widgetsProp = slot.FindPropertyRelative("widgets");
            if (widgetsProp == null) continue;

            for (int w = 0; w < widgetsProp.arraySize; w++)
            {
                var widget = widgetsProp.GetArrayElementAtIndex(w);
                var typeProp = widget.FindPropertyRelative("widgetType");
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
            string name = (nameProp != null ? nameProp.stringValue : string.Empty)?.Trim();

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

            var slot = _slotsProp.GetArrayElementAtIndex(i);
            var widgetsProp = slot.FindPropertyRelative("widgets");
            if (widgetsProp == null)
                continue;

            for (int w = 0; w < widgetsProp.arraySize; w++)
            {
                var widget = widgetsProp.GetArrayElementAtIndex(w);
                var typeProp = widget.FindPropertyRelative("widgetType");
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

        var slot = _slotsProp.GetArrayElementAtIndex(index);
        var nameProp = slot.FindPropertyRelative("slotName");
        string name = nameProp != null ? nameProp.stringValue : null;

        if (string.IsNullOrWhiteSpace(name))
            return $"Slot {index}";

        return name.Trim();
    }


    private bool GetSectionFoldout(string key, bool defaultValue)
    {
        if (_widgetSectionFoldoutStates.TryGetValue(key, out bool v))
            return v;
        _widgetSectionFoldoutStates[key] = defaultValue;
        return defaultValue;
    }

    private void SetSectionFoldout(string key, bool value)
    {
        _widgetSectionFoldoutStates[key] = value;
    }

    // ─────────────────────────────────────────────
    // Utility
    // ─────────────────────────────────────────────
    private void ApplyPresetToWidget(WidgetPreset preset, SerializedProperty widgetProp)
    {
        if (widgetProp == null) return;

        var rectModeProp = widgetProp.FindPropertyRelative("rectMode");
        var anchorMinProp = widgetProp.FindPropertyRelative("anchorMin");
        var anchorMaxProp = widgetProp.FindPropertyRelative("anchorMax");
        var pivotProp = widgetProp.FindPropertyRelative("pivot");
        var anchoredPosProp = widgetProp.FindPropertyRelative("anchoredPosition");
        var sizeDeltaProp = widgetProp.FindPropertyRelative("sizeDelta");

        rectModeProp.enumValueIndex = (int)preset.rectMode;
        anchorMinProp.vector2Value = preset.anchorMin;
        anchorMaxProp.vector2Value = preset.anchorMax;
        pivotProp.vector2Value = preset.pivot;
        anchoredPosProp.vector2Value = preset.anchoredPosition;
        sizeDeltaProp.vector2Value = preset.sizeDelta;
    }

    private void ResetWidgetSpecDefaults(SerializedProperty widgetProp, int index)
    {
        if (widgetProp == null) return;

        var typeProp = widgetProp.FindPropertyRelative("widgetType");
        var nameTagProp = widgetProp.FindPropertyRelative("nameTag");
        var textProp = widgetProp.FindPropertyRelative("text");
        var routeProp = widgetProp.FindPropertyRelative("onClickRoute");
        var prefabOverrideProp = widgetProp.FindPropertyRelative("prefabOverride");
        var textRoleProp = widgetProp.FindPropertyRelative("textRole");

        var rectModeProp = widgetProp.FindPropertyRelative("rectMode");
        var anchorMinProp = widgetProp.FindPropertyRelative("anchorMin");
        var anchorMaxProp = widgetProp.FindPropertyRelative("anchorMax");
        var pivotProp = widgetProp.FindPropertyRelative("pivot");
        var anchoredPosProp = widgetProp.FindPropertyRelative("anchoredPosition");
        var sizeDeltaProp = widgetProp.FindPropertyRelative("sizeDelta");

        var disabledProp = widgetProp.FindPropertyRelative("disabled");

        typeProp.enumValueIndex = (int)WidgetType.Text;
        nameTagProp.stringValue = $"Widget {index}";
        textProp.stringValue = string.Empty;
        routeProp.stringValue = string.Empty;
        prefabOverrideProp.objectReferenceValue = null;

        rectModeProp.enumValueIndex = (int)WidgetRectMode.OverrideInSlot;

        anchorMinProp.vector2Value = new Vector2(0.5f, 0.5f);
        anchorMaxProp.vector2Value = new Vector2(0.5f, 0.5f);
        pivotProp.vector2Value = new Vector2(0.5f, 0.5f);
        anchoredPosProp.vector2Value = Vector2.zero;
        sizeDeltaProp.vector2Value = new Vector2(300f, 80f);

        if (disabledProp != null)
            disabledProp.boolValue = false;

        var imageColorProp = widgetProp.FindPropertyRelative("imageColor");
        var imageNativeProp = widgetProp.FindPropertyRelative("imageSetNativeSize");
        var toggleInitialProp = widgetProp.FindPropertyRelative("toggleInitialValue");
        var toggleInteractProp = widgetProp.FindPropertyRelative("toggleInteractable");
        var sliderMinProp = widgetProp.FindPropertyRelative("sliderMin");
        var sliderMaxProp = widgetProp.FindPropertyRelative("sliderMax");
        var sliderInitProp = widgetProp.FindPropertyRelative("sliderInitialValue");
        var sliderWholeProp = widgetProp.FindPropertyRelative("sliderWholeNumbers");

        var imageSpriteProp = widgetProp.FindPropertyRelative("imageSprite");
        if (imageSpriteProp != null) imageSpriteProp.objectReferenceValue = null;
        if (imageColorProp != null) imageColorProp.colorValue = Color.white;
        if (imageNativeProp != null) imageNativeProp.boolValue = false;

        if (toggleInitialProp != null) toggleInitialProp.boolValue = false;
        if (toggleInteractProp != null) toggleInteractProp.boolValue = true;

        if (sliderMinProp != null) sliderMinProp.floatValue = 0f;
        if (sliderMaxProp != null) sliderMaxProp.floatValue = 1f;
        if (sliderInitProp != null) sliderInitProp.floatValue = 0.5f;
        if (sliderWholeProp != null) sliderWholeProp.boolValue = false;
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

    private void RebuildSlotGraphCache()
    {
        if (_slotsProp == null)
        {
            _hasParentCache = null;
            _parentIndexCache = null;
            _depthCache = null;
            _pathLabelCache = null;
            return;
        }

        int n = _slotsProp.arraySize;
        _hasParentCache = new bool[n];
        _parentIndexCache = new int[n];
        _depthCache = new int[n];
        _pathLabelCache = new string[n];

        for (int i = 0; i < n; i++)
            _parentIndexCache[i] = -1;

        // 1) slotName -> index
        var nameToIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < n; i++)
        {
            var slot = _slotsProp.GetArrayElementAtIndex(i);
            var nameProp = slot.FindPropertyRelative("slotName");
            string name = (nameProp != null ? nameProp.stringValue : string.Empty)?.Trim();
            if (!string.IsNullOrEmpty(name) && !nameToIndex.ContainsKey(name))
                nameToIndex.Add(name, i);
        }

        // 2) build parentIndex + hasParent via Slot widgets
        for (int parent = 0; parent < n; parent++)
        {
            var slot = _slotsProp.GetArrayElementAtIndex(parent);
            var widgetsProp = slot.FindPropertyRelative("widgets");
            if (widgetsProp == null) continue;

            for (int wi = 0; wi < widgetsProp.arraySize; wi++)
            {
                var widget = widgetsProp.GetArrayElementAtIndex(wi);
                var typeProp = widget.FindPropertyRelative("widgetType");
                if (typeProp == null) continue;

                if ((WidgetType)typeProp.enumValueIndex != WidgetType.Slot)
                    continue;

                var slotIdProp = widget.FindPropertyRelative("slotId");
                string id = (slotIdProp != null ? slotIdProp.stringValue : string.Empty)?.Trim();
                if (string.IsNullOrEmpty(id)) continue;

                if (!nameToIndex.TryGetValue(id, out int child))
                    continue;

                _hasParentCache[child] = true;

                // Keep the first discovered parent (matches old FindParentSlotIndex "first hit" behavior)
                if (_parentIndexCache[child] < 0)
                    _parentIndexCache[child] = parent;
            }
        }

        // 3) depth + path label
        for (int i = 0; i < n; i++)
            BuildDepthAndPathForSlot(i);
    }

    private void BuildDepthAndPathForSlot(int slotIndex)
    {
        // build chain child -> parent -> ...
        var chain = new List<int>(8);
        var visited = new HashSet<int>();

        int cur = slotIndex;
        while (cur >= 0 && cur < _parentIndexCache.Length && !visited.Contains(cur))
        {
            visited.Add(cur);
            chain.Add(cur);
            cur = _parentIndexCache[cur];
        }

        chain.Reverse();
        _depthCache[slotIndex] = Math.Max(0, chain.Count - 1);

        var names = new List<string>(chain.Count);
        foreach (int idx in chain)
        {
            var slot = _slotsProp.GetArrayElementAtIndex(idx);
            var nameProp = slot.FindPropertyRelative("slotName");
            string raw = nameProp != null ? nameProp.stringValue : string.Empty;

            string label;
            if (idx == 0)
                label = string.IsNullOrWhiteSpace(raw) ? "(root)" : raw.Trim();
            else
                label = NormalizeSlotLabel(raw);

            names.Add(label);
        }

        _pathLabelCache[slotIndex] = string.Join(" > ", names);
    }


    private bool[] BuildHasParentFlags()
    {
        if (_slotsProp == null)
            return Array.Empty<bool>();

        int slotCount = _slotsProp.arraySize;
        var hasParent = new bool[slotCount];

        // Fill parent information via Slot widgets (slotId → slotName).
        for (int i = 0; i < slotCount; i++)
        {
            var slot = _slotsProp.GetArrayElementAtIndex(i);
            var widgetsProp = slot.FindPropertyRelative("widgets");
            if (widgetsProp == null) continue;

            for (int w = 0; w < widgetsProp.arraySize; w++)
            {
                var widget = widgetsProp.GetArrayElementAtIndex(w);
                var typeProp = widget.FindPropertyRelative("widgetType");
                var slotIdProp = widget.FindPropertyRelative("slotId");

                if (typeProp == null || slotIdProp == null) continue;

                var widgetType = (WidgetType)typeProp.enumValueIndex;
                if (widgetType != WidgetType.Slot) continue;

                string id = (slotIdProp.stringValue ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(id)) continue;

                // If any slotName matches this id, that slot has a parent.
                for (int j = 0; j < slotCount; j++)
                {
                    var childSlot = _slotsProp.GetArrayElementAtIndex(j);
                    var childNameProp = childSlot.FindPropertyRelative("slotName");
                    string childName = (childNameProp != null ? childNameProp.stringValue : string.Empty).Trim();
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


    private void RefreshPresetLabelsIfNeeded()
    {
        int count = (_presetCatalog != null && _presetCatalog.presets != null) ? _presetCatalog.presets.Count : 0;
        bool hasCatalog = count > 0;

        if (_presetLabelsCache != null &&
            _presetLabelsCountCache == count &&
            _presetLabelsHasCatalogCache == hasCatalog)
            return;

        _presetLabelsHasCatalogCache = hasCatalog;
        _presetLabelsCountCache = count;

        if (!hasCatalog)
        {
            _presetLabelsCache = new[] { "(No presets configured)" };
            return;
        }

        _presetLabelsCache = new string[count + 1];
        _presetLabelsCache[0] = "Select Preset";
        for (int i = 0; i < count; i++)
        {
            var p = _presetCatalog.presets[i];
            _presetLabelsCache[i + 1] = string.IsNullOrEmpty(p.id) ? $"Preset {i}" : p.id;
        }
    }

    private bool TryGetCurrentWidgetsProp(out SerializedProperty widgetsProp)
    {
        widgetsProp = null;

        if (_slotsProp == null || _slotPath == null || _slotPath.Count == 0)
            return false;

        int slotIndex = _slotPath[_slotPath.Count - 1];
        if (slotIndex < 0 || slotIndex >= _slotsProp.arraySize)
            return false;

        var slot = _slotsProp.GetArrayElementAtIndex(slotIndex);
        widgetsProp = slot.FindPropertyRelative("widgets");
        return widgetsProp != null;
    }

    private void SetAllWidgetFoldoutsInCurrentSlot(bool expanded)
    {
        if (!TryGetCurrentWidgetsProp(out var widgetsProp))
            return;

        // 현재 슬롯의 모든 위젯 foldout을 강제로 동일 상태로 세팅
        for (int i = 0; i < widgetsProp.arraySize; i++)
        {
            var w = widgetsProp.GetArrayElementAtIndex(i);
            if (w == null) continue;

            _widgetFoldoutStates[w.propertyPath] = expanded;

            // (선택) 섹션 폴드아웃까지 같이 접고/펼치고 싶으면 아래도 같이 켜.
            // GetSectionFoldout/SetSectionFoldout을 이미 쓰고 있으니, key 규칙에 맞춰서 밀어 넣는다.
            var typeProp = w.FindPropertyRelative("widgetType");
            if (typeProp != null)
            {
                var widgetType = (WidgetType)typeProp.enumValueIndex;

                // 너가 쓰는 key 규칙: $"{w.propertyPath}/{widgetType}/XxxOptions"
                SetSectionFoldout($"{w.propertyPath}/{widgetType}/ButtonOptions", expanded);
                SetSectionFoldout($"{w.propertyPath}/{widgetType}/ImageOptions", expanded);
                SetSectionFoldout($"{w.propertyPath}/{widgetType}/ToggleOptions", expanded);
                SetSectionFoldout($"{w.propertyPath}/{widgetType}/SliderOptions", expanded);
                SetSectionFoldout($"{w.propertyPath}/{widgetType}/SlotOptions", expanded);

                // Prefab Override는 공통 섹션 키를 쓰는 버전도 있었어서 둘 다 커버
                SetSectionFoldout($"{w.propertyPath}/{widgetType}/PrefabOverride", expanded);
                SetSectionFoldout($"{w.propertyPath}/PrefabOverride", expanded);
            }
        }

        Repaint();
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

        int slotCount = _slotsProp.arraySize;
        // Make sure cache is up-to-date before cleaning
        if (_slotGraphDirty)
        {
            RebuildSlotGraphCache();
            _slotGraphDirty = false;
        }

        var hasParent = _hasParentCache ?? Array.Empty<bool>();
        var toDelete = new List<int>();

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
        MarkSlotGraphDirty();

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
            _widgetsList = null;

            // Very defensive fallback: recreate a root slot if nothing remains.
            EnsureRootSlotExists();
            _selectedSlotIndex = 0;
            SetRootSlot(0);
        }

        BuildSlotsList();
        Repaint();
    }

    private static bool WidgetTypeSupportsTextRole(WidgetType t)
    {
        return t == WidgetType.Text
               || t == WidgetType.Button
               || t == WidgetType.Toggle
               || t == WidgetType.Image;
        // 필요하면 Slider도 텍스트 라벨 쓸 경우 여기 추가 가능
    }

    private void DrawCurrentSlotHeader()
    {
        if (_slotsProp == null || _slotPath == null || _slotPath.Count == 0)
            return;

        int slotIndex = _slotPath[_slotPath.Count - 1];
        if (slotIndex < 0 || slotIndex >= _slotsProp.arraySize)
            return;

        var slotProp = _slotsProp.GetArrayElementAtIndex(slotIndex);
        var nameProp = slotProp.FindPropertyRelative("slotName");
        string currentName = nameProp != null ? nameProp.stringValue : string.Empty;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (slotIndex == 0)
            {
                // 🔹 Root는 기존처럼 왼쪽 리스트에서 편집하되,
                // 오른쪽에는 읽기 전용으로 표시만 해도 됨.
                EditorGUILayout.LabelField("Root Slot Id", GUILayout.Width(90f));
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField(currentName);
                }
            }
            else
            {
                EditorGUILayout.LabelField("Slot Id", GUILayout.Width(50f));

                EditorGUI.BeginChangeCheck();
                string newName = EditorGUILayout.TextField(currentName);
                if (EditorGUI.EndChangeCheck() && nameProp != null)
                {
                    newName = (newName ?? string.Empty).Trim();

                    if (string.IsNullOrEmpty(newName))
                    {
                        EditorUtility.DisplayDialog(
                            "Invalid Slot Id",
                            "Slot Id cannot be empty.\nPlease enter a unique id.",
                            "OK"
                        );
                    }
                    else if (IsSlotNameUsedByOtherSlots(slotIndex, newName))
                    {
                        EditorUtility.DisplayDialog(
                            "Duplicate Slot Id",
                            $"The id '{newName}' is already used by another slot.\n\n" +
                            "Slot Id must be unique.\n" +
                            "Please choose a different id or rename the other slot first.",
                            "OK"
                        );
                    }
                    else
                    {
                        nameProp.stringValue = newName;
                        _so.ApplyModifiedProperties();
                        MarkSlotGraphDirty(); // 이름 바뀌면 그래프/경로/레이블 다시 계산
                    }
                }
            }
        }

        EditorGUILayout.Space(4f);
    }

    private void DrawAllWidgetsPanel()
    {
        EditorGUILayout.LabelField("All Widgets (All Slots)", EditorStyles.boldLabel);
        EditorGUILayout.Space(2f);

        if (_slotsProp == null || _slotsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No slots defined.", MessageType.None);
            return;
        }

        // 현재 선택 상태 (중앙 패널 기준)
        int currentSlotIndex = -1;
        int currentWidgetIndex = -1;

        if (_slotPath != null && _slotPath.Count > 0)
            currentSlotIndex = _slotPath[_slotPath.Count - 1];

        if (_widgetsList != null)
            currentWidgetIndex = _widgetsList.index;

        _allWidgetsScroll = EditorGUILayout.BeginScrollView(_allWidgetsScroll);

        for (int i = 0; i < _slotsProp.arraySize; i++)
        {
            var slotProp = _slotsProp.GetArrayElementAtIndex(i);
            var nameProp = slotProp.FindPropertyRelative("slotName");
            var widgetsProp = slotProp.FindPropertyRelative("widgets");

            string slotName = (nameProp != null && !string.IsNullOrWhiteSpace(nameProp.stringValue))
                ? nameProp.stringValue.Trim()
                : $"Slot {i}";

            // 슬롯 헤더
            EditorGUILayout.LabelField(slotName, EditorStyles.miniBoldLabel);

            if (widgetsProp == null || widgetsProp.arraySize == 0)
            {
                EditorGUILayout.LabelField("  (no widgets)", EditorStyles.miniLabel);
                EditorGUILayout.Space(2f);
                continue;
            }

            EditorGUI.indentLevel++;
            for (int wIndex = 0; wIndex < widgetsProp.arraySize; wIndex++)
            {
                var w = widgetsProp.GetArrayElementAtIndex(wIndex);
                if (w == null) continue;

                var nameTagProp = w.FindPropertyRelative("nameTag");
                var typeProp = w.FindPropertyRelative("widgetType");
                var disabledProp = w.FindPropertyRelative("disabled");

                string nameTag = (nameTagProp != null && !string.IsNullOrWhiteSpace(nameTagProp.stringValue))
                    ? nameTagProp.stringValue.Trim()
                    : $"Widget {wIndex}";

                WidgetType widgetType = typeProp != null
                    ? (WidgetType)typeProp.enumValueIndex
                    : WidgetType.Text;

                bool isDisabled = disabledProp != null && disabledProp.boolValue;

                bool isSelected =
                    (i == currentSlotIndex) &&
                    (wIndex == currentWidgetIndex);

                string label = $"{nameTag}  [{widgetType}]";
                if (isDisabled)
                    label += "  (disabled)";

                GUIStyle style = isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;

                if (GUILayout.Button(label, style))
                {
                    // 🔗 오른쪽에서 클릭 → 왼쪽 Slot + 중앙 Widgets 모두 해당 항목으로 이동/선택
                    if (_slotsList != null)
                        _slotsList.index = i;

                    // Slot path / WidgetsList 재설정
                    RebuildSlotPathForSelected(i);

                    if (_widgetsList != null)
                        _widgetsList.index = wIndex;
                    ScrollWidgetsToIndex(wIndex);

                    Repaint();
                }
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4f);
        }

        EditorGUILayout.EndScrollView();
    }


    private void OnUndoRedo()
    {
        // 이 창과 관련 없는 Undo일 수도 있으니 방어
        if (_asset == null)
            return;

        // SerializedObject 다시 동기화
        if (_so == null)
            _so = new SerializedObject(_asset);
        else
            _so.Update();

        // spec / slots 프로퍼티 다시 찾아두기 (혹시 구조가 바뀌었을 수도 있으니)
        if (_specProp == null)
            _specProp = _so.FindProperty("spec");

        if (_specProp != null)
            _slotsProp = _specProp.FindPropertyRelative("slots");
        else
            _slotsProp = null;

        // 슬롯 개수가 줄었을 수 있으니 path/선택 인덱스 정리
        if (_slotsProp == null || _slotsProp.arraySize == 0)
        {
            _slotPath.Clear();
            _selectedSlotIndex = -1;
            _widgetsList = null;
        }
        else
        {
            // path가 비어 있으면 0번(root)로 초기화
            if (_slotPath.Count == 0)
                _slotPath.Add(0);

            int last = _slotPath[_slotPath.Count - 1];
            if (last < 0 || last >= _slotsProp.arraySize)
            {
                last = Mathf.Clamp(last, 0, _slotsProp.arraySize - 1);
                _slotPath[_slotPath.Count - 1] = last;
            }

            _selectedSlotIndex = last;
            BuildWidgetsListForCurrentSlot(); // 오른쪽 widgets 리스트도 다시 바인딩
        }

        // 슬롯 그래프 캐시(부모/깊이/경로 라벨) 다시 계산하도록 플래그
        MarkSlotGraphDirty();

        // 화면 다시 그리기
        Repaint();
    }

    [Serializable]
    private sealed class WidgetClipboardPayload
    {
        public string kind; // sanity marker
        public string json; // EditorJsonUtility serialized WidgetSpec
        public List<ObjectRef> refs; // UnityEngine.Object refs (prefab/sprite/etc.)
    }

    [Serializable]
    private sealed class ObjectRef
    {
        public string field; // field name on WidgetSpec
        public string globalId; // GlobalObjectId string
    }

    private bool TryGetCurrentSlotIndex(out int slotIndex)
    {
        slotIndex = -1;
        if (_slotsProp == null || _slotPath == null || _slotPath.Count == 0)
            return false;

        slotIndex = _slotPath[_slotPath.Count - 1];
        return slotIndex >= 0 && slotIndex < _asset.spec.slots.Count;
    }

    private bool TryGetSelectedWidgetIndex(out int widgetIndex)
    {
        widgetIndex = -1;
        if (_widgetsList == null) return false;

        widgetIndex = _widgetsList.index;
        if (widgetIndex < 0) return false;
        return true;
    }

    private static IEnumerable<System.Reflection.FieldInfo> EnumerateUnityObjectFields(Type t)
    {
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic;

        foreach (var f in t.GetFields(flags))
        {
            // public field OR [SerializeField] private field
            bool serializable =
                f.IsPublic ||
                Attribute.IsDefined(f, typeof(SerializeField));

            if (!serializable) continue;

            if (typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType))
                yield return f;
        }
    }

    private static string MakeClipboardString(WidgetClipboardPayload payload)
    {
        string json = JsonUtility.ToJson(payload);
        return WidgetClipboardPrefix + json;
    }

    private static bool TryReadClipboardPayload(out WidgetClipboardPayload payload)
    {
        payload = null;

        string buf = EditorGUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(buf)) return false;
        if (!buf.StartsWith(WidgetClipboardPrefix, StringComparison.Ordinal)) return false;

        string json = buf.Substring(WidgetClipboardPrefix.Length);
        if (string.IsNullOrEmpty(json)) return false;

        try
        {
            payload = JsonUtility.FromJson<WidgetClipboardPayload>(json);
            if (payload == null) return false;
            if (payload.kind != "WidgetSpec") return false;
            if (string.IsNullOrEmpty(payload.json)) return false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void CaptureUnityObjectRefs(object widgetSpecInstance, WidgetClipboardPayload payload)
    {
        if (widgetSpecInstance == null) return;

        payload.refs ??= new List<ObjectRef>();
        payload.refs.Clear();

#if UNITY_2020_1_OR_NEWER
        var type = widgetSpecInstance.GetType();
        foreach (var f in EnumerateUnityObjectFields(type))
        {
            var obj = f.GetValue(widgetSpecInstance) as UnityEngine.Object;
            if (obj == null) continue;

            // GlobalObjectId는 프로젝트 에셋/서브에셋 참조 복원에 비교적 안정적
            GlobalObjectId gid = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            payload.refs.Add(new ObjectRef
            {
                field = f.Name,
                globalId = gid.ToString()
            });
        }
#endif
    }

    private static void RestoreUnityObjectRefs(object widgetSpecInstance, WidgetClipboardPayload payload)
    {
        if (widgetSpecInstance == null) return;
        if (payload == null || payload.refs == null || payload.refs.Count == 0) return;

#if UNITY_2020_1_OR_NEWER
        var type = widgetSpecInstance.GetType();
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic;

        foreach (var r in payload.refs)
        {
            if (string.IsNullOrEmpty(r.field) || string.IsNullOrEmpty(r.globalId))
                continue;

            var f = type.GetField(r.field, flags);
            if (f == null) continue;
            if (!typeof(UnityEngine.Object).IsAssignableFrom(f.FieldType))
                continue;

            if (!GlobalObjectId.TryParse(r.globalId, out var gid))
                continue;

            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
            if (obj == null) continue;

            // 타입 호환 확인
            if (!f.FieldType.IsInstanceOfType(obj))
                continue;

            f.SetValue(widgetSpecInstance, obj);
        }
#endif
    }

    private void CopySelectedWidget()
    {
        if (_asset == null) return;
        if (!TryGetCurrentSlotIndex(out int slotIndex)) return;
        if (!TryGetSelectedWidgetIndex(out int widgetIndex)) return;

        var slot = _asset.spec.slots[slotIndex];
        if (slot?.widgets == null) return;
        if (widgetIndex < 0 || widgetIndex >= slot.widgets.Count) return;

        var widget = slot.widgets[widgetIndex];
        if (widget == null) return;

        // JSON + UnityEngine.Object refs
        var payload = new WidgetClipboardPayload
        {
            kind = "WidgetSpec",
            json = EditorJsonUtility.ToJson(widget, true),
            refs = new List<ObjectRef>()
        };
        CaptureUnityObjectRefs(widget, payload);

        EditorGUIUtility.systemCopyBuffer = MakeClipboardString(payload);
    }

    private void PasteWidgetBelowSelected()
    {
        if (_asset == null) return;
        if (!TryGetCurrentSlotIndex(out int slotIndex)) return;

        if (!TryReadClipboardPayload(out var payload))
            return;

        // 새 위젯 생성 & 복원
        var newWidget = new WidgetSpec();
        EditorJsonUtility.FromJsonOverwrite(payload.json, newWidget);
        RestoreUnityObjectRefs(newWidget, payload);

        // 삽입 위치: 선택된 위젯 아래, 없으면 맨 뒤
        int insertIndex = 0;
        if (TryGetSelectedWidgetIndex(out int selected))
            insertIndex = Mathf.Clamp(selected + 1, 0, _asset.spec.slots[slotIndex].widgets.Count);
        else
            insertIndex = _asset.spec.slots[slotIndex].widgets.Count;

        Undo.RecordObject(_asset, "Paste Widget");
        var slot = _asset.spec.slots[slotIndex];
        slot.widgets ??= new List<WidgetSpec>();
        slot.widgets.Insert(insertIndex, newWidget);

        EditorUtility.SetDirty(_asset);

        // Slot 위젯 붙여넣으면 그래프 영향 가능
        if (newWidget.widgetType == WidgetType.Slot)
            MarkSlotGraphDirty();

        // UI 갱신
        _so.Update();
        BuildWidgetsListForCurrentSlot();
        if (_widgetsList != null) _widgetsList.index = insertIndex;
        Repaint();
    }

    private void DuplicateSelectedWidget()
    {
        // Copy → Paste를 이용해서 바로 복제
        CopySelectedWidget();
        PasteWidgetBelowSelected();
    }

    private void ScrollWidgetsToIndex(int widgetIndex)
    {
        if (!TryGetCurrentWidgetsProp(out var widgetsProp))
            return;

        if (widgetsProp.arraySize == 0)
            return;

        widgetIndex = Mathf.Clamp(widgetIndex, 0, widgetsProp.arraySize - 1);

        float y = 0f;

        // 위에 있는 위젯들의 높이를 전부 더해서 목표 스크롤 위치 계산
        for (int i = 0; i < widgetIndex; i++)
        {
            y += CalcWidgetElementHeight(widgetsProp, i);
        }

        // 헤더/여백 조금 보정
        y += 24f;

        _widgetsScroll.y = y;
    }
}
#endif