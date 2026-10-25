using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlackHole.Unity
{
    // 데모의 화면 흐름. 게임의 ScreenFlow와 같은 방식이다 — 화면 하나에 partial 하나, 화면 이벤트는 열 때 묶고(BindView·AddBinding)
    // 닫힐 때 푼다(Unbind). 전환은 화면 전환(ScreenTransition)이 다 덮은 뒤에 한다.
    //
    // 화면과 UIPresentationFlow의 층:
    // - Root(SwitchRoot): 타이틀
    // - Panel(PushPanel·PopPanel, 쌓임): 모드 선택, 설정, 일시 정지, 확인 창. 아래 창은 흐려지고 입력을 받지 않는다
    // - Overlay(ShowOverlay·HideOverlay, 쌓이지 않음): 알림, 엔딩
    // - 화면 전환 덮개: 모든 층 위
    // 설정의 다크 모드·언어·글자 크기는 UISettingsSync가 UIContext로 바꿔 끼운다(UIManager.SetContext) — 보이는 화면이 그 자리에서 다시 적용된다.
    //
    // 흐름: 타이틀 → 시작 → 모드 선택 → 새 게임 → (데모 안내 확인 창) → 엔딩 → 타이틀
    //                                   계속 → 알림("이어 할 진행이 없다")
    //       타이틀 → 설정 / 종료(확인 창), Esc → 지금 화면에 맞게(창 닫기·일시 정지·엔딩 건너뛰기, BlackHoleDemoFlow.Keys)
    internal sealed partial class BlackHoleDemoFlow : IDisposable
    {
        private readonly UIManager _ui;
        private readonly BlackHoleDemoPresentations _presentations;
        private readonly GameSettings _settings;
        private readonly Localizer _localizer;
        private readonly ScreenTransition _transition;
        private readonly DemoButtonSounds _sounds;
        private readonly Dictionary<UIBase, List<Action>> _cleanupByScreen = new();

        public BlackHoleDemoFlow(
            UIManager ui,
            BlackHoleDemoPresentations presentations,
            GameSettings settings,
            Localizer localizer,
            ScreenTransition transition,
            DemoButtonSounds sounds)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _presentations = presentations ?? throw new ArgumentNullException(nameof(presentations));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _transition = transition != null ? transition : throw new ArgumentNullException(nameof(transition));
            _sounds = sounds;
        }

        // 매 프레임. Space를 누르고 있는지 엔딩에 넘긴다(빨리 감기).
        public void Tick(bool continueHeld) => TickEnding(continueHeld);

        private void PlaySwitchSound()
        {
            if (_sounds != null)
                _sounds.PlaySwitchScreen();
        }

        // 앱을 끝낸다. 에디터에서는 플레이를 멈춘다.
        private static void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BindView<T>(T screen, Action<T> apply) where T : UIBase
        {
            Unbind(screen);
            apply(screen);
        }

        private void AddBinding<T>(T screen, Action<T> attach, Action<T> detach) where T : UIBase
        {
            attach(screen);

            if (!_cleanupByScreen.TryGetValue(screen, out List<Action> cleanups))
            {
                cleanups = new List<Action>();
                _cleanupByScreen[screen] = cleanups;
            }

            cleanups.Add(() => detach(screen));
        }

        private void Unbind(UIBase screen)
        {
            if (!_cleanupByScreen.TryGetValue(screen, out List<Action> cleanups))
                return;

            _cleanupByScreen.Remove(screen);
            RunCleanups(cleanups);
        }

        private static void RunCleanups(List<Action> cleanups)
        {
            for (int i = cleanups.Count - 1; i >= 0; i--)
                cleanups[i]?.Invoke();
        }

        public void Dispose()
        {
            CloseEnding();
            DisposeNotice();

            foreach (List<Action> cleanups in _cleanupByScreen.Values)
                RunCleanups(cleanups);

            _cleanupByScreen.Clear();
        }
    }

    // 화면마다의 Presentation. 부트스트랩의 Inspector에서 연결한다(게임의 ScreenPresentations 에셋과 같은 역할).
    [Serializable]
    public sealed class BlackHoleDemoPresentations
    {
        public UIPresentationSpec Title;
        public UIPresentationSpec ModeSelect;
        public UIPresentationSpec Settings;
        public UIPresentationSpec Pause;
        public UIPresentationSpec Confirm;
        public UIPresentationSpec Notice;
        public UIPresentationSpec Ending;
    }
}
