#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIScreenCatalog))]
public class UIScreenCatalogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

        if (GUILayout.Button("Validate Catalog"))
        {
            var catalog = (UIScreenCatalog)target;
            List<string> problems = catalog.Validate();

            if (problems.Count == 0)
            {
                Debug.Log($"[UIScreenCatalog] '{catalog.name}' OK — {catalog.entries.Count} entries, no problems.", catalog);
            }
            else
            {
                foreach (string p in problems)
                    Debug.LogWarning($"[UIScreenCatalog] {p}", catalog);
            }
        }

        EditorGUILayout.EndVertical();
    }
}
#endif
