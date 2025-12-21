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
        minSize = new Vector2(700, 400); // 대충 이 정도
        Selection.selectionChanged += TryAutoBindFromSelection;
        TryAutoBindFromSelection();
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
            slot.FindPropertyRelative("slotName").stringValue = DefaultSlotNames.Length > 0 ? DefaultSlotNames[0] : "Body";

            var widgets = slot.FindPropertyRelative("widgets");
            widgets.ClearArray();

            _so.ApplyModifiedProperties();
            _selectedSlotIndex = i;
            BuildWidgetsList();
        };

        _slotsList.elementHeightCallback = index => EditorGUIUtility.singleLineHeight + 6f;

        _slotsList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            rect.y += 2f;
            rect.height = EditorGUIUtility.singleLineHeight;

            var slot = _slotsProp.GetArrayElementAtIndex(index);
            var nameProp = slot.FindPropertyRelative("slotName");

            // slotName 드롭다운(기본 3종 + 커스텀 입력 허용)
            var left = new Rect(rect.x, rect.y, rect.width * 0.55f, rect.height);
            var right = new Rect(rect.x + rect.width * 0.58f, rect.y, rect.width * 0.42f, rect.height);

            int popupIndex = IndexOf(DefaultSlotNames, nameProp.stringValue);
            int newIndex = EditorGUI.Popup(left, popupIndex < 0 ? 0 : popupIndex, DefaultSlotNames);

            // 드롭다운 선택 시 덮어쓰기
            if (newIndex >= 0 && newIndex < DefaultSlotNames.Length)
                nameProp.stringValue = DefaultSlotNames[newIndex];

            // 커스텀 편집도 가능하게 우측에 텍스트 필드 제공
            nameProp.stringValue = EditorGUI.TextField(right, nameProp.stringValue);
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

        _widgetsList.drawHeaderCallback = rect =>
        {
            var currentSlot = _slotsProp.GetArrayElementAtIndex(_selectedSlotIndex);
            var nameProp = currentSlot.FindPropertyRelative("slotName");
            EditorGUI.LabelField(rect, $"Widgets (Slot: {nameProp.stringValue})");
        };


        _widgetsList.elementHeightCallback = index =>
        {
            // widgetType + text + (button이면 route) = 최대 3줄
            var w = widgetsProp.GetArrayElementAtIndex(index);
            var type = (WidgetType)w.FindPropertyRelative("widgetType").enumValueIndex;

            int lines = (type == WidgetType.Button) ? 3 : 2;
            return lines * (EditorGUIUtility.singleLineHeight + 2f) + 6f;
        };

        _widgetsList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            rect.y += 2f;

            var w = widgetsProp.GetArrayElementAtIndex(index);
            var typeProp = w.FindPropertyRelative("widgetType");
            var textProp = w.FindPropertyRelative("text");
            var routeProp = w.FindPropertyRelative("onClickRoute");

            float lineH = EditorGUIUtility.singleLineHeight;
            var r0 = new Rect(rect.x, rect.y, rect.width, lineH);
            var r1 = new Rect(rect.x, rect.y + (lineH + 2f), rect.width, lineH);
            var r2 = new Rect(rect.x, rect.y + 2f * (lineH + 2f), rect.width, lineH);

            EditorGUI.PropertyField(r0, typeProp);

            var type = (WidgetType)typeProp.enumValueIndex;

            // Text는 text만, Button은 text + route
            textProp.stringValue = EditorGUI.TextField(r1, "Text", textProp.stringValue);

            if (type == WidgetType.Button)
            {
                // 여기서 routeProp을 드롭다운으로 바꾸고 싶으면, 네 RouteCatalog에서 목록을 받아서 Popup 처리하면 됨.
                routeProp.stringValue = EditorGUI.TextField(r2, "OnClick Route", routeProp.stringValue);
            }
            else
            {
                // Text 위젯이면 route 비움(실수 방지)
                routeProp.stringValue = string.Empty;
            }
        };
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);

        var newAsset = (UIScreenSpecAsset)EditorGUILayout.ObjectField("Spec Asset", _asset, typeof(UIScreenSpecAsset), false);
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
            EditorGUILayout.HelpBox("UIScreenSpecAsset 를 선택하거나 드래그해서 열어주세요.\n(Project 창에서 Spec Asset 클릭 → 자동 바인딩됨)", MessageType.Info);
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
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.38f)))
            {
                _slotsList?.DoLayoutList();
                DrawValidateButtons();
            }

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.VerticalScope())
            {
                if (_widgetsList == null)
                {
                    EditorGUILayout.HelpBox("좌측에서 Slot을 선택하세요.", MessageType.None);
                }
                else
                {
                    _widgetsList.DoLayoutList();
                }
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
                _so.Update();              // SerializedObject 쪽도 즉시 동기화
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
            if (arr[i] == v) return i;
        return -1;
    }
}
#endif
