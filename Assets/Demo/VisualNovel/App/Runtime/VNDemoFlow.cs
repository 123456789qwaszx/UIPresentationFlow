using System;
using Ked.Samples.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VisualNovelDemo
{
    /// <summary>
    /// 비주얼 노벨 데모의 화면 흐름 — 어떤 화면을 언제 열고 닫는지, 화면의 사건을 가짜 재생 · 메모리 저장에 잇는다.
    /// 화면(ked의 VN UI 묶음)은 그리기와 사건 전달만 한다. ked에서는 같은 자리를 KedUIFlow가 맡고 재생 본체 · 호스트에 잇는다.
    ///
    /// UIPF 층:
    ///   Root    TitleScreen · PlayScreen                  SwitchRoot
    ///   Panel   메뉴 · 저장/불러오기 · 백로그 · 설정        PushPanel / PopPanel / PopAllPanels
    ///   Page    설정의 탭 넷                               SwitchPage(SettingsPanel, …)
    ///   모달    확인 · 안내                                Panel(맨 위만 입력을 받는다)
    ///
    /// Panel이 열린 동안 Root를 막고(키보드 · 패드도), 닫으면 연 순간의 포커스로 돌아가는 것은 UIPF가 한다.
    /// 화면 사건은 만들 때 한 번만 구독한다(화면과 흐름의 수명이 같다).
    /// </summary>
    public sealed partial class VNDemoFlow
    {
        private readonly UIManager _ui;
        private readonly Localizer _texts;
        private readonly Func<Type, UIPresentationSpec> _presentationOf;
        private readonly KedExampleCard _example;

        private readonly TitleScreen _title;
        private readonly PlayScreen _play;
        private readonly SystemMenuPanel _menu;
        private readonly SaveLoadPanel _slots;
        private readonly BacklogPanel _backlog;
        private readonly SettingsPanel _settings;
        private readonly AudioSettingsPage _audioPage;
        private readonly TextSettingsPage _textPage;
        private readonly PlaybackSettingsPage _playbackPage;
        private readonly DisplaySettingsPage _displayPage;
        private readonly ConfirmPanel _confirm;
        private readonly NoticePanel _notice;

        private readonly VNDemoPlayer _player;
        private readonly VNDemoSaves _saves = new();

        // 타이틀로 나가며 남긴 자리 — 타이틀의 이어하기가 쓴다.
        private VNDemoPlayer.Snapshot _resume;
        private float _playSeconds;

        // 지난 프레임에 재생 화면에서 Panel 없이 대사를 보고 있었나. 이번 프레임에 화면이 바뀌었으면(타이틀의 버튼을
        // Space로 눌러 시작, Enter로 확인 창을 닫음) 그 키를 대사 진행으로 다시 읽지 않는다.
        private bool _watchingLastTick;

        public VNDemoFlow(
            UIManager ui,
            Localizer texts,
            Func<Type, UIPresentationSpec> presentationOf,
            VNDemoOptionItem optionItemPrefab,
            KedExampleCard example)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _texts = texts;
            _presentationOf = presentationOf ?? throw new ArgumentNullException(nameof(presentationOf));
            _example = example;

            _title = Require<TitleScreen>();
            _play = Require<PlayScreen>();
            _menu = Require<SystemMenuPanel>();
            _slots = Require<SaveLoadPanel>();
            _backlog = Require<BacklogPanel>();
            _settings = Require<SettingsPanel>();
            _audioPage = Require<AudioSettingsPage>();
            _textPage = Require<TextSettingsPage>();
            _playbackPage = Require<PlaybackSettingsPage>();
            _displayPage = Require<DisplaySettingsPage>();
            _confirm = Require<ConfirmPanel>();
            _notice = Require<NoticePanel>();

            _player = new VNDemoPlayer(VNDemoScript.Create(), _play, Text, optionItemPrefab);
            _player.ModesChanged += () => _play.SetModes(_player.Auto, _player.FastForward);
            _player.LineShown += () => _play.SetChapterLabel(ChapterLabel());

            SubscribeTitle();
            SubscribePlay();
            SubscribeMenu();
            SubscribeSlots();
            SubscribeBacklog();
            SubscribeSettings();
            SubscribeModals();

            InitializeSettings();
        }

        #region 매 프레임 — VNDemoHost가 부른다

        public void Tick(float deltaTime)
        {
            Keyboard keyboard = Keyboard.current;
            bool watching = IsPlayRoot && !_ui.HasPanel;

            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                    HandleCancel();
                else if (watching && _watchingLastTick)
                    HandlePlayKeys(keyboard);
            }

            _watchingLastTick = IsPlayRoot && !_ui.HasPanel;
            SyncPlayer();

            _player.Holding = _watchingLastTick && !_play.IsHudHidden && keyboard != null && keyboard.ctrlKey.isPressed;

            if (IsPlayRoot && _player.IsRunning && !_player.Paused)
                _playSeconds += deltaTime;

            _player.Tick(deltaTime);
        }

        private static bool AnyAdvanceKey(Keyboard keyboard) =>
            keyboard.spaceKey.wasPressedThisFrame ||
            keyboard.enterKey.wasPressedThisFrame ||
            keyboard.numpadEnterKey.wasPressedThisFrame;

        // 설정의 "키보드 조작"과 같은 키: Space · Enter 다음 줄, Ctrl 누르는 동안 빨리 넘기기(Tick), A 자동, S 빠르게 재생, R 되감기.
        // UI를 숨겼으면 Space · Enter는 다시 보이기만 하고 나머지 키는 듣지 않는다.
        private void HandlePlayKeys(Keyboard keyboard)
        {
            if (AnyAdvanceKey(keyboard))
                HandleAdvanceKey();
            else if (_play.IsHudHidden)
                return;
            else if (keyboard.aKey.wasPressedThisFrame)
                _player.ToggleAuto();
            else if (keyboard.sKey.wasPressedThisFrame)
                _player.ToggleFastForward();
            else if (keyboard.rKey.wasPressedThisFrame)
                _player.Rollback();
        }

        // Space · Enter: UI를 숨겼으면 다시 보이고, 아니면 다음 줄. 선택지가 떠 있으면 EventSystem이 고른 항목을 누른다.
        private void HandleAdvanceKey()
        {
            if (_play.IsHudHidden)
            {
                RestoreHud();
                return;
            }

            if (!_player.IsChoosing)
                _player.Advance();
        }

        #endregion

        #region Esc — 맨 위 한 층만

        // Esc 한 번에 한 층만 닫는다: 확인 → 취소, 안내 → 확인, 설정 → 닫기, 그 밖의 Panel → 뒤로. Panel이 없고 UI를 숨겼으면 다시 보인다.
        private void HandleCancel()
        {
            UIBase top = _ui.TopPanel;

            if (top == null)
            {
                if (_play.IsHudHidden && IsPlayRoot)
                    RestoreHud();

                return;
            }

            if (top == _confirm)
                _confirm.Cancel();
            else if (top == _notice)
                _notice.Accept();
            else if (top == _settings)
                CloseSettings();
            else
                ClosePanel();
        }

        #endregion

        #region 공통

        private bool IsPlayRoot => _ui.CurrentRoot == _play;

        private T Require<T>() where T : UIBase
        {
            T view = _ui.GetUI<T>();
            if (view == null)
                throw new InvalidOperationException(
                    $"[VN 데모] 화면 '{typeof(T).Name}'이 등록되지 않았다. 메뉴 UI Demo > Build Visual Novel Demo를 다시 실행한다.");

            return view;
        }

        private UIPresentationSpec Presentation<T>() => _presentationOf(typeof(T));

        private T PushPanel<T>(Action<T> afterPresented) where T : UIBase, IUIPanel
        {
            T panel = _ui.PushPanel(Presentation<T>(), afterPresented);
            SyncPlayer();
            return panel;
        }

        // 포커스는 UIPF가 되돌린다 — 닫으면 그 Panel을 연 순간의 포커스로, 전부 닫으면 맨 처음 Panel 전의 포커스로.
        private void ClosePanel()
        {
            _ui.PopPanel();
            SyncPlayer();
            EnsureFocus();
        }

        private void CloseAllPanels()
        {
            _ui.PopAllPanels();
            SyncPlayer();
            EnsureFocus();
        }

        // UIPF가 되돌리지 못했을 때만 대신 고른다: 남은 Panel의 첫 버튼, 재생 화면이면 떠 있는 선택지.
        private void EnsureFocus()
        {
            if (KedUIUtil.TryFocus(KedUIUtil.CurrentFocus()))
                return;

            UIBase top = _ui.TopPanel;

            if (top != null)
                KedUIUtil.FocusFirstIn(top);
            else if (IsPlayRoot)
                _play.RestoreFocus();
            else
                KedUIUtil.ClearFocus();
        }

        // 재생은 재생 화면에서 Panel 없이 대사를 볼 때만 나아간다 — 메뉴가 열린 동안 자동 · 빨리 넘기기도 멈춘다.
        private void SyncPlayer()
        {
            _player.Paused = !IsPlayRoot || _ui.HasPanel || _play.IsHudHidden;
        }

        private string Text(string key) => KedUIUtil.Text(_texts, key);
        private string Format(string key, params object[] args) => KedUIUtil.Format(_texts, key, args);

        #endregion
    }
}
