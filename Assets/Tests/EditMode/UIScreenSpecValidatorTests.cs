using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static ResolveTestKit;

// M2.8: authoring-time validation. Messages are asserted by substring so the
// wording can improve without breaking tests.
public class UIScreenSpecValidatorTests
{
    private static UIScreenSpec ValidSpec(Assets assets)
    {
        var spec = Spec("home",
            Rule("Wide",    100, Aspect(AspectRule.Range, 2.0f, 2.4f), layout: assets.Layout("Wide")),
            Rule("Compact", 100, Aspect(AspectRule.Range, 1.0f, 1.6f), layout: assets.Layout("Compact")));
        spec.templatePrefab = assets.Prefab("Template");
        return spec;
    }

    [Test]
    public void ValidSpec_HasNoProblems()
    {
        using var assets = new Assets();
        Assert.That(UIScreenSpecValidator.Validate(ValidSpec(assets)), Is.Empty);
    }

    [Test]
    public void NullSpec_IsReported()
    {
        Assert.That(UIScreenSpecValidator.Validate(null).Single(), Does.Contain("null"));
    }

    [Test]
    public void EmptyScreenKey_IsReported()
    {
        using var assets = new Assets();
        var spec = ValidSpec(assets);
        spec.screenKey = new ScreenKey("  ");

        Assert.That(UIScreenSpecValidator.Validate(spec), Has.Exactly(1).Contains("screenKey is empty"));
    }

    [Test]
    public void NullTemplatePrefab_IsReported()
    {
        using var assets = new Assets();
        var spec = ValidSpec(assets);
        spec.templatePrefab = null;

        Assert.That(UIScreenSpecValidator.Validate(spec), Has.Exactly(1).Contains("templatePrefab is null"));
    }

    [Test]
    public void EmptyVariantId_IsReported()
    {
        using var assets = new Assets();
        var spec = ValidSpec(assets);
        spec.variants[0].variantId = "";

        Assert.That(UIScreenSpecValidator.Validate(spec), Has.Exactly(1).Contains("variantId is empty"));
    }

    [Test]
    public void DuplicateVariantId_IsReported()
    {
        using var assets = new Assets();
        var spec = ValidSpec(assets);
        spec.variants[1].variantId = spec.variants[0].variantId;

        Assert.That(UIScreenSpecValidator.Validate(spec), Has.Exactly(1).Contains("duplicate variantId"));
    }

    [Test]
    public void NullCondition_IsReported()
    {
        using var assets = new Assets();
        var spec = ValidSpec(assets);
        spec.variants[0].condition = null;

        Assert.That(UIScreenSpecValidator.Validate(spec), Has.Exactly(1).Contains("condition is null"));
    }

    [Test]
    public void InvertedAspectRange_IsReported_OnlyWhenAspectIsEnabled()
    {
        using var assets = new Assets();
        var spec = ValidSpec(assets);
        spec.variants[0].condition.aspectMin = 2.4f;
        spec.variants[0].condition.aspectMax = 2.0f;

        Assert.That(UIScreenSpecValidator.Validate(spec), Has.Exactly(1).Contains("aspectMin"));

        spec.variants[0].condition.useAspectRatio = false;
        Assert.That(UIScreenSpecValidator.Validate(spec), Is.Empty);
    }

    [Test]
    public void NullRuleEntry_IsReported()
    {
        using var assets = new Assets();
        var spec = ValidSpec(assets);
        spec.variants = new[] { spec.variants[0], null };

        Assert.That(UIScreenSpecValidator.Validate(spec), Has.Exactly(1).Contains("null rule"));
    }

    [Test]
    public void ContextPrefix_IsPrependedToEveryMessage()
    {
        using var assets = new Assets();
        var spec = ValidSpec(assets);
        spec.templatePrefab = null;
        spec.variants[0].variantId = "";

        List<string> problems = UIScreenSpecValidator.Validate(spec, "entries[3] 'home'");

        Assert.That(problems, Has.Count.EqualTo(2));
        Assert.That(problems, Has.All.StartWith("entries[3] 'home'"));
    }
}

public class UIScreenCatalogValidateTests
{
    [Test]
    public void ValidCatalog_HasNoProblems()
    {
        using var assets = new Assets();
        var home = Spec("home"); home.templatePrefab = assets.Prefab("H");
        var shop = Spec("shop"); shop.templatePrefab = assets.Prefab("S");
        var catalog = assets.Catalog(init: false, assets.SpecAsset(home), assets.SpecAsset(shop));

        Assert.That(catalog.Validate(), Is.Empty);
    }

    [Test]
    public void DuplicateScreenKey_IsReported()
    {
        using var assets = new Assets();
        var a = Spec("home"); a.templatePrefab = assets.Prefab("A");
        var b = Spec("home"); b.templatePrefab = assets.Prefab("B");
        var catalog = assets.Catalog(init: false, assets.SpecAsset(a), assets.SpecAsset(b));

        Assert.That(catalog.Validate(), Has.Exactly(1).Contains("duplicate screenKey"));
    }

    [Test]
    public void MissingSpecAsset_IsReported()
    {
        using var assets = new Assets();
        var catalog = assets.Catalog(init: false);
        catalog.entries.Add(new UIScreenCatalog.ScreenEntry { screenKey = new ScreenKey("home"), specAsset = null });

        Assert.That(catalog.Validate(), Has.Exactly(1).Contains("specAsset is null"));
    }

    [Test]
    public void EntryKeyMismatchWithSpecKey_IsReported()
    {
        using var assets = new Assets();
        var spec = Spec("home"); spec.templatePrefab = assets.Prefab("H");
        var catalog = assets.Catalog(init: false, assets.SpecAsset(spec));
        catalog.entries[0].screenKey = new ScreenKey("hom");

        Assert.That(catalog.Validate(), Has.Exactly(1).Contains("mismatch"));
    }

    [Test]
    public void SpecProblems_ArePropagatedWithEntryContext()
    {
        using var assets = new Assets();
        var spec = Spec("home", Rule("", 1));   // empty variantId, null prefab
        var catalog = assets.Catalog(init: false, assets.SpecAsset(spec));

        List<string> problems = catalog.Validate();

        Assert.That(problems, Has.Some.Contains("variantId is empty"));
        Assert.That(problems, Has.All.StartWith("entries[0]"));
    }
}
