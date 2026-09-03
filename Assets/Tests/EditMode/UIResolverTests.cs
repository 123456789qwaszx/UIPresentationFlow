using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static ResolveTestKit;

// M2: UIResolver is strict on lookup failure, passes the DisplayContext down,
// and turns the decision into a patch list. The aspect test is the gate for
// "screen-ratio conditions evaluate in EditMode without Unity's Screen".
public class UIResolverTests
{
    [Test]
    public void UnknownScreenKey_Throws_WithKeyInMessage()
    {
        using var assets = new Assets();
        var catalog  = assets.Catalog(init: true, assets.SpecAsset(Spec("home")));
        var resolver = new UIResolver(catalog, Ui());

        var ex = Assert.Throws<KeyNotFoundException>(() => resolver.Resolve(new ScreenKey("shop"), Desktop169));
        Assert.That(ex.Message, Does.Contain("shop"));
    }

    [Test]
    public void UninitializedCatalog_Throws()
    {
        using var assets = new Assets();
        var catalog  = assets.Catalog(init: false, assets.SpecAsset(Spec("home")));
        var resolver = new UIResolver(catalog, Ui());

        Assert.Throws<KeyNotFoundException>(() => resolver.Resolve(new ScreenKey("home"), Desktop169));
    }

    [Test]
    public void NullCatalog_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new UIResolver(null, Ui()));
    }

    [Test]
    public void InvalidDisplay_Throws()
    {
        using var assets = new Assets();
        var catalog  = assets.Catalog(init: true, assets.SpecAsset(Spec("home")));
        var resolver = new UIResolver(catalog, Ui());

        Assert.Throws<ArgumentException>(() => resolver.Resolve(new ScreenKey("home"), default));
    }

    [Test]
    public void BuildsOnePatchPerResolvedThemeAndLayout()
    {
        using var assets = new Assets();
        var withBoth = Spec("both");
        withBoth.baseTheme  = assets.Theme("T");
        withBoth.baseLayout = assets.Layout("L");
        var withNone = Spec("none");

        var catalog  = assets.Catalog(init: true, assets.SpecAsset(withBoth), assets.SpecAsset(withNone));
        var resolver = new UIResolver(catalog, Ui());

        var both = resolver.Resolve(new ScreenKey("both"), Desktop169);
        var none = resolver.Resolve(new ScreenKey("none"), Desktop169);

        Assert.That(both.Patches.Select(p => p.GetType()), Is.EquivalentTo(new[] { typeof(ThemeSpecPatch), typeof(LayoutSpecPatch) }));
        Assert.That(none.Patches, Is.Empty);
        Assert.That(both.Trace.Lines.Last(), Is.EqualTo("[Patches] 2"));
    }

    [Test]
    public void DisplayContext_SelectsLayout_WithoutUnityScreen()
    {
        using var assets = new Assets();
        var baseLayout = assets.Layout("Base");
        var wide       = assets.Layout("Wide");
        var compact    = assets.Layout("Compact");

        var spec = Spec("home",
            Rule("Wide",    100, Aspect(AspectRule.Range, 2.0f, 2.4f), layout: wide),
            Rule("Compact", 100, Aspect(AspectRule.Range, 1.0f, 1.6f), layout: compact));
        spec.baseLayout = baseLayout;

        var catalog  = assets.Catalog(init: true, assets.SpecAsset(spec));
        var resolver = new UIResolver(catalog, Ui());
        var key = new ScreenKey("home");

        Assert.That(resolver.Resolve(key, Mobile209).Resolved.Layout,    Is.SameAs(wide),       "20:9");
        Assert.That(resolver.Resolve(key, Mobile195).Resolved.Layout,    Is.SameAs(wide),       "19.5:9");
        Assert.That(resolver.Resolve(key, Tablet43).Resolved.Layout,     Is.SameAs(compact),    "4:3");
        Assert.That(resolver.Resolve(key, Desktop169).Resolved.Layout,   Is.SameAs(baseLayout), "16:9 -> base");
        Assert.That(resolver.Resolve(key, Desktop169Hi).Resolved.Layout, Is.SameAs(baseLayout), "16:9 hi -> base");
    }

    [Test]
    public void UIContext_IsSessionLevel_AndExposed()
    {
        using var assets = new Assets();
        var catalog  = assets.Catalog(init: true, assets.SpecAsset(Spec("home")));
        var resolver = new UIResolver(catalog, Ui("Dark", "ja-JP"));

        Assert.That(resolver.Context.ThemeId,  Is.EqualTo("Dark"));
        Assert.That(resolver.Context.LocaleId, Is.EqualTo("ja-JP"));
    }
}
