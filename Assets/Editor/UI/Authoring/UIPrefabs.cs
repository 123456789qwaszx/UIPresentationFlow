using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// 빌드 산출물(화면 프리팹)을 저장한다.
//
// 산출물은 빌드마다 같은 경로에 덮어쓴다 — GUID가 그대로라 다른 에셋의 참조가 이어진다.
// 파일 안쪽 오브젝트의 fileID는 Unity가 오브젝트 이름으로 짝을 맞춰 지킨다. 그래서 이름이 겹치는 오브젝트
// (같은 위젯 여러 개 — 슬롯 카드 여섯 장의 CardButton)는 빌드마다 fileID가 바뀔 수 있다. 내용과 GUID는 같다.
// 다른 에셋이 프리팹 안쪽 오브젝트를 fileID로 가리키게 두지 않는다(그런 참조는 그 에셋도 빌드가 다시 만든다).
public static class UIPrefabs
{
    // 플레이 중에는 짓지 않는다. 플레이 중에 만든 오브젝트는 Awake가 돌아서, 실행 중에만 붙는 것
    // (버튼 연출이 붙이는 UI_EventHandler 등)이 섞인 채 저장된다.
    public static bool CanBuild(out string reason)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            reason = "플레이 중에는 빌드하지 않는다 — 플레이를 멈추고 다시 실행한다. " +
                     "플레이 중에 만든 오브젝트는 Awake가 돌아 실행 중에만 붙는 컴포넌트가 섞인 채 저장된다.";
            return false;
        }

        reason = null;
        return true;
    }

    // go를 path에 프리팹으로 저장하고 go는 지운다. 폴더가 없으면 만든다.
    public static GameObject Save(GameObject go, string path)
    {
        if (go == null)
            throw new ArgumentNullException(nameof(go));

        EnsureFolder(DirectoryOf(path));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path, out bool success);
        Object.DestroyImmediate(go);

        if (!success || prefab == null)
            throw new InvalidOperationException("프리팹을 저장하지 못했다: " + path);

        return prefab;
    }

    // 폴더가 없으면 부모부터 만든다. path는 "Assets/…" 꼴.
    public static void EnsureFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            return;

        int slash = path.LastIndexOf('/');
        if (slash <= 0)
            throw new ArgumentException("Assets/ 아래 폴더가 아니다: " + path, nameof(path));

        string parent = path.Substring(0, slash);
        string leaf = path.Substring(slash + 1);

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    public static string DirectoryOf(string assetPath) =>
        Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
}

// 직렬화 칸을 이름으로 쓴다 — [SerializeField] private 칸(부품 컴포넌트의 참조 · 연출 값)을 빌더가 채울 때.
// 칸이 없으면 경고를 남기고 false(필드 이름이 바뀌었을 때 조용히 지나가지 않게).
public static class UISerialized
{
    public static bool SetFloat(Object target, string property, float value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = Find(so, target, property);
        if (prop == null)
            return false;

        prop.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    public static bool SetObject(Object target, string property, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = Find(so, target, property);
        if (prop == null)
            return false;

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    private static SerializedProperty Find(SerializedObject so, Object target, string property)
    {
        SerializedProperty prop = so.FindProperty(property);
        if (prop == null)
            Debug.LogWarning($"[UI 빌더] {target.GetType().Name}.{property} 직렬화 칸이 없다.", target);

        return prop;
    }
}
