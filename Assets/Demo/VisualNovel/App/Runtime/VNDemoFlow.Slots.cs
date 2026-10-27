using System;
using System.Collections.Generic;
using System.Globalization;
using Ked.Samples.UI;
using UnityEngine;

namespace VisualNovelDemo
{
    // 05 저장 · 06 불러오기 — 메모리 저장(VNDemoSaves). 화면 규칙은 ked 예제와 같다:
    // 최근 저장이 01, 저장 모드에서는 목록 다음 칸 하나가 "빈 슬롯(새로 저장)"이고 그 뒤 칸은 누를 수 없는 빈 자리.
    // 위젯(SaveSlotCard) 여섯 장이 한 쪽이다.
    public sealed partial class VNDemoFlow
    {
        private enum SlotMode
        {
            Save,
            Load,
        }

        private SlotMode _slotMode;
        private int _slotPage;
        private int _slotSelected = -1;

        private IReadOnlyList<VNDemoSaves.Entry> Entries => _saves.Entries;

        private void SubscribeSlots()
        {
            _slots.BackClicked += ClosePanel;
            _slots.CloseClicked += CloseAllPanels;
            _slots.PreviousClicked += () => TurnSlotPage(-1);
            _slots.NextClicked += () => TurnSlotPage(+1);
            _slots.CardClicked += SelectSlotCard;
            _slots.CardDoubleClicked += index =>
            {
                SelectSlotCard(index);
                ActOnSelectedSlot();
            };
            _slots.ActionClicked += ActOnSelectedSlot;
        }

        private void OpenSlots(SlotMode mode)
        {
            _slotMode = mode;

            // 저장은 "새로 저장" 칸을 먼저 고른다 — 열자마자 실행 버튼을 눌러도 기록을 덮어쓰지 않게.
            _slotSelected = mode == SlotMode.Save ? Entries.Count : FirstSelectableSlot();
            _slotPage = _slotSelected >= 0 ? _slotSelected / SaveLoadPanel.SlotsPerPage : 0;

            PushPanel<SaveLoadPanel>(_ => RedrawSlots(focus: true));
        }

        private bool CanSave => _player.IsRunning;
        private int CellCount => Entries.Count + (_slotMode == SlotMode.Save ? 1 : 0);
        private int PageCount => Mathf.Max(1, Mathf.CeilToInt(CellCount / (float)SaveLoadPanel.SlotsPerPage));

        private bool IsFilledCell(int cell) => cell >= 0 && cell < Entries.Count;
        private bool IsNewSlotCell(int cell) => _slotMode == SlotMode.Save && cell == Entries.Count;
        private bool IsSelectableCell(int cell) => IsFilledCell(cell) || IsNewSlotCell(cell);
        private bool IsOnCurrentPage(int cell) => cell >= 0 && cell / SaveLoadPanel.SlotsPerPage == _slotPage;

        private int FirstSelectableSlot()
        {
            for (int cell = 0; cell < CellCount; cell++)
            {
                if (IsSelectableCell(cell))
                    return cell;
            }

            return -1;
        }

        // 쪽을 넘기면 선택도 그 쪽의 첫 칸으로 옮긴다 — 보이지 않는 칸에 실행이 걸리지 않게.
        private void TurnSlotPage(int delta)
        {
            int next = Mathf.Clamp(_slotPage + delta, 0, PageCount - 1);
            if (next == _slotPage)
                return;

            _slotPage = next;
            _slotSelected = -1;

            int first = _slotPage * SaveLoadPanel.SlotsPerPage;
            int end = Mathf.Min(CellCount, first + SaveLoadPanel.SlotsPerPage);
            for (int cell = first; cell < end && _slotSelected < 0; cell++)
            {
                if (IsSelectableCell(cell))
                    _slotSelected = cell;
            }

            RedrawSlots(focus: false);
        }

        private void SelectSlotCard(int indexOnPage)
        {
            int cell = _slotPage * SaveLoadPanel.SlotsPerPage + indexOnPage;
            if (!IsSelectableCell(cell))
                return;

            _slotSelected = cell;
            RedrawSlots(focus: false);
        }

        private void RedrawSlots(bool focus)
        {
            bool save = _slotMode == SlotMode.Save;
            int first = _slotPage * SaveLoadPanel.SlotsPerPage;

            var cards = new SaveSlotCard.Model[SaveLoadPanel.SlotsPerPage];
            for (int i = 0; i < cards.Length; i++)
                cards[i] = CardFor(first + i);

            string hint = !save && Entries.Count == 0
                ? Text("slots.hint.none")
                : Text(save ? "slots.hint.save" : "slots.hint.load");

            _slots.Present(new SaveLoadPanel.Model
            {
                Title = Text(save ? "slots.save.title" : "slots.load.title"),
                Subtitle = Text(save ? "slots.save.subtitle" : "slots.load.subtitle"),
                TabLabel = Text("slots.tab.manual"),
                PageInfo = Format("slots.pageInfo", KedUIUtil.TwoDigits(_slotPage + 1), KedUIUtil.TwoDigits(PageCount)),
                PageNumbers = PageNumbers(),
                Hint = hint,
                ActionLabel = Text(save ? "slots.save.action" : "slots.load.action"),
                ActionEnabled = IsOnCurrentPage(_slotSelected) && IsSelectableCell(_slotSelected) && (!save || CanSave),
                CanPrevious = _slotPage > 0,
                CanNext = _slotPage < PageCount - 1,
                Cards = cards,
                FocusCard = focus && _slotSelected >= first && _slotSelected < first + cards.Length
                    ? _slotSelected - first
                    : -1,
            });
        }

