using System;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 13 설정 · 화면·조작 탭. 화면 모드(창/전체 화면)와 키보드 조작 안내.
    /// 키 목록은 재생 입력(VNAdvanceInputBindings)의 기본 키다 — 인스펙터에서 키를 바꾸면 이 안내도 같이 고쳐야 한다.
    /// 에디터의 게임 창은 화면 모드를 바꾸지 않는다(빌드에서만). 그 사실을 화면에 적는다.
    /// </summary>
    [UIViewKey("Ked.DisplaySettingsPage")]
    public sealed class DisplaySettingsPage : UIPage<DisplaySettingsPage.Refs>
    {
        public enum Refs
        {
            DisplayHeading,
            DisplayDescription,
            WindowModeLabel,
            Windowed,
            WindowedLabel,
            Fullscreen,
            FullscreenLabel,
            WindowModeNote,
            ShortcutHeading,
            ShortcutList,
            KeycapText_0,
            Shortcut_0,
            KeycapText_1,
            Shortcut_1,
            KeycapText_2,
            Shortcut_2,
            KeycapText_3,
            Shortcut_3,
            KeycapText_4,
            Shortcut_4,
            KeycapText_5,
            Shortcut_5,
        }

        public sealed class Model
        {
            public bool Fullscreen;
            public string Note;
        }

        public event Action<bool> WindowModeClicked;

        private KedButton _windowed;
        private KedButton _fullscreen;

        protected override void OnInitialize()
        {
            Button windowed = View.Button(Refs.Windowed);
            Button fullscreen = View.Button(Refs.Fullscreen);

            KedUIUtil.OnClick(windowed, () => WindowModeClicked?.Invoke(false));
            KedUIUtil.OnClick(fullscreen, () => WindowModeClicked?.Invoke(true));

            if (windowed != null)
                windowed.TryGetComponent(out _windowed);

            if (fullscreen != null)
                fullscreen.TryGetComponent(out _fullscreen);
        }

        public void Present(Model model)
        {
            _windowed?.SetSelectedLook(!model.Fullscreen);
            _fullscreen?.SetSelectedLook(model.Fullscreen);

            KedUIUtil.SetText(View.Text(Refs.WindowModeNote), model.Note);
            KedUIUtil.SetActive(View.Text(Refs.WindowModeNote), !string.IsNullOrEmpty(model.Note));
        }

        public Selectable FirstSelectable => _windowed != null ? _windowed.Button : null;
    }
}
