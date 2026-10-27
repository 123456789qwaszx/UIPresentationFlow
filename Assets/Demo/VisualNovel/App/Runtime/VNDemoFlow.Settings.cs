using System.Collections.Generic;
using Ked.Samples.UI;
using UnityEngine;

namespace VisualNovelDemo
{
    // 07 · 11 · 12 · 13 설정. 탭은 UIPF Page — SettingsPanel(Page Owner)의 PageRoot 아래 Page 넷을 SwitchPage로 바꿔 낀다.
    // 바꾸는 즉시 적용한다. 데모라 저장하지 않는다(플레이를 멈추면 기본값).
    //   음량   전체 · 음소거 → AudioListener(데모에는 소리가 버튼 말고 없다). 배경음 · 효과음 · 음성은 값만 기억한다
    //   대사   글자 크기 → UIContext.TextScale(보이는 화면 모두), 출력 속도 → 타자기, 대사창 불투명도 → 대사창 그림
    //   재생   자동 대기 · 빠르게 재생 배속 → 가짜 재생
    //   화면   창 모드 · 전체 화면 → Screen.fullScreenMode(에디터의 게임 창에서는 바뀌지 않는다)
    public sealed partial class VNDemoFlow
    {
        private const int AudioTab = 0;
        private const int TextTab = 1;
        private const int PlaybackTab = 2;
        private const int DisplayTab = 3;

        private sealed class Values
        {
            public float Master = 1f;
            public float Bgm = 0.8f;
            public float Sfx = 0.8f;
            public float Voice = 0.8f;
            public bool Muted;

            public float TextScale = 1f;
            public float TypeSpeed = 30f;
            public float BoxOpacity = 1f;

            public float AutoDelay = 1.5f;
            public float SpeedUp = 4f;

            public Values Clone() => (Values)MemberwiseClone();
        }

        private static readonly float[] SpeedChoices = { 2f, 4f, 8f };

        private readonly Values _defaults = new();
        private Values _values;
        private int _settingsTab;

        private void SubscribeSettings()
        {
            _settings.CloseClicked += CloseSettings;
            _settings.TabClicked += SelectSettingsTab;
            _settings.ResetClicked += ResetSettingsTab;

            _audioPage.VolumeChanged += (channel, value) =>
            {
                switch (channel)
                {
                    case AudioSettingsPage.Channel.Master:
                        _values.Master = Mathf.Clamp01(value);
                        ApplyMasterVolume();
                        break;
                    case AudioSettingsPage.Channel.Bgm:
                        _values.Bgm = value;
                        break;
                    case AudioSettingsPage.Channel.Sfx:
                        _values.Sfx = value;
                        break;
                    case AudioSettingsPage.Channel.Voice:
                        _values.Voice = value;
                        break;
                }
            };

            _audioPage.MuteChanged += muted =>
            {
                _values.Muted = muted;
                ApplyMasterVolume();
            };

            _textPage.TextScaleChanged += scale =>
            {
                _values.TextScale = scale;
                ApplyTextScale();
            };

            _textPage.TypeSpeedChanged += speed =>
            {
                _values.TypeSpeed = speed;
                _player.TypeSpeed = speed;
            };

            _textPage.BoxOpacityChanged += opacity =>
            {
                _values.BoxOpacity = opacity;
                _play.SetDialogueOpacity(opacity);
            };

            _playbackPage.AutoDelayChanged += seconds =>
            {
                _values.AutoDelay = seconds;
                _player.AutoDelay = seconds;
            };

            _playbackPage.SpeedChoiceClicked += multiplier =>
            {
                _values.SpeedUp = multiplier;
                _player.SpeedUp = multiplier;
                PresentPlaybackPage(_playbackPage);
            };

            _displayPage.WindowModeClicked += fullscreen =>
            {
                Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                PresentDisplayPage(_displayPage, fullscreen);
            };
        }

        private void InitializeSettings()
        {
            _values = _defaults.Clone();
            ApplyAll();
        }

        private void ApplyAll()
        {
            ApplyMasterVolume();
            ApplyTextScale();
            _player.TypeSpeed = _values.TypeSpeed;
            _play.SetDialogueOpacity(_values.BoxOpacity);
            _player.AutoDelay = _values.AutoDelay;
            _player.SpeedUp = _values.SpeedUp;
        }

