using System;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 백로그 한 줄(시안 08). BacklogPanel이 숨겨 둔 견본(LogRowTemplate)을 복제해 쓴다. UIPF 위젯 — 등록하지 않는다.
    /// 이동 버튼은 그 줄로 실제로 갈 수 있을 때만 보인다(현재 장면 되감기 또는 과거 장면 갈라지기).
    /// </summary>
    public sealed class BacklogRow : UIBase<BacklogRow.Refs>
    {
        public enum Refs
        {
            RowHighlight,
            RowSpeaker,
            RowText,
            RowDivider,
            RowJump,
            RowJumpBase,
            RowJumpIcon,
        }

        public sealed class Model
        {
            public string Speaker;
            public string Text;
            public bool CanJump;
            public bool IsCurrent;
            public bool ShowDivider;
        }

        public event Action<BacklogRow> JumpClicked;

        public int Index { get; set; }

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.RowJump), () => JumpClicked?.Invoke(this));
        }

        public void Present(Model model)
        {
            KedUIUtil.SetText(View.Text(Refs.RowSpeaker), model.Speaker);
            KedUIUtil.SetText(View.Text(Refs.RowText), model.Text);

            KedUIUtil.SetActive(View.Rect(Refs.RowHighlight), model.IsCurrent);
            KedUIUtil.SetActive(View.Rect(Refs.RowDivider), model.ShowDivider && !model.IsCurrent);

            // 갈 수 없는 줄은 버튼을 숨긴다. 글자 칸은 폭이 정해져 있어 버튼이 빠져도 줄이 흔들리지 않는다.
            KedUIUtil.SetButton(View.Button(Refs.RowJump), model.CanJump, model.CanJump);
        }

        public TMPro.TMP_Text SpeakerText => View.Text(Refs.RowSpeaker);
        public TMPro.TMP_Text BodyText => View.Text(Refs.RowText);
    }
}
