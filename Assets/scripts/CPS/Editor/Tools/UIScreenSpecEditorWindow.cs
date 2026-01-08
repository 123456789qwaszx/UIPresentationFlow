#if UNITY_EDITOR
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

    // 현재 "어디까지 들어와 있는지"를 나타내는 Slot 인덱스 경로
    // ex) [0] -> [0, 2] -> [0, 2, 5]
    private readonly List<int> _slotPath = new();

    // 위젯별 Foldout 상태
    private readonly Dictionary<string, bool> _widgetFoldoutStates = new();

    // 위젯 프리셋 카탈로그
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

    // ─────────────────────────────────────────────
    // Slots 리스트
    // ─────────────────────────────────────────────
    private void BuildSlotsList()
    {
        _slotsList = new ReorderableList(_so, _slotsProp, true, true, true, true);

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

            // 새 슬롯은 루트처럼 취급
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
            EditorGUIUtility.singleLineHeight + 6f;

        _slotsList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            rect.y += 2f;

            const float horizontalPadding = 4f;
            rect.x += horizontalPadding;
            rect.width -= horizontalPadding * 2f;
            rect.height = EditorGUIUtility.singleLineHeight;

            var slot = _slotsProp.GetArrayElementAtIndex(index);
            var widgetsProp = slot.FindPropertyRelative("widgets");
            int widgetCount = widgetsProp != null ? widgetsProp.arraySize : 0;

            // 🔹 새로 추가할 부분: 경로 + depth 표시
            int depth;
            string pathLabel = GetSlotDisplayPath(index, out depth);

            // 예: [depth2] Root > C1 > C2 (3)
            EditorGUI.LabelField(rect, $"[depth{depth}] {pathLabel} ({widgetCount})");
        };
    }

    // ─────────────────────────────────────────────
    // Slot 경로 & 현재 Widgets 리스트
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
    /// Slots 리스트에서 어떤 슬롯을 클릭했을 때,
    /// Slot 위젯의 slotId 연결을 따라가면서
    /// 루트 → ... → targetIndex 경로를 찾아서 _slotPath를 재구성.
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

        // 1) slotName -> index 맵
        var nameToIndex = new Dictionary<string, int>();
        for (int i = 0; i < slotCount; i++)
        {
            var slot = _slotsProp.GetArrayElementAtIndex(i);
            var nameProp = slot.FindPropertyRelative("slotName");
            string name = (nameProp != null ? nameProp.stringValue : string.Empty)?.Trim();
            if (!string.IsNullOrEmpty(name) && !nameToIndex.ContainsKey(name))
                nameToIndex.Add(name, i);
        }

        // 2) parent -> children graph 구성 (Slot 위젯의 slotId 기준)
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

        // 3) 루트 후보들 찾기 (부모가 없는 슬롯들)
        var roots = new List<int>();
        for (int i = 0; i < slotCount; i++)
        {
            if (!hasParent[i])
                roots.Add(i);
        }

        // 4) 루트들에서 DFS로 targetIndex까지 경로 찾기
        var path = new List<int>();
        var visiting = new HashSet<int>();

        bool TryDfs(int current)
        {
            if (visiting.Contains(current))
                return false; // cycle 방어

            visiting.Add(current);
            path.Add(current);

            if (current == targetIndex)
                return true;

            foreach (int child in children[current])
            {
                if (TryDfs(child))
                    return true;
            }

            // 실패하면 되돌리기
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
            // 그래프 상에 경로를 못 찾으면, 그냥 단독 루트 취급
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
    // 개별 위젯 높이 계산
    // ─────────────────────────────────────────────
    private float CalcWidgetElementHeight(SerializedProperty widgetsProp, int index)
    {
        float lineH = EditorGUIUtility.singleLineHeight;
        float vGap = 2f;
        float borderPadding = 2f;

        if (widgetsProp == null || index < 0 || index >= widgetsProp.arraySize)
            return lineH + 2f * borderPadding;

        var w = widgetsProp.GetArrayElementAtIndex(index);

        string foldKey = w.propertyPath;
        bool expanded = true;
        _widgetFoldoutStates.TryGetValue(foldKey, out expanded);

        if (!expanded)
        {
            int collapsedLines = 1;
            float collapsedHeight = collapsedLines * (lineH + vGap) + vGap;
            return collapsedHeight + borderPadding * 2f + 4f;
        }

        int lines = 0;

        // 1줄: Name + Type
        lines += 1;
        // 프리셋 드롭다운
        lines += 1;
        // Text 2줄
        lines += 2;

        var typeProp = w.FindPropertyRelative("widgetType");
        var widgetType = (WidgetType)typeProp.enumValueIndex;

        // Route + Prefab
        lines += (widgetType == WidgetType.Button) ? 2 : 1;

        // Layout Mode
        lines += 1;

        var rectModeProp = w.FindPropertyRelative("rectMode");
        var rectMode = (WidgetRectMode)rectModeProp.enumValueIndex;
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
    // 개별 위젯 렌더링
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

        // 우클릭 메뉴 (Add / Delete)
        if (e.type == EventType.ContextClick && borderRect.Contains(e.mousePosition))
        {
            var menu = new GenericMenu();
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

        // 헤더: Foldout + Enabled 토글 + Name + Type
        string foldKey = w.propertyPath;
        bool expanded = true;
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
        const float gap = 4f;

        float typeX = rect.x + rect.width - typeWidth;
        var typeRect = new Rect(typeX, y, typeWidth, lineH);

        float nameWidth = typeX - x - gap;
        if (nameWidth < 60f) nameWidth = 60f;
        var nameFieldRect = new Rect(x, y, nameWidth, lineH);

        nameProp.stringValue = EditorGUI.TextField(nameFieldRect, nameProp.stringValue);
        EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);

        y += lineH + vGap;

        if (!expanded)
            return;

        var widgetType = (WidgetType)typeProp.enumValueIndex;

        // 프리셋 선택
        {
            string[] labels;
            bool hasPresetCatalog =
                _presetCatalog != null &&
                _presetCatalog.presets != null &&
                _presetCatalog.presets.Count > 0;

            if (hasPresetCatalog)
            {
                var presets = _presetCatalog.presets;
                int presetCount = presets.Count;

                labels = new string[presetCount + 1];
                labels[0] = "Select Preset";
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
                    var chosen = presets[newIndex - 1];
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
            float fieldGap = 4f;
            float rowHeight = lineH;

            Rect MakeRowRect() => new Rect(rect.x, y, rect.width, rowHeight);

            // Anchor Min
            var rowRect = MakeRowRect();
            var labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowHeight);
            var valueRect = new Rect(
                rowRect.x + labelWidth + fieldGap,
                rowRect.y,
                rowRect.width - labelWidth - fieldGap,
                rowHeight
            );

            EditorGUI.LabelField(labelRect, "Anchor Min");
            var anchorMinValue = anchorMinProp.vector2Value;
            anchorMinValue = EditorGUI.Vector2Field(valueRect, GUIContent.none, anchorMinValue);
            anchorMinProp.vector2Value = anchorMinValue;
            y += rowHeight + vGap;

            // Anchor Max
            rowRect = MakeRowRect();
            labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowHeight);
            valueRect = new Rect(
                rowRect.x + labelWidth + fieldGap,
                rowRect.y,
                rowRect.width - labelWidth - fieldGap,
                rowHeight
            );

            EditorGUI.LabelField(labelRect, "Anchor Max");
            var anchorMaxValue = anchorMaxProp.vector2Value;
            anchorMaxValue = EditorGUI.Vector2Field(valueRect, GUIContent.none, anchorMaxValue);
            anchorMaxProp.vector2Value = anchorMaxValue;
            y += rowHeight + vGap;

            // Pivot
            rowRect = MakeRowRect();
            labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowHeight);
            valueRect = new Rect(
                rowRect.x + labelWidth + fieldGap,
                rowRect.y,
                rowRect.width - labelWidth - fieldGap,
                rowHeight
            );

            EditorGUI.LabelField(labelRect, "Pivot");
            var pivotValue = pivotProp.vector2Value;
            pivotValue = EditorGUI.Vector2Field(valueRect, GUIContent.none, pivotValue);
            pivotProp.vector2Value = pivotValue;
            y += rowHeight + vGap;

            // Size
            rowRect = MakeRowRect();
            labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowHeight);
            valueRect = new Rect(
                rowRect.x + labelWidth + fieldGap,
                rowRect.y,
                rowRect.width - labelWidth - fieldGap,
                rowHeight
            );

            EditorGUI.LabelField(labelRect, "Size");
            var sizeValue = sizeDeltaProp.vector2Value;
            sizeValue = EditorGUI.Vector2Field(valueRect, GUIContent.none, sizeValue);
            sizeDeltaProp.vector2Value = sizeValue;
            y += rowHeight + vGap;

            // Position
            rowRect = MakeRowRect();
            labelRect = new Rect(rowRect.x, rowRect.y, labelWidth, rowHeight);
            valueRect = new Rect(
                rowRect.x + labelWidth + fieldGap,
                rowRect.y,
                rowRect.width - labelWidth - fieldGap,
                rowHeight
            );

            EditorGUI.LabelField(labelRect, "Position");
            var posValue = anchoredPosProp.vector2Value;
            posValue = EditorGUI.Vector2Field(valueRect, GUIContent.none, posValue);
            anchoredPosProp.vector2Value = posValue;
            y += rowHeight + vGap;

            // 타입별 추가 옵션
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
            int textLines = 2;
            float textHeight = (lineH + 2f) * textLines;

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

    // Slot 위젯의 Slot Id를 기준으로 child Slot을 열고, 경로에 추가
    private void OpenChildSlot(string slotName)
    {
        if (_slotsProp == null) return;

        slotName = (slotName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(slotName)) return;

        int childIndex = -1;
        for (int i = 0; i < _slotsProp.arraySize; i++)
        {
            var slot = _slotsProp.GetArrayElementAtIndex(i);
            var nameProp = slot.FindPropertyRelative("slotName");
            if (nameProp != null && nameProp.stringValue == slotName)
            {
                childIndex = i;
                break;
            }
        }

        // 없으면 새로 생성
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
        }

        // 현재 경로의 마지막 슬롯이 parent이므로, childIndex를 path 뒤에 붙임
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
                "UIScreenSpecAsset 를 선택하거나 드래그해서 열어주세요.\n(Project 창에서 Spec Asset 클릭 → 자동 바인딩됨)",
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
            // 왼쪽: Slot 리스트
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.4f)))
            {
                _slotsScroll = EditorGUILayout.BeginScrollView(_slotsScroll);
                _slotsList?.DoLayoutList();
                EditorGUILayout.EndScrollView();
            }

            GUILayout.Space(4f);

            // 오른쪽: Slot Path + Widgets
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                DrawSlotPathBreadcrumb();

                _widgetsScroll = EditorGUILayout.BeginScrollView(_widgetsScroll);

                if (_widgetsList == null)
                {
                    EditorGUILayout.HelpBox(
                        "좌측에서 Slot을 선택하거나, Slot 위젯의 Slot Id를 입력한 후 'Open Child Slot' 버튼으로 하위 Slot을 열 수 있습니다.",
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
    private string GetSlotDisplayPath(int slotIndex, out int depth)
    {
        depth = 0;

        if (_slotsProp == null || slotIndex < 0 || slotIndex >= _slotsProp.arraySize)
            return "(invalid)";

        // child -> parent -> grandparent...
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

        // root -> ... -> child 순으로 뒤집기
        chain.Reverse();
        depth = chain.Count - 1;

        var names = new List<string>();
        foreach (int idx in chain)
        {
            var slot = _slotsProp.GetArrayElementAtIndex(idx);
            var nameProp = slot.FindPropertyRelative("slotName");
            string rawName = nameProp != null ? nameProp.stringValue : string.Empty;
            string label = NormalizeSlotLabel(rawName);
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

        // 모든 슬롯을 돌면서, Slot 위젯의 slotId가 childName인 놈을 찾는다 → 그 슬롯이 부모
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

        // "Slot 0", "Slot 1" 같이 기본 자동 이름이면 표시상으론 숨겨버리기
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
    // ─────────────────────────────────────────────
    // 유틸
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

        rectModeProp.enumValueIndex = (int)WidgetRectMode.UseSlotLayout;

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
}
#endif