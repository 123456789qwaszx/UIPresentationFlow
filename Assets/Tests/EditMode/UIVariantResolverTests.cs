using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static ResolveTestKit;

// M2 contract for UIVariantResolver:
//   deterministic, input-preserving, highest-priority-wins-per-field,
//   authored order on ties, forced override semantics, explainable trace.
public class UIVariantResolverTests
{
    private readonly UIVariantResolver _resolver = new();
    private static UIContext Light => Ui("Light");

    // ---- base / no match ----

    [Test]
    public void NoRules_ReturnsBaseFields()
    {
        using var assets = new Assets();
        var spec = Spec("home");
        spec.templatePrefab = assets.Prefab("Base");
        spec.baseTheme      = assets.Theme("BaseTheme");
        spec.baseLayout     = assets.Layout("BaseLayout");

        var r = _resolver.Resolve(spec, Light, Desktop169);

        Assert.That(r.ScreenKey, Is.EqualTo(new ScreenKey("home")));
        Assert.That(r.BaseSpec,  Is.SameAs(spec));
        Assert.That(r.Prefab,    Is.SameAs(spec.templatePrefab));
        Assert.That(r.Theme,     Is.SameAs(spec.baseTheme));
        Assert.That(r.Layout,    Is.SameAs(spec.baseLayout));
        Assert.That(r.AppliedVariantIds, Is.Empty);
    }

    [Test]
    public void NoMatchingRule_ReturnsBase()
    {
        using var assets = new Assets();
        var baseLayout = assets.Layout("Base");
        var spec = Spec("home", Rule("Dark", 100, Theme("Dark"), layout: assets.Layout("DarkLayout")));
        spec.baseLayout = baseLayout;

        var r = _resolver.Resolve(spec, Light, Desktop169);

        Assert.That(r.Layout, Is.SameAs(baseLayout));
        Assert.That(r.AppliedVariantIds, Is.Empty);
    }

    // ---- determinism & purity ----

    [Test]
    public void SameInputs_SameOutput_100Times()
    {
        using var assets = new Assets();
        var wide = assets.Layout("Wide");
        var dark = assets.Theme("Dark");
        var spec = Spec("home",
            Rule("A", 10,  Always(), layout: assets.Layout("A")),
            Rule("B", 100, Always(), layout: wide),
            Rule("C", 50,  Always(), theme: dark));

        var first = _resolver.Resolve(spec, Light, Mobile209);

        for (int i = 0; i < 100; i++)
        {
            var again = _resolver.Resolve(spec, Light, Mobile209);
            Assert.That(again.Prefab, Is.SameAs(first.Prefab));
            Assert.That(again.Theme,  Is.SameAs(dark));
            Assert.That(again.Layout, Is.SameAs(wide));
            Assert.That(again.AppliedVariantIds, Is.EqualTo(first.AppliedVariantIds));
        }
    }

    [Test]
    public void Resolve_DoesNotReorderOrReplaceAuthoredVariants()
    {
        using var assets = new Assets();
        var a = Rule("A", 10,  Always(), layout: assets.Layout("A"));
        var b = Rule("B", 100, Always(), layout: assets.Layout("B"));
        var c = Rule("C", 50,  Always(), layout: assets.Layout("C"));
        var spec = Spec("home", a, b, c);
        UIVariantRule[] arrayBefore = spec.variants;

        _resolver.Resolve(spec, Light, Desktop169);

        Assert.That(spec.variants, Is.SameAs(arrayBefore), "array instance replaced");
        Assert.That(spec.variants, Is.EqualTo(new[] { a, b, c }).AsCollection, "authored order changed");
    }

    // ---- priority policy: highest wins per field ----

    [Test]
    public void Prefab_HighestPriorityWins_RegardlessOfAuthoredOrder()
    {
        using var assets = new Assets();
        var low  = assets.Prefab("Low");
        var high = assets.Prefab("High");

        var lowFirst  = Spec("home", Rule("L", 50, prefab: low),  Rule("H", 100, prefab: high));
        var highFirst = Spec("home", Rule("H", 100, prefab: high), Rule("L", 50, prefab: low));

        Assert.That(_resolver.Resolve(lowFirst,  Light, Desktop169).Prefab, Is.SameAs(high));
        Assert.That(_resolver.Resolve(highFirst, Light, Desktop169).Prefab, Is.SameAs(high));
    }

