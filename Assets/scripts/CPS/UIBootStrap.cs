using System.Collections.Generic;
using UnityEngine;

public class UIBootStrap : MonoBehaviour
{
    [Header("Widget Prefabs")]
    public GameObject textPrefab;
    public GameObject buttonPrefab;
    public GameObject imagePrefab;
    public GameObject togglePrefab;
    public GameObject sliderPrefab;
    public GameObject gameObjectPrefab;

    [Header("Root")]
    [SerializeField] private Transform uiRoot;
    [SerializeField] private UIScreenCatalog catalog;
    
    private UIOpener _uiOpener;
    public UIOpener Opener => _uiOpener;

    private void Awake()
    {
        if (uiRoot == null) uiRoot = transform;
        if (catalog == null) catalog = FindFirstObjectByType<UIScreenCatalog>();
        
        UISlotBinder              binder  = new();
        UIPatchApplier        patcher = new();
        WidgetRectApplier rectApplier = new();
        RouteKeyResolver routeKeyResolver = new(catalog);
        
        IHudView hudView = null;
        UIRouter router = null;
        
        CompositeUiActionBinder uiActionBinder = new (
            new UIActionBinder(() => hudView),
            new RouteActionBinder(() => router, routeKeyResolver)
        );
        
        WidgetFactory    widgetFactory = new(textPrefab, buttonPrefab, imagePrefab, togglePrefab, sliderPrefab, gameObjectPrefab, uiActionBinder, true);
        UIComposer            composer = new(widgetFactory, rectApplier);
        
        UIContext       context = UIContext.Default;
        UIResolver     resolver = new(catalog, context);
        UIScreenFactory factory = new(uiRoot, binder, patcher, composer);
        
        router = new(resolver, factory, routeKeyResolver);
        
        hudView   = new HudPresenter(() => router.CurrentScreen);
        _uiOpener = new UIOpener(router, hudView);
    }
}
