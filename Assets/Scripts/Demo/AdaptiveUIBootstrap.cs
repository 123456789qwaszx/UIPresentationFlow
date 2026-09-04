using UnityEngine;

public sealed class AdaptiveUIBootstrap : MonoBehaviour
{
    [Header("UI Layers")]
    [SerializeField] private RectTransform rootLayer;
    [SerializeField] private RectTransform panelLayer;

    [Header("Registered Views")]
    [SerializeField] private UIBase[] views;

    [Header("Presentations")]
    [SerializeField] private UIPresentationSpec titlePresentation;

    [Header("UI Context")]
    [SerializeField] private string themeId = "Light";
    [SerializeField] private string localeId = "ko-KR";

    [Header("Panel Stack Policy")]
    [SerializeField, Min(1)]
    private int panelKeepAliveDepth = 3;

    [SerializeField, Range(0f, 1f)]
    private float coveredPanelAlpha = 0.9f;

    [Header("Runtime")]
    [SerializeField] private UIDisplayRefreshDriver displayRefreshDriver;
    [SerializeField] private AdaptiveDemoDiagnostics adaptiveDemoDiagnostics;

    private UIManager _ui;
    private VNScreenBindings _screens;

    private void Awake()
    {
        UIContext context = new(themeId, localeId);
        UIResolver resolver = new(context);
        UIPresentationApplier presentationApplier = new();

        _ui = new UIManager(
            rootLayer,
            panelLayer,
            resolver,
            presentationApplier,
            panelKeepAliveDepth,
            coveredPanelAlpha);

        foreach (UIBase view in views)
        {
            if (view == null)
                continue;

            view.gameObject.SetActive(false);
            _ui.Register(view);
        }

        _screens = new VNScreenBindings(_ui, titlePresentation);

        displayRefreshDriver?.Initialize(_ui);
        adaptiveDemoDiagnostics?.Initialize(_ui);
    }

    private void Start()
    {
        _screens.OpenTitleMenu();
    }

    private void OnDestroy()
    {
        _screens?.Dispose();
    }
}