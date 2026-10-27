using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R = Ked.Samples.UI.SystemMenuPanel.Refs;

namespace Ked.Samples.UI.Editor
{
    /// <summary>04 시스템 메뉴 — Design/04_menu.png. 버튼 열은 세로 레이아웃(예제에 따라 저장·불러오기가 빠진다).</summary>
    public static class MenuSpec
    {
        public static GameObject Build(KedUIKit kit)
        {
            UINode view = kit.NewView<SystemMenuPanel>(nameof(SystemMenuPanel));

            UINode dim = kit.Fill(view, nameof(R.Dim));
            kit.Solid(dim, KedColors.Dim, raycast: true);

            UINode safe = kit.Fill(view, nameof(R.SafeAreaRoot));
            UINode frame = kit.Frame(safe, "MenuFrame");

            UINode panel = kit.Node(frame, nameof(R.MenuPanel), 560, 68, 480, 766);
            kit.Image(panel, "panel_menu", raycast: true);

            kit.Label(frame, nameof(R.MenuTitle), "메뉴", 602, 116, 320, 44, 28, KedColors.Navy, bold: true);
            kit.IconButton(frame, nameof(R.CloseMenu), "close", new Rect(964, 85, 46, 46), padding: 12f);

            UINode actions = kit.Node(frame, nameof(R.MenuActions), 616, 170, 368, 600);
            VerticalLayoutGroup layout = actions.Go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 26f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            (string name, string label, string text, KedButtonStyle style)[] buttons =
            {
                (nameof(R.MenuContinue), nameof(R.MenuContinueLabel), "계속하기", KedButtonStyle.Primary),
                (nameof(R.MenuSave), nameof(R.MenuSaveLabel), "저장", KedButtonStyle.Secondary),
                (nameof(R.MenuLoad), nameof(R.MenuLoadLabel), "불러오기", KedButtonStyle.Secondary),
                (nameof(R.MenuSettings), nameof(R.MenuSettingsLabel), "설정", KedButtonStyle.Secondary),
                (nameof(R.MenuSkipEpisode), nameof(R.MenuSkipEpisodeLabel), "이 에피소드 건너뛰기", KedButtonStyle.Secondary),
                (nameof(R.MenuTitleReturn), nameof(R.MenuTitleReturnLabel), "타이틀로", KedButtonStyle.Secondary),
            };

            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                kit.TextButton(actions, b.name, b.label, b.text, new Rect(616, 190 + i * 92, 368, 66), b.style);
            }

            return view.Go;
        }
    }
}
