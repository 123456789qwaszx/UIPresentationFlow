using UnityEngine;

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

    private UIRouter _router;
    private DisplayContext _shownWith;

    private void Awake()
    {
        catalog.Init();
        foreach (string problem in catalog.Validate())
            Debug.LogWarning($"[UIPresentationInstaller] Catalog: {problem}", catalog);
        
        UIContext ctx = new(themeId, localeId, null, null);
        
        UIResolver resolver = new(catalog, ctx);
        UIScreenFactory factory  = new(uiRoot, new UIPatchApplier());
        
        _router = new UIRouter(resolver, factory);
    }

    private void Start()
    {
        Show(new ScreenKey(initialScreenKey));
    }

    private void Update()
    {
        if (!reapplyOnDisplayChange || _router == null || _router.CurrentScreen == null)
            return;

        DisplayContext now = UnityDisplayContextProvider.GetCurrent();
        if (now == _shownWith)
            return;

        Show(_router.CurrentKey);
    }

    public void Show(ScreenKey key)
    {
        UIScreen screen = _router.Show(key);
        _shownWith = _router.LastDisplay;

        if (logTrace)
            Debug.Log(_router.LastResult.Trace.Dump(), this);

        if (screen != null)
            screen.GetComponentInChildren<DisplayInfoLabel>(includeInactive: true)?.
                Set(_router.LastDisplay, _router.LastResult.Resolved);
    }
}