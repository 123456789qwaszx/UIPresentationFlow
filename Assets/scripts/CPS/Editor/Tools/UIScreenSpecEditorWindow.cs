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

        // 🔹 여기 추가: 최소 1개의 Root Slot 보장
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

            // 처음 기본 이름은 비워두거나 "Root" 정도로.
            // 어차피 나중에 템플릿의 UISlot.id와 맞춰주기 위해 직접 수정 가능해야 함.
            if (nameProp != null)
                nameProp.stringValue = "Root";

            if (widgetsProp != null)
                widgetsProp.ClearArray();

            _so.ApplyModifiedProperties();
        }
    }

    // ─────────────────────────────────────────────
    // Slots 리스트
    // ─────────────────────────────────────────────
    private void BuildSlotsList()
    {
        // 🔹 add/remove/drag 비활성화: 읽기 전용 리스트로
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
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            float vGap = 2f;

            // root(0번)는 2줄, 나머지는 1줄
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
    rect.x    += horizontalPadding;
    rect.width -= horizontalPadding * 2f;
    rect.height = lineH;

    var slot        = _slotsProp.GetArrayElementAtIndex(index);
    var nameProp    = slot.FindPropertyRelative("slotName");
    var widgetsProp = slot.FindPropertyRelative("widgets");
    int widgetCount = widgetsProp != null ? widgetsProp.arraySize : 0;

    int    depth;
    string pathLabel = GetSlotDisplayPath(index, out depth);

    // orphan 여부 계산 (index 0은 Root)
    bool isOrphan = false;
    if (index > 0)
    {
        var hasParent = BuildHasParentFlags();
        if (index < hasParent.Length)
            isOrphan = !hasParent[index];
    }

    string labelText;
    if (index == 0)
    {
        // Root 슬롯 표시용 라벨 (첫 줄)
        labelText = $"[Root] {pathLabel} ({widgetCount})";
    }
    else if (isOrphan)
    {
        // 부모가 없는 슬롯
        labelText = $"(!) [unlinked] {pathLabel} ({widgetCount})";
    }
    else
    {
        // 정상 depth 슬롯
        labelText = $"[depth{depth}] {pathLabel} ({widgetCount})";
    }

    // ──────────────────────
    // 1줄차: 라벨 + (Root가 아니면 ↑↓ 버튼 영역 고려)
    // ──────────────────────
    const float btnWidth = 18f;
    const float btnGap   = 2f;

    float labelWidth = rect.width;
    if (index > 0)
        labelWidth -= (btnWidth * 2f + btnGap * 2f);

    var labelRect = new Rect(rect.x, rect.y, labelWidth, lineH);
    EditorGUI.LabelField(labelRect, labelText);

    // Root 이외 슬롯이면 ↑↓ 버튼
    if (index > 0)
    {
        var upRect   = new Rect(labelRect.xMax + btnGap, rect.y, btnWidth, lineH);
        var downRect = new Rect(upRect.xMax + btnGap, rect.y, btnWidth, lineH);

        using (new EditorGUI.DisabledScope(index <= 1))
        {
            if (GUI.Button(upRect, "↑"))
            {
                MoveSlot(index, index - 1);
            }
        }

        using (new EditorGUI.DisabledScope(index >= _slotsProp.arraySize - 1))
        {
            if (GUI.Button(downRect, "↓"))
            {
                MoveSlot(index, index + 1);
            }
        }
    }

    // ──────────────────────
    // 2줄차: Root 한정 SlotId 편집
    // ──────────────────────
    if (index == 0)
    {
        var idRect = new Rect(rect.x, rect.y + lineH + vGap, rect.width, lineH);

        string currentName = nameProp != null ? nameProp.stringValue : string.Empty;

        EditorGUI.BeginChangeCheck();
        string newName = EditorGUI.TextField(idRect, "Root Slot Id", currentName);
        if (EditorGUI.EndChangeCheck() && nameProp != null)
        {
            nameProp.stringValue = newName;
            _so.ApplyModifiedProperties();
        }
    }
}


    private void MoveSlot(int from, int to)
    {
        if (_slotsProp == null) return;

        int size = _slotsProp.arraySize;
        if (from < 0 || from >= size) return;
        if (to < 0 || to >= size) return;

        // 🔹 0번은 Root 고정이므로, 절대 to=0 으로 보내지 않는다.
        if (to == 0) return;

        _slotsProp.MoveArrayElement(from, to);
        _so.ApplyModifiedProperties();

        // 선택 인덱스 업데이트 + 경로 재구성
        _slotsList.index = to;
        _selectedSlotIndex = to;
        RebuildSlotPathForSelected(to);

        Repaint();
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

        // 🔹 현재 부모 슬롯 index (Breadcrumb의 마지막)
        int currentParentIndex = -1;
        if (_slotPath != null && _slotPath.Count > 0)
            currentParentIndex = _slotPath[_slotPath.Count - 1];

        // 🔹 1) 순환 구조(조상으로 되돌아가는 링크) 방지
        if (WouldCreateCycleFromCurrentPath(slotName))
        {
            EditorUtility.DisplayDialog(
                "Invalid Slot Link",
                $"'{slotName}' 슬롯은 현재 Slot 경로의 조상 슬롯과 이름이 같아서\n" +
                "순환 구조가 생길 수 있습니다.\n\n" +
                "Root → ... → " + slotName + " → ... → " + slotName + " 형태는 허용되지 않습니다.",
                "OK"
            );
            return;
        }

        // 🔹 2) 멀티 부모 구조 경고 (직계 조상은 아니지만, 이미 다른 부모가 있는 경우)
        if (currentParentIndex >= 0 &&
            HasOtherParentForSlotName(slotName, currentParentIndex, out int otherParentIndex))
        {
            string currentParentName = GetSlotNameByIndex(currentParentIndex);
            string otherParentName = GetSlotNameByIndex(otherParentIndex);

            EditorUtility.DisplayDialog(
                "Ambiguous Slot Graph",
                $"슬롯 '{slotName}' 은 이미 다른 슬롯에서도 하위 슬롯으로 사용 중입니다.\n\n" +
                $"- 기존 부모: '{otherParentName}'\n" +
                $"- 현재 부모: '{currentParentName}'\n\n" +
                "이렇게 하나의 Slot을 여러 부모가 공유하면,\n" +
                "Slot Path 표시가 예상과 다르게 보이거나 구조가 복잡해질 수 있습니다.",
                "OK"
            );
            // ⚠️ 여기서는 '경고만' 하고 계속 진행 (원하면 나중에 여기서 return; 으로 차단도 가능)
        }

        // 🔹 3) 실제 child 슬롯 찾기 / 생성
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

        // 🔹 4) 경로에 child 추가 후 해당 Slot의 Widgets 표시
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

                bool hasAnySlot =
                    _slotsProp != null &&
                    _slotsProp.arraySize > 0;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    // 🔹 Orphan 정리 버튼
                    EditorGUI.BeginDisabledGroup(!hasAnySlot || _asset == null);
                    if (GUILayout.Button("Clean Unlinked Slots", GUILayout.Width(180f)))
                    {
                        CleanupOrphanSlots();
                    }
                    EditorGUI.EndDisabledGroup();

                    GUILayout.Space(4f);

                    // 🔹 기존 Enable All Widgets 버튼
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

            string label;

            if (idx == 0)
            {
                // 🔹 Root 슬롯: 사용자가 입력한 Root Slot Id를 그대로 사용
                // 비어 있으면 "(root)"로 표시
                label = string.IsNullOrWhiteSpace(rawName)
                    ? "(root)"
                    : rawName.Trim();
            }
            else
            {
                // 🔹 나머지 슬롯은 기존 규칙 유지 (Slot 0, Slot 1 같은 기본 이름 숨기기)
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

    private bool WouldCreateCycleFromCurrentPath(string targetSlotName)
    {
        if (_slotsProp == null) return false;

        targetSlotName = (targetSlotName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(targetSlotName))
            return false;

        if (_slotPath == null || _slotPath.Count == 0)
            return false;

        // 현재 Breadcrumb 경로에 있는 모든 Slot의 slotName을 검사
        for (int i = 0; i < _slotPath.Count; i++)
        {
            int slotIndex = _slotPath[i];
            if (slotIndex < 0 || slotIndex >= _slotsProp.arraySize)
                continue;

            var slotProp = _slotsProp.GetArrayElementAtIndex(slotIndex);
            var nameProp = slotProp.FindPropertyRelative("slotName");
            string name = (nameProp != null ? nameProp.stringValue : string.Empty)?.Trim();

            if (string.IsNullOrEmpty(name))
                continue;

            if (string.Equals(name, targetSlotName, StringComparison.Ordinal))
                return true; // 조상으로 되돌아가는 링크 → 잠재적 사이클
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
            // 지금 열고 있는 부모 슬롯(현재 경로의 마지막)은 제외
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
    
    private bool[] BuildHasParentFlags()
    {
        if (_slotsProp == null)
            return System.Array.Empty<bool>();

        int slotCount = _slotsProp.arraySize;
        var hasParent = new bool[slotCount];

        // Slot 위젯의 slotId -> SlotSpec.slotName 매칭으로 parent 정보 구성
        for (int i = 0; i < slotCount; i++)
        {
            var slot = _slotsProp.GetArrayElementAtIndex(i);
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

                // id 와 같은 slotName 을 가진 Slot 이 있으면 그 Slot 은 "부모가 있다"
                for (int j = 0; j < slotCount; j++)
                {
                    var childSlot     = _slotsProp.GetArrayElementAtIndex(j);
                    var childNameProp = childSlot.FindPropertyRelative("slotName");
                    string childName  = (childNameProp != null ? childNameProp.stringValue : string.Empty).Trim();
                    if (string.IsNullOrEmpty(childName)) continue;

                    if (string.Equals(childName, id, System.StringComparison.Ordinal))
                    {
                        hasParent[j] = true;
                    }
                }
            }
        }

        return hasParent;
    }
    
    private void CleanupOrphanSlots()
    {
        if (_slotsProp == null || _slotsProp.arraySize == 0)
        {
            EditorUtility.DisplayDialog(
                "Clean Orphan Slots",
                "정리할 Slot이 없습니다.",
                "OK");
            return;
        }

        int slotCount = _slotsProp.arraySize;
        var hasParent = BuildHasParentFlags();

        var toDelete = new List<int>();

        // 🔹 0번은 항상 진짜 Root로 보호.
        for (int i = 1; i < slotCount; i++)
        {
            // 부모가 전혀 없으면 orphan
            if (!hasParent[i])
                toDelete.Add(i);
        }

        if (toDelete.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Clean Orphan Slots",
                "부모가 없는 Slot은 없습니다.",
                "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "Clean Orphan Slots",
                $"부모가 없는 Slot {toDelete.Count}개를 삭제합니다." +
                "\n\n정말 계속할까요?",
                "Delete",
                "Cancel"))
        {
            return;
        }

        // 인덱스 밀림 방지를 위해 뒤에서부터 삭제
        toDelete.Sort();
        for (int idx = toDelete.Count - 1; idx >= 0; idx--)
        {
            int slotIndex = toDelete[idx];
            _slotsProp.DeleteArrayElementAtIndex(slotIndex);
        }

        _so.ApplyModifiedProperties();

        // 🔹 루트는 무조건 0번으로 취급
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

            // 혹시 모르니 Root 재생성 (이론상 안 올 것)
            EnsureRootSlotExists();
            _selectedSlotIndex = 0;
            SetRootSlot(0);
        }

        BuildSlotsList();
        Repaint();
    }


}
#endif