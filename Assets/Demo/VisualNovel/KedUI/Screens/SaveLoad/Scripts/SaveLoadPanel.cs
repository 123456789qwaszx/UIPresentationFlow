using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 05 저장 · 06 불러오기. 한 Panel이 두 모드를 갖고 슬롯 카드(SaveSlotCard) 6장을 격자(GridLayoutGroup)로 보인다.
    /// 무엇을 보일지(목록·쪽·선택·막힘)는 흐름이 정해 Present로 넘긴다 — 이 화면은 그리기와 사건 전달만.
    ///
    /// 뒤로(←)는 한 층 위로(메뉴), 닫기(×)는 메뉴까지 모두 닫는다. Esc는 뒤로와 같다.
    /// </summary>
    public sealed class SaveLoadPanel : UIPanel<SaveLoadPanel.Refs>
    {
        public const int SlotsPerPage = 6;

        public enum Refs
        {
            SafeAreaRoot,
            Background,
            Frame,

            Back,
            BackBase,
            BackIcon,
            PageTitle,
            PageSubtitle,
            Close,
            CloseBase,
            CloseIcon,

            ManualTab,
            ManualLabel,
            PageInfo,

            SlotGrid,
            Slot_1,
            Slot_2,
            Slot_3,
            Slot_4,
            Slot_5,
            Slot_6,

            SlotHint,
            PreviousPage,
            PreviousPageBase,
            PreviousPageIcon,
            CurrentPage,
            NextPage,
            NextPageBase,
            NextPageIcon,
            SlotAction,
            SlotActionLabel,
        }

        public sealed class Model
        {
            public string Title;
            public string Subtitle;
            public string TabLabel;
            public string PageInfo;
            public string PageNumbers;
            public string Hint;
            public string ActionLabel;
            public bool ActionEnabled;
            public bool CanPrevious;
            public bool CanNext;
            public SaveSlotCard.Model[] Cards;
            public int FocusCard = -1;
        }

        public event Action BackClicked;
        public event Action CloseClicked;
        public event Action PreviousClicked;
        public event Action NextClicked;
        public event Action ActionClicked;
        public event Action<int> CardClicked;
        public event Action<int> CardDoubleClicked;

        private readonly List<SaveSlotCard> _cards = new(SlotsPerPage);

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.Back), () => BackClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.Close), () => CloseClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.PreviousPage), () => PreviousClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.NextPage), () => NextClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.SlotAction), () => ActionClicked?.Invoke());

            Refs[] slots = { Refs.Slot_1, Refs.Slot_2, Refs.Slot_3, Refs.Slot_4, Refs.Slot_5, Refs.Slot_6 };

            for (int i = 0; i < slots.Length; i++)
            {
                SaveSlotCard card = View.Widget<SaveSlotCard>(slots[i]);
                if (card == null)
                    continue;

                card.EnsureInitialized();
                card.Index = i;
                card.Clicked += c => CardClicked?.Invoke(c.Index);
                card.DoubleClicked += c => CardDoubleClicked?.Invoke(c.Index);

                // 카드 글자는 사용자 글자 크기를 따르지 않는다 — 크기가 정해진 카드라 커지면 겹친다. 긴 이름은 말줄임.
                _cards.Add(card);
            }
        }

        public void Present(Model model)
        {
            KedUIUtil.SetText(View.Text(Refs.PageTitle), model.Title);
            KedUIUtil.SetText(View.Text(Refs.PageSubtitle), model.Subtitle);
            KedUIUtil.SetText(View.Text(Refs.ManualLabel), model.TabLabel);
            KedUIUtil.SetText(View.Text(Refs.PageInfo), model.PageInfo);
            KedUIUtil.SetText(View.Text(Refs.CurrentPage), model.PageNumbers);
            KedUIUtil.SetText(View.Text(Refs.SlotHint), model.Hint);
            KedUIUtil.SetText(View.Text(Refs.SlotActionLabel), model.ActionLabel);

            KedUIUtil.SetButton(View.Button(Refs.SlotAction), true, model.ActionEnabled);
            KedUIUtil.SetButton(View.Button(Refs.PreviousPage), true, model.CanPrevious);
            KedUIUtil.SetButton(View.Button(Refs.NextPage), true, model.CanNext);

            for (int i = 0; i < _cards.Count; i++)
            {
                SaveSlotCard.Model card = model.Cards != null && i < model.Cards.Length ? model.Cards[i] : null;

                KedUIUtil.SetActive(_cards[i], card != null);

                if (card != null)
                    _cards[i].Present(card);
            }

            if (model.FocusCard >= 0 && model.FocusCard < _cards.Count)
                KedUIUtil.Focus(_cards[model.FocusCard].Selectable);
        }
    }
}
