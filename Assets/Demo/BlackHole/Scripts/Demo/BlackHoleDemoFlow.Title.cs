namespace BlackHole.Unity
{
    internal sealed partial class BlackHoleDemoFlow
    {
        private const float HintHoldSeconds = 6f;

        // 앱을 켠 뒤 처음 타이틀을 보일 때 데모 안내를 한 번 띄운다.
        private bool _hintShown;

        // 타이틀로(앱 시작). 화면 전환은 덮인 채 시작해, 원이 열리며 타이틀이 보인다.
        public void GoToTitle() => _transition.Play(ShowTitle);

        private void ShowTitle()
        {
            _ui.SwitchRoot<TitleScreen>(
                _presentations.Title,
                afterPresented: root =>
                {
                    BindView(root, ApplyBindings);
                    ShowHintOnce();
                },
                afterClosed: Unbind);
        }

        private void ApplyBindings(TitleScreen root)
        {
            AddBinding(root,
                r => r.StartClicked += OpenModeSelect,
                r => r.StartClicked -= OpenModeSelect);

            AddBinding(root,
                r => r.SettingsClicked += OpenSettings,
                r => r.SettingsClicked -= OpenSettings);

            AddBinding(root,
                r => r.QuitClicked += ConfirmQuit,
                r => r.QuitClicked -= ConfirmQuit);
        }

        private void ShowHintOnce()
        {
            if (_hintShown)
                return;

            _hintShown = true;
            ShowNotice(_localizer.Get(DemoText.Demo.Hint), HintHoldSeconds);
        }

        // 종료: 확인 창을 거친다.
        private void ConfirmQuit() =>
            OpenConfirm(
                _localizer.Get(DemoText.Demo.QuitTitle),
                _localizer.Get(DemoText.Demo.QuitBody),
                _localizer.Get(DemoText.Demo.QuitConfirm),
                _localizer.Get(DemoText.Cancel),
                QuitApp);
    }
}
