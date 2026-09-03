using System.Collections.Generic;

public sealed class UIResolveResult
{
    public ResolvedUIScreen Resolved { get; }
    public List<IUIPatch> Patches { get; }
    public UIResolveTrace Trace { get; }

    public UIResolveResult(ResolvedUIScreen resolved, List<IUIPatch> patches, UIResolveTrace trace)
    {
        Resolved = resolved;
        Patches  = patches;
        Trace    = trace;
    }
}