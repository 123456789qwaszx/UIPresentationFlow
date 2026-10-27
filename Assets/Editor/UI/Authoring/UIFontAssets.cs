using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// TMP 글꼴 자산 — 빌더가 화면과 함께 만드는 산출물.
//
// 동적 SDF는 쓰는 글자를 그때그때 아틀라스에 넣는다. 화면을 세우거나 플레이하면 그 글자가 자산에 저장되어,
// 글자 몇 개만 들어가도 2048² 아틀라스가 통째로(수 MB) 변경으로 잡힌다. 빌드 끝에 ClearDynamicData로 비우면
// 아틀라스가 1×1로 줄어 몇 KB다. 실행 중 쓰는 글자는 다시 채워진다(원본 TTF가 자산에 걸려 있다).
// 쓸 글자가 정해져 있으면(데모처럼) 정적 글꼴을 구워 이 문제를 처음부터 피할 수 있다.
public static class UIFontAssets
{
    // 동적 SDF 글꼴 자산. 있으면 그대로 돌려준다(처음 한 번만 만든다).
    // 아틀라스와 재질을 자산 안에 두고(TMP 메뉴로 만든 자산과 같은 꼴), Clear Dynamic Data On Build를 켠다.
    public static TMP_FontAsset EnsureDynamic(
        string fontPath,
        string assetPath,
        List<string> problems,
        int samplingPointSize = 72,
        int atlasPadding = 7,
        int atlasSize = 2048)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null)
            return existing;

        Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null)
        {
            problems.Add($"글꼴 파일 없음: {fontPath}");
            return null;
        }

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
            font,
            samplingPointSize,
            atlasPadding,
            GlyphRenderMode.SDFAA,
            atlasSize,
            atlasSize,
            AtlasPopulationMode.Dynamic,
            true);

        if (asset == null)
        {
            problems.Add($"TMP 글꼴 자산을 만들지 못했다: {fontPath}");
            return null;
        }

        UIPrefabs.EnsureFolder(UIPrefabs.DirectoryOf(assetPath));

        asset.name = Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(asset, assetPath);

        if (asset.atlasTextures != null && asset.atlasTextures.Length > 0 && asset.atlasTextures[0] != null)
        {
            asset.atlasTextures[0].name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
        }

        if (asset.material != null)
        {
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
        }

        // 플레이어 빌드에는 비운 아틀라스로 싣는다.
        var so = new SerializedObject(asset);
        SerializedProperty clear = so.FindProperty("m_ClearDynamicDataOnBuild");
        if (clear != null)
        {
            clear.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    // 동적 글꼴이 그동안 채운 글자와 아틀라스를 비운다(TMP 톱니 메뉴 Clear Dynamic Data와 같은 공개 API).
    // 굵기 표 · 원본 TTF 연결 · Clear Dynamic Data On Build는 그대로다. 정적 글꼴은 건드리지 않는다.
    public static void ClearDynamicData(string assetPath, List<string> problems)
    {
        TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (asset == null)
        {
            problems.Add($"비울 TMP 글꼴 자산이 없다: {assetPath}");
            return;
        }

        ClearDynamicData(asset);
    }

    public static void ClearDynamicData(TMP_FontAsset asset)
    {
        if (asset == null || asset.atlasPopulationMode == AtlasPopulationMode.Static)
            return;

        asset.ClearFontAssetData(true);

        EditorUtility.SetDirty(asset);
        if (asset.atlasTextures == null)
            return;

        foreach (Texture2D atlas in asset.atlasTextures)
        {
            if (atlas != null)
                EditorUtility.SetDirty(atlas);
        }
    }

    // 굵기 표의 한 칸(weight 100–900)에 보통 글자체를 건다 — 예: Medium의 700 칸에 Bold 글꼴을 걸면
    // 글자 스타일 Bold가 가짜 굵게가 아니라 그 글꼴로 그려진다.
    public static bool SetWeightTypeface(TMP_FontAsset asset, int weight, TMP_FontAsset typeface, List<string> problems)
    {
        var so = new SerializedObject(asset);
        SerializedProperty table = so.FindProperty("m_FontWeightTable");
        if (table == null || !table.isArray)
        {
            problems.Add("TMP 글꼴 굵기 표(m_FontWeightTable)를 찾지 못했다 — Bold는 가짜 굵게로 그려진다.");
            return false;
        }

        int index = Mathf.Clamp(weight / 100, 1, 9);
        if (table.arraySize < 10)
            table.arraySize = 10;

        SerializedProperty regular = table.GetArrayElementAtIndex(index).FindPropertyRelative("regularTypeface");
        if (regular == null)
        {
            problems.Add("TMP 글꼴 굵기 표의 regularTypeface 칸이 없다.");
            return false;
        }

        regular.objectReferenceValue = typeface;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return true;
    }
}
