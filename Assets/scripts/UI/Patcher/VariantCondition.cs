using System;
using UnityEngine;

public enum VariantPlatform
{
    Any,
    Desktop,
    Mobile,
    Console
}

public enum AspectRule
{
    Any,
    Portrait,   // width/height <= 1
    Landscape,  // width/height > 1
    Range       // minAspect ~ maxAspect
}

//defines the exact scope in which a theme or variant should be applied
[Serializable]
public sealed class VariantCondition
{
    [Header("Theme / Locale")]
    public string themeId;
    public string localeId;
    
    [Header("Experiment")]
    // Experiment (A/B test) identifier.
    // If set, this variant is applied only when the current UIContext
    // contains the given experiment key in its Experiments map.
    public ExperimentKey experimentKey;
    
    // Specific experiment variant value (e.g. "A", "B", "NewUI").
    // If set, this variant is applied only when the experiment's assigned
    // variant ID exactly matches this value.
    public VariantId experimentVariantId;
    
    [Header("Platform (optional)")]
    public bool usePlatform;           // false이면 플랫폼 무시
    public VariantPlatform platform = VariantPlatform.Any;

    [Header("Aspect Ratio (optional)")]
    public bool useAspectRatio;             // false이면 비율 무시
    public AspectRule aspectRule = AspectRule.Any;
    public float aspectMin = 1.5f;       // Range 모드에서만 사용
    public float aspectMax = 2.5f;

    public bool Matches(in UIContext ctx)
    {
        if (!string.IsNullOrEmpty(themeId) && ctx.ThemeId != themeId)
            return false;

        if (!string.IsNullOrEmpty(localeId) && ctx.LocaleId != localeId)
            return false;

        if (experimentKey.IsValid)
        {
            if (ctx.Experiments == null)
                return false;

            if (!ctx.Experiments.TryGetValue(experimentKey, out VariantId variantId))
                return false;

            if (!string.IsNullOrEmpty(experimentVariantId.Value) && variantId != experimentVariantId)
                return false;
        }
        
        if (usePlatform && !MatchesPlatform())
            return false;
        
        if (useAspectRatio && !MatchesAspectRatio())
            return false;

        return true;
    }
    
    private bool MatchesPlatform()
    {
        if (platform == VariantPlatform.Any)
            return true;

        VariantPlatform current = DetectCurrentPlatform();
        return current == platform;
    }

    private static VariantPlatform DetectCurrentPlatform()
    {
        switch (Application.platform)
        {
            // Desktop
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
                return VariantPlatform.Desktop;

            // Mobile
            case RuntimePlatform.Android:
            case RuntimePlatform.IPhonePlayer:
                return VariantPlatform.Mobile;
            
            default:
                return VariantPlatform.Desktop;
        }
    }

    private bool MatchesAspectRatio()
    {
        if (aspectRule == AspectRule.Any)
            return true;

        float ratio = GetCurrentAspectRatio();
        switch (aspectRule)
        {
            case AspectRule.Portrait:
                return ratio <= 1.0f;

            case AspectRule.Landscape:
                return ratio > 1.0f;

            case AspectRule.Range:
                return ratio >= aspectMin && ratio <= aspectMax;

            default:
                return true;
        }
    }

    private static float GetCurrentAspectRatio()
    {
        if (Screen.height == 0)
            return 1.0f;
        return (float)Screen.width / Screen.height;
    }
}