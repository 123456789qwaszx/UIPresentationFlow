using NUnit.Framework;
using UnityEngine;

// M1: the provider is the only Unity-API boundary. Its pure helpers
// (platform mapping, safe-area clamping) are tested as tables; GetCurrent()
// is checked against the live Screen state of the Editor running the tests.
public class UnityDisplayContextProviderTests
{
    // ---- Platform mapping table (M1.1) ----

    [TestCase(RuntimePlatform.WindowsEditor,      DisplayPlatform.Desktop)]
    [TestCase(RuntimePlatform.WindowsPlayer,      DisplayPlatform.Desktop)]
    [TestCase(RuntimePlatform.OSXEditor,          DisplayPlatform.Desktop)]
    [TestCase(RuntimePlatform.OSXPlayer,          DisplayPlatform.Desktop)]
    [TestCase(RuntimePlatform.LinuxEditor,        DisplayPlatform.Desktop)]
    [TestCase(RuntimePlatform.LinuxPlayer,        DisplayPlatform.Desktop)]
    [TestCase(RuntimePlatform.Android,            DisplayPlatform.Mobile)]
    [TestCase(RuntimePlatform.IPhonePlayer,       DisplayPlatform.Mobile)]
    [TestCase(RuntimePlatform.PS4,                DisplayPlatform.Console)]
    [TestCase(RuntimePlatform.PS5,                DisplayPlatform.Console)]
    [TestCase(RuntimePlatform.XboxOne,            DisplayPlatform.Console)]
    [TestCase(RuntimePlatform.GameCoreXboxOne,    DisplayPlatform.Console)]
    [TestCase(RuntimePlatform.GameCoreXboxSeries, DisplayPlatform.Console)]
    [TestCase(RuntimePlatform.Switch,             DisplayPlatform.Console)]
    [TestCase(RuntimePlatform.WebGLPlayer,        DisplayPlatform.Unknown)]
    public void MapPlatform_FollowsDocumentedTable(RuntimePlatform input, DisplayPlatform expected)
    {
        Assert.That(UnityDisplayContextProvider.MapPlatform(input), Is.EqualTo(expected));
    }

    // ---- Safe-area clamping at the boundary ----

    private static readonly Vector2Int Res = new Vector2Int(1920, 1080);

    [Test]
    public void ClampToResolution_RectInside_IsUnchanged()
    {
        var inside = new Rect(100, 40, 1700, 1000);
        Assert.That(UnityDisplayContextProvider.ClampToResolution(inside, Res), Is.EqualTo(inside));
    }

    [Test]
    public void ClampToResolution_RectExceedingBounds_IsClampedToResolution()
    {
        var oversized = new Rect(-50, -50, 3000, 2000);
        var clamped   = UnityDisplayContextProvider.ClampToResolution(oversized, Res);

        Assert.That(clamped, Is.EqualTo(new Rect(0, 0, 1920, 1080)));
    }

    [Test]
    public void ClampToResolution_PartialOverflow_KeepsInsidePortion()
    {
        var overflowRight = new Rect(1800, 0, 500, 1080);      // xMax = 2300
        var clamped       = UnityDisplayContextProvider.ClampToResolution(overflowRight, Res);

        Assert.That(clamped, Is.EqualTo(new Rect(1800, 0, 120, 1080)));
    }

    [Test]
    public void ClampToResolution_NegativeWidth_CollapsesToZeroWidth()
    {
        var negative = new Rect(100, 0, -50, 1080);
        var clamped  = UnityDisplayContextProvider.ClampToResolution(negative, Res);

        Assert.That(clamped.x,     Is.EqualTo(100f));
        Assert.That(clamped.width, Is.EqualTo(0f));
    }

    [Test]
    public void ClampToResolution_NonFinite_FallsBackToFullScreen()
    {
        var nan = new Rect(float.NaN, 0, 100, 100);
        Assert.That(UnityDisplayContextProvider.ClampToResolution(nan, Res), Is.EqualTo(new Rect(0, 0, 1920, 1080)));
    }

    [Test]
    public void ClampToResolution_Output_IsAlwaysAcceptedByDisplayContext()
    {
        // Whatever Screen.safeArea reports, the clamped rect must construct a context.
        var weird = new Rect(-999, 5000, 99999, -3);
        var rect  = UnityDisplayContextProvider.ClampToResolution(weird, Res);

        Assert.DoesNotThrow(() => new DisplayContext(Res, rect, DisplayPlatform.Unknown));
    }

    // ---- Live capture (M1 §12 manual gate, automated) ----

    [Test]
    public void GetCurrent_ReflectsEditorScreenState()
    {
        int expectedW = Screen.width;
        int expectedH = Screen.height;

        var ctx = new UnityDisplayContextProvider().GetCurrent();

        Assert.That(ctx.IsValid, Is.True);
        Assert.That(ctx.Resolution, Is.EqualTo(new Vector2Int(expectedW, expectedH)));
        Assert.That(ctx.Platform, Is.EqualTo(DisplayPlatform.Desktop),
            "Tests run inside the Editor, which maps to Desktop.");
        Assert.That(ctx.SafeAreaPixels.xMax, Is.LessThanOrEqualTo(expectedW));
        Assert.That(ctx.SafeAreaPixels.yMax, Is.LessThanOrEqualTo(expectedH));
    }
}
