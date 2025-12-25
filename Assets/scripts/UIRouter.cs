using System.Collections.Generic;

public class UIRouter
{
    private readonly UIResolver _resolver;
    private readonly UIScreenFactory _factory;
    
    private readonly IReadOnlyDictionary<string, ScreenKey> _routes;
    
    private readonly ScreenKey _defaultKey = ScreenKey.Home;
    public UIScreen CurrentScreen { get; private set; }
    
    public UIRouter(UIResolver resolver, UIScreenFactory factory, IReadOnlyDictionary<string, ScreenKey> routes)
    {
        _resolver = resolver;
        _factory  = factory;
        _routes = routes;
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
        
        CurrentScreen = _factory.Create(result, this);
    }
}

