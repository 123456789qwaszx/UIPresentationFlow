using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 01 타이틀. UIPF Root — 타이틀과 재생 화면(PlayScreen)이 Root 층을 번갈아 차지한다.
    /// 처음부터 · 이어하기 · 불러오기(저장 슬롯이 있는 예제) · 다른 예제로 · 설정 · 종료.
    /// 예제 이름·소개는 호스트가 준 키로 표에서 읽는다. 이어할 기록이 없으면 이어하기를 막는다.
    /// </summary>
    [UIViewKey("Ked.TitleScreen")]
    public sealed class TitleScreen : UIRoot<TitleScreen.Refs>, ILocalizedView
    {
        public enum Refs
        {
            SafeAreaRoot,
            Background,
            Frame,

            Brand,
            BrandSubtitle,
            Tagline,
            CurrentExample,

            MenuColumn,
            NewGame,
            NewGameLabel,
            Continue,
            ContinueLabel,
            Load,
            LoadLabel,
            SwitchExample,
            SwitchExampleLabel,
            OpenSettings,
            OpenSettingsLabel,
            Quit,
            QuitLabel,

            ScenePreview,
            PreviewScene,
            PreviewCaption,
            PreviewTitle,
            PreviewSub,
            BuildLabel,
        }

        public event Action StartClicked;
        public event Action ContinueClicked;
        public event Action LoadClicked;
        public event Action SwitchExampleClicked;
        public event Action SettingsClicked;
        public event Action QuitClicked;

        private Localizer _texts;
        private Model _model;

        public sealed class Model
        {
            public KedExampleCard Example;
            public bool CanContinue;
            public bool ShowLoad;
            public bool CanSwitchExample;
        }

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.NewGame), () => StartClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.Continue), () => ContinueClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.Load), () => LoadClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.SwitchExample), () => SwitchExampleClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.OpenSettings), () => SettingsClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.Quit), () => QuitClicked?.Invoke());
        }

        public void Present(Model model)
        {
            _model = model;
            Redraw();

            // 키보드로 바로 고를 수 있게 첫 버튼에 둔다. 이어할 것이 있으면 이어하기.
            KedUIUtil.Focus(model != null && model.CanContinue
                ? View.Button(Refs.Continue)
                : View.Button(Refs.NewGame));
        }

        public void Relocalize(Localizer localizer)
        {
            _texts = localizer;
            Redraw();
        }

        private void Redraw()
        {
            if (_model == null)
                return;

            KedExampleCard example = _model.Example;

            KedUIUtil.SetText(
                View.Text(Refs.CurrentExample),
                Format("title.currentExample", Text(example?.NameKey)));

            KedUIUtil.SetText(View.Text(Refs.PreviewTitle), Text(example?.TitleKey));
            KedUIUtil.SetText(View.Text(Refs.PreviewSub), Text(example?.SummaryKey));

            Image preview = View.Image(Refs.PreviewScene);
            if (preview != null)
            {
                preview.sprite = example?.Preview;
                // 그림이 없으면 바탕색만 — 시안 샘플 그림을 대신 넣지 않는다.
                preview.color = example?.Preview != null ? Color.white : KedColors.Divider;
            }

            KedUIUtil.SetButton(View.Button(Refs.Continue), visible: true, interactable: _model.CanContinue);
            KedUIUtil.SetButton(View.Button(Refs.Load), visible: _model.ShowLoad, interactable: true);
            KedUIUtil.SetButton(View.Button(Refs.SwitchExample), visible: _model.CanSwitchExample, interactable: true);
        }

        private string Text(string key) => KedUIUtil.Text(_texts, key);

        private string Format(string key, params object[] args) => KedUIUtil.Format(_texts, key, args);
    }
}
