using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using R = Ked.Samples.UI.SettingsPanel.Refs;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// 07 설정 + 11 · 12 · 13 — Design/07_settings.png 외 셋. 바깥(Panel)과 탭 안(Page 넷)을 따로 만든다.
    /// Page 프리팹은 Panel 프리팹의 PageRoot 아래에 중첩 프리팹으로 들어간다(UIPF: Page는 Owner의 PageRoot 자식이어야 한다).
    /// </summary>
    public static class SettingsSpec
    {
        // 탭 안 영역(PageRoot)의 시안 사각형. Page의 좌표는 모두 이 안의 절대 좌표로 적는다.
        public static readonly Rect PageArea = new(430, 150, 1060, 600);

        private static readonly string[] TabLabels = { "음량", "대사", "재생", "화면 · 조작" };

        public static GameObject Build(KedUIKit kit, IReadOnlyList<GameObject> pagePrefabs)
        {
            UINode view = kit.NewView<SettingsPanel>(nameof(SettingsPanel));
            kit.Backdrop(view, nameof(R.Background), "background_title");

            UINode safe = kit.Fill(view, nameof(R.SafeAreaRoot));
            UINode frame = kit.Frame(safe, nameof(R.Frame));

            UINode surface = kit.Node(frame, nameof(R.SettingsSurface), 80, 60, 1440, 790);
            kit.Image(surface, "panel_settings", raycast: true);

            kit.Label(frame, nameof(R.SettingsTitle), "설정", 115, 78, 800, 45, 30, KedColors.White, bold: true);
            kit.IconButton(frame, nameof(R.SettingsClose), "close", new Rect(1430, 72, 44, 44), padding: 11f);

            UINode tabs = kit.Node(frame, nameof(R.SettingsTabs), 114, 181, 256, 305);
            for (int i = 0; i < TabLabels.Length; i++)
            {
                kit.Tab(tabs, "SettingsTab_" + i, "SettingsTabText_" + i, TabLabels[i],
                    new Rect(114, 181 + i * 83, 256, 56),
                    new Rect(130, 190 + i * 83, 210, 38));
            }

            UINode pageRoot = kit.Node(frame, nameof(R.PageRoot), PageArea, UIAnchor.TopLeft);

            foreach (GameObject prefab in pagePrefabs)
            {
                if (prefab == null)
                    continue;

                var page = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                page.transform.SetParent(pageRoot.Rect, false);
                page.SetActive(false);
            }

            kit.Label(frame, nameof(R.ApplyNote), "변경한 설정은 즉시 적용되고 저장됩니다.", 445, 770, 680, 32, 17, KedColors.Muted);
            kit.TextButton(frame, nameof(R.ResetSettings), nameof(R.ResetSettingsLabel), "기본값 복원",
                new Rect(1160, 769, 260, 55), KedButtonStyle.Secondary, fontSize: 21f);

            return view.Go;
        }

        #region Pages

        public static GameObject AudioPage(KedUIKit kit)
        {
            UINode page = kit.NewView<AudioSettingsPage>(nameof(AudioSettingsPage), PageArea);
            Heading(kit, page, "AudioHeading", "음량", "AudioDescription", "이야기에 집중할 수 있도록 소리를 조절하세요.");

            string[] labels = { "전체 음량", "배경음", "효과음", "음성" };
            for (int i = 0; i < labels.Length; i++)
            {
                float y = 295 + i * 93;
                kit.Label(page, "AudioLabel_" + i, labels[i], 448, y, 230, 42, 23, KedColors.Navy, bold: true);
                kit.Slider(page, "AudioSlider_" + i, "Audio" + i, new Rect(716, y + 15, 550, 16));
                kit.Label(page, "AudioValue_" + i, "0", 1322, y, 90, 42, 23, KedColors.Navy, TextAlignmentOptions.Right, bold: true);
            }

            kit.Label(page, "MuteLabel", "음소거", 449, 686, 300, 42, 23, KedColors.Navy, bold: true);
            kit.Switch(page, "MuteToggle", "Mute", new Rect(1320, 685, 90, 40));

            return page.Go;
        }

        public static GameObject TextPage(KedUIKit kit)
        {
            UINode page = kit.NewView<TextSettingsPage>(nameof(TextSettingsPage), PageArea);
            Heading(kit, page, "TextHeading", "대사 표시", "TextDescription", "편안하게 읽을 수 있는 크기와 속도를 선택하세요.");

            string[] labels = { "글자 크기", "글자 출력 속도", "대사창 불투명도" };
            for (int i = 0; i < labels.Length; i++)
            {
                float y = 295 + i * 82;
                kit.Label(page, "TextLabel_" + i, labels[i], 448, y, 280, 42, 23, KedColors.Navy, bold: true);
                kit.Slider(page, "TextSlider_" + i, "Text" + i, new Rect(760, y + 15, 500, 16));
                kit.Label(page, "TextValue_" + i, "0", 1300, y, 120, 42, 23, KedColors.Navy, TextAlignmentOptions.Right, bold: true);
            }

            kit.Label(page, "TextPreviewCaption", "미리보기", 448, 568, 500, 32, 18, KedColors.Muted);

            UINode surface = kit.Node(page, "TextPreviewSurface", 447, 613, 970, 105);
            kit.Surface(surface, KedColors.Ivory, radius: 14f);
            UINode line = kit.Fill(surface, "TextPreviewLine");
            kit.Outline(line, KedColors.SurfaceLine, 1f);

            UINode text = kit.Node(surface, "TextPreviewText", 478, 630, 908, 75);
            kit.Text(text, "이야기가 화면이 되는 순간. 함께 시작해 볼까요?", 25, KedColors.Navy, TextAlignmentOptions.Left, wrap: true);

            return page.Go;
        }

        public static GameObject PlaybackPage(KedUIKit kit)
        {
            UINode page = kit.NewView<PlaybackSettingsPage>(nameof(PlaybackSettingsPage), PageArea);
            Heading(kit, page, "PlaybackHeading", "재생", "PlaybackDescription", "자동 진행과 빠르게 재생할 때의 동작을 설정합니다.");

            kit.Label(page, "AutoDelayLabel", "자동 진행 대기", 448, 298, 300, 42, 23, KedColors.Navy, bold: true);
            kit.Slider(page, "AutoDelaySlider", "AutoDelay", new Rect(760, 314, 500, 16));
            kit.Label(page, "AutoDelayValue", "0.0초", 1300, 299, 125, 42, 23, KedColors.Navy, TextAlignmentOptions.Right, bold: true);

            kit.Label(page, "SpeedLabel", "빠르게 재생", 448, 405, 300, 42, 23, KedColors.Navy, bold: true);
            for (int i = 0; i < PlaybackSettingsPage.SpeedChoiceCount; i++)
            {
                kit.TextButton(page, "SpeedChoice_" + i, "SpeedChoiceLabel_" + i, "2×",
                    new Rect(758 + i * 160, 398, 145, 58), KedButtonStyle.Secondary, fontSize: 21f, choice: true);
            }

            kit.Label(page, "PlaybackHelp",
                "빠르게 재생은 S 키로 켜고 끕니다. 배속은 다음에 켤 때부터 적용됩니다.\n" +
                "읽은 대사만 넘기기는 재생 기록에 읽음 표시가 없어 제공하지 않습니다.",
                448, 560, 960, 110, 20, KedColors.Muted, wrap: true);

            return page.Go;
        }

        public static GameObject DisplayPage(KedUIKit kit)
        {
            UINode page = kit.NewView<DisplaySettingsPage>(nameof(DisplaySettingsPage), PageArea);
            Heading(kit, page, "DisplayHeading", "화면 · 조작", "DisplayDescription", "실행 환경에 맞는 화면 모드와 조작 방법을 확인합니다.");

            kit.Label(page, "WindowModeLabel", "화면 모드", 448, 300, 280, 42, 23, KedColors.Navy, bold: true);
            kit.TextButton(page, "Windowed", "WindowedLabel", "창 모드", new Rect(760, 292, 250, 60),
                KedButtonStyle.Secondary, fontSize: 21f, choice: true);
            kit.TextButton(page, "Fullscreen", "FullscreenLabel", "전체 화면", new Rect(1030, 292, 250, 60),
                KedButtonStyle.Secondary, fontSize: 21f, choice: true);
            kit.Label(page, "WindowModeNote", "", 760, 358, 640, 28, 15, KedColors.Muted);

            kit.Label(page, "ShortcutHeading", "키보드 조작", 448, 405, 900, 42, 25, KedColors.Navy, bold: true);

            (string key, string action)[] shortcuts =
            {
                ("Space", "다음 대사"),
                ("Ctrl", "누르는 동안 빨리 넘기기"),
                ("A", "자동 진행 켜고 끄기"),
                ("S", "빠르게 재생 켜고 끄기"),
                ("R", "한 줄 되감기"),
                ("Esc", "현재 창 닫기 · 숨긴 UI 다시 보기"),
            };

            UINode list = kit.Node(page, "ShortcutList", 448, 458, 960, 284);
            for (int i = 0; i < shortcuts.Length; i++)
            {
                float y = 460 + i * 47;

                UINode cap = kit.Node(list, "Keycap_" + i, 451, y, 110, 36);
                kit.Surface(cap, KedColors.Ivory, radius: 10f);
                UINode capLine = kit.Fill(cap, "KeycapLine_" + i);
                kit.Outline(capLine, KedColors.SurfaceLine, 1f);
                UINode capText = kit.Fill(cap, "KeycapText_" + i);
                kit.Text(capText, shortcuts[i].key, 17, KedColors.Navy, TextAlignmentOptions.Center, bold: true);

                kit.Label(list, "Shortcut_" + i, shortcuts[i].action, 590, y - 1, 800, 38, 21, KedColors.Navy);
            }

            return page.Go;
        }

        private static void Heading(KedUIKit kit, UINode page, string headingName, string heading, string descriptionName, string description)
        {
            kit.Label(page, headingName, heading, 445, 168, 900, 44, 30, KedColors.Navy, bold: true);
            kit.Label(page, descriptionName, description, 446, 220, 960, 35, 18, KedColors.Muted);
        }

        #endregion
    }
}
