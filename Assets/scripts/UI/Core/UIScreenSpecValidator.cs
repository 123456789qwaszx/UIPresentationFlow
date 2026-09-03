using System;
using System.Collections.Generic;

// Authoring-time checks for one UIScreenSpec. Pure: returns messages instead
// of logging, so Editor validation and tests share one implementation.
//
// Scope is deliberately small: only conditions that make resolve results
// ambiguous or the factory throw. Style/lint concerns do not belong here.
public static class UIScreenSpecValidator
{
    public static List<string> Validate(UIScreenSpec spec, string context = null)
    {
        var problems = new List<string>();
        string prefix = string.IsNullOrEmpty(context) ? string.Empty : context + ": ";

        if (spec == null)
        {
            problems.Add(prefix + "spec is null");
            return problems;
        }

        if (string.IsNullOrWhiteSpace(spec.screenKey.Value))
            problems.Add(prefix + "screenKey is empty");

        if (spec.templatePrefab == null)
            problems.Add(prefix + "templatePrefab is null (factory will throw)");

        if (spec.variants == null)
            return problems;

        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < spec.variants.Length; i++)
        {
            UIVariantRule rule = spec.variants[i];
            string at = $"{prefix}variants[{i}]";

            if (rule == null)
            {
                problems.Add($"{at}: null rule");
                continue;
            }

            if (string.IsNullOrWhiteSpace(rule.variantId))
                problems.Add($"{at}: variantId is empty");
            else if (!seenIds.Add(rule.variantId))
                problems.Add($"{at}: duplicate variantId '{rule.variantId}' (forced override would be ambiguous)");

            if (rule.condition == null)
            {
                problems.Add($"{at} '{rule.variantId}': condition is null (rule can never match)");
                continue;
            }

            VariantCondition c = rule.condition;
            if (c.useAspectRatio && c.aspectRule == AspectRule.Range && c.aspectMin > c.aspectMax)
                problems.Add($"{at} '{rule.variantId}': aspectMin {c.aspectMin} > aspectMax {c.aspectMax}");
        }

        return problems;
    }
}
