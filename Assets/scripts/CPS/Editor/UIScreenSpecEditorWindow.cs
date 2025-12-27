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

    // 현재 prefab에서 발견된 UISlot id 목록 캐시
    private string[] _slotIdOptions = Array.Empty<string>();
    private GameObject _cachedTemplatePrefab;

    private ReorderableList _slotsList;
    private ReorderableList _widgetsList;

    private Vector2 _slotsScroll;
    private Vector2 _widgetsScroll;

    private int _selectedSlotIndex = -1;

    // 🔹 위젯별 접힘/펼침 상태 (SerializedProperty.propertyPath 기준)
    private readonly Dictionary<string, bool> _widgetFoldoutStates = new();

    private readonly List<string> _lastIssues = new List<string>();
    private Vector2 _issuesScroll;


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
        _lastIssues.Clear();
        _issuesScroll = Vector2.zero;

        _so = new SerializedObject(_asset);

        _specProp = _so.FindProperty("spec");
        if (_specProp == null)
        {
            Debug.LogError("[UIScreenSpecEditor] 'spec' property not found on UIScreenSpecAsset.");
            return;
        }

        _slotsProp = _specProp.FindPropertyRelative("slots");

        // 여기서 한 번
        RefreshSlotIdOptionsFromPrefab();

        BuildSlotsList();
        BuildWidgetsList();
    }

    private void BuildSlotsList()
    {
        _slotsList = new ReorderableList(_so, _slotsProp, true, true, true, true);

        _slotsList.drawHeaderCallback = rect =>
            EditorGUI.LabelField(rect, "Slots");

        _slotsList.onSelectCallback = list =>
        {
            _selectedSlotIndex = list.index;
            BuildWidgetsList();
        };

        _slotsList.onAddCallback = list =>
        {
            int i = _slotsProp.arraySize;
            _slotsProp.InsertArrayElementAtIndex(i);
            var slot = _slotsProp.GetArrayElementAtIndex(i);

            var options = _slotIdOptions;
            string initialName =
                (options != null && options.Length > 0)
                    ? options[0]
                    : string.Empty; // 이제 Header/Body/Footer 없이 비워두는 게 맞음

            slot.FindPropertyRelative("slotName").stringValue = initialName;

            var widgets = slot.FindPropertyRelative("widgets");
            widgets.ClearArray();

            _so.ApplyModifiedProperties();
            _selectedSlotIndex = i;
            BuildWidgetsList();
        };

        // 🔹 여기 추가
        _slotsList.onRemoveCallback = list =>
        {
            if (list.index < 0 || list.index >= _slotsProp.arraySize)
                return;

            // 현재 선택된 슬롯이 지워지는 상황 고려
            int removeIndex = list.index;

            _slotsProp.DeleteArrayElementAtIndex(removeIndex);
            _so.ApplyModifiedProperties();

            // 슬롯이 하나도 안 남았으면
            if (_slotsProp.arraySize == 0)
            {
                _selectedSlotIndex = -1;
                _widgetsList = null;
                return;
            }

            // 남아있는 슬롯 범위 내에서 선택 인덱스 다시 잡기
            int newIndex = Mathf.Clamp(removeIndex, 0, _slotsProp.arraySize - 1);
            _selectedSlotIndex = newIndex;

            // 새 슬롯의 widgets 기준으로 ReorderableList 재생성
            BuildWidgetsList();
            Repaint();
        };

        _slotsList.elementHeightCallback = index =>
            EditorGUIUtility.singleLineHeight + 6f;

        _slotsList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            rect.y += 2f;

            // 좌우 패딩 살짝
            const float horizontalPadding = 4f;
            rect.x += horizontalPadding;
            rect.width -= horizontalPadding * 2f;

            rect.height = EditorGUIUtility.singleLineHeight;

            var slot = _slotsProp.GetArrayElementAtIndex(index);
            var nameProp = slot.FindPropertyRelative("slotName");
            var widgetsProp = slot.FindPropertyRelative("widgets");

            int widgetCount = widgetsProp != null ? widgetsProp.arraySize : 0;

            const float leftWidth = 55f; // Slot 0 (2) 영역
            const float rightBlankWidth = 8f; // 살짝만 여유
            const float gap = 4f;

// 🔹 왼쪽: 슬롯 인덱스 + 위젯 개수 표시
            var leftRect = new Rect(rect.x, rect.y, leftWidth, rect.height);
            EditorGUI.LabelField(leftRect, $"Slot {index} ({widgetCount})");

// 🔹 가운데: Popup만 넓게
            float usableWidth = rect.width - leftWidth - rightBlankWidth - gap * 2f;
            if (usableWidth < 0) usableWidth = 0;

            float popupX = rect.x + leftWidth + gap;
            var popupRect = new Rect(popupX, rect.y, usableWidth, rect.height);
// textRect는 이제 필요 없음

            var options = _slotIdOptions;

            if (options == null || options.Length == 0)
            {
                EditorGUI.LabelField(popupRect, "(No UISlot in Prefab)");
            }
            else
            {
                int popupIndex = IndexOf(options, nameProp.stringValue);
                if (popupIndex < 0) popupIndex = 0;

                int newIndex = EditorGUI.Popup(popupRect, popupIndex, options);
                if (newIndex >= 0 && newIndex < options.Length)
                    nameProp.stringValue = options[newIndex];
            }

            //직접 타이핑 하기를 원한다면.
            //nameProp.stringValue = EditorGUI.TextField(textRect, nameProp.stringValue);
        };
    }

    private void BuildWidgetsList()
    {
        _widgetsList = null;

        if (_selectedSlotIndex < 0 || _selectedSlotIndex >= _slotsProp.arraySize)
            return;

        var slot = _slotsProp.GetArrayElementAtIndex(_selectedSlotIndex);
        var widgetsProp = slot.FindPropertyRelative("widgets");

        _widgetsList = new ReorderableList(_so, widgetsProp, true, true, true, true);

        _widgetsList.drawElementBackgroundCallback = (rect, index, isActive, isFocused) =>
        {
            const float padding = 2f;

            // 살짝 안쪽으로 줄인 영역만 배경 처리
            Rect bgRect = new Rect(
                rect.x + padding,
                rect.y + padding,
                rect.width - padding * 2f,
                rect.height - padding * 2f
            );

            // 공통 배경 컬러 (선택 전/후만 농도 차이)
            Color normalBg = new Color(0.3f, 0.3f, 0.3f, 0.5f); // 기본
            Color selectedBg = new Color(0f, 0f, 0f, 0.24f); // 선택 시 약간 더 진하게

            EditorGUI.DrawRect(bgRect, isActive ? selectedBg : normalBg);
        };

        _widgetsList.drawHeaderCallback = rect =>
        {
            var currentSlot = _slotsProp.GetArrayElementAtIndex(_selectedSlotIndex);
            var nameProp = currentSlot.FindPropertyRelative("slotName");
            EditorGUI.LabelField(rect, $"Widgets (Slot: {nameProp.stringValue})");
        };

        _widgetsList.onRemoveCallback = list =>
        {
            if (list.index < 0) return;
            if (list.index >= widgetsProp.arraySize) return;

            widgetsProp.DeleteArrayElementAtIndex(list.index);
            _so.ApplyModifiedProperties();
            BuildWidgetsList(); // 선택 인덱스 갱신용 (선택)
            Repaint();
        };

        _widgetsList.elementHeightCallback = index =>
        {
            float lineH = EditorGUIUtility.singleLineHeight;
            float vGap = 2f;
            float borderPadding = 2f;

            if (widgetsProp == null || index < 0 || index >= widgetsProp.arraySize)
                return lineH + 2f * borderPadding;

            var w = widgetsProp.GetArrayElementAtIndex(index);

            // 🔹 접힘 상태 확인
            string foldKey = w.propertyPath;
            bool expanded = true;
            _widgetFoldoutStates.TryGetValue(foldKey, out expanded);

            if (!expanded)
            {
                // 접혀 있을 때: 헤더 한 줄 정도만 보이게
                int collapsedLines = 1; // Foldout + Enabled + Name + Type 한 줄
                float collapsedHeight = collapsedLines * (lineH + vGap) + vGap;
                return collapsedHeight + borderPadding * 2f + 4f;
            }

            int lines = 0;

            // 1줄: Name + Type
            lines += 1;

            // 2줄: Text 멀티라인
            lines += 2;

            // Route + Prefab
            var typeProp = w.FindPropertyRelative("widgetType");
            var widgetType = (WidgetType)typeProp.enumValueIndex;
            lines += (widgetType == WidgetType.Button) ? 2 : 1;

            // Layout Mode (항상 1줄)
            lines += 1;

            // OverrideInSlot일 때만 추가 5줄 (AnchorMin, AnchorMax, Pivot, Size, Position)
            var rectModeProp = w.FindPropertyRelative("rectMode");
            var rectMode = (WidgetRectMode)rectModeProp.enumValueIndex;
            if (rectMode == WidgetRectMode.OverrideInSlot)
            {
                lines += 5;
            }

            // 🔹 타입별 추가 옵션 라인수
            switch (widgetType)
            {
                case WidgetType.Image:
                    // [Image Options] 헤더 + Sprite + Color + SetNativeSize
                    lines += 4;
                    break;
                case WidgetType.Toggle:
                    // [Toggle Options] 헤더 + Initial + Interactable
                    lines += 3;
                    break;
                case WidgetType.Slider:
                    // [Slider Options] 헤더 + Min + Max + Initial + WholeNumbers
                    lines += 5;
                    break;
            }

            float contentHeight = lines * (lineH + vGap) + vGap;
            return contentHeight + borderPadding * 2f + 4f;
        };

        _widgetsList.onAddCallback = list =>
        {
            if (widgetsProp == null) return;

            int insertIndex = widgetsProp.arraySize;
            widgetsProp.InsertArrayElementAtIndex(insertIndex);

            var newElem = widgetsProp.GetArrayElementAtIndex(insertIndex);
            ResetWidgetSpecDefaults(newElem, insertIndex);

            _so.ApplyModifiedProperties();
            BuildWidgetsList();
            if (_widgetsList != null)
                _widgetsList.index = insertIndex;

            Repaint();
        };

        _widgetsList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var e = Event.current;

            // 전체 element 구간 살짝 축소해서 배경/테두리용 rect 만들기
            const float borderPadding = 2f;
            var borderRect = new Rect(
                rect.x + borderPadding,
                rect.y + borderPadding,
                rect.width - borderPadding * 2f,
                rect.height - borderPadding * 2f
            );

            // 🔹 배경 살짝 깔기 (아주 옅은 회색/어두운 색)
            EditorGUI.DrawRect(borderRect, new Color(0.25f, 0.25f, 0.25f, 0.3f));

            // 🔹 아래쪽 경계선
            var bottomLine = new Rect(
                borderRect.x,
                borderRect.yMax - 1f,
                borderRect.width,
                1f
            );
            //EditorGUI.DrawRect(bottomLine, new Color(0.3f, 0.3f, 0.3f, 0.5f));

            // 이제 실제 컨텐츠용 rect를 약간 더 안쪽으로
            float vGap = 2f;
            const float horizontalPadding = 6f;

            rect = borderRect; // borderRect 안쪽을 기준으로 쓸 거야
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

            // 🔹 우클릭 메뉴 (Add / Delete) – 기존에 쓰던 거 있으면 그대로 유지
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
                    BuildWidgetsList();
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
                    BuildWidgetsList();
                    Repaint();
                });

                menu.ShowAsContext();
                e.Use();
            }

            // === 헤더: Foldout + Enabled 토글 + Name + Type ===
            string foldKey = w.propertyPath;
            bool expanded = true;
            _widgetFoldoutStates.TryGetValue(foldKey, out expanded);