        #region 열고 닫기 · 탭

        private void OpenSettings()
        {
            PushPanel<SettingsPanel>(_ => SelectSettingsTab(_settingsTab));
        }

        private void CloseSettings()
        {
            ClosePanel();
        }

        private void SelectSettingsTab(int tab)
        {
            _settingsTab = Mathf.Clamp(tab, 0, SettingsPanel.TabCount - 1);
            _settings.SetSelectedTab(_settingsTab);
            _settings.SetResetAvailable(_settingsTab != DisplayTab);

            switch (_settingsTab)
            {
                case AudioTab:
                    _ui.SwitchPage<AudioSettingsPage>(_settings, Presentation<AudioSettingsPage>(), PresentAudioPage);
                    break;
                case TextTab:
                    _ui.SwitchPage<TextSettingsPage>(_settings, Presentation<TextSettingsPage>(), PresentTextPage);
                    break;
                case PlaybackTab:
                    _ui.SwitchPage<PlaybackSettingsPage>(_settings, Presentation<PlaybackSettingsPage>(), PresentPlaybackPage);
                    break;
                default:
                    _ui.SwitchPage<DisplaySettingsPage>(
                        _settings,
                        Presentation<DisplaySettingsPage>(),
                        page => PresentDisplayPage(page, Screen.fullScreen));
                    break;
            }

            _settings.FocusTab(_settingsTab);
        }

        private void ResetSettingsTab()
        {
            switch (_settingsTab)
            {
                case AudioTab:
                    _values.Master = _defaults.Master;
                    _values.Bgm = _defaults.Bgm;
                    _values.Sfx = _defaults.Sfx;
                    _values.Voice = _defaults.Voice;
                    _values.Muted = _defaults.Muted;
                    PresentAudioPage(_audioPage);
                    break;

                case TextTab:
                    _values.TextScale = _defaults.TextScale;
                    _values.TypeSpeed = _defaults.TypeSpeed;
                    _values.BoxOpacity = _defaults.BoxOpacity;
                    PresentTextPage(_textPage);
                    break;

                case PlaybackTab:
                    _values.AutoDelay = _defaults.AutoDelay;
                    _values.SpeedUp = _defaults.SpeedUp;
                    PresentPlaybackPage(_playbackPage);
                    break;
            }

            ApplyAll();
        }

        #endregion

        #region 탭 그리기

        private void PresentAudioPage(AudioSettingsPage page)
        {
            page.Present(new AudioSettingsPage.Model
            {
                Master = _values.Master,
                Bgm = _values.Bgm,
                Sfx = _values.Sfx,
                Voice = _values.Voice,
                Muted = _values.Muted,
            });
        }

        private void PresentTextPage(TextSettingsPage page)
        {
            page.Present(new TextSettingsPage.Model
            {
                TextScale = _values.TextScale,
                TypeSpeed = _values.TypeSpeed,
                BoxOpacity = _values.BoxOpacity,
            });
        }

        private void PresentPlaybackPage(PlaybackSettingsPage page)
        {
            page.Present(new PlaybackSettingsPage.Model
            {
                AutoDelay = _values.AutoDelay,
                SpeedChoices = SpeedChoices,
                SpeedUpMultiplier = _values.SpeedUp,
                FormatSeconds = seconds => Format("settings.playback.seconds", seconds.ToString("0.0")),
            });
        }

        private void PresentDisplayPage(DisplaySettingsPage page, bool fullscreen)
        {
            page.Present(new DisplaySettingsPage.Model
            {
                Fullscreen = fullscreen,
                Note = Application.isEditor ? Text("settings.display.editorNote") : string.Empty,
            });
        }

        #endregion

        #region 적용

        private void ApplyMasterVolume()
        {
            AudioListener.volume = _values.Muted ? 0f : Mathf.Clamp01(_values.Master);
        }

        // 글자 크기는 UIPF의 사용자 문맥(UIContext.TextScale)이다. 바꾸면 보이는 화면에 바로 다시 적용된다.
        private void ApplyTextScale()
        {
            UIContext context = _ui.Context;
            _ui.SetContext(context.With(context.ThemeId, context.LocaleId, _values.TextScale));
        }

        #endregion
    }
}
