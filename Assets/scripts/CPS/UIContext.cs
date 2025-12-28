using System.Collections.Generic;

public readonly struct UIContext
{
    public readonly string ThemeId;   // e.g., "Light", "Dark"
    public readonly string LocaleId;  // e.g., "ko-KR", "ja-JP"
    
    // Active experiment assignments for the current UI context.
    // Key   : experiment identifier (e.g. "ShopLayoutTest")
    // Value : assigned variant ID (e.g. "Shop_A", "Shop_B", "NewUI")
    public readonly IReadOnlyDictionary<ExperimentKey, VariantId> Experiments;

    // Per-screen forced variant overrides.
    public readonly IReadOnlyDictionary<ScreenKey, VariantId> ScreenOverrides;

    public UIContext(
        string themeId,
        string localeId,
        IReadOnlyDictionary<ExperimentKey, VariantId> experiments,
        IReadOnlyDictionary<ScreenKey, VariantId> screenOverrides)
    {
        ThemeId        = themeId;
        LocaleId       = localeId;
        Experiments    = experiments;
        ScreenOverrides = screenOverrides;
    }

    public static UIContext Default =>
        new UIContext("Light", "ko-KR", null, null);
}