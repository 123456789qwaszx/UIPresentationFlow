#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIScreenCatalog))]
public class UIScreenCatalogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 Inspector
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        // 강조 박스
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

        if (GUILayout.Button("Verify Route Mapping"))
        {
            var catalog = (UIScreenCatalog)target;
            catalog.ValidateAll();

            // Inspector 갱신
            EditorUtility.SetDirty(catalog);
        }

        EditorGUILayout.EndVertical();
    }
}
#endif