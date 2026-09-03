using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using static ResolveTestKit;

// Concrete presentation root used by the R4 integration tests.
// Its enum names are the external ref ids consumed by LayoutSpecPatch.
public sealed class LayoutPatchTestRoot : UIBase<LayoutPatchTestRoot.Refs>
{
    public enum Refs
    {
        PrimaryContent,
        SideInfo,
    }
}

public class LayoutPatchIntegrationTests
{
    private static GameObject BuildPresentationScreen(
        Assets assets,
        out RectTransform primary,
        out RectTransform side)
    {
        GameObject root = assets.NewGameObject(
            "Screen",
            typeof(RectTransform),
            typeof(UIScreen));

        primary = Child(root.transform, "PrimaryContent");
        side = Child(root.transform, "SideInfo");

        // Add UIBase only after children exist.
        // AddComponent invokes Awake, and UIBase binds refs during initialization.
        root.AddComponent<LayoutPatchTestRoot>();

        return root;
    }

    private static RectTransform Child(Transform parent, string goName)
    {
        var go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-190, 20);
        rt.sizeDelta = new Vector2(1140, 640);
        return rt;
    }

    // Legacy registry remains during R4 because Theme/Factory still use UIScreen.
    // This test intentionally verifies that R4 has not silently broken it.
    [Test]
    public void LegacyTags_CanStillBeRegisteredDuringTransition()
    {
        using var assets = new Assets();

        GameObject root = assets.NewGameObject(
            "Screen",
            typeof(RectTransform),
            typeof(UIScreen));

        var child = new GameObject(
            "HierarchyName",
            typeof(RectTransform),
            typeof(UIWidgetTag));

        child.transform.SetParent(root.transform, false);
        child.GetComponent<UIWidgetTag>().nameTag = "SemanticName";

        UIScreen screen = root.GetComponent<UIScreen>();
        screen.RegisterAuthoredWidgets();

        Assert.That(screen.GetWidgetHandle("SemanticName"), Is.Not.Null);
        Assert.That(screen.GetWidgetHandle("HierarchyName"), Is.Null);
    }

    [Test]
    public void LayoutPatch_AppliesThroughPresentationRefProvider()
    {
        using var assets = new Assets();
        GameObject root = BuildPresentationScreen(
            assets,
            out RectTransform primary,
            out RectTransform side);

        LayoutPatchSpec layout = assets.Layout("Compact");
        layout.widgets.Add(new WidgetLayoutPatch
        {
            refId = "PrimaryContent",
            rect =
            {
                overrideAnchoredPosition = true,
                anchoredPosition = new Vector2(0, 20),
                overrideSizeDelta = true,
                sizeDelta = new Vector2(1240, 640),
            },
        });
        layout.widgets.Add(new WidgetLayoutPatch
        {
            refId = "SideInfo",
            overrideActive = true,
            active = false,
        });

        new LayoutSpecPatch(layout).Apply(root.GetComponent<UIScreen>());

        Assert.That(primary.anchoredPosition, Is.EqualTo(new Vector2(0, 20)));
        Assert.That(primary.sizeDelta, Is.EqualTo(new Vector2(1240, 640)));
        Assert.That(
            primary.anchorMin,
            Is.EqualTo(new Vector2(0.5f, 0.5f)),
            "anchors were not flagged; unchanged");
        Assert.That(side.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void LayoutPatch_DoesNotNeedWidgetTagForTargetLookup()
    {
        using var assets = new Assets();
        GameObject root = BuildPresentationScreen(
            assets,
            out RectTransform primary,
            out _);

        Assert.That(
            primary.GetComponent<UIWidgetTag>(),
            Is.Null,
            "R4 target should be addressable without UIWidgetTag.");

        LayoutPatchSpec layout = assets.Layout("NoTag");
        layout.widgets.Add(new WidgetLayoutPatch
        {
            refId = "PrimaryContent",
            rect =
            {
                overrideSizeDelta = true,
                sizeDelta = new Vector2(1500, 700),
            },
        });

        new LayoutSpecPatch(layout).Apply(root.GetComponent<UIScreen>());

        Assert.That(primary.sizeDelta, Is.EqualTo(new Vector2(1500, 700)));
    }

    [Test]
    public void LayoutPatch_UnknownRef_IsIgnored()
    {
        using var assets = new Assets();
        GameObject root = BuildPresentationScreen(
            assets,
            out RectTransform primary,
            out _);

        LayoutPatchSpec layout = assets.Layout("Broken");
        layout.widgets.Add(new WidgetLayoutPatch
        {
            refId = "DoesNotExist",
            rect =
            {
                overrideSizeDelta = true,
                sizeDelta = Vector2.zero,
            },
        });

        Assert.DoesNotThrow(
            () => new LayoutSpecPatch(layout).Apply(root.GetComponent<UIScreen>()));

        Assert.That(primary.sizeDelta, Is.EqualTo(new Vector2(1140, 640)));
    }

    [Test]
    public void ReapplyingSamePatch_IsIdempotent()
    {
        using var assets = new Assets();
        GameObject root = BuildPresentationScreen(
            assets,
            out RectTransform primary,
            out _);

        LayoutPatchSpec layout = assets.Layout("Wide");
        layout.widgets.Add(new WidgetLayoutPatch
        {
            refId = "PrimaryContent",
            rect =
            {
                overrideAnchoredPosition = true,
                anchoredPosition = new Vector2(-250, 20),
                overrideSizeDelta = true,
                sizeDelta = new Vector2(1400, 640),
            },
        });

        var patch = new LayoutSpecPatch(layout);
        UIScreen screen = root.GetComponent<UIScreen>();

        patch.Apply(screen);
        Vector2 posOnce = primary.anchoredPosition;
        Vector2 sizeOnce = primary.sizeDelta;

        patch.Apply(screen);

        Assert.That(primary.anchoredPosition, Is.EqualTo(posOnce));
        Assert.That(primary.sizeDelta, Is.EqualTo(sizeOnce));
    }

    [Test]
    public void Factory_AppliesResolvedLayoutThroughPresentationRefs()
    {
        using var assets = new Assets();

        GameObject uiRoot = assets.NewGameObject(
            "UIRoot",
            typeof(RectTransform));

        GameObject prefab = BuildPresentationScreen(
            assets,
            out _,
            out _);

        LayoutPatchSpec wide = assets.Layout("Wide");
        wide.widgets.Add(new WidgetLayoutPatch
        {
            refId = "PrimaryContent",
            rect =
            {
                overrideSizeDelta = true,
                sizeDelta = new Vector2(1400, 640),
            },
        });

        var spec = Spec("home");
        spec.templatePrefab = prefab;

        var resolved = new ResolvedUIScreen(
            new ScreenKey("home"),
            spec,
            prefab,
            null,
            wide,
            new List<string> { "Wide" });

        var patches = new List<IUIPatch>();
        wide.BuildPatches(patches);

        var result = new UIResolveResult(
            resolved,
            patches,
            new UIResolveTrace());

        var factory = new UIScreenFactory(
            uiRoot.transform,
            new UIPatchApplier());

        UIScreen screen = factory.Create(result);
        assets.Track(screen.gameObject);

        Assert.That(screen, Is.Not.Null);
        Assert.That(
            screen.GetComponent<IUIPresentationRefProvider>(),
            Is.Not.Null);

        var provider = screen.GetComponent<IUIPresentationRefProvider>();
        Assert.That(
            provider.TryGetRect("PrimaryContent", out RectTransform primary),
            Is.True);
        Assert.That(
            primary.sizeDelta,
            Is.EqualTo(new Vector2(1400, 640)));
    }
}