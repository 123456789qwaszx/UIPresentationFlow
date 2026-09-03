using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using static ResolveTestKit;

// M3 / D0-8: authored prefabs expose targets through UIWidgetTag; the factory
// materializes a screen from the prefab alone. These are the RectTransform-
// level integration tests M5 asks for.
public class LayoutPatchIntegrationTests
{
    private static GameObject BuildScreen(Assets assets, out RectTransform primary, out RectTransform side)
    {
        GameObject root = assets.NewGameObject("Screen", typeof(RectTransform), typeof(UIScreen));

        primary = Child(root.transform, "PanelA", "PrimaryContent");
        side    = Child(root.transform, "PanelB", "SideInfo");
        return root;
    }

    private static RectTransform Child(Transform parent, string goName, string tag)
    {
        var go = new GameObject(goName, typeof(RectTransform), typeof(UIWidgetTag));
        go.transform.SetParent(parent, false);
        go.GetComponent<UIWidgetTag>().nameTag = tag;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-190, 20);
        rt.sizeDelta        = new Vector2(1140, 640);
        return rt;
    }

    [Test]
    public void Tags_AreRegisteredByNameTag_NotByGameObjectName()
    {
        using var assets = new Assets();
        GameObject root = BuildScreen(assets, out RectTransform primary, out _);
        var screen = root.GetComponent<UIScreen>();

        screen.RegisterAuthoredWidgets();

        Assert.That(screen.GetWidgetHandle("PrimaryContent").RectTransform, Is.SameAs(primary));
        Assert.That(screen.GetWidgetHandle("PanelA"), Is.Null, "GameObject name is not a lookup key");
    }

    [Test]
    public void LayoutPatch_AppliesOnlyFlaggedFields_AndActive()
    {
        using var assets = new Assets();
        GameObject root = BuildScreen(assets, out RectTransform primary, out RectTransform side);
        root.GetComponent<UIScreen>().RegisterAuthoredWidgets();

        LayoutPatchSpec layout = assets.Layout("Compact");
        layout.widgets.Add(new WidgetLayoutPatch
        {
            nameTag = "PrimaryContent",
            rect = { overrideAnchoredPosition = true, anchoredPosition = new Vector2(0, 20),
                     overrideSizeDelta = true,        sizeDelta        = new Vector2(1240, 640) },
        });
        layout.widgets.Add(new WidgetLayoutPatch { nameTag = "SideInfo", overrideActive = true, active = false });

        new LayoutSpecPatch(layout).Apply(root.GetComponent<UIScreen>());

        Assert.That(primary.anchoredPosition, Is.EqualTo(new Vector2(0, 20)));
        Assert.That(primary.sizeDelta,        Is.EqualTo(new Vector2(1240, 640)));
        Assert.That(primary.anchorMin,        Is.EqualTo(new Vector2(0.5f, 0.5f)), "anchors were not flagged; unchanged");
        Assert.That(side.gameObject.activeSelf, Is.False);
    }

    [Test]
    public void LayoutPatch_UnknownTag_IsIgnored()
    {
        using var assets = new Assets();
        GameObject root = BuildScreen(assets, out RectTransform primary, out _);
        root.GetComponent<UIScreen>().RegisterAuthoredWidgets();

        LayoutPatchSpec layout = assets.Layout("Broken");
        layout.widgets.Add(new WidgetLayoutPatch { nameTag = "DoesNotExist", rect = { overrideSizeDelta = true, sizeDelta = Vector2.zero } });

        Assert.DoesNotThrow(() => new LayoutSpecPatch(layout).Apply(root.GetComponent<UIScreen>()));
        Assert.That(primary.sizeDelta, Is.EqualTo(new Vector2(1140, 640)));
    }

    [Test]
    public void ReapplyingSamePatch_IsIdempotent()
    {
        using var assets = new Assets();
        GameObject root = BuildScreen(assets, out RectTransform primary, out _);
        var screen = root.GetComponent<UIScreen>();
        screen.RegisterAuthoredWidgets();

        LayoutPatchSpec layout = assets.Layout("Wide");
        layout.widgets.Add(new WidgetLayoutPatch
        {
            nameTag = "PrimaryContent",
            rect = { overrideAnchoredPosition = true, anchoredPosition = new Vector2(-250, 20),
                     overrideSizeDelta = true,        sizeDelta        = new Vector2(1400, 640) },
        });
        var patch = new LayoutSpecPatch(layout);

        patch.Apply(screen);
        Vector2 posOnce  = primary.anchoredPosition;
        Vector2 sizeOnce = primary.sizeDelta;
        patch.Apply(screen);

        Assert.That(primary.anchoredPosition, Is.EqualTo(posOnce));
        Assert.That(primary.sizeDelta,        Is.EqualTo(sizeOnce));
    }

    [Test]
    public void Factory_RegistersTagsAndAppliesResolvedLayout()
    {
        using var assets = new Assets();
        GameObject uiRoot = assets.NewGameObject("UIRoot", typeof(RectTransform));
        GameObject prefab = BuildScreen(assets, out _, out _);   // scene object stands in for a prefab asset

        LayoutPatchSpec wide = assets.Layout("Wide");
        wide.widgets.Add(new WidgetLayoutPatch
        {
            nameTag = "PrimaryContent",
            rect = { overrideSizeDelta = true, sizeDelta = new Vector2(1400, 640) },
        });

        var spec = Spec("home");
        spec.templatePrefab = prefab;
        var resolved = new ResolvedUIScreen(new ScreenKey("home"), spec, prefab, null, wide, new List<string> { "Wide" });
        var patches  = new List<IUIPatch>();
        wide.BuildPatches(patches);
        var result   = new UIResolveResult(resolved, patches, new UIResolveTrace());

        var factory = new UIScreenFactory(uiRoot.transform, new UIPatchApplier());
        UIScreen screen = factory.Create(result);
        assets.Track(screen.gameObject);

        Assert.That(screen, Is.Not.Null);
        Assert.That(screen.transform.parent, Is.SameAs(uiRoot.transform));
        Assert.That(screen, Is.Not.SameAs(prefab.GetComponent<UIScreen>()), "instantiated, not the source");

        WidgetHandle handle = screen.GetWidgetHandle("PrimaryContent");
        Assert.That(handle, Is.Not.Null);
        Assert.That(handle.RectTransform.sizeDelta, Is.EqualTo(new Vector2(1400, 640)));
    }
}
