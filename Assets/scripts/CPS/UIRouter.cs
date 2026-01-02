using System.Collections.Generic;

public sealed class RouteKeyResolver
{
    private readonly UIScreenCatalog _catalog;

    public RouteKeyResolver(UIScreenCatalog catalog)
    {
        _catalog = catalog;
    }

    public bool TryGetRouteKey(UIActionKey routeActionKey, out ScreenKey key)
    {
        return _catalog.TryGetRouteScreenKey(routeActionKey.Value, out key);
    }
}

public class UIRouter
{
    private readonly UIResolver _resolver;
    private readonly UIScreenFactory _factory;
    
    private readonly RouteKeyResolver _routeKeyResolver;
    
    private readonly ScreenKey _defaultKey = new ("home");
    
    public UIRouter(UIResolver resolver, UIScreenFactory factory, RouteKeyResolver routeKeyResolver)
    {
        _resolver = resolver;
        _factory  = factory;
        _routeKeyResolver = routeKeyResolver;
    }
    
    private readonly Dictionary<ScreenKey, UIScreen> _screens = new();
    public UIScreen CurrentScreen { get; private set; }
    
    public bool TryGetScreen(ScreenKey key, out UIScreen screen)
        => _screens.TryGetValue(key, out screen);
    
    public void Navigate(UIRequest request)
    {
        UIActionKey action = request.Action;
        
        if (!_routeKeyResolver.TryGetRouteKey(action, out ScreenKey key))
        {
            UnityEngine.Debug.LogWarning($"[UIRouter] Unknown route='{action.Value}'. Fallback to {_defaultKey}.");
            key = _defaultKey;
        }

        UIResolveResult result = _resolver.Resolve(key, request);
        UIScreen screen = _factory.Create(result);

        result.Trace.Dump();

        if (_screens.TryGetValue(key, out UIScreen existing) && existing != null)
        {
            UnityEngine.Object.Destroy(existing.gameObject);
        }

        _screens[key] = screen;
        CurrentScreen = screen;
    }
}

