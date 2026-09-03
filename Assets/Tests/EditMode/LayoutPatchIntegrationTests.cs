using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using static ResolveTestKit;

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
    private static GameObject BuildPresentationRoot(
        Assets assets,
        out RectTransform primary,
        out RectTransform side)
    {
        GameObject root = assets.NewGameObject(
            "Screen",
            typeof(RectTransform));

        primary = Child(root.transform, "PrimaryContent");
        side = Child(root.transform, "SideInfo");

        // Add UIBase after children exist because Awake binds enum refs.
        root.AddComponent<LayoutPatchTestRoot>();
        return root;
    }

    private static RectTransform Child(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot =
            new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-190, 20);
        rect.sizeDelta = new Vector2(1140, 640);

        return rect;
    }

    [Test]
    public void LayoutPatch_AppliesWithoutUIScreen()
    {
        using var assets = new Assets();

        GameObject root = BuildPresentationRoot(
            assets,
            out RectTransform primary,
            out RectTransform side);

        Assert.That(root.GetComponent<UIScreen>(), Is.Null);

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

        var refs = root.GetComponent<LayoutPatchTestRoot>();
        new LayoutSpecPatch(layout).Apply(refs);

        Assert.That(
            primary.anchoredPosition,
            Is.EqualTo(new Vector2(0, 20)));

        Assert.That(
            primary.sizeDelta,
            Is.EqualTo(new Vector2(1240, 640)));

        Assert.That(side.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void LayoutPatch_UnknownRef_IsIgnored()
    {
        using var assets = new Assets();

        GameObject root = BuildPresentationRoot(
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

        var refs = root.GetComponent<LayoutPatchTestRoot>();

        Assert.DoesNotThrow(
            () => new LayoutSpecPatch(layout).Apply(refs));

        Assert.That(
            primary.sizeDelta,
            Is.EqualTo(new Vector2(1140, 640)));
    }

    [Test]
    public void ReapplyingSamePatch_IsIdempotent()
    {
        using var assets = new Assets();

        GameObject root = BuildPresentationRoot(
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
        var refs = root.GetComponent<LayoutPatchTestRoot>();

        patch.Apply(refs);
        Vector2 positionOnce = primary.anchoredPosition;
        Vector2 sizeOnce = primary.sizeDelta;

        patch.Apply(refs);

        Assert.That(primary.anchoredPosition, Is.EqualTo(positionOnce));
        Assert.That(primary.sizeDelta, Is.EqualTo(sizeOnce));
    }

    [Test]
    public void Factory_CreatesUIBaseAndAppliesLayoutWithoutUIScreen()
    {
        using var assets = new Assets();

        GameObject uiRoot = assets.NewGameObject(
            "UIRoot",
            typeof(RectTransform));

        GameObject prefab = BuildPresentationRoot(
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

        UIScreenSpec spec = Spec("home");
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

        UIBase screen = factory.Create(result);
        assets.Track(screen.gameObject);

        Assert.That(screen, Is.Not.Null);
        Assert.That(screen, Is.TypeOf<LayoutPatchTestRoot>());
        Assert.That(screen.GetComponent<UIScreen>(), Is.Null);
        Assert.That(screen.transform.parent, Is.SameAs(uiRoot.transform));

        var refs = (IUIPresentationRefProvider)screen;

        Assert.That(
            refs.TryGetRect("PrimaryContent", out RectTransform primary),
            Is.True);

        Assert.That(
            primary.sizeDelta,
            Is.EqualTo(new Vector2(1400, 640)));
    }
}