    [Test]
    public void Theme_HighestPriorityWins_RegardlessOfAuthoredOrder()
    {
        using var assets = new Assets();
        var low  = assets.Theme("Low");
        var high = assets.Theme("High");

        var lowFirst  = Spec("home", Rule("L", 50, theme: low),  Rule("H", 100, theme: high));
        var highFirst = Spec("home", Rule("H", 100, theme: high), Rule("L", 50, theme: low));

        Assert.That(_resolver.Resolve(lowFirst,  Light, Desktop169).Theme, Is.SameAs(high));
        Assert.That(_resolver.Resolve(highFirst, Light, Desktop169).Theme, Is.SameAs(high));
    }

    [Test]
    public void Layout_HighestPriorityWins_RegardlessOfAuthoredOrder()
    {
        using var assets = new Assets();
        var low  = assets.Layout("Low");
        var high = assets.Layout("High");

        var lowFirst  = Spec("home", Rule("L", 50, layout: low),  Rule("H", 100, layout: high));
        var highFirst = Spec("home", Rule("H", 100, layout: high), Rule("L", 50, layout: low));

        Assert.That(_resolver.Resolve(lowFirst,  Light, Desktop169).Layout, Is.SameAs(high));
        Assert.That(_resolver.Resolve(highFirst, Light, Desktop169).Layout, Is.SameAs(high));
    }

    [Test]
    public void Fields_ResolveIndependently()
    {
        using var assets = new Assets();
        var wide = assets.Layout("Wide");
        var dark = assets.Theme("Dark");
        var spec = Spec("home",
            Rule("Wide", 100, layout: wide),   // layout only
            Rule("Dark", 50,  theme:  dark));  // theme only

        var r = _resolver.Resolve(spec, Light, Desktop169);

        Assert.That(r.Layout, Is.SameAs(wide));
        Assert.That(r.Theme,  Is.SameAs(dark));
    }

    [Test]
    public void SamePriority_AuthoredOrderWins()
    {
        using var assets = new Assets();
        var la = assets.Layout("LA");
        var lb = assets.Layout("LB");

        var aFirst = Spec("home", Rule("A", 100, layout: la), Rule("B", 100, layout: lb));
        var bFirst = Spec("home", Rule("B", 100, layout: lb), Rule("A", 100, layout: la));

        Assert.That(_resolver.Resolve(aFirst, Light, Desktop169).Layout, Is.SameAs(la));
        Assert.That(_resolver.Resolve(bFirst, Light, Desktop169).Layout, Is.SameAs(lb));
    }

    [Test]
    public void NonMatchingHighPriority_DoesNotLockTheField()
    {
        using var assets = new Assets();
        var x = assets.Layout("X");
        var y = assets.Layout("Y");
        var spec = Spec("home",
            Rule("DarkOnly", 100, Theme("Dark"), layout: x),
            Rule("Fallback", 50,  Always(),      layout: y));

        Assert.That(_resolver.Resolve(spec, Light, Desktop169).Layout, Is.SameAs(y));
    }

    [Test]
    public void AppliedVariantIds_ListMatchesInPriorityOrder()
    {
        using var assets = new Assets();
        var spec = Spec("home",
            Rule("A", 10),
            Rule("B", 100),
            Rule("C", 50),
            Rule("D", 100, Theme("Dark")));   // does not match

        var r = _resolver.Resolve(spec, Light, Desktop169);

        Assert.That(r.AppliedVariantIds, Is.EqualTo(new[] { "B", "C", "A" }).AsCollection);
    }

    // ---- robustness ----

    [Test]
    public void NullRuleEntry_IsSkipped()
    {
        using var assets = new Assets();
        var wide = assets.Layout("Wide");
        var spec = Spec("home", null, Rule("Wide", 10, layout: wide));

        Assert.That(_resolver.Resolve(spec, Light, Desktop169).Layout, Is.SameAs(wide));
    }

