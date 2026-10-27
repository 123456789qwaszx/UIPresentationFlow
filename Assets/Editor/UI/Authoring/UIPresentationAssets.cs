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
//
// Variant도 같은 규칙이다 — EnsureLayoutVariant는 그 id의 Variant가 없을 때만 더하고, 있으면 건드리지 않는다
// (조건 · 우선순위 · 패치 값을 에셋에서 고쳐도 남는다). 그 Variant를 없애려면 에셋과 그것을 더하는 빌더 줄을 같이 지운다.
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

    // 레이아웃을 바꾸는 Variant를 id로 찾아 없을 때만 더한다. 레이아웃 패치는 Presentation 옆 파일
    // "{Presentation 파일 이름}.{variantId}.asset"으로 만들고(오브젝트 이름 = 파일 이름), fill이 채운다.
    // 돌려주는 값: 이번에 더했으면 true.
    public static bool EnsureLayoutVariant(
        UIPresentationSpec spec,
        string variantId,
        int priority,
        VariantCondition condition,
        Action<LayoutPatchSpec> fill)
    {
        if (spec == null)
            throw new ArgumentNullException(nameof(spec));
        if (string.IsNullOrWhiteSpace(variantId))
            throw new ArgumentException("variantId가 비었다.", nameof(variantId));

        UIVariantRule[] rules = spec.variants ?? Array.Empty<UIVariantRule>();
        foreach (UIVariantRule rule in rules)
        {
            if (rule != null && rule.variantId == variantId)
                return false;
        }

        string specPath = AssetDatabase.GetAssetPath(spec);
        if (string.IsNullOrEmpty(specPath))
            throw new InvalidOperationException("Presentation이 에셋이 아니다 — 먼저 Ensure로 만든다.");

        string patchName = Path.GetFileNameWithoutExtension(specPath) + "." + variantId;
        string patchPath = UIPrefabs.DirectoryOf(specPath) + "/" + patchName + ".asset";

        LayoutPatchSpec patch = AssetDatabase.LoadAssetAtPath<LayoutPatchSpec>(patchPath);
        if (patch == null)
        {
            patch = ScriptableObject.CreateInstance<LayoutPatchSpec>();
            patch.name = patchName;
            fill?.Invoke(patch);
            AssetDatabase.CreateAsset(patch, patchPath);
        }

        var added = new UIVariantRule
        {
            variantId = variantId,
            priority = priority,
            condition = condition ?? new VariantCondition(),
            overrideLayout = patch,
        };

        var next = new UIVariantRule[rules.Length + 1];
        Array.Copy(rules, next, rules.Length);
        next[rules.Length] = added;
        spec.variants = next;

        EditorUtility.SetDirty(spec);
        return true;
    }

    // 화면 Refs 하나의 RectTransform을 옮기는 패치 한 줄. 넘긴 값만 덮어쓴다(null이면 그대로).
    public static WidgetLayoutPatch MoveWidget(
        string refId,
        Vector2? anchorMin = null,
        Vector2? anchorMax = null,
        Vector2? pivot = null,
        Vector2? anchoredPosition = null,
        Vector2? sizeDelta = null)
    {
        var rect = new RectTransformPatch();

        if (anchorMin.HasValue || anchorMax.HasValue)
        {
            rect.overrideAnchors = true;
            rect.anchorMin = anchorMin ?? anchorMax.Value;
            rect.anchorMax = anchorMax ?? anchorMin.Value;
        }

        if (pivot.HasValue)
        {
            rect.overridePivot = true;
            rect.pivot = pivot.Value;
        }

        if (anchoredPosition.HasValue)
        {
            rect.overrideAnchoredPosition = true;
            rect.anchoredPosition = anchoredPosition.Value;
        }

        if (sizeDelta.HasValue)
        {
            rect.overrideSizeDelta = true;
            rect.sizeDelta = sizeDelta.Value;
        }

        return new WidgetLayoutPatch { refId = refId, rect = rect };
    }
}
