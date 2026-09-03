using System.Collections.Generic;
using NUnit.Framework;
using static ResolveTestKit;

// M2: VariantCondition.Matches(ui, display) is pure. Every branch is driven
// by the two explicit inputs; nothing here can depend on the Editor's screen.
public class VariantConditionTests
{
    private static readonly UIContext Light = Ui("Light", "ko-KR");
    private static readonly UIContext Dark  = Ui("Dark",  "ko-KR");

    // ---- empty = any ----

    [Test]
    public void EmptyCondition_MatchesAnyInput()
    {
        Assert.That(Always().Matches(Light, Desktop169), Is.True);
        Assert.That(Always().Matches(Ui("Dark", "en-US"), Portrait), Is.True);
    }

    // ---- UIContext axis ----

    [Test]
    public void Theme_MatchesOnlyEqualId()
    {
        Assert.That(Theme("Dark").Matches(Dark,  Desktop169), Is.True);
        Assert.That(Theme("Dark").Matches(Light, Desktop169), Is.False);
    }

    [Test]
    public void Locale_MatchesOnlyEqualId()
    {
        Assert.That(Locale("ko-KR").Matches(Light, Desktop169), Is.True);
        Assert.That(Locale("ja-JP").Matches(Light, Desktop169), Is.False);
    }

    [Test]
    public void Experiment_RequiresKeyToBePresent()
    {
        var cond = new VariantCondition { experimentKey = "HomeLayoutTest" };

        var withKey   = Ui(experiments: new Dictionary<ExperimentKey, VariantId> { { "HomeLayoutTest", "B" } });
        var otherKey  = Ui(experiments: new Dictionary<ExperimentKey, VariantId> { { "ShopTest", "B" } });
        var noneAtAll = Ui(experiments: null);

        Assert.That(cond.Matches(withKey,   Desktop169), Is.True);
        Assert.That(cond.Matches(otherKey,  Desktop169), Is.False);
        Assert.That(cond.Matches(noneAtAll, Desktop169), Is.False);
    }

    [Test]
    public void Experiment_VariantValueMustMatch_WhenSpecified()
    {
        var wantsB = new VariantCondition { experimentKey = "HomeLayoutTest", experimentVariantId = "B" };
        var anyVal = new VariantCondition { experimentKey = "HomeLayoutTest" };

        var assignedA = Ui(experiments: new Dictionary<ExperimentKey, VariantId> { { "HomeLayoutTest", "A" } });
        var assignedB = Ui(experiments: new Dictionary<ExperimentKey, VariantId> { { "HomeLayoutTest", "B" } });

        Assert.That(wantsB.Matches(assignedB, Desktop169), Is.True);
        Assert.That(wantsB.Matches(assignedA, Desktop169), Is.False);
        Assert.That(anyVal.Matches(assignedA, Desktop169), Is.True);
    }

    // ---- DisplayContext axis ----

    [Test]
    public void Platform_ComparesDisplayPlatform()
    {
        Assert.That(Platform(DisplayPlatform.Desktop).Matches(Light, Desktop169), Is.True);
        Assert.That(Platform(DisplayPlatform.Desktop).Matches(Light, Mobile209),  Is.False);
        Assert.That(Platform(DisplayPlatform.Mobile).Matches(Light, Mobile209),   Is.True);
        Assert.That(Platform(DisplayPlatform.Console).Matches(Light, Desktop169), Is.False);
    }

    [Test]
    public void Platform_IgnoredWhenToggleOff()
    {
        var cond = new VariantCondition { usePlatform = false, platform = DisplayPlatform.Console };
        Assert.That(cond.Matches(Light, Desktop169), Is.True);
    }

    [Test]
    public void Aspect_Landscape_MatchesLandscapeOnly()
    {
        var cond = Aspect(AspectRule.Landscape);

        Assert.That(cond.Matches(Light, Desktop169), Is.True);
        Assert.That(cond.Matches(Light, Tablet43),   Is.True);
        Assert.That(cond.Matches(Light, Portrait),   Is.False);
        Assert.That(cond.Matches(Light, Square),     Is.False, "Square is neither landscape nor portrait");
    }

    [Test]
    public void Aspect_Portrait_MatchesPortraitOnly()
    {
        var cond = Aspect(AspectRule.Portrait);

        Assert.That(cond.Matches(Light, Portrait),   Is.True);
        Assert.That(cond.Matches(Light, Desktop169), Is.False);
        Assert.That(cond.Matches(Light, Square),     Is.False);
    }

    [Test]
    public void Aspect_Range_IsInclusiveOnBothEdges()
    {
        // Edges computed the same way DisplayContext computes them, so equality is exact.
        float lo = 1920f / 1080f;   // 16:9
        float hi = 2400f / 1080f;   // 20:9
        var cond = Aspect(AspectRule.Range, lo, hi);

        Assert.That(cond.Matches(Light, Desktop169), Is.True,  "low edge");
        Assert.That(cond.Matches(Light, Mobile209),  Is.True,  "high edge");
        Assert.That(cond.Matches(Light, Mobile195),  Is.True,  "inside");
        Assert.That(cond.Matches(Light, Tablet43),   Is.False, "below");
        Assert.That(cond.Matches(Light, DisplayContext.FullScreen(2520, 1080)), Is.False, "above (21:9)");
    }

    [Test]
    public void Aspect_IgnoredWhenToggleOff()
    {
        var cond = new VariantCondition { useAspectRatio = false, aspectRule = AspectRule.Portrait };
        Assert.That(cond.Matches(Light, Desktop169), Is.True);
    }

    // ---- combination ----

    [Test]
    public void AllEnabledClauses_AreAndTogether()
    {
        var cond = new VariantCondition
        {
            themeId        = "Dark",
            usePlatform    = true,  platform   = DisplayPlatform.Mobile,
            useAspectRatio = true,  aspectRule = AspectRule.Landscape,
        };

        Assert.That(cond.Matches(Dark,  Mobile209),  Is.True);
        Assert.That(cond.Matches(Light, Mobile209),  Is.False, "theme");
        Assert.That(cond.Matches(Dark,  Desktop169), Is.False, "platform");
        Assert.That(cond.Matches(Dark,  Portrait),   Is.False, "aspect");
    }
}