    [Test]
    public void NullCondition_IsSkippedAndTraced()
    {
        using var assets = new Assets();
        var baseLayout = assets.Layout("Base");
        var rule = Rule("Broken", 100, layout: assets.Layout("Broken"));
        rule.condition = null;
        var spec = Spec("home", rule);
        spec.baseLayout = baseLayout;

        var trace = new UIResolveTrace();
        var r = _resolver.Resolve(spec, Light, Desktop169, trace);

        Assert.That(r.Layout, Is.SameAs(baseLayout));
        Assert.That(trace.Lines.Any(l => l.Contains("Broken") && l.Contains("SKIP")), Is.True, trace.Dump());
    }

    [Test]
    public void InvalidDisplay_Throws()
    {
        var spec = Spec("home");
        Assert.Throws<ArgumentException>(() => _resolver.Resolve(spec, Light, default));
    }

    [Test]
    public void NullSpec_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _resolver.Resolve(null, Light, Desktop169));
    }

    // ---- forced override ----

    [Test]
    public void Forced_AppliesNamedRule_IgnoringItsConditionAndOtherRules()
    {
        using var assets = new Assets();
        var wide = assets.Layout("Wide");
        var darkLayout = assets.Layout("DarkLayout");
        var spec = Spec("home",
            Rule("Wide", 100, Always(),      layout: wide),
            Rule("Dark", 50,  Theme("Dark"), layout: darkLayout));

        var ui = Ui("Light", overrides: Force("home", "Dark"));
        var r = _resolver.Resolve(spec, ui, Desktop169);

        Assert.That(r.Layout, Is.SameAs(darkLayout), "forced rule applied although its theme condition fails");
        Assert.That(r.AppliedVariantIds, Is.EqualTo(new[] { "Dark" }).AsCollection, "other matching rules ignored");
    }

    [Test]
    public void Forced_UnknownId_FallsBackToNormalEvaluation()
    {
        using var assets = new Assets();
        var wide = assets.Layout("Wide");
        var spec = Spec("home", Rule("Wide", 100, layout: wide));

        var ui = Ui("Light", overrides: Force("home", "DoesNotExist"));
        var trace = new UIResolveTrace();
        var r = _resolver.Resolve(spec, ui, Desktop169, trace);

        Assert.That(r.Layout, Is.SameAs(wide));
        Assert.That(trace.Lines.Any(l => l.Contains("DoesNotExist") && l.Contains("not found")), Is.True, trace.Dump());
    }

    [Test]
    public void Forced_OverrideForOtherScreen_IsIgnored()
    {
        using var assets = new Assets();
        var wide = assets.Layout("Wide");
        var spec = Spec("home", Rule("Wide", 100, layout: wide));

        var ui = Ui("Light", overrides: Force("shop", "Whatever"));

        Assert.That(_resolver.Resolve(spec, ui, Desktop169).Layout, Is.SameAs(wide));
    }

    [Test]
    public void Forced_DuplicateId_FirstAuthoredWins()
    {
        using var assets = new Assets();
        var first  = assets.Layout("First");
        var second = assets.Layout("Second");
        var spec = Spec("home", Rule("Dup", 1, layout: first), Rule("Dup", 100, layout: second));

        var ui = Ui("Light", overrides: Force("home", "Dup"));

        Assert.That(_resolver.Resolve(spec, ui, Desktop169).Layout, Is.SameAs(first));
    }

    // ---- trace ----

    [Test]
    public void Trace_RecordsDisplayInputMatchesMissesAndWinners()
    {
        using var assets = new Assets();
        var spec = Spec("home",
            Rule("Wide", 100, Aspect(AspectRule.Range, 2.0f, 2.4f), layout: assets.Layout("WideLayout")),
            Rule("Dark", 50,  Theme("Dark"),                         theme:  assets.Theme("DarkTheme")));

        var trace = new UIResolveTrace();
        _resolver.Resolve(spec, Light, Mobile209, trace);
        string dump = trace.Dump();

        Assert.That(dump, Does.Contain("[Input] display 2400x1080"));
        Assert.That(dump, Does.Contain("Wide p100 MATCH"));
        Assert.That(dump, Does.Contain("Dark p50 MISS"));
        Assert.That(dump, Does.Contain("[Winner] layout <- Wide"));
        Assert.That(dump, Does.Contain("[Result]"));
    }
}
