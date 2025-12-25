using System.Collections.Generic;
using UnityEngine;

public class UIBootStrap : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject textWidgetPrefab;
    public GameObject buttonWidgetPrefab;

    [Header("Root")]
    [SerializeField] private Transform uiRoot;
    [SerializeField] private UIScreenCatalog catalog;
    
    private UIOpener _uiOpener;
    public UIOpener Opener => _uiOpener;

    private void Awake()
    {
        if (uiRoot == null) uiRoot = transform;
        if (catalog == null) catalog = FindFirstObjectByType<UIScreenCatalog>();
        
        UIBinder              binder  = new();
        UIPatchApplier        patcher = new();
        WidgetRectApplier rectApplier = new();
        RouteKeyResolver routeKeyResolver = new();
        
        IHudView hudView = null;
        UIRouter router = null;
        
        CompositeUiActionBinder uiActionBinder = new (
            new UIActionBinder(() => hudView),
            new RouteActionBinder(() => router, routeKeyResolver)
        );
        
        WidgetFactory    widgetFactory = new(textWidgetPrefab, buttonWidgetPrefab, uiActionBinder);
        UIComposer            composer = new(widgetFactory, rectApplier);
        
        UIContext       context = UIContext.Default;
        UIResolver     resolver = new(catalog, context);
        UIScreenFactory factory = new(uiRoot, binder, patcher, composer);
        
        router = new(resolver, factory, routeKeyResolver);
        
        hudView   = new HudPresenter(() => router.CurrentScreen);
        _uiOpener = new UIOpener(router, hudView);
    }
}
