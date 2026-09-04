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

// Presentation orchestration boundary.
//
// Responsibilities:
//   pure variant decision -> patch recipe creation.
//
// It deliberately does NOT perform route lookup, prefab selection, Unity global
// reads, or Unity object mutation.
public sealed class UIResolver
{
    private readonly UIVariantResolver _variantResolver = new();
    private readonly UIContext _context;

    public UIContext Context => _context;

    public UIResolver(UIContext context)
    {
        _context = context;
    }

    public UIResolveResult Resolve(
        UIPresentationSpec spec,
        in DisplayContext display)
    {
        if (spec == null)
            throw new ArgumentNullException(nameof(spec));

        if (!display.IsValid)
            throw new ArgumentException(
                "DisplayContext must be valid. Capture or construct an explicit display snapshot before resolving.",
                nameof(display));

        var trace = new UIResolveTrace();
        ResolvedUIPresentation resolved = _variantResolver.Resolve(spec, _context, display, trace);

        var patches = new List<IUIPatch>(2);
        resolved.Theme?.BuildPatches(patches);
        resolved.Layout?.BuildPatches(patches);
        trace.Add($"[Patches] {patches.Count}");

        return new UIResolveResult(resolved, patches, trace);
    }
}