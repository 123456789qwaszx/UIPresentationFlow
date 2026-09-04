using UnityEngine;
using UnityEngine.Serialization;

// AdaptiveDemo composition root.
//
// View creation happens once here because the current demo scene does not yet
// contain pre-registered UI instances. After registration, UIManager owns Root /
// Panel lifecycle and all presentation refreshes reuse the same View instance.
public sealed class UIPresentationInstaller : MonoBehaviour
{
    [Header("Adaptive Demo Root")]
    [SerializeField] private GameObject initialRootPrefab;
    [SerializeField] private UIPresentationSpec initialRootPresentation;

    [Header("UI Layers")]
    [FormerlySerializedAs("uiRoot")]
    [SerializeField] private RectTransform rootLayer;
    [Tooltip("Optional for the current demo. If omitted, panels mount under Root Layer.")]
    [SerializeField] private RectTransform panelLayer;

    [Header("UI Context (session)")]
    [SerializeField] private string themeId = "Light";
    [SerializeField] private string localeId = "ko-KR";

    [Header("Behaviour")]
    [SerializeField] private bool reapplyOnDisplayChange = true;
    [SerializeField] private bool logTrace = true;

    private UIManager _ui;
    private AdaptiveDemoUIRoot _demoRoot;
    private DisplayContext _shownWith;

    private void Awake()
    {
        if (rootLayer == null)
            throw new System.InvalidOperationException("[UIPresentationInstaller] Root Layer is required.");

        if (initialRootPrefab == null)
            throw new System.InvalidOperationException("[UIPresentationInstaller] Initial Root Prefab is required.");

        if (initialRootPresentation == null)
            throw new System.InvalidOperationException("[UIPresentationInstaller] Initial Root Presentation is required.");

        UIContext context = new(themeId, localeId);
        _ui = new UIManager(
            rootLayer,
            panelLayer,
            new UIResolver(context),
            new UIPatchApplier());

        GameObject instance = Instantiate(initialRootPrefab, rootLayer);
        _demoRoot = instance.GetComponent<AdaptiveDemoUIRoot>();
        if (_demoRoot == null)
        {
            Destroy(instance);
            throw new System.InvalidOperationException(
                $"[UIPresentationInstaller] Prefab '{initialRootPrefab.name}' must have " +
                $"{nameof(AdaptiveDemoUIRoot)} on its root.");
        }

        _demoRoot.gameObject.SetActive(false);
        _ui.Register(_demoRoot);
    }

    private void Start()
    {
        ShowAdaptiveDemo();
    }

    private void Update()
    {
        if (!reapplyOnDisplayChange || _ui?.CurrentRoot == null)
            return;

        DisplayContext now = UnityDisplayContextProvider.GetCurrent();
        if (now == _shownWith)
            return;

        _ui.ReapplyVisible(now);
        _shownWith = now;
        RefreshDebugLabel();
        LogTrace();
    }

    public void ShowAdaptiveDemo()
    {
        _ui.SwitchRoot<AdaptiveDemoUIRoot>(
            initialRootPresentation,
            _ => RefreshDebugLabel());

        _shownWith = _ui.LastDisplay;
        LogTrace();
    }

    private void RefreshDebugLabel()
    {
        if (_demoRoot == null || _ui?.LastResult?.Resolved == null)
            return;

        _demoRoot.GetComponentInChildren<DisplayInfoLabel>(includeInactive: true)?
            .Set(_ui.LastDisplay, _ui.LastResult.Resolved);
    }

    private void LogTrace()
    {
        if (logTrace && _ui?.LastResult?.Trace != null)
            Debug.Log(_ui.LastResult.Trace.Dump(), this);
    }
}