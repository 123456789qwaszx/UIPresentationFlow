using System.Collections.Generic;
using UnityEngine;

// Immutable output of UIVariantResolver: what to materialize, and which
// rules participated. The explanation of *why* lives in UIResolveTrace,
// which travels alongside this in UIResolveResult.
public sealed class ResolvedUIScreen
{
    public ScreenKey ScreenKey { get; }
    public UIScreenSpec BaseSpec { get; }

    public GameObject Prefab { get; }
    public ThemeSpec Theme { get; }
    public LayoutPatchSpec Layout { get; }

    // Ids of the rules that matched, in evaluation (priority) order.
    // A forced override yields exactly one entry.
    public IReadOnlyList<string> AppliedVariantIds { get; }

    public ResolvedUIScreen(
        ScreenKey screenKey,
        UIScreenSpec baseSpec,
        GameObject prefab,
        ThemeSpec theme,
        LayoutPatchSpec layout,
        List<string> appliedVariantIds)
    {
        ScreenKey         = screenKey;
        BaseSpec          = baseSpec;
        Prefab            = prefab;
        Theme             = theme;
        Layout            = layout;
        AppliedVariantIds = (appliedVariantIds ?? new List<string>()).AsReadOnly();
    }
}
