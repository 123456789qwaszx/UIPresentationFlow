#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(UIRouteKeyAttribute))]
public sealed class UIRouteKeyDrawer : PropertyDrawer
{
    private static string[] _cachedRoutes;

    private static void EnsureCache()
    {
        if (_cachedRoutes != null) return;

        var list = new List<string>();

        var fields = TypeCache.GetFieldsWithAttribute<UIRouteDefinitionAttribute>();
        foreach (var f in fields)
        {
            if (!f.IsStatic) continue;
            if (f.FieldType != typeof(string)) continue;

            var val = f.GetValue(null) as string;
            if (string.IsNullOrEmpty(val)) continue;
            if (list.Contains(val)) continue;

            list.Add(val);
        }

        _cachedRoutes = list.ToArray();
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnsureCache();

        if (_cachedRoutes == null || _cachedRoutes.Length == 0)
        {
            // 등록된 Route가 없으면 그냥 기본 string 필드로 그리기
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        string current = property.stringValue;
        int currentIndex = System.Array.IndexOf(_cachedRoutes, current);
        if (currentIndex < 0) currentIndex = 0;

        // 🔧 여기 수정
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, _cachedRoutes);

        if (newIndex >= 0 && newIndex < _cachedRoutes.Length)
        {
            property.stringValue = _cachedRoutes[newIndex];
        }
    }
}
#endif