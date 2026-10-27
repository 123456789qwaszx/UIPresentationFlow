using TMPro;
using UnityEngine;
using C = Ked.Samples.UI.ConfirmPanel.Refs;
using N = Ked.Samples.UI.NoticePanel.Refs;

namespace Ked.Samples.UI.Editor
{
    /// <summary>09 확인 · 10 안내 — Design/09_confirm.png · 10_notice.png. 딤이 화면 전체의 포인터를 막는다.</summary>
    public static class ModalSpec
    {
        public static GameObject Confirm(KedUIKit kit)
        {
            UINode view = kit.NewView<ConfirmPanel>(nameof(ConfirmPanel));

            UINode dim = kit.Fill(view, nameof(C.ConfirmDim));
            kit.Solid(dim, KedColors.Dim, raycast: true);

            UINode safe = kit.Fill(view, nameof(C.SafeAreaRoot));
            UINode frame = kit.Frame(safe, "ConfirmFrame");

            UINode popup = kit.Node(frame, nameof(C.ConfirmPopup), 400, 235, 800, 430);
            kit.Image(popup, "panel_popup", raycast: true);

            kit.Label(frame, nameof(C.ConfirmTitle), "", 440, 257, 650, 48, 28, KedColors.White, bold: true,
                overflow: TextOverflowModes.Ellipsis);
            kit.IconButton(frame, nameof(C.ConfirmClose), "close", new Rect(1128, 249, 42, 42), padding: 11f);

            // 두 줄 이상의 본문도 버튼에 닿지 않게 시안(116)보다 높게 둔다.
            UINode body = kit.Node(frame, nameof(C.ConfirmBody), 452, 335, 696, 190);
            kit.Text(body, "", 25, KedColors.Navy, TextAlignmentOptions.Center, wrap: true, lineSpacing: 12f);

            kit.TextButton(frame, nameof(C.ConfirmCancel), nameof(C.ConfirmCancelLabel), "취소",
                new Rect(462, 542, 278, 68), KedButtonStyle.Secondary);
            kit.TextButton(frame, nameof(C.ConfirmAccept), nameof(C.ConfirmAcceptLabel), "확인",
                new Rect(860, 542, 278, 68), KedButtonStyle.Primary);

            return view.Go;
        }

        public static GameObject Notice(KedUIKit kit)
        {
            UINode view = kit.NewView<NoticePanel>(nameof(NoticePanel));

            UINode dim = kit.Fill(view, nameof(N.NoticeDim));
            kit.Solid(dim, KedColors.Dim, raycast: true);

            UINode safe = kit.Fill(view, nameof(N.SafeAreaRoot));
            UINode frame = kit.Frame(safe, "NoticeFrame");

            UINode popup = kit.Node(frame, nameof(N.NoticePopup), 400, 235, 800, 430);
            kit.Image(popup, "panel_popup", raycast: true);

            kit.Label(frame, nameof(N.NoticeTitle), "", 440, 257, 650, 48, 28, KedColors.White, bold: true,
                overflow: TextOverflowModes.Ellipsis);

            UINode check = kit.Node(frame, nameof(N.NoticeCheck), 756, 340, 88, 72);
            kit.Image(check, "checkmark", preserveAspect: true);

            UINode warning = kit.Node(frame, nameof(N.NoticeWarning), 768, 342, 64, 64);
            kit.Image(warning, "icon_close", preserveAspect: true);
            warning.Go.SetActive(false);

            UINode body = kit.Node(frame, nameof(N.NoticeBody), 452, 420, 696, 110);
            kit.Text(body, "", 25, KedColors.Navy, TextAlignmentOptions.Center, wrap: true, lineSpacing: 10f);

            kit.TextButton(frame, nameof(N.NoticeAccept), nameof(N.NoticeAcceptLabel), "확인",
                new Rect(660, 544, 280, 66), KedButtonStyle.Primary);

            return view.Go;
        }
    }
}
