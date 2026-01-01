using System.Collections.Generic;

public sealed class RouteKeyResolver
{
    public bool TryGetRouteKey(UIActionKey routeActionKey, out ScreenKey key)
    {
        return UIRouteKeyRegistry.TryGetRouteKey(routeActionKey.Value, out key);
    }
}

public class UIRouter
{
    private readonly UIResolver _resolver;
    private readonly UIScreenFactory _factory;
    
    private readonly RouteKeyResolver _routeKeyResolver;
    
    private readonly ScreenKey _defaultKey = UIScreenKeys.Home;
    
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

        if (!_screens.TryGetValue(key, out UIScreen screen))
        {
            UIResolveResult result = _resolver.Resolve(key, request);
            screen = _factory.Create(result);
            _screens[key] = screen;
            
            result.Trace.Dump();
        }
        
        CurrentScreen = screen;
    }
}

