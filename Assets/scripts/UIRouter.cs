using System.Collections.Generic;

public class UIRouter
{
    private readonly UIResolver _resolver;
    private readonly UIScreenFactory _factory;
    
    private readonly Dictionary<string, ScreenKey> _routes;
    
    private readonly ScreenKey _defaultKey = ScreenKey.Home;
    private UIScreen _current;
    
    public UIRouter(UIResolver resolver, UIScreenFactory factory)
    {
        _resolver = resolver;
        _factory  = factory;
        _routes = new Dictionary<string, ScreenKey>()
        {
            { "home", ScreenKey.Home },
            { "shop", ScreenKey.Shop },
        };
    }
    
    public void Navigate(UIRequest request)
    {
        if (!_routes.TryGetValue(request.route, out ScreenKey key))
        {
            UnityEngine.Debug.LogWarning($"[UIRouter] Unknown route='{request.route}'. Fallback to {_defaultKey}.");
            key = _defaultKey;
        }
        
        UIResolveResult result = _resolver.Resolve(key, request);
        UnityEngine.Debug.Log(result.Trace.Dump());
        
        _current = _factory.Create(result, this);
    }
}