// Foldout 아이콘
            var foldoutRect = new Rect(rect.x, y, 14f, lineH);
            expanded = EditorGUI.Foldout(foldoutRect, expanded, GUIContent.none);
            _widgetFoldoutStates[foldKey] = expanded;

            float x = foldoutRect.xMax + 2f;

// Enabled 토글 (실제 저장은 disabled)
            var toggleRect = new Rect(x, y, 18f, lineH);
            bool enabled = disabledProp != null ? !disabledProp.boolValue : true;
            enabled = EditorGUI.Toggle(toggleRect, enabled);
            if (disabledProp != null)
                disabledProp.boolValue = !enabled;

            x = toggleRect.xMax + 4f;

// ---- Type 드롭다운 고정 폭 ----
            const float typeWidth = 70f; // 드롭다운 고정 폭 (원하면 80~110 사이로 조절 가능)
            const float gap = 4f;

// 오른쪽 끝에서 고정 폭만큼 확보
            float typeX = rect.x + rect.width - typeWidth;
            var typeRect = new Rect(typeX, y, typeWidth, lineH);

// Name 전체 가로 폭 (남은 공간 전부)
            float nameTotalWidth = typeX - x - gap;
            if (nameTotalWidth < 60f) nameTotalWidth = 60f;

// Name 라벨 + 필드 쪼개기
            const float nameLabelWidth = 80f;
            const float nameInnerGap = 2f;

            var nameLabelRect = new Rect(x, y, nameLabelWidth, lineH);
            var nameFieldRect = new Rect(
                nameLabelRect.xMax + nameInnerGap,
                y,
                nameTotalWidth - (nameLabelWidth + nameInnerGap),
                lineH
            );

