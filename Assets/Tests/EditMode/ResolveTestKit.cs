using System;
using System.Collections.Generic;
using UnityEngine;

// Shared fixtures for resolver-level tests: canonical DisplayContexts (the
// M3 device matrix), UIContext/rule/spec builders, and an `Assets` scope that
// owns temporary ScriptableObjects/GameObjects for one test.
public static class ResolveTestKit
{
    // ---- Device matrix (PLAN §4) ----
    public static readonly DisplayContext Desktop169   = DisplayContext.FullScreen(1920, 1080, DisplayPlatform.Desktop);
    public static readonly DisplayContext Desktop169Hi = DisplayContext.FullScreen(2560, 1440, DisplayPlatform.Desktop);
    public static readonly DisplayContext Mobile195    = DisplayContext.FullScreen(2340, 1080, DisplayPlatform.Mobile);
    public static readonly DisplayContext Mobile209    = DisplayContext.FullScreen(2400, 1080, DisplayPlatform.Mobile);
    public static readonly DisplayContext Tablet43     = DisplayContext.FullScreen(2048, 1536, DisplayPlatform.Mobile);
    public static readonly DisplayContext Portrait     = DisplayContext.FullScreen(1080, 2400, DisplayPlatform.Mobile);
    public static readonly DisplayContext Square       = DisplayContext.FullScreen(1000, 1000, DisplayPlatform.Desktop);

    // ---- UIContext ----
    public static UIContext Ui(
        string theme  = "Light",
        string locale = "ko-KR",
        IReadOnlyDictionary<ExperimentKey, VariantId> experiments = null,
        IReadOnlyDictionary<ScreenKey, VariantId> overrides = null)
        => new UIContext(theme, locale, experiments, overrides);

    public static IReadOnlyDictionary<ScreenKey, VariantId> Force(string screenKey, string variantId)
        => new Dictionary<ScreenKey, VariantId> { { new ScreenKey(screenKey), variantId } };

    // ---- Conditions ----
    public static VariantCondition Always()               => new VariantCondition();
    public static VariantCondition Theme(string id)       => new VariantCondition { themeId = id };
    public static VariantCondition Locale(string id)      => new VariantCondition { localeId = id };
    public static VariantCondition Platform(DisplayPlatform p) => new VariantCondition { usePlatform = true, platform = p };
    public static VariantCondition Aspect(AspectRule rule, float min = 1.5f, float max = 2.5f)
        => new VariantCondition { useAspectRatio = true, aspectRule = rule, aspectMin = min, aspectMax = max };

    // ---- Rules / specs ----
    public static UIVariantRule Rule(
        string id, int priority,
        VariantCondition condition = null,
        GameObject prefab = null, ThemeSpec theme = null, LayoutPatchSpec layout = null)
        => new UIVariantRule
        {
            variantId      = id,
            priority       = priority,
            condition      = condition ?? Always(),
            overridePrefab = prefab,
            overrideTheme  = theme,
            overrideLayout = layout,
        };

    public static UIScreenSpec Spec(string key, params UIVariantRule[] rules)
        => new UIScreenSpec { screenKey = new ScreenKey(key), variants = rules };

    // ---- Temporary Unity objects ----
    public sealed class Assets : IDisposable
    {
        private readonly List<UnityEngine.Object> _owned = new();

        public LayoutPatchSpec Layout(string name) => Own(ScriptableObject.CreateInstance<LayoutPatchSpec>(), name);
        public ThemeSpec       Theme(string name)  => Own(ScriptableObject.CreateInstance<ThemeSpec>(), name);
        public GameObject      Prefab(string name) => Own(new GameObject(name), name);

        public UIScreenSpecAsset SpecAsset(UIScreenSpec spec)
        {
            var asset = Own(ScriptableObject.CreateInstance<UIScreenSpecAsset>(), spec.screenKey.Value ?? "spec");
            asset.spec = spec;
            return asset;
        }

        public UIScreenCatalog Catalog(bool init, params UIScreenSpecAsset[] specs)
        {
            var catalog = Own(ScriptableObject.CreateInstance<UIScreenCatalog>(), "TestCatalog");
            foreach (UIScreenSpecAsset s in specs)
                catalog.entries.Add(new UIScreenCatalog.ScreenEntry { screenKey = s.spec.screenKey, specAsset = s });
            if (init)
                catalog.Init();
            return catalog;
        }

        private T Own<T>(T obj, string name) where T : UnityEngine.Object
        {
            obj.name = name;
            _owned.Add(obj);
            return obj;
        }

        public void Dispose()
        {
            foreach (UnityEngine.Object o in _owned)
            {
                if (o != null)
                    UnityEngine.Object.DestroyImmediate(o);
            }
            _owned.Clear();
        }
    }
}
