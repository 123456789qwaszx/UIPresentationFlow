using System.Collections.Generic;

public readonly struct UIContext
{
    public readonly string ThemeId;   // e.g., "Light", "Dark"
    public readonly string LocaleId;  // e.g., "ko-KR", "ja-JP"

    public readonly IReadOnlyDictionary<string, string> Experiments;

    // screenId -> forcedVariantId ("Shop" -> "Shop_Layout_B")
    public readonly IReadOnlyDictionary<string, string> ScreenOverrides;

    public UIContext(
        string themeId,
        string localeId,
        IReadOnlyDictionary<string, string> experiments,
        IReadOnlyDictionary<string, string> screenOverrides)
    {
        ThemeId        = themeId;
        LocaleId       = localeId;
        Experiments    = experiments;
        ScreenOverrides = screenOverrides;
    }

    public static UIContext Default =>
        new UIContext("Light", "ko-KR", null, null);
}