// 실제 그리기
            EditorGUI.LabelField(nameLabelRect, "Name (editor)");
            nameProp.stringValue = EditorGUI.TextField(nameFieldRect, GUIContent.none, nameProp.stringValue);
            EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);

            y += lineH + vGap;

// 접혀 있으면 여기서 조기 리턴 (헤더만 표시)
            if (!expanded)
                return;

            var widgetType = (WidgetType)typeProp.enumValueIndex;

            // === 2줄: Text (멀티라인) ===
            int textLines = 2; // 살짝만 멀티라인
            float textHeight = (lineH + 2f) * textLines;

            var textRect = new Rect(rect.x, y, rect.width, textHeight);
            textProp.stringValue = EditorGUI.TextArea(textRect, textProp.stringValue, EditorStyles.textArea);
            y += textHeight + vGap;

            if (widgetType == WidgetType.Button)
            {
                // === Route ===
                var routeRect = new Rect(rect.x, y, rect.width, lineH);
                routeProp.stringValue = EditorGUI.TextField(routeRect, "OnClick Route", routeProp.stringValue);
                y += lineH + vGap;

                // === Prefab Override ===
                var prefabRect = new Rect(rect.x, y, rect.width, lineH);
                EditorGUI.PropertyField(prefabRect, prefabProp, new GUIContent("Prefab Override"));
                y += lineH + vGap;
            }
            else
            {
                routeProp.stringValue = string.Empty;

                var prefabRect = new Rect(rect.x, y, rect.width, lineH);
                EditorGUI.PropertyField(prefabRect, prefabProp, new GUIContent("Prefab Override"));
                y += lineH + vGap;
            }

            // 1) RectMode 드롭다운
            var layoutModeRect = new Rect(rect.x, y, rect.width, lineH);
            EditorGUI.PropertyField(layoutModeRect, rectModeProp, new GUIContent("Layout Mode"));
            y += lineH + vGap;

