using System.Collections.Generic;
using UnityEngine;

public sealed class ResolvedUIScreen
{
    public ScreenKey ScreenKey { get; }
    public UIScreenSpec BaseSpec { get; }

    public GameObject Prefab { get; }
    public ThemeSpec Theme { get; }
    public LayoutPatchSpec Layout { get; }

    public IReadOnlyList<string> AppliedVariantIds { get; }
    public string DecisionTrace { get; }

    public ResolvedUIScreen(
        ScreenKey screenKey,
        UIScreenSpec baseSpec,
        GameObject prefab,
        ThemeSpec theme,
        LayoutPatchSpec layout,
        List<string> appliedVariantIds,
        string decisionTrace)
    {
        ScreenKey         = screenKey;
        BaseSpec         = baseSpec;
        Prefab           = prefab;
        Theme            = theme;
        Layout           = layout;
        AppliedVariantIds = appliedVariantIds.AsReadOnly();
        DecisionTrace    = decisionTrace;
    }
}