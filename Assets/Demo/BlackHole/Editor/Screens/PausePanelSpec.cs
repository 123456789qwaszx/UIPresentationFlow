using UnityEngine;
using BlackHole.Unity;

namespace BlackHole.EditorTools
{
    // 일시 정지 창의 빌드 명세. 숫자는 시안 판(1920×1080, 왼쪽 위 원점)의 절대 좌표다.
    // 노드 이름은 화면의 Refs — nameof라 Refs와 어긋나면 컴파일이 깨진다.
    //
    // 게임에서 가져온 손 프리팹을 명세로 다시 지은 것이다(같은 자리 · 같은 GUID). 데모 화면 가운데
    // 이것만 명세로 짓고, 나머지 여섯은 손 프리팹 그대로다(docs/AUTHORING.md "특수 화면").
    internal static class PausePanelSpec
    {
        private const float ButtonTop = 495f;  // 작은 버튼 셋(150)의 위 변. 계속하기(190)는 같은 가운데 줄에서 20 위.
        private const float LabelTop = 679f;   // 버튼 이름 글자의 위 변 — 크기가 달라도 한 줄에 선다.

        public static GameObject Build(BlackHoleScreenKit kit)
        {
            UINode root = UINodes.Root(nameof(PausePanel), BlackHoleScreenKit.Screen);
            kit.Dim(root, 0.35f);
            root.Go.AddComponent<PausePanel>();

            UINode safe = UINodes.Fill(root, nameof(PausePanel.Refs.SafeAreaRoot));

            UINode panel = UINodes.Node(safe, nameof(PausePanel.Refs.Panel_Image), new Rect(400, 280, 1120, 520), UIAnchor.Center);
            kit.Image(panel, "panel", UnityEngine.UI.Image.Type.Sliced, pixelsPerUnitMultiplier: 2f);

            UINode title = UINodes.Node(panel, nameof(PausePanel.Refs.Title_Text), new Rect(560, 320, 800, 80), UIAnchor.TopCenter);
            kit.Text(title, "PAUSED", 56f, 12f);

            Button(kit, panel, PausePanel.Refs.ResumeBtn_Button, PausePanel.Refs.ResumeBtn_Image, PausePanel.Refs.ResumeBtn_Text,
                "play", "RESUME", new Rect(490, ButtonTop - 20f, 190, 190));
            Button(kit, panel, PausePanel.Refs.SettingsBtn_Button, PausePanel.Refs.SettingsBtn_Image, PausePanel.Refs.SettingsBtn_Text,
                "settings", "SETTINGS", new Rect(780, ButtonTop, 150, 150));
            Button(kit, panel, PausePanel.Refs.MainMenuBtn_Button, PausePanel.Refs.MainMenuBtn_Image, PausePanel.Refs.MainMenuBtn_Text,
                "home", "MAIN MENU", new Rect(1030, ButtonTop, 150, 150));
            Button(kit, panel, PausePanel.Refs.QuitBtn_Button, PausePanel.Refs.QuitBtn_Image, PausePanel.Refs.QuitBtn_Text,
                "exit", "QUIT", new Rect(1280, ButtonTop, 150, 150));

            return root.Go;
        }

        // 그림 버튼과 그 아래에 매단 이름(버튼 아래 변 가운데에 앵커, 글자 위 변 가운데가 기준점).
        private static void Button(
            BlackHoleScreenKit kit,
            UINode parent,
            PausePanel.Refs button,
            PausePanel.Refs image,
            PausePanel.Refs label,
            string icon,
            string text,
            Rect design)
        {
            kit.IconButton(parent, button.ToString(), image.ToString(), icon, design, out UINode node);

            var labelRect = new Rect(design.center.x - 130f, LabelTop, 260f, 64f);
            UINode labelNode = UINodes.Node(node, label.ToString(), labelRect, UIAnchor.BottomCenter, UIAnchor.TopCenter);
            kit.Text(labelNode, text, 36f, 6f, autoSizeMin: 22f);
        }
    }
}
