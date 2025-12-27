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
    
    private readonly ScreenKey _defaultKey = ScreenKey.Home;
    public UIScreen CurrentScreen { get; private set; }
    
    public UIRouter(UIResolver resolver, UIScreenFactory factory, RouteKeyResolver routeKeyResolver)
    {
        _resolver = resolver;
        _factory  = factory;
        _routeKeyResolver = routeKeyResolver;
    }
    
    public void Navigate(UIRequest request)
    {
        UIActionKey action = request.Action;
        
        if (!_routeKeyResolver.TryGetRouteKey(action, out ScreenKey key))
        {
            UnityEngine.Debug.LogWarning($"[UIRouter] Unknown route='{action.Value}'. Fallback to {_defaultKey}.");
            key = _defaultKey;
        }
        
        UIResolveResult result = _resolver.Resolve(key, request);
        UnityEngine.Debug.Log(result.Trace.Dump());

        CurrentScreen = _factory.Create(result);
    }
}

