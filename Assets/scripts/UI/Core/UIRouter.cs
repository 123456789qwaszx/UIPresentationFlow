using System.Collections.Generic;

// Temporary R9 migration bridge.
// Application routing still enters through ScreenKey for the demo, but the
// presentation decision itself is already independent from route/prefab data.
public sealed class UIRouter
{
    private readonly UIScreenCatalog _catalog;
    private readonly UIResolver _resolver;
    private readonly UIScreenFactory _factory;

    public UIBase CurrentScreen { get; private set; }
    public ScreenKey CurrentKey { get; private set; }

    public DisplayContext LastDisplay { get; private set; }
    public UIResolveResult LastResult { get; private set; }

    public UIRouter(
        UIScreenCatalog catalog,
        UIResolver resolver,
        UIScreenFactory factory)
    {
        _catalog = catalog;
        _resolver = resolver;
        _factory = factory;
    }

    public UIBase Show(ScreenKey key)
    {
        if (!_catalog.TryGetScreenEntry(key, out UIScreenCatalog.ScreenEntry entry))
            throw new KeyNotFoundException(
                $"[UIRouter] No screen route registered for ScreenKey '{key}' in catalog '{_catalog.name}'");

        DisplayContext display = UnityDisplayContextProvider.GetCurrent();
        UIResolveResult result = _resolver.Resolve(entry.presentation, display);

        LastDisplay = display;
        LastResult = result;

        UIBase screen = _factory.Create(entry.templatePrefab, result);
        if (screen == null)
            return null;

        screen.gameObject.name = key.ToString();

        if (CurrentScreen != null)
            UnityEngine.Object.Destroy(CurrentScreen.gameObject);

        CurrentScreen = screen;
        CurrentKey = key;
        return screen;
    }
}