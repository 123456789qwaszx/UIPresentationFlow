using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ScreenKey))]
public class ScreenKeyDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        var valueProp = property.FindPropertyRelative("value");
        var keys = UIScreenKeyDiscovery.All;

        if (keys == null || keys.Count == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        int currentIndex = 0;
        string currentValue = valueProp.stringValue;

        string[] options = new string[keys.Count];
        for (int i = 0; i < keys.Count; i++)
        {
            options[i] = keys[i].Value;
            if (keys[i].Value == currentValue)
                currentIndex = i;
        }

        int newIndex = EditorGUI.Popup(
            position,
            label.text,
            currentIndex,
            options);

        valueProp.stringValue = options[newIndex];
    }
}