using Ked.Samples.UI;
using UnityEngine;

namespace VisualNovelDemo
{
    // 01 타이틀 · 02 대화 · 03 선택지 · 04 시스템 메뉴 · 09 확인 · 10 안내.
    public sealed partial class VNDemoFlow
    {
        #region 타이틀 · Root 전환

        private void SubscribeTitle()
        {
            _title.StartClicked += StartNew;
            _title.ContinueClicked += Resume;
            _title.LoadClicked += () => OpenSlots(SlotMode.Load);
            _title.SettingsClicked += OpenSettings;
            _title.QuitClicked += Quit;
        }

        /// <summary>타이틀로. 열린 Panel을 모두 닫고 Root를 타이틀로 바꾼다.</summary>
        public void GoToTitle()
        {
            CloseAllPanels();

            _ui.SwitchRoot<TitleScreen>(
                Presentation<TitleScreen>(),
                root => root.Present(new TitleScreen.Model
                {
                    Example = _example,
                    CanContinue = _resume != null,
                    ShowLoad = true,
                    CanSwitchExample = false,
                }));

            SyncPlayer();
        }

        private void StartNew()
        {
            _playSeconds = 0f;
            GoToPlay();
            _player.Begin();
        }

        private void Resume()
        {
            if (_resume == null)
                return;

            GoToPlay();
            _player.Restore(_resume);
        }

        // 재생 화면으로. 이미 재생 화면이면 Presentation만 다시 적용된다(불러오기).
        private void GoToPlay()
        {
            bool entering = !IsPlayRoot;

            _ui.SwitchRoot<PlayScreen>(
                Presentation<PlayScreen>(),
                root =>
                {
                    if (entering)
                        root.SetHudHidden(false);

                    root.SetChapterLabel(ChapterLabel());
                });

            SyncPlayer();
        }

        private void Quit()
        {
            if (Application.isEditor)
            {
                Notice(Text("notice.quit.title"), Text("notice.quit.body"), NoticePanel.Kind.Success);
                return;
            }

            Application.Quit();
        }

        #endregion

        #region 대화 · 퀵메뉴

        private void SubscribePlay()
        {
            _play.AdvanceClicked += () => _player.Advance();
            _play.AutoClicked += () => _player.ToggleAuto();
            _play.FastForwardClicked += () => _player.ToggleFastForward();
            _play.RollbackClicked += () => _player.Rollback();
            _play.BacklogClicked += OpenBacklog;
            _play.HideClicked += HideHud;
            _play.MenuClicked += OpenMenu;
            _play.RestoreClicked += RestoreHud;
        }

        private void HideHud()
        {
            _play.SetHudHidden(true);
            SyncPlayer();
        }

        private void RestoreHud()
        {
            _play.SetHudHidden(false);
            SyncPlayer();
        }

        // 대사창 위 꼬리표: "데모 · 지금 장면".
        private string ChapterLabel()
        {
            string example = Text(_example?.NameKey);
            string scene = _player.SceneOfCurrent;

            if (string.IsNullOrEmpty(scene))
                return example;

            return Format("play.chapterTag", example, Text("scene." + scene));
        }

        #endregion

        #region 시스템 메뉴

        private void SubscribeMenu()
        {
            _menu.CloseClicked += ClosePanel;
            _menu.SaveClicked += () => OpenSlots(SlotMode.Save);
            _menu.LoadClicked += () => OpenSlots(SlotMode.Load);
            _menu.SettingsClicked += OpenSettings;
            _menu.TitleClicked += ConfirmReturnToTitle;
        }

        private void OpenMenu()
        {
            PushPanel<SystemMenuPanel>(panel => panel.Present(new SystemMenuPanel.Model
            {
                HasSaveSlots = true,
                CanSave = _player.IsRunning,
                CanSkipEpisode = false,
            }));
        }

        private void ConfirmReturnToTitle()
        {
            Confirm(new ConfirmPanel.Request
            {
                Title = Text("confirm.title.title"),
                Body = Text("confirm.title.body"),
                AcceptLabel = Text("confirm.title.accept"),
                CancelLabel = Text("confirm.title.cancel"),
                OnAccept = ReturnToTitle,
            });
        }

        // 지금 자리를 이어하기로 남기고 나간다.
        private void ReturnToTitle()
        {
            if (_player.IsRunning)
                _resume = _player.Capture();

            _player.Stop();
            GoToTitle();
        }

        #endregion

        #region 확인 · 안내 — 모달

        // 요청 하나에 콜백은 한 번(화면이 보장). 먼저 팝업을 닫고 그다음 콜백을 부른다 — 콜백이 새 Panel을 열 수 있게.
        private void SubscribeModals()
        {
            _confirm.Resolved += (request, accepted) =>
            {
                if (_ui.TopPanel == _confirm)
                    ClosePanel();

                if (accepted)
                    request.OnAccept?.Invoke();
                else
                    request.OnCancel?.Invoke();
            };

            _notice.Resolved += request =>
            {
                if (_ui.TopPanel == _notice)
                    ClosePanel();

                request.OnClosed?.Invoke();
            };
        }

        private void Confirm(ConfirmPanel.Request request)
        {
            PushPanel<ConfirmPanel>(panel => panel.Present(request));
        }

        private void Notice(string title, string body, NoticePanel.Kind kind, System.Action onClosed = null)
        {
            var request = new NoticePanel.Request
            {
                Title = title,
                Body = body,
                AcceptLabel = Text("common.ok"),
                Kind = kind,
                OnClosed = onClosed,
            };

            PushPanel<NoticePanel>(panel => panel.Present(request));
        }

        #endregion
    }
}
