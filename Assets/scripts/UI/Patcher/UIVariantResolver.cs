using System;
using System.Collections.Generic;
using UnityEngine;

// Contract
//  - never mutates the spec (authored data) or either context
//  - reads no global state; same inputs -> same output
//  - explains itself through the optional UIResolveTrace
//
// Priority policy: highest priority wins, per field.
//  Rules are visited in priority-descending order; ties keep authored (array) order.
//  The first matching rule that overrides a field locks that field.
//  A field no matching rule overrides keeps the base value.
//
// Forced override (UIContext.ScreenOverrides[screenKey] = variantId):
//  A debug/QA tool.
//  The named rule is applied without evaluating its condition and no other rule is considered.
public sealed class UIVariantResolver
{
    public ResolvedUIScreen Resolve(
        UIScreenSpec spec,
        in UIContext ui,
        in DisplayContext display,
        UIResolveTrace trace = null)
    {
        GameObject      prefab    = spec.templatePrefab;
        ThemeSpec       theme     = spec.baseTheme;
        LayoutPatchSpec layout    = spec.baseLayout;
        List<string> matchedRules = new(4);

        trace?.Add($"[Resolve] screen={spec.screenKey} base prefab={Name(prefab)} theme={Name(theme)} layout={Name(layout)}");
        trace?.Add($"[Input] ui theme={ui.ThemeId} locale={ui.LocaleId} experiments={ui.Experiments?.Count ?? 0} overrides={ui.ScreenOverrides?.Count ?? 0}");
        trace?.Add($"[Input] display {display}");

        UIVariantRule[] rules = spec.variants;

        // 1) forced override
        if (TryGetForcedVariantId(spec, ui, out string forcedId))
        {
            UIVariantRule forced = FindFirstById(rules, forcedId);
            if (forced != null)
            {
                matchedRules.Add(forced.variantId);
                if (forced.overridePrefab != null) prefab = forced.overridePrefab;
                if (forced.overrideTheme  != null) theme  = forced.overrideTheme;
                if (forced.overrideLayout != null) layout = forced.overrideLayout;

                trace?.Add($"[Forced] variantId={forcedId} applied; rule conditions skipped");
                trace?.Add(ResultLine(prefab, theme, layout, matchedRules));
                return new ResolvedUIScreen(spec.screenKey, spec, prefab, theme, layout, matchedRules);
            }

            trace?.Add($"[Forced] variantId={forcedId} not found in spec; evaluating rules normally");
        }

        // 2) normal evaluation
        if (rules != null && rules.Length > 0)
        {
            bool prefabLocked = false, themeLocked = false, layoutLocked = false;

            foreach ((UIVariantRule rule, int index) in OrderByPriority(rules))
            {
                if (rule.condition == null)
                {
                    trace?.Add($"[Rule] {rule.variantId} p{rule.priority} SKIP (null condition, index {index})");
                    continue;
                }

                bool match = rule.condition.Matches(ui, display);
                trace?.Add($"[Rule] {rule.variantId} p{rule.priority} {(match ? "MATCH" : "MISS")}");
                if (!match)
                    continue;

                matchedRules.Add(rule.variantId);

                if (!prefabLocked && rule.overridePrefab != null)
                {
                    prefab = rule.overridePrefab;
                    prefabLocked = true;
                    trace?.Add($"[Winner] prefab <- {rule.variantId} ({Name(prefab)})");
                }

                if (!themeLocked && rule.overrideTheme != null)
                {
                    theme = rule.overrideTheme;
                    themeLocked = true;
                    trace?.Add($"[Winner] theme <- {rule.variantId} ({Name(theme)})");
                }

                if (!layoutLocked && rule.overrideLayout != null)
                {
                    layout = rule.overrideLayout;
                    layoutLocked = true;
                    trace?.Add($"[Winner] layout <- {rule.variantId} ({Name(layout)})");
                }
            }
        }

        trace?.Add(ResultLine(prefab, theme, layout, matchedRules));
        return new ResolvedUIScreen(spec.screenKey, spec, prefab, theme, layout, matchedRules);
    }

    // Priority descending, then authored index ascending. The comparison
    // includes the index, so the result does not depend on List.Sort being
    // stable. The source array is never touched.
    private static List<(UIVariantRule rule, int index)> OrderByPriority(UIVariantRule[] rules)
    {
        var ordered = new List<(UIVariantRule rule, int index)>(rules.Length);
        for (int i = 0; i < rules.Length; i++)
        {
            if (rules[i] != null)
                ordered.Add((rules[i], i));
        }

        ordered.Sort((a, b) =>
            a.rule.priority != b.rule.priority
                ? b.rule.priority.CompareTo(a.rule.priority)
                : a.index.CompareTo(b.index));

        return ordered;
    }

    private static bool TryGetForcedVariantId(UIScreenSpec spec, in UIContext ui, out string variantId)
    {
        variantId = null;

        if (ui.ScreenOverrides == null)
            return false;

        if (!ui.ScreenOverrides.TryGetValue(spec.screenKey, out VariantId id) || !id.IsValid)
            return false;

        variantId = id.Value;
        return true;
    }

    private static UIVariantRule FindFirstById(UIVariantRule[] rules, string variantId)
    {
        if (rules == null)
            return null;

        for (int i = 0; i < rules.Length; i++)
        {
            UIVariantRule r = rules[i];
            if (r != null && r.variantId == variantId)
                return r;
        }

        return null;
    }

    private static string ResultLine(GameObject prefab, ThemeSpec theme, LayoutPatchSpec layout, List<string> applied)
        => $"[Result] prefab={Name(prefab)} theme={Name(theme)} layout={Name(layout)} applied=[{string.Join(", ", applied)}]";

    private static string Name(UnityEngine.Object o) => o != null ? o.name : "null";
}