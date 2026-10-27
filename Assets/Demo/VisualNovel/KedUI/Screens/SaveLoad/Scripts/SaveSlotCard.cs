using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 저장·불러오기가 함께 쓰는 슬롯 카드(시안 05·06의 save_slot). UIPF 위젯 — 화면(SaveLoadPanel)의 Ref 하나에
    /// 붙은 UIBase로, UIManager에 등록하지 않는다. 카드 안의 이름은 카드 안에서만 찾는다.
    ///
    /// 그림 창: 지금 저장은 화면을 찍지 않는다. 그래서 save_slot 그림의 밝은 자리(알려진 자리 그림)를 그대로 두고,
    /// 그 위에 실제 메타데이터(장면·플레이 시간)를 적는다. 화면 캡처는 별도 작업이다.
    /// </summary>
    [UIViewKey("Ked.SaveSlotCard")]
    public sealed class SaveSlotCard : UIBase<SaveSlotCard.Refs>
    {
        public enum Refs
        {
            CardButton,
            CardThumbnail,
            CardChapter,
            CardPlayTime,
            CardNumber,
            CardNumberText,
            CardTitle,
            CardDate,
            CardEmpty,
            CardEmptyHint,
            CardSelected,
        }

        public sealed class Model
        {
            public bool Filled;
            public string Number;
            public string Title;
            public string Date;
            public string Chapter;
            public string PlayTime;
            public string EmptyTitle;
            public string EmptyHint;
            public bool Selectable;
            public bool Selected;
        }

        public event Action<SaveSlotCard> Clicked;
        public event Action<SaveSlotCard> DoubleClicked;

        public int Index { get; set; }

        protected override void OnInitialize()
        {
            Button button = View.Button(Refs.CardButton);
            KedUIUtil.OnClick(button, () => Clicked?.Invoke(this));

            // 두 번 누르면 바로 실행(선택 → 실행 버튼과 같은 길). 클릭 수는 포인터 사건에서만 알 수 있다.
            UIEvents.Bind(button, OnPointerClicked);
        }

        public void Present(Model model)
        {
            bool filled = model.Filled;

            KedUIUtil.SetActive(View.Rect(Refs.CardNumber), filled);
            KedUIUtil.SetActive(View.Rect(Refs.CardTitle), filled);
            KedUIUtil.SetActive(View.Rect(Refs.CardDate), filled);
            KedUIUtil.SetActive(View.Rect(Refs.CardChapter), filled);
            KedUIUtil.SetActive(View.Rect(Refs.CardPlayTime), filled);
            KedUIUtil.SetActive(View.Rect(Refs.CardEmpty), !filled);
            KedUIUtil.SetActive(View.Rect(Refs.CardEmptyHint), !filled && !string.IsNullOrEmpty(model.EmptyHint));

            KedUIUtil.SetText(View.Text(Refs.CardNumberText), model.Number);
            KedUIUtil.SetText(View.Text(Refs.CardTitle), model.Title);
            KedUIUtil.SetText(View.Text(Refs.CardDate), model.Date);
            KedUIUtil.SetText(View.Text(Refs.CardChapter), model.Chapter);
            KedUIUtil.SetText(View.Text(Refs.CardPlayTime), model.PlayTime);
            KedUIUtil.SetText(View.Text(Refs.CardEmpty), model.EmptyTitle);
            KedUIUtil.SetText(View.Text(Refs.CardEmptyHint), model.EmptyHint);

            KedUIUtil.SetButton(View.Button(Refs.CardButton), true, model.Selectable);
            KedUIUtil.SetActive(View.Rect(Refs.CardSelected), model.Selected);
        }

        public Selectable Selectable => View.Button(Refs.CardButton);

        private void OnPointerClicked(PointerEventData eventData)
        {
            if (eventData != null && eventData.clickCount >= 2)
                DoubleClicked?.Invoke(this);
        }
    }
}
