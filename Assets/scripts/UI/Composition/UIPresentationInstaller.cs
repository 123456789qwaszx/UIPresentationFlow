using UnityEngine;

// Composition root (M0 D0-1 / D0-2). The one MonoBehaviour that wires the
// pipeline: catalog -> resolver -> factory -> router.
//
// Nothing below this class knows about scenes or Unity's global display
// state: the router receives an IDisplayContextProvider and captures one
// DisplayContext per Show(). This class polls the same provider to notice
// display changes (Game View resize, rotation, safe-area change) and
// re-shows the current screen, which is what makes the demo adaptive live.
public sealed class UIPresentationInstaller : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField] private UIScreenCatalog catalog;
    [SerializeField] private string initialScreenKey = "adaptive_demo";

    [Header("Scene")]
    [SerializeField] private RectTransform uiRoot;

    [Header("UI Context (session)")]
    [SerializeField] private string themeId  = "Light";
    [SerializeField] private string localeId = "ko-KR";

    [Header("Behaviour")]
    [Tooltip("Re-resolve and rebuild the current screen whenever the DisplayContext changes.")]
    [SerializeField] private bool reapplyOnDisplayChange = true;
    [SerializeField] private bool logTrace = true;

    public UIRouter                Router          { get; private set; }
    public IDisplayContextProvider DisplayProvider { get; private set; }

    private DisplayContext _shownWith;

    private void Awake()
    {
        if (catalog == null)
        {
            Debug.LogError("[UIPresentationInstaller] Catalog is not assigned.", this);
            enabled = false;
            return;
        }

        if (uiRoot == null)
        {
            Debug.LogError("[UIPresentationInstaller] UI Root is not assigned.", this);
            enabled = false;
            return;
        }

        catalog.Init();
        foreach (string problem in catalog.Validate())
            Debug.LogWarning($"[UIPresentationInstaller] Catalog: {problem}", catalog);

        DisplayProvider = new UnityDisplayContextProvider();

        var resolver = new UIResolver(catalog, new UIContext(themeId, localeId, null, null));
        var factory  = new UIScreenFactory(uiRoot, new UIPatchApplier(), strict: true);
        Router = new UIRouter(resolver, factory, DisplayProvider);
    }

    private void Start()
    {
        Show(new ScreenKey(initialScreenKey));
    }

    private void Update()
    {
        if (!reapplyOnDisplayChange || Router == null || Router.CurrentScreen == null)
            return;

        DisplayContext now = DisplayProvider.GetCurrent();
        if (now == _shownWith)
            return;

        Show(Router.CurrentKey);
    }

    public void Show(ScreenKey key)
    {
        UIScreen screen = Router.Show(key);
        _shownWith = Router.LastDisplay;

        if (logTrace)
            Debug.Log(Router.LastResult.Trace.Dump(), this);

        if (screen != null)
            screen.GetComponentInChildren<DisplayInfoLabel>(includeInactive: true)
                  ?.Set(Router.LastDisplay, Router.LastResult.Resolved);
    }
}
