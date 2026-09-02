using NUnit.Framework;
using UnityEngine;

// M0 baseline smoke tests.
// Purpose: prove the Tests assembly can reference runtime code and that the
// resolver runs without touching Unity environment APIs (no platform/aspect
// conditions are enabled here). Behavioral coverage starts in M1/M2.
public class UIVariantResolverSmokeTests
{
    [Test]
    public void Resolve_NoVariants_ReturnsBaseFields()
    {
        var spec = new UIScreenSpec
        {
            screenKey = new ScreenKey("smoke"),
            variants  = null,
        };

        ResolvedUIScreen resolved = new UIVariantResolver().Resolve(spec, UIContext.Default);

        Assert.That(resolved.ScreenKey, Is.EqualTo(new ScreenKey("smoke")));
        Assert.That(resolved.Prefab, Is.Null);
        Assert.That(resolved.Theme, Is.Null);
        Assert.That(resolved.Layout, Is.Null);
        Assert.That(resolved.AppliedVariantIds, Is.Empty);
        Assert.That(resolved.DecisionTrace, Does.Contain("screen=smoke"));
    }

    [Test]
    public void Resolve_MatchingThemeRule_OverridesLayout()
    {
        LayoutPatchSpec darkLayout = ScriptableObject.CreateInstance<LayoutPatchSpec>();
        try
        {
            var spec = new UIScreenSpec
            {
                screenKey = new ScreenKey("smoke"),
                variants = new[]
                {
                    new UIVariantRule
                    {
                        variantId      = "Dark",
                        priority       = 10,
                        condition      = new VariantCondition { themeId = "Dark" },
                        overrideLayout = darkLayout,
                    },
                },
            };
            var ctx = new UIContext("Dark", "ko-KR", null, null);

            ResolvedUIScreen resolved = new UIVariantResolver().Resolve(spec, ctx);

            Assert.That(resolved.Layout, Is.SameAs(darkLayout));
            Assert.That(resolved.AppliedVariantIds, Is.EqualTo(new[] { "Dark" }));
        }
        finally
        {
            Object.DestroyImmediate(darkLayout);
        }
    }
}
