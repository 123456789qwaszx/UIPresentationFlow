using System.Collections.Generic;

public sealed class RouteKeyResolver
{
    public bool TryGetRouteKey(string route, out ScreenKey key)
    {
        return UIRouteKeyRegistry.TryGetRouteKey(route, out key);
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
        if (!_routeKeyResolver.TryGetRouteKey(request.route, out ScreenKey key))
        {
            UnityEngine.Debug.LogWarning($"[UIRouter] Unknown route='{request.route}'. Fallback to {_defaultKey}.");
            key = _defaultKey;
        }
        
        //UIScreenSpec baseSpec = _catalog.GetScreenSpec(screenKey);
        
        UIResolveResult result = _resolver.Resolve(key, request);
        UnityEngine.Debug.Log(result.Trace.Dump());
        
        CurrentScreen = _factory.Create(result);
    }
}

