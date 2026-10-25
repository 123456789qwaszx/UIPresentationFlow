namespace BlackHole.Unity
{
    // 키 입력. 부트스트랩이 키보드에서 읽어 넘긴다.
    // Esc는 지금 화면에 따라 한 가지 일만 한다 — 맨 위 창을 닫거나, 창이 없으면 일시 정지 창을 연다:
    //   화면 전환 중        → 무시(덮이는 동안 창을 열면 바뀐 화면 위에 남는다)
    //   엔딩               → 건너뛰기
    //   확인 창            → 취소
    //   설정 창            → 닫기(저장)
    //   모드 선택 창        → 닫기
    //   일시 정지 창        → 닫기(계속)
    //   창 없음(타이틀)     → 일시 정지 창 열기
    // Space는 엔딩에서만 쓴다(감사 문구에서 끝내기, 누르고 있으면 빨리 감기).
    internal sealed partial class BlackHoleDemoFlow
    {
        public void HandleEsc()
        {
            if (_transition.IsPlaying)
                return;

            if (IsEndingPlaying)
            {
                SkipEnding();
                return;
            }

            switch (_ui.TopPanel)
            {
                case ConfirmPanel:
                    CancelConfirm();
                    return;
                case SettingsPanel:
                    CloseSettings();
                    return;
                case ModeSelectPanel:
                    CloseModeSelect();
                    return;
                case PausePanel:
                    ClosePause();
                    return;
                case null when _ui.CurrentRoot is TitleScreen:
                    OpenPause();
                    return;
            }
        }

        public void HandleSpace()
        {
            if (IsEndingPlaying)
                ContinueEnding();
        }
    }
}
