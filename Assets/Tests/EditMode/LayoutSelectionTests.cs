using NUnit.Framework;
using static ResolveTestKit;

// M3: the demo spec (class-based rules) selects the expected layout asset for
// every device in the matrix, end to end through UIResolver — no Unity Screen.
public class LayoutSelectionTests
{
    private const string BaseName = "Base", WideName = "Wide", CompactName = "Compact";

    // Mirrors AdaptiveDemoAssetGenerator.CreateSpec.
    private static UIScreenSpec DemoSpec(Assets assets)
    {
        var wide    = assets.Layout(WideName);
        var compact = assets.Layout(CompactName);

        var spec = Spec("adaptive_demo",
            ClassRule("Wide",      DisplayLayoutClass.Wide,      wide),
            ClassRule("UltraWide", DisplayLayoutClass.UltraWide, wide),
            ClassRule("Compact",   DisplayLayoutClass.Compact,   compact));
        spec.baseLayout = assets.Layout(BaseName);
        return spec;
    }

    private static UIVariantRule ClassRule(string id, DisplayLayoutClass cls, LayoutPatchSpec layout)
        => Rule(id, 100, new VariantCondition { useLayoutClass = true, layoutClass = cls }, layout: layout);

    [TestCase(1920, 1080, BaseName,    TestName = "D1 1920x1080 -> Base")]
    [TestCase(2560, 1440, BaseName,    TestName = "D2 2560x1440 -> Base")]
    [TestCase(2340, 1080, WideName,    TestName = "D3 2340x1080 -> Wide")]
    [TestCase(2400, 1080, WideName,    TestName = "D4 2400x1080 -> Wide")]
    [TestCase(2048, 1536, CompactName, TestName = "D5 2048x1536 -> Compact")]
    [TestCase(2520, 1080, WideName,    TestName = "21:9 -> Wide (UltraWide rule, same asset)")]
    [TestCase(1920, 1200, BaseName,    TestName = "16:10 -> Base")]
    [TestCase(1500, 1000, CompactName, TestName = "3:2 -> Compact")]
    public void DeviceMatrix_SelectsExpectedLayout(int w, int h, string expectedLayout)
    {
        using var assets = new Assets();
        var spec     = DemoSpec(assets);
        var resolver = new UIResolver(assets.Catalog(init: true, assets.SpecAsset(spec)), Ui());

        var result = resolver.Resolve(new ScreenKey("adaptive_demo"), DisplayContext.FullScreen(w, h));

        Assert.That(result.Resolved.Layout.name, Is.EqualTo(expectedLayout), result.Trace.Dump());
    }

    [Test]
    public void AppliedVariant_IsRecorded_ForNonBaseSelections()
    {
        using var assets = new Assets();
        var spec     = DemoSpec(assets);
        var resolver = new UIResolver(assets.Catalog(init: true, assets.SpecAsset(spec)), Ui());
        var key      = new ScreenKey("adaptive_demo");

        Assert.That(resolver.Resolve(key, Mobile209).Resolved.AppliedVariantIds,  Is.EqualTo(new[] { "Wide" }).AsCollection);
        Assert.That(resolver.Resolve(key, Tablet43).Resolved.AppliedVariantIds,   Is.EqualTo(new[] { "Compact" }).AsCollection);
        Assert.That(resolver.Resolve(key, Desktop169).Resolved.AppliedVariantIds, Is.Empty);
    }

    [Test]
    public void LayoutClass_And_AspectRange_AreAnded()
    {
        var cond = new VariantCondition
        {
            useLayoutClass = true,  layoutClass = DisplayLayoutClass.Wide,
            useAspectRatio = true,  aspectRule  = AspectRule.Range, aspectMin = 2.1f, aspectMax = 2.3f,
        };
        var ui = Ui();

        Assert.That(cond.Matches(ui, Mobile195), Is.True,  "19.5:9 = 2.167: Wide and in range");
        Assert.That(cond.Matches(ui, Mobile209), Is.True,  "20:9 = 2.222: Wide and in range");
        Assert.That(cond.Matches(ui, DisplayContext.FullScreen(2160, 1080)), Is.False, "18:9 = 2.0: Wide but below range");
        Assert.That(cond.Matches(ui, DisplayContext.FullScreen(2520, 1080)), Is.False, "21:9: in neither");
    }

    [Test]
    public void LayoutClass_IgnoredWhenToggleOff()
    {
        var cond = new VariantCondition { useLayoutClass = false, layoutClass = DisplayLayoutClass.UltraWide };
        Assert.That(cond.Matches(Ui(), Desktop169), Is.True);
    }

    [Test]
    public void WideLayout_And_DarkTheme_BothApply_FieldIndependent()
    {
        using var assets = new Assets();
        var wide = assets.Layout("Wide");
        var dark = assets.Theme("Dark");
        var spec = Spec("home",
            ClassRule("Wide", DisplayLayoutClass.Wide, wide),
            Rule("Dark", 50, Theme("Dark"), theme: dark));

        var r = new UIVariantResolver().Resolve(spec, Ui("Dark"), Mobile209);

        Assert.That(r.Layout, Is.SameAs(wide));
        Assert.That(r.Theme,  Is.SameAs(dark));
        Assert.That(r.AppliedVariantIds, Is.EqualTo(new[] { "Wide", "Dark" }).AsCollection);
    }
}
