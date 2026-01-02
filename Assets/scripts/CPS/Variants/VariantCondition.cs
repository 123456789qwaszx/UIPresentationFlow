using System;

//defines the exact scope in which a theme or variant should be applied
[Serializable]
public sealed class VariantCondition
{
    public string themeId;       // Ignored if null or empty
    public string localeId;      // Ignored if null or empty
    
    // Experiment (A/B test) identifier.
    // If set, this variant is applied only when the current UIContext
    // contains the given experiment key in its Experiments map.
    public ExperimentKey experimentKey; // Ignored if null or empty
    
    // Specific experiment variant value (e.g. "A", "B", "NewUI").
    // If set, this variant is applied only when the experiment's assigned
    // variant ID exactly matches this value.
    public VariantId experimentVariantId; // Ignored if null or empty

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

        return true;
    }
}