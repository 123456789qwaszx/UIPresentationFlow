using System;
using UnityEngine;

// Demo-level navigation: one screen at a time.
//
// Captures the DisplayContext exactly once per Show() so that resolve and
// materialize see the same input. Deliberately not a navigation framework —
// no back stack, layers, transitions or lifecycle. If those are ever needed
// they belong in a new type, not bolted onto this one.
public sealed class UIRouter
{
    private readonly UIResolver              _resolver;
    private readonly UIScreenFactory         _factory;
    private readonly IDisplayContextProvider _display;

    public UIScreen       CurrentScreen { get; private set; }
    public ScreenKey      CurrentKey    { get; private set; }
    public UIResolveTrace LastTrace     { get; private set; }

    public UIRouter(UIResolver resolver, UIScreenFactory factory, IDisplayContextProvider display)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _factory  = factory  ?? throw new ArgumentNullException(nameof(factory));
        _display  = display  ?? throw new ArgumentNullException(nameof(display));
    }

    // Resolves and instantiates `key`, then destroys whatever was showing.
    // Returns null only when the factory is in non-strict mode and could not build the screen.
    public UIScreen Show(ScreenKey key)
    {
        DisplayContext  display = _display.GetCurrent();
        UIResolveResult result  = _resolver.Resolve(key, display);
        LastTrace = result.Trace;

        UIScreen screen = _factory.Create(result);
        if (screen == null)
            return null;

        screen.gameObject.name = key.ToString();

        if (CurrentScreen != null)
            UnityEngine.Object.Destroy(CurrentScreen.gameObject);

        CurrentScreen = screen;
        CurrentKey    = key;
        return screen;
    }
}
