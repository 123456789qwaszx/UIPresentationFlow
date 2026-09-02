using System;
using NUnit.Framework;
using UnityEngine;

// M1: DisplayContext is a strict, immutable fact model. These tests pin the
// derived-value rules (aspect, orientation, normalized safe area) and the
// validation policy (invalid input throws; only default() is ever invalid).
public class DisplayContextTests
{
    private const float Eps = 1e-5f;

    // ---- Aspect ratio & orientation (M1 §11 + M3 device matrix) ----

    [TestCase(1920, 1080, 1.7777778f, DisplayOrientation.Landscape)] // 16:9
    [TestCase(2560, 1440, 1.7777778f, DisplayOrientation.Landscape)] // 16:9 hi
    [TestCase(2340, 1080, 2.1666667f, DisplayOrientation.Landscape)] // 19.5:9
    [TestCase(2400, 1080, 2.2222222f, DisplayOrientation.Landscape)] // 20:9
    [TestCase(2048, 1536, 1.3333334f, DisplayOrientation.Landscape)] // 4:3
    [TestCase(1080, 2400, 0.45f,      DisplayOrientation.Portrait)]  // 20:9 portrait
    [TestCase(1000, 1000, 1.0f,       DisplayOrientation.Square)]
    public void AspectAndOrientation_AreDerivedFromResolution(
        int w, int h, float expectedAspect, DisplayOrientation expectedOrientation)
    {
        var ctx = DisplayContext.FullScreen(w, h);

        Assert.That(ctx.IsValid, Is.True);
        Assert.That(ctx.Resolution, Is.EqualTo(new Vector2Int(w, h)));
        Assert.That(ctx.AspectRatio, Is.EqualTo(expectedAspect).Within(Eps));
        Assert.That(ctx.Orientation, Is.EqualTo(expectedOrientation));
    }

    // ---- Safe area ----

    [Test]
    public void FullScreen_HasNoInsets_AndNormalizedIsUnitRect()
    {
        var ctx = DisplayContext.FullScreen(1920, 1080);

        Assert.That(ctx.HasSafeAreaInsets, Is.False);
        AssertRect(ctx.SafeAreaNormalized, 0f, 0f, 1f, 1f);
    }

    [Test]
    public void SideInsets_NormalizeAgainstResolution()
    {
        // 2400x1080 with 100px cut from each side
        var ctx = new DisplayContext(
            new Vector2Int(2400, 1080),
            new Rect(100, 0, 2200, 1080),
            DisplayPlatform.Mobile);

        Assert.That(ctx.HasSafeAreaInsets, Is.True);
        AssertRect(ctx.SafeAreaNormalized, 100f / 2400f, 0f, 2200f / 2400f, 1f);
    }

    [Test]
    public void CombinedInsets_NormalizeOnBothAxes()
    {
        // 100 left/right, 40 bottom, 40 top -> (100, 40, W-200, H-80)
        var ctx = new DisplayContext(
            new Vector2Int(2400, 1080),
            new Rect(100, 40, 2200, 1000),
            DisplayPlatform.Mobile);

        AssertRect(ctx.SafeAreaNormalized,
            100f / 2400f, 40f / 1080f, 2200f / 2400f, 1000f / 1080f);
    }

    [Test]
    public void SafeAreaEqualToResolution_CountsAsNoInsets()
    {
        var ctx = new DisplayContext(
            new Vector2Int(1920, 1080),
            new Rect(0, 0, 1920, 1080),
            DisplayPlatform.Desktop);

        Assert.That(ctx.HasSafeAreaInsets, Is.False);
    }

    // ---- Validation policy (A: constructor rejects invalid input) ----

    [TestCase(0, 1080)]
    [TestCase(1920, 0)]
    [TestCase(-1920, 1080)]
    [TestCase(1920, -1080)]
    public void InvalidResolution_Throws(int w, int h)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DisplayContext(new Vector2Int(w, h), new Rect(0, 0, 1, 1), DisplayPlatform.Unknown));
    }

    private static readonly object[] InvalidSafeAreas =
    {
        new object[] { new Rect(-1, 0, 1920, 1080),               "negative x" },
        new object[] { new Rect(0, -1, 1920, 1080),               "negative y" },
        new object[] { new Rect(0, 0, 1921, 1080),                "exceeds width" },
        new object[] { new Rect(0, 0, 1920, 1081),                "exceeds height" },
        new object[] { new Rect(100, 0, 1900, 1080),              "x + width exceeds" },
        new object[] { new Rect(0, 0, -10, 1080),                 "negative width" },
        new object[] { new Rect(0, 0, 1920, -10),                 "negative height" },
        new object[] { new Rect(float.NaN, 0, 1, 1),              "NaN" },
        new object[] { new Rect(0, 0, float.PositiveInfinity, 1), "infinite" },
    };

    [TestCaseSource(nameof(InvalidSafeAreas))]
    public void SafeAreaOutsideResolution_Throws(Rect safe, string why)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DisplayContext(new Vector2Int(1920, 1080), safe, DisplayPlatform.Unknown), why);
    }

    [Test]
    public void ZeroSizeSafeArea_IsAllowed()
    {
        // Degenerate but not contradictory; what to do with it is a consumer policy.
        var ctx = new DisplayContext(new Vector2Int(1920, 1080), new Rect(0, 0, 0, 0), DisplayPlatform.Unknown);
        Assert.That(ctx.HasSafeAreaInsets, Is.True);
    }

    [Test]
    public void Default_IsInvalid_AndDerivedValuesDoNotThrow()
    {
        DisplayContext ctx = default;

        Assert.That(ctx.IsValid, Is.False);
        Assert.That(ctx.AspectRatio, Is.EqualTo(0f));
        Assert.That(ctx.HasSafeAreaInsets, Is.False);
        Assert.That(ctx.SafeAreaNormalized, Is.EqualTo(default(Rect)));
        Assert.That(ctx.ToString(), Does.Contain("invalid"));
    }

    // ---- Equality (needed by M2 "same input -> same output" tests) ----

    [Test]
    public void Equality_IsValueBased()
    {
        var a = new DisplayContext(new Vector2Int(2400, 1080), new Rect(100, 0, 2200, 1080), DisplayPlatform.Mobile);
        var b = new DisplayContext(new Vector2Int(2400, 1080), new Rect(100, 0, 2200, 1080), DisplayPlatform.Mobile);
        var differentPlatform = new DisplayContext(new Vector2Int(2400, 1080), new Rect(100, 0, 2200, 1080), DisplayPlatform.Desktop);
        var differentSafe     = new DisplayContext(new Vector2Int(2400, 1080), new Rect(0, 0, 2400, 1080),   DisplayPlatform.Mobile);

        Assert.That(a, Is.EqualTo(b));
        Assert.That(a == b, Is.True);
        Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
        Assert.That(a, Is.Not.EqualTo(differentPlatform));
        Assert.That(a, Is.Not.EqualTo(differentSafe));
        Assert.That(a != differentSafe, Is.True);
    }

    private static void AssertRect(Rect actual, float x, float y, float w, float h)
    {
        Assert.That(actual.x,      Is.EqualTo(x).Within(Eps), "x");
        Assert.That(actual.y,      Is.EqualTo(y).Within(Eps), "y");
        Assert.That(actual.width,  Is.EqualTo(w).Within(Eps), "width");
        Assert.That(actual.height, Is.EqualTo(h).Within(Eps), "height");
    }
}
