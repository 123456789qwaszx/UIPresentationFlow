using System;

public sealed class RouteActionBinder : IUiActionBinder
{
    private readonly Func<UIRouter> _getRouter;
    private readonly RouteKeyResolver _routeResolver;

    public RouteActionBinder(Func<UIRouter> getRouter, RouteKeyResolver routeResolver)
    {
        _getRouter = getRouter;
        _routeResolver = routeResolver;
    }

    // ScreenAsset.OnClickRoute를 받기 위한 Bind
    public bool TryBind(WidgetHandle widget, UIActionKey key)
    {
        if (key == UIActionKey.None)
            return false;

        // route → ScreenKey 매핑이 없으면 이 바인더의 대상이 아님
        if (!_routeResolver.TryGetRouteKey(key, out _))
            return false;
        
        if (widget.Button == null)
            return false;

        widget.Button.onClick.RemoveAllListeners();
        widget.Button.onClick.AddListener(() =>
        {
            _getRouter()?.Navigate(new UIRequest(key));
        });

        return true;
    }
}