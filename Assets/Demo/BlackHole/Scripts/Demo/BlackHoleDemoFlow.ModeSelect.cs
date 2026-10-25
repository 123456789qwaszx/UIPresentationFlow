using UnityEngine;

namespace BlackHole.Unity
{
    internal sealed partial class BlackHoleDemoFlow
    {
        private const string NormalModeId = "normal";
        private static readonly Color NormalModeColor = new(0.62f, 0.33f, 0.35f);
        private const float ContinueNoticeSeconds = 3f;

        // 모드 카드 하나. 계속 버튼은 알림을 보이기 위해 켜 둔다(데모에는 저장이 없다).
        private ModeSelectPanel.ModeItem[] Modes() => new[]
        {
            new ModeSelectPanel.ModeItem(
                NormalModeId,
                _localizer.Get(DemoText.Mode.NormalTitle),
                _localizer.Get(DemoText.Mode.NormalDescription),
                NormalModeColor,
                canContinue: true),
        };

        // 지금 루트(타이틀) 위에 모드 선택 창을 쌓는다.
        private void OpenModeSelect()
        {
            _ui.PushPanel<ModeSelectPanel>(
                _presentations.ModeSelect,
                afterPresented: panel =>
                {
                    BindView(panel, ApplyBindings);
                    panel.Build(Modes(), NormalModeId);
                },
                afterClosed: Unbind);
        }

        private void ApplyBindings(ModeSelectPanel panel)
        {
            AddBinding(panel,
                p => p.NewGameClicked += HandleModeSelectNewGameClicked,
                p => p.NewGameClicked -= HandleModeSelectNewGameClicked);

            AddBinding(panel,
                p => p.ContinueClicked += HandleModeSelectContinueClicked,
                p => p.ContinueClicked -= HandleModeSelectContinueClicked);

            AddBinding(panel,
                p => p.BackClicked += CloseModeSelect,
                p => p.BackClicked -= CloseModeSelect);
        }

        // 새 게임: 게임 플레이가 없는 데모라는 것을 확인 창으로 알리고, 확인하면 엔딩 연출로 간다.
        private void HandleModeSelectNewGameClicked(string modeId) =>
            OpenConfirm(
                _localizer.Get(DemoText.Demo.NewGameTitle),
                _localizer.Get(DemoText.Demo.NewGameBody),
                _localizer.Get(DemoText.Demo.NewGameConfirm),
                _localizer.Get(DemoText.Cancel),
                PlayEnding);

        // 계속: 이어 할 진행이 없다는 알림(Overlay). 창은 그대로 둔다.
        private void HandleModeSelectContinueClicked(string modeId) =>
            ShowNotice(_localizer.Get(DemoText.Demo.ContinueNotice), ContinueNoticeSeconds);

        private void CloseModeSelect() => _ui.PopPanel(Unbind);
    }
}
