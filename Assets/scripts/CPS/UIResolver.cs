using System.Collections.Generic;
using UnityEngine;

public class UIResolveTrace
{
    private readonly List<string> _lines = new();
    public void Add(string message) => _lines.Add(message);
    public string Dump() => "[Trace]\n- " + string.Join("\n- ", _lines);
}

public class UIRequest
{
    public UIActionKey Action { get; }
    public object Payload;
    
    public string Route => Action.Value;

    public UIRequest(UIActionKey action, object payload = null)
    {
        Action  = action;
        Payload = payload;
    }
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

public class UIResolver
{
    private readonly UIScreenCatalog  _catalog;
    private readonly UIVariantResolver _variantResolver;
    private readonly UIContext         _context; // Current UIContext

    public UIResolver(UIScreenCatalog catalog, UIContext context)
    {
        _catalog         = catalog;
        _variantResolver = new UIVariantResolver();
        _context         = context;
    }

    public UIResolveResult Resolve(ScreenKey screenKey, UIRequest request)
    {
        UIResolveTrace trace = new UIResolveTrace();
        trace.Add($"Resolve ScreenKey = {screenKey}");
        trace.Add($"Action = {request.Action.Value}");
        trace.Add($"Payload = {(request.Payload != null ? request.Payload.ToString() : "null")}");

        // 1) Take UIScreenSpec from catalog
        UIScreenSpec baseSpec = _catalog.GetScreenSpec(screenKey);
        if (baseSpec == null)
        {
            trace.Add($"[Resolver] No UIScreenSpec found for key={screenKey}. It may be removed during runtime from catalog.");
            Debug.LogError(trace.Dump());
        }

        // 2) Resolve final prefab / theme / layout via VariantResolver
        ResolvedUIScreen resolved = _variantResolver.Resolve(baseSpec, _context);
        trace.Add(resolved.DecisionTrace);

        // 3)Build IUIPatch list from theme / layout specs
        List<IUIPatch> patches = new List<IUIPatch>();

        if (resolved.Theme != null)
            resolved.Theme.BuildPatches(patches);

        if (resolved.Layout != null)
            resolved.Layout.BuildPatches(patches);

        return new UIResolveResult(resolved, patches, trace);
    }
}