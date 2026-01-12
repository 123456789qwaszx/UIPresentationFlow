using System.Collections.Generic;
using UnityEngine;

public class UIResolveTrace
{
    private readonly List<string> _lines = new();
    public void Add(string message) => _lines.Add(message);
    public string Dump() => "[Trace]\n- " + string.Join("\n- ", _lines);
}

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

public sealed class UIResolver
{
    private readonly UIScreenCatalog   _catalog;
    private readonly UIVariantResolver _variantResolver;
    private readonly UIContext         _context;

    public UIResolver(UIScreenCatalog catalog, UIContext context)
    {
        _catalog         = catalog;
        _variantResolver = new UIVariantResolver();
        _context         = context;
    }

    public UIResolveResult Resolve(ScreenKey screenKey, UIActionKey? action = null)
    {
        UIResolveTrace trace = new UIResolveTrace();
        trace.Add($"Resolve ScreenKey = {screenKey}");

        if (action.HasValue)
            trace.Add($"Action = {action.Value.Value}");

        if (!_catalog.TryGetScreenSpec(screenKey, out UIScreenSpec baseSpec))
        {
            trace.Add($"[Resolver] No UIScreenSpec found for key={screenKey}.");
            Debug.LogError(trace.Dump());
        }

        ResolvedUIScreen resolved = _variantResolver.Resolve(baseSpec, _context);
        trace.Add(resolved.DecisionTrace);

        List<IUIPatch> patches = new();
        resolved.Theme?.BuildPatches(patches);
        resolved.Layout?.BuildPatches(patches);

        return new UIResolveResult(resolved, patches, trace);
    }
}