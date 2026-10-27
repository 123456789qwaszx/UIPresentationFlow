using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// 화면마다 Presentation 하나 — 처음 한 번만 만드는 데이터다.
// 없을 때만 만들고, 그 뒤로는 에셋에서 고친다(빌드가 덮지 않는다 — 사람이 고친 Variant · 테마가 남는다).
//
// presentationId는 화면 키(UIViewKey.Of). 오브젝트 이름은 파일 이름과 같게 둔다 — 다르면 Unity가
// "main object name should match the asset filename" 경고를 낸다. UIManager는 presentationId로 찾으므로
// 이름을 맞춰도 동작은 같다. 파일만 옮기거나 이름을 바꿔 어긋난 것은 여기서 맞춘다.
public static class UIPresentationAssets
{
    public static UIPresentationSpec Ensure(Type viewType, string path)
    {
        if (viewType == null)
            throw new ArgumentNullException(nameof(viewType));

        UIPrefabs.EnsureFolder(UIPrefabs.DirectoryOf(path));

        string fileName = Path.GetFileNameWithoutExtension(path);
        var spec = AssetDatabase.LoadAssetAtPath<UIPresentationSpec>(path);

        if (spec == null)
        {
            spec = ScriptableObject.CreateInstance<UIPresentationSpec>();
            spec.name = fileName;
            spec.presentationId = UIViewKey.Of(viewType);
            AssetDatabase.CreateAsset(spec, path);
        }
        else if (spec.name != fileName)
        {
            spec.name = fileName;
            EditorUtility.SetDirty(spec);
        }

        return spec;
    }
}
