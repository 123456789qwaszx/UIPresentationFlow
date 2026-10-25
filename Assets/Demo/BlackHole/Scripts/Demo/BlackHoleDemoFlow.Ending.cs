using System.Collections.Generic;

namespace BlackHole.Unity
{
    // 엔딩(Overlay). 게임에서는 마지막 이정표 뒤 붕괴 연출 다음에 열리고, 데모에서는 모드 선택의 새 게임에서 연다.
    // 1. 엔딩 화면(EndingOverlay)이 지금 화면 위를 검게 덮는다. 다 덮이면(Covered) 아래의 창을 모두 닫는다(게임에서는 이때 판을 결산한다).
    // 2. 엔딩 문구 → 크레딧 → 감사 문구. 문구·크레딧은 누르고 있으면(클릭·터치, Space) 빨리 흐른다. Esc는 건너뛴다.
    // 3. 감사 문구에서 누르면 화면 전환이 덮은 뒤 엔딩을 닫고 타이틀을 보인다.
    internal sealed partial class BlackHoleDemoFlow
    {
        // 엔딩 글자(ending.*). 줄·크레딧 묶음은 1부터 번호가 끊길 때까지 읽는다.
        // 크레딧 이름 칸 안에서 사람을 나누는 글자(한 줄에 한 사람).
        private const char EndingNameSeparator = '|';
        private const int EndingMaxRows = 64;

        private EndingOverlay _endingScreen;

        // 엔딩 중인가(연 때부터 타이틀로 갈 때까지). 이 동안 Esc·Space는 엔딩만 다룬다.
        private bool IsEndingPlaying => _endingScreen != null;

        private void PlayEnding()
        {
            if (_endingScreen != null)
                return;

            EndingTexts texts = EndingTextsOf();

            _endingScreen = _ui.ShowOverlay<EndingOverlay>(_presentations.Ending, ending =>
            {
                ending.Covered += HandleEndingCovered;
                ending.Finished += HandleEndingFinished;
                ending.Present(texts);
                ending.Play();
            });
        }

        // 빨리 감기 키(Space)를 누르고 있는지 넘긴다.
        private void TickEnding(bool continueHeld)
        {
            if (_endingScreen != null)
                _endingScreen.FastForwardKeyHeld = continueHeld;
        }

        // 다 덮였다: 가려진 창(모드 선택)을 닫는다.
        private void HandleEndingCovered() => _ui.PopAllPanels(Unbind);

        private void HandleEndingFinished()
        {
            PlaySwitchSound();
            _transition.Play(() =>
            {
                CloseEnding();
                ShowTitle();
            });
        }

        // Esc: 문구·크레딧 중이면 감사 문구로, 감사 문구에서는 끝낸다.
        private void SkipEnding()
        {
            if (_endingScreen != null)
                _endingScreen.Skip();
        }

        // Space를 눌렀다: 감사 문구에서 끝낸다. 문구·크레딧 중에는 누르고 있는 동안 빨리 감는다(TickEnding).
        private void ContinueEnding()
        {
            if (_endingScreen != null)
                _endingScreen.Continue();
        }

        private void CloseEnding()
        {
            if (_endingScreen == null)
                return;

            _endingScreen.Covered -= HandleEndingCovered;
            _endingScreen.Finished -= HandleEndingFinished;
            _ui.HideOverlay<EndingOverlay>();
            _endingScreen = null;
        }

        // 글자 표의 엔딩 글자. 빈 칸은 건너뛴다.
        private EndingTexts EndingTextsOf()
        {
            var lines = new List<string>();

            for (int i = 1; i <= EndingMaxRows && _localizer.TryGet(DemoText.Ending.Line(i), out string line); i++)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
            }

            var credits = new List<EndingCredit>();

            for (int i = 1; i <= EndingMaxRows && _localizer.TryGet(DemoText.Ending.CreditRole(i), out string role); i++)
            {
                var names = new List<string>();

                if (_localizer.TryGet(DemoText.Ending.CreditNames(i), out string cell))
                {
                    foreach (string name in cell.Split(EndingNameSeparator))
                    {
                        if (!string.IsNullOrWhiteSpace(name))
                            names.Add(name.Trim());
                    }
                }

                credits.Add(new EndingCredit(role, names));
            }

            return new EndingTexts
            {
                Lines = lines,
                CreditsTitle = _localizer.TryGet(DemoText.Ending.CreditsTitle, out string title) ? title : "",
                Credits = credits,
                Thanks = _localizer.Get(DemoText.Ending.Thanks),
                Prompt = _localizer.Get(DemoText.Ending.Prompt),
            };
        }
    }
}
