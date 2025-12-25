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
        WidgetFactory  widgetFactory  = new(textWidgetPrefab, buttonWidgetPrefab);
        
        IHudView              hudView = null;
        UiActionBinder uiActionBinder = new UiActionBinder(() => hudView);
        UIComposer           composer = new(widgetFactory, uiActionBinder);
        
        UIContext       context = UIContext.Default;
        UIResolver     resolver = new(catalog, context);
        UIScreenFactory factory = new(uiRoot, binder, patcher, composer);
        
        Dictionary<string, ScreenKey> routeKeys = new (System.StringComparer.OrdinalIgnoreCase);
        foreach (var e in catalog.entries)
        {
            if (e == null) continue;
            routeKeys[e.key.ToString()] = e.key;
        }
        
        UIRouter router = new(resolver, factory, routeKeys);
        
        
        hudView   = new HudPresenter(() => router.CurrentScreen);
        _uiOpener = new UIOpener(router, hudView);
    }
}
