namespace BlackHole.Unity
{
    internal sealed partial class BlackHoleDemoFlow
    {
        // 지금 화면 위에 일시 정지 창을 쌓는다. 데모에는 멈출 판이 없어 창만 연다.
        private void OpenPause()
        {
            _ui.PushPanel<PausePanel>(
                _presentations.Pause,
                afterPresented: panel => BindView(panel, ApplyBindings),
                afterClosed: Unbind);
        }

        private void ApplyBindings(PausePanel panel)
        {
            AddBinding(panel,
                p => p.ResumeClicked += ClosePause,
                p => p.ResumeClicked -= ClosePause);

            // 설정: 일시 정지 창 위에 설정 창을 쌓는다. 설정 창을 닫으면 일시 정지 창으로 돌아온다.
            AddBinding(panel,
                p => p.SettingsClicked += OpenSettings,
                p => p.SettingsClicked -= OpenSettings);

            AddBinding(panel,
                p => p.MainMenuClicked += ConfirmMainMenu,
                p => p.MainMenuClicked -= ConfirmMainMenu);

            AddBinding(panel,
                p => p.QuitClicked += ConfirmQuit,
                p => p.QuitClicked -= ConfirmQuit);
        }

        private void ClosePause() => _ui.PopPanel(Unbind);

        // 메인 메뉴: 확인하면 화면 전환으로 덮은 뒤 창을 모두 닫고 타이틀을 다시 보인다.
        private void ConfirmMainMenu() =>
            OpenConfirm(
                _localizer.Get(DemoText.Demo.MainMenuTitle),
                _localizer.Get(DemoText.Demo.MainMenuBody),
                _localizer.Get(DemoText.Demo.MainMenuConfirm),
                _localizer.Get(DemoText.Cancel),
                ReturnToTitle);

        private void ReturnToTitle()
        {
            PlaySwitchSound();
            _transition.Play(() =>
            {
                _ui.PopAllPanels(Unbind);
                ShowTitle();
            });
        }
    }
}
