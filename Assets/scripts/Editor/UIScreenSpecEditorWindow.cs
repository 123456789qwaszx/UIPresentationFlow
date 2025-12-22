#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public sealed class UIScreenSpecEditorWindow : EditorWindow
{
    private static readonly string[] DefaultSlotNames = { "Header", "Body", "Footer" };

    private UIScreenSpecAsset _asset;
    private SerializedObject _so;

    private SerializedProperty _specProp;
    private SerializedProperty _slotsProp;

    private ReorderableList _slotsList;
    private ReorderableList _widgetsList;

    private Vector2 _slotsScroll;
    private Vector2 _widgetsScroll;

    private int _selectedSlotIndex = -1;

    // (선택) 네가 RouteCatalog 같은 걸 가지고 있다면 여기 연결해서 드롭다운 제공 가능
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

        BuildSlotsList();
        BuildWidgetsList(); // selected slot 기준으로 빌드됨
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
            slot.FindPropertyRelative("slotName").stringValue =
                DefaultSlotNames.Length > 0 ? DefaultSlotNames[0] : "Body";

            var widgets = slot.FindPropertyRelative("widgets");
            widgets.ClearArray();

            _so.ApplyModifiedProperties();
            _selectedSlotIndex = i;
            BuildWidgetsList();
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

            int popupIndex = IndexOf(DefaultSlotNames, nameProp.stringValue);
            int newIndex = EditorGUI.Popup(popupRect, popupIndex < 0 ? 0 : popupIndex, DefaultSlotNames);

            if (newIndex >= 0 && newIndex < DefaultSlotNames.Length)
                nameProp.stringValue = DefaultSlotNames[newIndex];

            nameProp.stringValue = EditorGUI.TextField(textRect, nameProp.stringValue);
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
                rect.width  - padding * 2f,
                rect.height - padding * 2f
            );

            // 공통 배경 컬러 (선택 전/후만 농도 차이)
            Color normalBg   = new Color(0.3f, 0.3f, 0.3f, 0.5f); // 기본
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
            var w = widgetsProp.GetArrayElementAtIndex(index);
            var type = (WidgetType)w.FindPropertyRelative("widgetType").enumValueIndex;

            float lineH = EditorGUIUtility.singleLineHeight;
            float vGap = 2f;

            // 1줄: name+type
            float h = lineH + vGap;

            // TextArea 높이 (🔸 여기서도 2줄로 맞추기)
            int textLines = 2;
            float textHeight = (lineH + 2f) * textLines;
            h += textHeight + vGap;

            int extraLines = (type == WidgetType.Button) ? 2 : 1;
            h += extraLines * (lineH + vGap);

            h += 4f;
            return h;
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
            var nameProp = w.FindPropertyRelative("nameTag");
            var typeProp = w.FindPropertyRelative("widgetType");
            var textProp = w.FindPropertyRelative("text");
            var routeProp = w.FindPropertyRelative("onClickRoute");
            var prefabProp = w.FindPropertyRelative("prefabOverride");

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

        // 상단 기본 정보
        var screenId = _specProp.FindPropertyRelative("screenId");
        var nameProp = _specProp.FindPropertyRelative("name");
        var prefab = _specProp.FindPropertyRelative("templatePrefab");

        EditorGUILayout.LabelField("Base", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(screenId);
        EditorGUILayout.PropertyField(nameProp);
        EditorGUILayout.PropertyField(prefab);

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

    private void DrawValidateButtons()
    {
        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
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

        if (s.templatePrefab == null)
            issues.Add("- templatePrefab is null");

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