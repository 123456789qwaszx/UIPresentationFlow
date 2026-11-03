using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

// 동적 글꼴이 채운 글자를 디스크에 남기지 않는다 — 글꼴 자산이 저장되기 직전에 비운다.
//
// 동적 SDF는 그린 글자를 아틀라스와 표에 더한다. 플레이뿐 아니라 편집 모드에서 씬 · 프리팹을 그릴 때도 더한다.
// 그 상태가 저장(Ctrl+S · Save Project · 에디터 종료)에 실리면 글자 몇 개로 2048² 아틀라스와 글리프 · 커닝 표가
// 통째로(수백 KB–수 MB) 변경으로 잡힌다. TMP가 스스로 비우는 자리는 둘인데 둘 다 모자란다.
//   - 빌드 전처리(TMP_PreBuildProcessor): 플레이어를 빌드할 때만 비운다.
//   - 에디터 종료(TMP_EditorResourceManager): 글자 · 글리프 표와 아틀라스만 비우고 글꼴 기능 표(커닝 · 합자 ·
//     결합 위치)는 남긴다. 그래서 종료 뒤 "표는 비었는데 기능 표만 1 MB"인 반쪽 자산이 저장된다.
// 여기서는 TMP 톱니 메뉴 Clear Dynamic Data와 같은 공개 API(UIFontAssets.ClearDynamicData → ClearFontAssetData(true))로
// 전부 비운다. 디스크의 자산은 언제나 갓 만든 꼴(아틀라스 1×1, 표 빔)이고, 메모리의 글꼴은 다음 그리기에서 다시 채운다.
//
// 대상: Assets/ 아래에서 동적(Dynamic · DynamicOS)이고 Clear Dynamic Data On Build가 켜진 글꼴 — "채운 데이터는
// 버려도 된다"는 표시다(UIFontAssets.EnsureDynamic이 켜고, TMP의 종료 · 빌드 정리도 같은 표시를 본다).
// 정적 글꼴과, 표시가 꺼진 동적 글꼴(채운 글자를 일부러 싣는 것)은 건드리지 않는다.
//
// 저장 직후 화면의 글자는 한 번 다시 그려진다(비운 아틀라스를 다시 채운다).
// 이미 부푼 채로 디스크에 있는 자산은 다시 저장될 때 준다. 바로 줄이려면 Tools/UI Presentation/Clear Dynamic Font Data.
[InitializeOnLoad]
internal sealed class UIDynamicFontGuard : AssetModificationProcessor
{
    private const string AssetsRoot = "Assets/";

    static UIDynamicFontGuard()
    {
        // 종료 때의 저장 앞. TMP의 종료 정리와 어느 쪽이 먼저 돌아도 결과는 같다 — 이쪽이 기능 표까지 비운다.
        EditorApplication.quitting += ClearLoadedOnQuit;
    }

    // 저장 목록은 그대로 돌려준다 — 저장을 막지 않고, 저장될 내용만 비운다.
    private static string[] OnWillSaveAssets(string[] paths)
    {
        foreach (string path in paths)
        {
            if (!path.StartsWith(AssetsRoot, StringComparison.Ordinal) ||
                !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ||
                AssetDatabase.GetMainAssetTypeAtPath(path) != typeof(TMP_FontAsset))
                continue;

            var font = AssetDatabase.LoadMainAssetAtPath(path) as TMP_FontAsset;
            if (NeedsClear(font))
                UIFontAssets.ClearDynamicData(font);
        }

        return paths;
    }

    // 이미 메모리에 올라온 글꼴만 본다 — 올라오지 않은 글꼴은 디스크 그대로라 이번 세션이 더한 것이 없다.
    private static void ClearLoadedOnQuit()
    {
        foreach (TMP_FontAsset font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (!EditorUtility.IsPersistent(font) || !NeedsClear(font))
                continue;

            UIFontAssets.ClearDynamicData(font);
            AssetDatabase.SaveAssetIfDirty(font);
        }
    }

    // 디스크에 이미 부푼 채로 있는 자산(커밋된 것 포함)을 지금 줄인다.
    [MenuItem("Tools/UI Presentation/Clear Dynamic Font Data")]
    private static void ClearAllInProject()
    {
        var cleared = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (!NeedsClear(font))
                continue;

            UIFontAssets.ClearDynamicData(font);
            AssetDatabase.SaveAssetIfDirty(font);
            cleared.Add(path);
        }

        Debug.Log(cleared.Count == 0
            ? "[UIDynamicFontGuard] 비울 동적 글꼴이 없다."
            : $"[UIDynamicFontGuard] 동적 글꼴 {cleared.Count}개를 비웠다.\n{string.Join("\n", cleared)}");
    }

    internal static bool NeedsClear(TMP_FontAsset font)
        => font != null &&
           font.atlasPopulationMode != AtlasPopulationMode.Static &&
           HasDynamicData(font) &&
           IsUnderAssets(font) &&
           IsMarkedDisposable(font);

    // ClearFontAssetData(true)가 비우는 것 가운데 하나라도 남아 있는가.
    // 다중 대체 표(MultipleSubstitution)는 ClearFontAssetData가 비우지 않으므로 보지 않는다 — 보면 저장마다 헛돈다.
    private static bool HasDynamicData(TMP_FontAsset font)
    {
        if (Any(font.characterTable) || Any(font.glyphTable))
            return true;

        Texture2D[] atlases = font.atlasTextures;
        if (atlases != null && atlases.Length > 1)
            return true;

        Texture2D atlas = font.atlasTexture;
        if (atlas != null && (atlas.width > 1 || atlas.height > 1))
            return true;

        TMP_FontFeatureTable features = font.fontFeatureTable;
        return features != null &&
               (Any(features.glyphPairAdjustmentRecords) ||
                Any(features.ligatureRecords) ||
                Any(features.MarkToBaseAdjustmentRecords) ||
                Any(features.MarkToMarkAdjustmentRecords));
    }

    private static bool IsUnderAssets(TMP_FontAsset font)
        => AssetDatabase.GetAssetPath(font).StartsWith(AssetsRoot, StringComparison.Ordinal);

    // clearDynamicDataOnBuild는 internal이다 — 직렬화 칸으로 읽는다(UIFontAssets.EnsureDynamic이 쓰는 그 칸).
    private static bool IsMarkedDisposable(TMP_FontAsset font)
    {
        using (var so = new SerializedObject(font))
        {
            SerializedProperty clear = so.FindProperty("m_ClearDynamicDataOnBuild");
            return clear != null && clear.boolValue;
        }
    }

    private static bool Any<T>(List<T> list) => list != null && list.Count > 0;
}
