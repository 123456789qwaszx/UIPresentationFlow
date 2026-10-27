using System.Collections.Generic;
using Ked.Samples.UI;

namespace VisualNovelDemo
{
    // 08 백로그 — 본 줄의 기록(VNDemoPlayer.History)을 그대로 보인다. 위젯(BacklogRow)을 줄마다 찍은 스크롤 목록.
    // 지금 줄을 뺀 줄에는 이동 버튼이 있고, 누르면 그 줄부터 다시(되감기와 같은 기록을 자른다).
    // 이동 버튼 위에서 시작한 끌기도 목록을 끈다 — UIPF UI_EventHandler가 끌기를 부모 ScrollRect에 넘긴다.
    public sealed partial class VNDemoFlow
    {
        private void SubscribeBacklog()
        {
            _backlog.CloseClicked += ClosePanel;
            _backlog.JumpClicked += JumpToBacklog;
        }

        private void OpenBacklog()
        {
            IReadOnlyList<int> history = _player.History;
            var rows = new List<BacklogRow.Model>(history.Count);
            int last = history.Count - 1;

            for (int i = 0; i < history.Count; i++)
            {
                rows.Add(new BacklogRow.Model
                {
                    Speaker = _player.SpeakerOf(history[i]),
                    Text = _player.TextOf(history[i]),
                    CanJump = i < last,
                    IsCurrent = i == last,
                    ShowDivider = i < last,
                });
            }

            PushPanel<BacklogPanel>(panel => panel.Present(rows));
        }

        private void JumpToBacklog(int index)
        {
            CloseAllPanels();
            _player.JumpTo(index);
        }
    }
}
