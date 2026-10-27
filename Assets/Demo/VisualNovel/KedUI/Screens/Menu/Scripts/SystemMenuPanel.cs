using System;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 04 시스템 메뉴. UIPF Panel — 재생 화면 위에 쌓인다. 여닫는 것은 UI 흐름이 하고, 재생은 건드리지 않는다
    /// (메뉴가 열려 있는 동안 입력 소유권만 UI가 갖는다 — 재생 단위를 멈추거나 기억을 지우지 않는다).
    ///
    /// 계속하기 · 저장 · 불러오기 · 설정 · 이 에피소드 건너뛰기 · 타이틀로.
    /// 저장·불러오기는 저장 슬롯이 있는 예제에만 보인다. 저장은 지금 저장할 수 없으면 막는다.
    /// "이 에피소드 건너뛰기"는 시안에 없지만 옛 화면의 기능(에피소드 스킵)을 잃지 않으려고 둔다.
    /// </summary>
    public sealed class SystemMenuPanel : UIPanel<SystemMenuPanel.Refs>
    {
        public enum Refs
        {
            SafeAreaRoot,
            Dim,
            MenuPanel,
            MenuTitle,
            CloseMenu,
            CloseMenuBase,
            CloseMenuIcon,

            MenuActions,
            MenuContinue,
            MenuContinueLabel,
            MenuSave,
            MenuSaveLabel,
            MenuLoad,
            MenuLoadLabel,
            MenuSettings,
            MenuSettingsLabel,
            MenuSkipEpisode,
            MenuSkipEpisodeLabel,
            MenuTitleReturn,
            MenuTitleReturnLabel,
        }

        public sealed class Model
        {
            public bool HasSaveSlots;
            public bool CanSave;
            public bool CanSkipEpisode;
        }

        public event Action CloseClicked;
        public event Action SaveClicked;
        public event Action LoadClicked;
        public event Action SettingsClicked;
        public event Action SkipEpisodeClicked;
        public event Action TitleClicked;

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.CloseMenu), () => CloseClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.MenuContinue), () => CloseClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.MenuSave), () => SaveClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.MenuLoad), () => LoadClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.MenuSettings), () => SettingsClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.MenuSkipEpisode), () => SkipEpisodeClicked?.Invoke());
            KedUIUtil.OnClick(View.Button(Refs.MenuTitleReturn), () => TitleClicked?.Invoke());
        }

        public void Present(Model model)
        {
            KedUIUtil.SetButton(View.Button(Refs.MenuSave), model.HasSaveSlots, model.CanSave);
            KedUIUtil.SetButton(View.Button(Refs.MenuLoad), model.HasSaveSlots, true);
            KedUIUtil.SetButton(View.Button(Refs.MenuSkipEpisode), true, model.CanSkipEpisode);

            KedUIUtil.Focus(View.Button(Refs.MenuContinue));
        }
    }
}
