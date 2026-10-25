using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackHole.Unity
{
    // 블랙홀 키우기 UI 데모의 시작점. 게임의 GameBootstrap에서 UI에 쓰는 부분만 남겼다. 게임 규칙·전투·저장·서버는 없다.
    // 조립 순서는 게임과 같다:
    // 1. 설정(GameSettings 사본)과 글자 표(Localizer)
    // 2. UIManager: 시작 UIContext(테마·언어·글자 배율) + 공유 디자인 에셋(UIThemeAssets) + 화면 층 셋(Root·Panel·Overlay). 화면을 모두 등록한다
    // 3. 버튼 소리(ButtonFeedback.Sounds), 프리팹 글자 번역(ViewTexts), 설정 → UIContext 동기화(UISettingsSync), 화면 전환(ScreenTransition)
    // 4. 화면 흐름(BlackHoleDemoFlow)이 타이틀부터 시작한다. 키(Esc·Space)는 매 프레임 흐름에 넘긴다
    // 씬은 메뉴 UI Demo > Build BlackHole Demo Scene이 만든다(BlackHoleDemoSceneBuilder).
    public sealed class BlackHoleDemoBootstrap : MonoBehaviour
    {
        [Header("UI Layers")]
        [SerializeField] private RectTransform _rootLayer;
        [SerializeField] private RectTransform _panelLayer;
        [SerializeField] private RectTransform _overlayLayer;

        [Header("Registered Views")]
        [SerializeField] private UIBase[] _views;

        [Header("Presentations")]
        [SerializeField] private BlackHoleDemoPresentations _presentations = new();

        [Header("Shared Theme Assets")]
        [SerializeField] private UIFontSet _fonts;
        [SerializeField] private UIPalette _palette;
        [SerializeField] private UISpriteSet _sprites;

        [Header("Texts")]
        [Tooltip("UI 글자 표(BlackHoleDemoText.csv). 형식은 Localizer에 적었다.")]
        [SerializeField] private TextAsset _texts;

        [Header("Screen Transition")]
        [SerializeField] private ScreenTransitionLook _transitionLook;

        [Header("Sounds")]
        [SerializeField] private DemoButtonSounds _sounds;

        private GameSettings _settings;
        private Localizer _localizer;
        private ViewTexts _viewTexts;
        private UISettingsSync _uiSettings;
        private BlackHoleDemoFlow _flow;

        private void Awake()
        {
            _settings = GameSettings.Load();

            if (_texts == null)
                Debug.LogError("[데모] 부트스트랩에 UI 글자 표(BlackHoleDemoText.csv)를 연결해야 한다. 지금은 프리팹 글자로 보인다.", this);

            _localizer = new Localizer(_texts != null ? _texts.text : null, _settings.LanguageLocale.ToId());
            _settings.Changed += HandleSettingChanged;

            UIManager ui = CreateUI();

            if (_sounds != null)
                _sounds.Bind(_settings);

            ButtonFeedback.Sounds = _sounds;
            _viewTexts = new ViewTexts(_views, _localizer);
            _uiSettings = new UISettingsSync(ui, _settings);

            ScreenTransition transition = ScreenTransition.Create(CanvasRoot(), _transitionLook);
            _flow = new BlackHoleDemoFlow(ui, _presentations, _settings, _localizer, transition, _sounds);
        }

        private void Start() => _flow.GoToTitle();

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
                _flow.HandleEsc();

            if (keyboard.spaceKey.wasPressedThisFrame)
                _flow.HandleSpace();

            _flow.Tick(keyboard.spaceKey.isPressed);
        }

        private void OnDestroy()
        {
            if (_settings != null)
                _settings.Changed -= HandleSettingChanged;

            _flow?.Dispose();
            _uiSettings?.Dispose();
            _viewTexts?.Dispose();

            if (ReferenceEquals(ButtonFeedback.Sounds, _sounds))
                ButtonFeedback.Sounds = null;
        }

        // 언어 설정 → 글자 표의 언어. 글꼴은 UISettingsSync가 UIContext의 언어로 바꾼다.
        private void HandleSettingChanged(string id)
        {
            if (id == GameSettings.Language)
                _localizer.SetLocale(_settings.LanguageLocale.ToId());
        }

        private UIManager CreateUI()
        {
            UIContext context = UISettingsSync.ContextFor(_settings);
            UIResolver resolver = new(context, new UIThemeAssets(_fonts, _palette, _sprites));

            UIManager ui = new(
                _rootLayer,
                _panelLayer,
                resolver,
                new UIPresentationApplier(),
                overlayLayer: _overlayLayer);

            foreach (UIBase view in _views)
            {
                if (view == null)
                    continue;

                view.gameObject.SetActive(false);
                ui.Register(view);
            }

            UIDisplayRefreshDriver displayRefresh = GetOrAdd<UIDisplayRefreshDriver>(gameObject);
            displayRefresh.Initialize(ui);

            return ui;
        }

        // UI 캔버스의 루트. 화면 전환 덮개가 모든 층 위에 놓인다.
        private Transform CanvasRoot()
        {
            Canvas canvas = _rootLayer.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.transform : _rootLayer.parent;
        }

        private static T GetOrAdd<T>(GameObject owner) where T : Component =>
            owner.TryGetComponent(out T component) ? component : owner.AddComponent<T>();
    }
}
