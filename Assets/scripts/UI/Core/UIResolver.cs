using System;
using System.Collections.Generic;

public sealed class UIResolveTrace
{
    private readonly List<string> _lines = new();

    public IReadOnlyList<string> Lines => _lines;

    public void Add(string line) => _lines.Add(line);

    public string Dump()
        => _lines.Count == 0 ? "[Trace] (empty)" : "[Trace]\n- " + string.Join("\n- ", _lines);
}

public sealed class UIResolver
{
    private readonly UIScreenCatalog   _catalog;
    private readonly UIVariantResolver _variantResolver = new();
    private readonly UIContext         _context;

    public UIContext Context => _context;

    public UIResolver(UIScreenCatalog catalog, UIContext context)
    {
        _catalog = catalog;
        _context = context;
    }

    public UIResolveResult Resolve(ScreenKey screenKey, in DisplayContext display)
    {
        if (!_catalog.TryGetScreenSpec(screenKey, out UIScreenSpec spec))
            throw new KeyNotFoundException(
                $"[UIResolver] No UIScreenSpec registered for ScreenKey '{screenKey}' in catalog '{_catalog.name}'");

        var trace = new UIResolveTrace();
        ResolvedUIScreen resolved = _variantResolver.Resolve(spec, _context, display, trace);

        var patches = new List<IUIPatch>(2);
        resolved.Theme?.BuildPatches(patches);
        resolved.Layout?.BuildPatches(patches);
        trace.Add($"[Patches] {patches.Count}");

        return new UIResolveResult(resolved, patches, trace);
    }
}