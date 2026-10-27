using System;
using System.Collections.Generic;
using Ked.Samples.UI;
using UnityEngine;

namespace VisualNovelDemo
{
    /// <summary>
    /// 비주얼 노벨 데모의 캔버스 루트. 메뉴 UI Demo > Build Visual Novel Demo가 씬을 지으며 붙이고 참조를 잇는다.
    /// UIPF를 세우는 일 — 층 셋(Root · Panel · Overlay), UIResolver(문맥 · 글꼴), UIManager, 화면 등록,
    /// 글자 표(Localizer · ViewTexts) — 과 흐름(VNDemoFlow)의 수명을 갖는다. ked의 KedUIHost와 같은 자리이고,
    /// 재생 런타임과 호스트 계약 대신 가짜 재생(VNDemoPlayer)을 쓴다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VNDemoHost : MonoBehaviour
    {
        [Header("UIPF 층")]
        [SerializeField] private RectTransform rootLayer;
        [SerializeField] private RectTransform panelLayer;
        [SerializeField] private RectTransform overlayLayer;
        [Tooltip("Panel 스택에서 살아 있는(보이는) 깊이. 맨 위 + 그 아래 하나.")]
        [SerializeField, Min(1)] private int panelKeepAliveDepth = 2;

        [Header("UIPF 테마")]
        [SerializeField] private UIFontSet fontSet;
        [SerializeField] private string themeId = "Light";
        [Tooltip("글자 표의 언어 열(ko-KR · en-US).")]
        [SerializeField] private string localeId = "ko-KR";
        [SerializeField] private TextAsset texts;
        [Tooltip("화면별 Presentation. presentationId = 화면 키(UIViewKey — 묶음은 Ked.{타입}).")]
        [SerializeField] private UIPresentationSpec[] presentations = Array.Empty<UIPresentationSpec>();

        [Header("데모")]
        [SerializeField] private VNDemoOptionItem optionItemPrefab;
        [Tooltip("타이틀 카드의 그림. 비우면 중립 바탕.")]
        [SerializeField] private Sprite examplePreview;

        private UIManager _ui;
        private Localizer _localizer;
        private ViewTexts _viewTexts;
        private VNDemoFlow _flow;
        private readonly Dictionary<string, UIPresentationSpec> _presentationById = new(StringComparer.Ordinal);
        private readonly Dictionary<Type, UIPresentationSpec> _presentationByType = new();

        private void Start()
        {
            foreach (UIPresentationSpec spec in presentations)
            {
                if (spec != null && !string.IsNullOrWhiteSpace(spec.presentationId))
                    _presentationById[spec.presentationId.Trim()] = spec;
            }

            UIContext context = new(themeId, localeId);
            UIResolver resolver = new(context, new UIThemeAssets(fontSet, null, null));

            _ui = new UIManager(
                rootLayer,
                panelLayer,
                resolver,
                new UIPresentationApplier(),
                panelKeepAliveDepth,
                coveredPanelAlpha: 1f,
                overlayLayer: overlayLayer);

            List<UIBase> views = RegisterViews();

            _localizer = new Localizer(texts != null ? texts.text : string.Empty, localeId);
            _viewTexts = new ViewTexts(views, _localizer);

            var example = new KedExampleCard
            {
                NameKey = "demo.example.name",
                TitleKey = "demo.example.title",
                SummaryKey = "demo.example.summary",
                Preview = examplePreview,
            };

            _flow = new VNDemoFlow(_ui, _localizer, PresentationOf, optionItemPrefab, example);

            if (TryGetComponent(out UIDisplayRefreshDriver driver))
                driver.Initialize(_ui);

            _flow.GoToTitle();
        }

        private void Update()
        {
            _flow?.Tick(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            _viewTexts?.Dispose();
        }

        // 층 아래의 화면(Root · Panel · Page · Overlay)을 모두 등록하고 감춘다. 위젯(카드 · 줄)은 화면의 일부라 등록하지 않는다.
        private List<UIBase> RegisterViews()
        {
            var views = new List<UIBase>();

            foreach (UIBase view in GetComponentsInChildren<UIBase>(includeInactive: true))
            {
                if (view is not (IUIRoot or IUIPanel or IUIPage or IUIOverlay))
                    continue;

                _ui.Register(view);
                views.Add(view);

                if (view.gameObject.activeSelf)
                    view.gameObject.SetActive(false);
            }

            return views;
        }

        private UIPresentationSpec PresentationOf(Type viewType)
        {
            if (_presentationByType.TryGetValue(viewType, out UIPresentationSpec spec))
                return spec;

            string key = UIViewKey.Of(viewType);

            if (!_presentationById.TryGetValue(key, out spec) || spec == null)
            {
                spec = ScriptableObject.CreateInstance<UIPresentationSpec>();
                spec.name = key;
                spec.presentationId = key;
            }

            _presentationByType[viewType] = spec;
            return spec;
        }
    }
}