// enum 값 읽기
            var rectMode = (WidgetRectMode)rectModeProp.enumValueIndex;

// 2) OverrideInSlot일 때만 상세값 노출
            if (rectMode == WidgetRectMode.OverrideInSlot)
            {
                float labelWidth = 90f; // 라벨이 차지할 폭
                float fieldGap = 4f; // 라벨과 값 사이 간격
                float rowHeight = lineH; // 한 줄 높이(그냥 singleLineHeight로 유지)

                Rect MakeRowRect() => new Rect(rect.x, y, rect.width, rowHeight);

                // --- Anchor Min ---
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

                // --- Anchor Max ---
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

                // --- Pivot ---
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

                // --- Size ---
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

                // --- Position ---
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

                switch (widgetType)
                {
                    case WidgetType.Image:
                    {
                        // 헤더
                        var headerRect = new Rect(rect.x, y, rect.width, lineH);
                        EditorGUI.LabelField(headerRect, "[Image Options]", EditorStyles.miniBoldLabel);
                        y += lineH + vGap;

                        // Sprite
                        var spriteRect = new Rect(rect.x, y, rect.width, lineH);
                        EditorGUI.PropertyField(spriteRect, imageSpriteProp, new GUIContent("Sprite"));
                        y += lineH + vGap;

                        // Color
                        var colorRect = new Rect(rect.x, y, rect.width, lineH);
                        EditorGUI.PropertyField(colorRect, imageColorProp, new GUIContent("Color"));
                        y += lineH + vGap;

                        // Set Native Size
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
                }
            }
        };
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);

        var newAsset =
            (UIScreenSpecAsset)EditorGUILayout.ObjectField("Spec Asset", _asset, typeof(UIScreenSpecAsset), false);

        if (newAsset != _asset)
        {
            _lastIssues.Clear();
            _issuesScroll = Vector2.zero;

            if (newAsset == null)
            {
                _asset = null;
                _so = null;
                return;
            }

            Bind(newAsset);
        }

        if (_asset != null && _so == null)
        {
            Bind(_asset);
        }

        if (_asset == null || _so == null)
        {
            EditorGUILayout.HelpBox("UIScreenSpecAsset 를 선택하거나 드래그해서 열어주세요.\n(Project 창에서 Spec Asset 클릭 → 자동 바인딩됨)",
                MessageType.Info);
            return;
        }

        _so.Update();

        var prefabProp = _specProp.FindPropertyRelative("templatePrefab");

        EditorGUILayout.LabelField("Template", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(prefabProp, new GUIContent("Template Prefab"));
        if (EditorGUI.EndChangeCheck())
        {
            _so.ApplyModifiedProperties();
            RefreshSlotIdOptionsFromPrefab(force: true);
        }

        EditorGUILayout.Space(8);

        // 좌/우 분할
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.4f)))
                //using (new EditorGUILayout.VerticalScope(GUILayout.Width(180f)))
            {
                _slotsScroll = EditorGUILayout.BeginScrollView(_slotsScroll);
                _slotsList?.DoLayoutList();
                EditorGUILayout.EndScrollView();


                // 🔹 Validate 결과 패널
                if (_lastIssues.Count > 0)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

                    _issuesScroll = EditorGUILayout.BeginScrollView(_issuesScroll, GUILayout.Height(150));

                    foreach (var msg in _lastIssues)
                    {
                        MessageType mt;
                        if (msg.StartsWith("[Error]")) mt = MessageType.Error;
                        else if (msg.StartsWith("[Warn]")) mt = MessageType.Warning;
                        else mt = MessageType.Info;

                        EditorGUILayout.HelpBox(msg, mt);
                    }

                    EditorGUILayout.EndScrollView();
                }

                DrawValidateButtons();
            }

            GUILayout.Space(4f);

            // 오른쪽: Widgets 영역
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                // 🔹 위젯 리스트는 스크롤 안에서만
                _widgetsScroll = EditorGUILayout.BeginScrollView(_widgetsScroll);

                // 슬롯 개수 방어
                if (_slotsProp != null)
                {
                    int slotCount = _slotsProp.arraySize;
                    if (slotCount == 0)
                    {
                        _selectedSlotIndex = -1;
                        _widgetsList = null;
                    }
                    else if (_selectedSlotIndex < 0 || _selectedSlotIndex >= slotCount)
                    {
                        _selectedSlotIndex = Mathf.Clamp(_selectedSlotIndex, 0, slotCount - 1);
                        BuildWidgetsList();
                    }
                }

                if (_widgetsList == null)
                {
                    EditorGUILayout.HelpBox("좌측에서 Slot을 선택하세요.", MessageType.None);
                }
                else
                {
                    _widgetsList.DoLayoutList();
                }

                EditorGUILayout.EndScrollView();

                // 🔹 스크롤뷰 밖, 오른쪽 아래에 버튼 배치
                EditorGUILayout.Space(4f);

                bool hasSlotSelected =
                    _slotsProp != null &&
                    _slotsProp.arraySize > 0 &&
                    _selectedSlotIndex >= 0 &&
                    _selectedSlotIndex < _slotsProp.arraySize;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace(); // 오른쪽 정렬

                    EditorGUI.BeginDisabledGroup(!hasSlotSelected || _asset == null);
                    if (GUILayout.Button("Enable All Widgets", GUILayout.Width(180f)))
                    {
                        // 🔹 확인 팝업
                        bool ok = EditorUtility.DisplayDialog(
                            "Enable All Widgets",
                            "Enable all disabled widgets in every slot?",
                            "Yes, enable all",
                            "Cancel"
                        );

                        if (ok)
                        {
                            EnableAllDisabledWidgets(_asset.spec);
                            _so.Update();
                            EditorUtility.SetDirty(_asset);

                            // Validate 다시 실행해서 결과 패널 갱신
                            _lastIssues.Clear();
                            var issues = ValidateSpec(_asset.spec);
                            if (issues.Count == 0)
                                _lastIssues.Add("[Info] OK (no issues after Enable All Widgets)");
                            else
                                _lastIssues.AddRange(issues);

                            _issuesScroll = Vector2.zero;
                            BuildWidgetsList();
                            Repaint();
                        }
                    }

                    EditorGUI.EndDisabledGroup();
                }
            }
        }

        _so.ApplyModifiedProperties();
    }

    private void RefreshSlotIdOptionsFromPrefab(bool force = false)
    {
        if (_asset == null)
        {
            _slotIdOptions = Array.Empty<string>();
            _cachedTemplatePrefab = null;
            return;
        }

        var spec = _asset.spec;
        var prefab = spec != null ? spec.templatePrefab : null;

        if (prefab == null)
        {
            _slotIdOptions = Array.Empty<string>();
            _cachedTemplatePrefab = null;
            return;
        }

        // prefab 레퍼런스가 같고, 이미 뭔가 목록이 있다면 건너뛰기 (자동 호출용)
        if (!force && _cachedTemplatePrefab == prefab && _slotIdOptions.Length > 0)
            return;

        _cachedTemplatePrefab = prefab;

        var slots = prefab.GetComponentsInChildren<UISlot>(true);
        var ids = new List<string>();

        foreach (var slot in slots)
        {
            if (slot == null) continue;
            var id = (slot.id ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(id)) continue;
            if (!ids.Contains(id))
                ids.Add(id);
        }

        _slotIdOptions = ids.ToArray();
    }

    private void DrawValidateButtons()
    {
        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Refresh Slots"))
            {
                RefreshSlotIdOptionsFromPrefab(force: true);
                Repaint();
            }

            if (GUILayout.Button("Validate"))
            {
                _lastIssues.Clear();
                if (_asset != null)
                {
                    var issues = ValidateSpec(_asset.spec);
                    if (issues.Count == 0)
                    {
                        _lastIssues.Add("[Info] OK (no issues)");
                    }
                    else
                    {
                        _lastIssues.AddRange(issues);
                    }
                }
            }
        }
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

        // 타입/기본 텍스트
        typeProp.enumValueIndex = (int)WidgetType.Text;
        nameTagProp.stringValue = $"Widget {index}";
        textProp.stringValue = string.Empty;
        routeProp.stringValue = string.Empty;
        prefabOverrideProp.objectReferenceValue = null;

        // Rect 모드 & 기본 값들
        rectModeProp.enumValueIndex = (int)WidgetRectMode.UseSlotLayout;

        anchorMinProp.vector2Value = new Vector2(0.5f, 0.5f);
        anchorMaxProp.vector2Value = new Vector2(0.5f, 0.5f);
        pivotProp.vector2Value = new Vector2(0.5f, 0.5f);
        anchoredPosProp.vector2Value = Vector2.zero;
        sizeDeltaProp.vector2Value = new Vector2(300f, 80f);

        if (disabledProp != null)
            disabledProp.boolValue = false; // 새로 만든 위젯은 기본적으로 활성

        // 타입별 옵션들 기본값 (지금 있던 코드 그대로)
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

    // templatePrefab이 CPS-UI용 프리팹이 맞는지 (UIScreen 존재 하는지)
    // templatePrefab 없는데 slots만 있는지
    // Prefab 안의 UISlot.id 수집 + 중복 id 경고
    // Spec의 slotName이 실제 Prefab의 UISlot.id와 매칭되는지
    // slots null/empty
    // 각 slot null인지, slotName 비었는지
    // slot.widgets null인지
    // 슬롯 내부에서 nameTag 중복 경고
    // 위젯이 전부 disabled면 “활성 위젯 없음” 경고
    // disabled == true면 나머지 검사는 스킵
    // Button: route 없으면 Error
    // Image: prefab도, sprite도 없으면 Warning
    // Slider: min/max 역전이면 Error, initialValue 범위 밖이면 Warning
    // Rect Override: anchorMin > anchorMax면 Error, 0~1 밖이면 Warning
    // prefabOverride가 위젯 타입이 요구하는 컴포넌트를 실제로 들고 있는지
    private static List<string> ValidateSpec(UIScreenSpec s)
    {
        var issues = new List<string>();

        // ---- 0) templatePrefab 관련 ----
        if (s.templatePrefab == null)
        {
            if (s.slots != null && s.slots.Count > 0)
            {
                issues.Add("[Error] templatePrefab is null but slots are defined");
            }
            // templatePrefab 없이 '추상 스펙'으로 쓰고 싶다면, 여기서 Warning 으로 완화할 수도 있음.
        }
        else
        {
            // 1) UIScreen 컴포넌트 존재 여부
            if (s.templatePrefab.GetComponent<UIScreen>() == null)
            {
                issues.Add("[Error] templatePrefab has no UIScreen component");
            }
        }

        // ---- 2) Prefab 내 UISlot id 수집 ----
        HashSet<string> prefabSlotIds = null;

        if (s.templatePrefab != null)
        {
            var slotsInPrefab = s.templatePrefab.GetComponentsInChildren<UISlot>(true);
            prefabSlotIds = new HashSet<string>();
            foreach (var slot in slotsInPrefab)
            {
                if (slot == null) continue;
                var id = (slot.id ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(id))
                {
                    if (!prefabSlotIds.Add(id))
                    {
                        issues.Add($"[Warn] Duplicate UISlot id '{id}' found in templatePrefab");
                    }
                }
            }
        }

        // ---- 3) Slot 리스트 기본 체크 ----
        if (s.slots == null || s.slots.Count == 0)
            issues.Add("[Error] slots is empty");

        if (s.slots == null)
            return issues;

        // slotName 중복 체크용
        var slotNameSet = new HashSet<string>();

        for (int i = 0; i < s.slots.Count; i++)
        {
            var slot = s.slots[i];
            if (slot == null)
            {
                issues.Add($"[Error] slots[{i}] is null");
                continue;
            }

            string slotName = slot.slotName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(slotName))
            {
                issues.Add($"[Error] slots[{i}].slotName is empty");
            }
            else
            {
                // 슬롯 이름 중복
                if (!slotNameSet.Add(slotName))
                {
                    issues.Add($"[Warn] Duplicate slotName '{slotName}' in slots (index {i})");
                }

                // prefab 에 실제로 존재하는 UISlot 인지
                if (prefabSlotIds != null && !prefabSlotIds.Contains(slotName))
                {
                    issues.Add($"[Error] slots[{i}].slotName '{slotName}' does not exist in templatePrefab UISlots");
                }
            }

            if (slot.widgets == null)
            {
                issues.Add($"[Error] slots[{i}].widgets is null");
                continue;
            }

            // ---- 4) 위젯 검증 ----
            var nameTagSet = new HashSet<string>();
            bool hasActiveWidget = false;

            for (int w = 0; w < slot.widgets.Count; w++)
            {
                var widget = slot.widgets[w];
                if (widget == null)
                {
                    issues.Add($"[Error] slots[{i}].widgets[{w}] is null");
                    continue;
                }

                // nameTag 중복 체크 (슬롯 내)
                string nameTag = widget.nameTag ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(nameTag))
                {
                    if (!nameTagSet.Add(nameTag))
                    {
                        issues.Add(
                            $"[Warn] Duplicate nameTag '{nameTag}' in slots[{i}].widgets (index {w})");
                    }
                }

                // 비활성 위젯이면 이후 검증 스킵
                if (widget.disabled)
                    continue;

                hasActiveWidget = true;

                // ---- 4-1) prefabOverride 타입 호환성 체크 ----
                if (widget.prefabOverride != null)
                {
                    var go = widget.prefabOverride;

                    switch (widget.widgetType)
                    {
                        case WidgetType.Button:
                            if (go.GetComponentInChildren<UnityEngine.UI.Button>(true) == null)
                            {
                                issues.Add(
                                    $"[Warn] Button widget prefabOverride has no Button component: slots[{i}].widgets[{w}] (nameTag='{widget.nameTag}')");
                            }

                            break;

                        case WidgetType.Text:
                            if (go.GetComponentInChildren<TMPro.TMP_Text>(true) == null)
                            {
                                issues.Add(
                                    $"[Warn] Text widget prefabOverride has no TMP_Text component: slots[{i}].widgets[{w}] (nameTag='{widget.nameTag}')");
                            }

                            break;

                        case WidgetType.Image:
                            if (go.GetComponentInChildren<UnityEngine.UI.Image>(true) == null)
                            {
                                issues.Add(
                                    $"[Warn] Image widget prefabOverride has no Image component: slots[{i}].widgets[{w}] (nameTag='{widget.nameTag}')");
                            }

                            break;
                    }
                }

                // ---- 4-2) 타입별 필수 값 체크 ----

                // Button: route 필수
                if (widget.widgetType == WidgetType.Button &&
                    string.IsNullOrWhiteSpace(widget.onClickRoute))
                {
                    issues.Add(
                        $"[Error] Button route missing: slots[{i}].widgets[{w}] (nameTag='{widget.nameTag}')");
                }

                // Image: sprite 또는 prefabOverride 둘 다 없으면 경고
                if (widget.widgetType == WidgetType.Image)
                {
                    bool hasPrefab = widget.prefabOverride != null;
                    bool hasSprite = widget.imageSprite != null;

                    if (!hasPrefab && !hasSprite)
                    {
                        issues.Add(
                            $"[Warn] Image widget has neither prefabOverride nor imageSprite: slots[{i}].widgets[{w}] (nameTag='{widget.nameTag}')");
                    }
                }

                // Slider: min/max/initial 검증
                if (widget.widgetType == WidgetType.Slider)
                {
                    if (widget.sliderMax <= widget.sliderMin)
                    {
                        issues.Add(
                            $"[Error] Slider min/max invalid (min >= max) in slots[{i}].widgets[{w}] (nameTag='{widget.nameTag}')");
                    }

                    if (widget.sliderInitialValue < widget.sliderMin ||
                        widget.sliderInitialValue > widget.sliderMax)
                    {
                        issues.Add(
                            $"[Warn] Slider initialValue out of range [{widget.sliderMin}, {widget.sliderMax}] " +
                            $"in slots[{i}].widgets[{w}] (nameTag='{widget.nameTag}')");
                    }
                }

                // Rect Override 모드일 때 Anchor/Size 검사
                if (widget.rectMode == WidgetRectMode.OverrideInSlot)
                {
                    if (widget.anchorMin.x > widget.anchorMax.x ||
                        widget.anchorMin.y > widget.anchorMax.y)
                    {
                        issues.Add(
                            $"[Error] Rect anchorMin > anchorMax in slots[{i}].widgets[{w}] (nameTag='{widget.nameTag}')");
                    }

                    if (widget.anchorMin.x < 0f || widget.anchorMin.x > 1f ||
                        widget.anchorMax.x < 0f || widget.anchorMax.x > 1f ||
                        widget.anchorMin.y < 0f || widget.anchorMin.y > 1f ||
                        widget.anchorMax.y < 0f || widget.anchorMax.y > 1f)
                    {
                        issues.Add(
                            $"[Warn] Rect anchor out of [0,1] range in slots[{i}].widgets[{w}] (nameTag='{widget.nameTag}')");
                    }
                }
            }

            // 이 슬롯 안에 활성 위젯이 하나도 없을 때
            if (!hasActiveWidget)
            {
                issues.Add($"[Warn] slots[{i}] ('{slotName}') has no active widgets (all disabled or empty)");
            }
        }

        return issues;
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

    private static int IndexOf(string[] arr, string v)
    {
        if (arr == null) return -1;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] == v)
                return i;
        return -1;
    }
}
#endif