using System.Collections.Generic;

public sealed class ResolvedUIPresentation
{
    public string PresentationId { get; }
    public UIPresentationSpec BaseSpec { get; }

    public ThemeSpec Theme { get; }
    public LayoutPatchSpec Layout { get; }

    // IDs of matching rules in evaluation (priority) order.
    // A forced override yields exactly one entry.
    public IReadOnlyList<string> AppliedVariantIds { get; }

    public ResolvedUIPresentation(
        UIPresentationSpec baseSpec,
        ThemeSpec theme,
        LayoutPatchSpec layout,
        List<string> appliedVariantIds)
    {
        BaseSpec          = baseSpec;
        PresentationId    = baseSpec != null ? baseSpec.presentationId : null;
        Theme             = theme;
        Layout            = layout;
        AppliedVariantIds = (appliedVariantIds ?? new List<string>()).AsReadOnly();
    }
}