using NUnit.Framework;

// M3.1/M3.2: the device matrix is written as expectations first; the
// classifier thresholds are pinned here so a change is a deliberate edit.
public class DisplayLayoutClassifierTests
{
    // ---- Device matrix (M3 §4) ----

    [TestCase(1920, 1080, DisplayLayoutClass.Standard,  TestName = "D1 1920x1080 16:9 -> Standard")]
    [TestCase(2560, 1440, DisplayLayoutClass.Standard,  TestName = "D2 2560x1440 16:9 -> Standard")]
    [TestCase(2340, 1080, DisplayLayoutClass.Wide,      TestName = "D3 2340x1080 19.5:9 -> Wide")]
    [TestCase(2400, 1080, DisplayLayoutClass.Wide,      TestName = "D4 2400x1080 20:9 -> Wide")]
    [TestCase(2048, 1536, DisplayLayoutClass.Compact,   TestName = "D5 2048x1536 4:3 -> Compact")]
    // ---- Boundary aspects (M3 §4 추가) ----
    [TestCase(1920, 1200, DisplayLayoutClass.Standard,  TestName = "16:10 = 1.6 -> Standard (lower edge)")]
    [TestCase(1500, 1000, DisplayLayoutClass.Compact,   TestName = "3:2 = 1.5 -> Compact")]
    [TestCase(2160, 1080, DisplayLayoutClass.Wide,      TestName = "18:9 = 2.0 -> Wide (lower edge)")]
    [TestCase(2520, 1080, DisplayLayoutClass.UltraWide, TestName = "21:9 = 2.333 -> UltraWide")]
    [TestCase(1080, 2400, DisplayLayoutClass.Compact,   TestName = "portrait -> Compact (out of M3 scope, documented)")]
    public void DeviceMatrix_ClassifiesAsExpected(int w, int h, DisplayLayoutClass expected)
    {
        var display = DisplayContext.FullScreen(w, h);
        Assert.That(DisplayLayoutClassifier.Classify(display), Is.EqualTo(expected));
    }

    [Test]
    public void Thresholds_AreInclusiveLowerBounds()
    {
        Assert.That(DisplayLayoutClassifier.Classify(DisplayLayoutClassifier.StandardMin),  Is.EqualTo(DisplayLayoutClass.Standard));
        Assert.That(DisplayLayoutClassifier.Classify(DisplayLayoutClassifier.WideMin),      Is.EqualTo(DisplayLayoutClass.Wide));
        Assert.That(DisplayLayoutClassifier.Classify(DisplayLayoutClassifier.UltraWideMin), Is.EqualTo(DisplayLayoutClass.UltraWide));

        Assert.That(DisplayLayoutClassifier.Classify(DisplayLayoutClassifier.StandardMin  - 0.0001f), Is.EqualTo(DisplayLayoutClass.Compact));
        Assert.That(DisplayLayoutClassifier.Classify(DisplayLayoutClassifier.WideMin      - 0.0001f), Is.EqualTo(DisplayLayoutClass.Standard));
        Assert.That(DisplayLayoutClassifier.Classify(DisplayLayoutClassifier.UltraWideMin - 0.0001f), Is.EqualTo(DisplayLayoutClass.Wide));
    }

    [Test]
    public void Thresholds_AreOrdered()
    {
        Assert.That(DisplayLayoutClassifier.StandardMin, Is.LessThan(DisplayLayoutClassifier.WideMin));
        Assert.That(DisplayLayoutClassifier.WideMin,     Is.LessThan(DisplayLayoutClassifier.UltraWideMin));
    }

    [Test]
    public void ContextOverload_UsesAspectRatio()
    {
        var display = DisplayContext.FullScreen(2400, 1080);
        Assert.That(DisplayLayoutClassifier.Classify(display),
            Is.EqualTo(DisplayLayoutClassifier.Classify(display.AspectRatio)));
    }
}