        private SaveSlotCard.Model CardFor(int cell)
        {
            bool selected = cell == _slotSelected;

            if (IsFilledCell(cell))
            {
                VNDemoSaves.Entry entry = Entries[cell];

                return new SaveSlotCard.Model
                {
                    Filled = true,
                    Number = KedUIUtil.TwoDigits(cell + 1),
                    Title = entry.Title,
                    Date = entry.SavedAt.ToString("yyyy.MM.dd  HH:mm", CultureInfo.InvariantCulture),
                    Chapter = entry.Chapter,
                    PlayTime = Format("slots.playTime", FormatPlayTime(entry.PlaySeconds)),
                    Selectable = true,
                    Selected = selected,
                };
            }

            bool isNew = IsNewSlotCell(cell);

            return new SaveSlotCard.Model
            {
                Filled = false,
                EmptyTitle = Text("slots.empty"),
                EmptyHint = isNew ? Text("slots.empty.hint") : string.Empty,
                Selectable = isNew,
                Selected = selected && isNew,
            };
        }

        // "1 2 3 4 5" — 지금 쪽만 청록. 쪽이 많으면 지금 쪽 둘레만.
        private string PageNumbers()
        {
            const int window = 5;
            int count = PageCount;
            int start = Mathf.Clamp(_slotPage - window / 2, 0, Mathf.Max(0, count - window));
            int end = Mathf.Min(count, start + window);

            var parts = new List<string>(window);
            for (int p = start; p < end; p++)
            {
                string number = (p + 1).ToString(CultureInfo.InvariantCulture);
                parts.Add(p == _slotPage ? $"<color=#13BDC5>{number}</color>" : number);
            }

            return string.Join("   ", parts);
        }

        private void ActOnSelectedSlot()
        {
            if (!IsOnCurrentPage(_slotSelected) || !IsSelectableCell(_slotSelected))
                return;

            if (_slotMode == SlotMode.Save)
            {
                if (!CanSave)
                    return;

                if (IsNewSlotCell(_slotSelected))
                {
                    SaveInto(null);
                    return;
                }

                VNDemoSaves.Entry entry = Entries[_slotSelected];
                Confirm(new ConfirmPanel.Request
                {
                    Title = Text("confirm.overwrite.title"),
                    Body = Format("confirm.overwrite.body", KedUIUtil.TwoDigits(_slotSelected + 1)),
                    AcceptLabel = Text("confirm.overwrite.accept"),
                    CancelLabel = Text("common.cancel"),
                    OnAccept = () => SaveInto(entry.Id),
                });
                return;
            }

            VNDemoSaves.Entry target = Entries[_slotSelected];

            // 재생 중이면 지금 자리를 벗어난다는 것을 확인한다. 타이틀에서는 잃을 것이 없다.
            if (_player.IsRunning)
            {
                Confirm(new ConfirmPanel.Request
                {
                    Title = Text("confirm.load.title"),
                    Body = Format("confirm.load.body", KedUIUtil.TwoDigits(_slotSelected + 1)),
                    AcceptLabel = Text("confirm.load.accept"),
                    CancelLabel = Text("common.cancel"),
                    OnAccept = () => LoadFrom(target),
                });
                return;
            }

            LoadFrom(target);
        }

        private void SaveInto(string overwriteId)
        {
            VNDemoPlayer.Snapshot snapshot = _player.Capture();
            string title = Preview(snapshot.Step);
            string chapter = ChapterLabel();
            int seconds = Mathf.FloorToInt(_playSeconds);

            if (overwriteId == null)
                _saves.Create(title, chapter, seconds, snapshot);
            else
                _saves.Overwrite(overwriteId, title, chapter, seconds, snapshot);

            _slotPage = 0;
            _slotSelected = 0;
            RedrawSlots(focus: true);

            Notice(
                Text("notice.saved.title"),
                Format("notice.saved.body", KedUIUtil.TwoDigits(1)),
                NoticePanel.Kind.Success);
        }

        private void LoadFrom(VNDemoSaves.Entry entry)
        {
            _player.StopFastForward();
            CloseAllPanels();

            _playSeconds = entry.PlaySeconds;
            GoToPlay();
            _player.Restore(entry.Snapshot);
        }

        // 카드 제목 — 그 줄의 앞부분(이름이 있으면 "이름: ").
        private string Preview(int step)
        {
            if (step < 0)
                return string.Empty;

            string speaker = _player.SpeakerOf(step);
            string text = _player.TextOf(step);
            string line = string.IsNullOrEmpty(speaker) ? text : speaker + ": " + text;

            const int max = 28;
            return line.Length > max ? line.Substring(0, max) + "…" : line;
        }

        private static string FormatPlayTime(int seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0, seconds));
            return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }
    }
}
