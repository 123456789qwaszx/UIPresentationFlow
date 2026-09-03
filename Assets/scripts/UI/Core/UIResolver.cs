using System;
using System.Collections.Generic;

// Human-readable record of one resolve. Console and Editor tooling read the
// same lines; M5 may add a structured model on top, but this stays the floor.
public sealed class UIResolveTrace
{
    private readonly List<string> _lines = new();

    public IReadOnlyList<string> Lines => _lines;

    public void Add(string line) => _lines.Add(line);

    public string Dump()
        => _lines.Count == 0 ? "[Trace] (empty)" : "[Trace]\n- " + string.Join("\n- ", _lines);
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

// ScreenKey -> (spec lookup) -> variant decision -> patch list.
//
// Owns the session-level UIContext (theme, locale, experiments, overrides).
// The DisplayContext is passed per call because it is the input that changes
// between devices, previews and tests.
//
// Strict: an unknown ScreenKey is a programming/authoring error and throws;
// it is never reported as a "successful" empty result.
public sealed class UIResolver
{
    private readonly UIScreenCatalog   _catalog;
    private readonly UIVariantResolver _variantResolver = new();
    private readonly UIContext         _context;

    public UIContext Context => _context;

    public UIResolver(UIScreenCatalog catalog, UIContext context)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _context = context;
    }

    public UIResolveResult Resolve(ScreenKey screenKey, in DisplayContext display)
    {
        if (!display.IsValid)
            throw new ArgumentException(
                "DisplayContext is invalid (default). Capture one from an IDisplayContextProvider.",
                nameof(display));

        if (!_catalog.TryGetScreenSpec(screenKey, out UIScreenSpec spec))
            throw new KeyNotFoundException(
                $"[UIResolver] No UIScreenSpec registered for ScreenKey '{screenKey}' in catalog '{_catalog.name}'. " +
                "Check the catalog entries and that catalog.Init() was called.");

        var trace = new UIResolveTrace();
        ResolvedUIScreen resolved = _variantResolver.Resolve(spec, _context, display, trace);

        var patches = new List<IUIPatch>(2);
        resolved.Theme?.BuildPatches(patches);
        resolved.Layout?.BuildPatches(patches);
        trace.Add($"[Patches] {patches.Count}");

        return new UIResolveResult(resolved, patches, trace);
    }
}
