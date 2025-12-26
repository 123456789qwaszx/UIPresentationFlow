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
    
    // 🔹 추가: 현재 prefab에서 발견된 UISlot id 목록 캐시
    private string[] _slotIdOptions = Array.Empty<string>();
    private GameObject _cachedTemplatePrefab;

    private ReorderableList _slotsList;
    private ReorderableList _widgetsList;

    private Vector2 _slotsScroll;
    private Vector2 _widgetsScroll;

    private int _selectedSlotIndex = -1;

    // RouteCatalog 같은 걸 가지고 있다면 여기 연결해서 드롭다운 제공 가능
    // public RouteCatalog routeCatalog;

    [MenuItem("Tools/UI/UIScreen Spec Editor")]
    public static void Open()
    {
        var w = GetWindow<UIScreenSpecEditorWindow>();
        w.titleContent = new GUIContent("UIScreen Spec Editor");
        w.Show();
    }

    private void OnEnable()
    {
        minSize = new Vector2(680, 400);
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

            const float leftWidth = 55f; // 살짝 넓혀서 텍스트+카운트 표시
            const float rightBlankWidth = 40f;
            const float gap = 4f;

            // 🔹 왼쪽: 슬롯 인덱스 + 위젯 개수 표시
            var leftRect = new Rect(rect.x, rect.y, leftWidth, rect.height);
            EditorGUI.LabelField(leftRect, $"Slot {index} ({widgetCount})");

            // 🔹 가운데: Popup + TextField
            float usableWidth = rect.width - leftWidth - rightBlankWidth - gap * 2f;
            if (usableWidth < 0) usableWidth = 0;

            float popupWidth = usableWidth * 0.4f;
            float textWidth = usableWidth * 0.6f;

            float popupX = rect.x + leftWidth + gap;
            float textX = popupX + popupWidth + gap;

            var popupRect = new Rect(popupX, rect.y, popupWidth, rect.height);
            var textRect = new Rect(textX, rect.y, textWidth, rect.height);
            
            var options = _slotIdOptions;

            if (options == null || options.Length == 0)
            {
                // 템플릿 프리팹에 UISlot이 없는 상태
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

            int lines = 0;

            // 1줄: Name + Type
            lines += 1;

            // 2줄: Text 멀티라인
            lines += 2;

            // Route + Prefab
            var w = widgetsProp.GetArrayElementAtIndex(index);
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

            float contentHeight = lines * (lineH + vGap) + vGap;

            // 여유 조금 더 주기 위해 +4f 정도
            return contentHeight + borderPadding * 2f + 4f;
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
                    if (newElem != null)
                    {
                        newElem.FindPropertyRelative("nameTag").stringValue = $"Widget {insertIndex}";
                        newElem.FindPropertyRelative("widgetType").enumValueIndex = (int)WidgetType.Text;
                        newElem.FindPropertyRelative("text").stringValue = string.Empty;
                        newElem.FindPropertyRelative("onClickRoute").stringValue = string.Empty;
                        newElem.FindPropertyRelative("prefabOverride").objectReferenceValue = null;
                    }

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

            // === 1줄: Name + Type ===
            var nameRect = new Rect(rect.x, y, rect.width * 0.6f, lineH);
            var typeRect = new Rect(rect.x + rect.width * 0.62f, y, rect.width * 0.36f, lineH);

            nameProp.stringValue = EditorGUI.TextField(nameRect, "Name", nameProp.stringValue);
            EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);
            y += lineH + vGap;

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
            if (newAsset == null)
            {
                _asset = null;
                _so = null;
                return;
            }

            Bind(newAsset);
        }

        if (_asset == null || _so == null)
        {
            EditorGUILayout.HelpBox("UIScreenSpecAsset 를 선택하거나 드래그해서 열어주세요.\n(Project 창에서 Spec Asset 클릭 → 자동 바인딩됨)",
                MessageType.Info);
            return;
        }

        _so.Update();

        var screenId = _specProp.FindPropertyRelative("screenId");
        var nameProp = _specProp.FindPropertyRelative("name");
        var prefabProp = _specProp.FindPropertyRelative("templatePrefab");

        EditorGUILayout.LabelField("Base", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(screenId);
        EditorGUILayout.PropertyField(nameProp);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(prefabProp);
        if (EditorGUI.EndChangeCheck())
        {
            // Inspector에서 templatePrefab을 변경했을 때만 다시 스캔
            _so.ApplyModifiedProperties();
            RefreshSlotIdOptionsFromPrefab();
        }

        EditorGUILayout.Space(8);

        // 좌/우 분할
        using (new EditorGUILayout.HorizontalScope())
        {
            // 왼쪽: Slots 영역
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.4f)))
            {
                _slotsScroll = EditorGUILayout.BeginScrollView(_slotsScroll);
                _slotsList?.DoLayoutList();
                EditorGUILayout.EndScrollView();

                DrawValidateButtons();
            }

            GUILayout.Space(4f);

            // 오른쪽: Widgets 영역
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                _widgetsScroll = EditorGUILayout.BeginScrollView(_widgetsScroll);

                // 🔹 슬롯 개수가 바뀐 뒤 인덱스가 꼬인 경우 방어
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
            if (GUILayout.Button("Refresh Slots From Prefab"))
            {
                RefreshSlotIdOptionsFromPrefab(force: true);
                Repaint();
            }
            
            if (GUILayout.Button("Validate"))
            {
                var issues = ValidateSpec(_asset.spec);
                if (issues.Count == 0)
                    EditorUtility.DisplayDialog("Validate", "OK (no issues)", "Close");
                else
                    EditorUtility.DisplayDialog("Validate", string.Join("\n", issues), "Close");
            }

            if (GUILayout.Button("Auto-Fix (Safe)"))
            {
                AutoFixSafe(_asset.spec);
                _so.Update(); // SerializedObject 쪽도 즉시 동기화
                EditorUtility.SetDirty(_asset);
            }
            
        }
    }

    private static List<string> ValidateSpec(UIScreenSpec s)
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(s.screenId))
            issues.Add("- screenId is empty");

        //프리팹이 바뀌었는데, Spec이 옛 이름을 들고 있는 경우 Validate에서 알려줌.
        if (s.templatePrefab != null)
        {
            var slotsInPrefab = s.templatePrefab.GetComponentsInChildren<UISlot>(true);
            var ids = new HashSet<string>();
            foreach (var slot in slotsInPrefab)
            {
                if (slot == null) continue;
                var id = (slot.id ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(id))
                    ids.Add(id);
            }

            for (int i = 0; i < s.slots.Count; i++)
            {
                var slot = s.slots[i];
                if (slot == null) continue;
                if (!string.IsNullOrWhiteSpace(slot.slotName) && !ids.Contains(slot.slotName))
                {
                    issues.Add($"- slots[{i}].slotName '{slot.slotName}' does not exist in templatePrefab UISlots");
                }
            }
        }

        if (s.slots == null || s.slots.Count == 0)
            issues.Add("- slots is empty");

        if (s.slots != null)
        {
            for (int i = 0; i < s.slots.Count; i++)
            {
                var slot = s.slots[i];
                if (slot == null)
                {
                    issues.Add($"- slots[{i}] is null");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(slot.slotName))
                    issues.Add($"- slots[{i}].slotName is empty");

                if (slot.widgets == null)
                    issues.Add($"- slots[{i}].widgets is null");
                else
                {
                    for (int w = 0; w < slot.widgets.Count; w++)
                    {
                        var widget = slot.widgets[w];
                        if (widget == null)
                        {
                            issues.Add($"- slots[{i}].widgets[{w}] is null");
                            continue;
                        }

                        if (widget.widgetType == WidgetType.Button && string.IsNullOrWhiteSpace(widget.onClickRoute))
                            issues.Add($"- Button route missing: slots[{i}].widgets[{w}]");
                    }
                }
            }
        }

        return issues;
    }

    private static void AutoFixSafe(UIScreenSpec s)
    {
        if (s.slots == null) s.slots = new List<SlotSpec>();

        foreach (var slot in s.slots)
        {
            if (slot == null) continue;
            if (slot.widgets == null) slot.widgets = new List<WidgetSpec>();

            foreach (var w in slot.widgets)
            {
                if (w == null) continue;
                if (w.widgetType != WidgetType.Button)
                    w.onClickRoute = string.Empty;
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