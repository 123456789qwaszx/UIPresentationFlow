using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 12 설정 · 재생 탭. 자동 진행 대기(초)와 "빠르게 재생"(S 키·속도 올림 모드)의 배속.
    /// 배속 보기는 본체가 허용하는 값만 낸다 — 시안의 1.5×는 본체 하한(2배) 아래라 넣지 않는다.
    /// 시안의 "읽은 대사만 넘기기"는 본체에 읽음 기록이 없어 만들지 않는다(안내 글만).
    /// </summary>
    [UIViewKey("Ked.PlaybackSettingsPage")]
    public sealed class PlaybackSettingsPage : UIPage<PlaybackSettingsPage.Refs>
    {
        public const int SpeedChoiceCount = 3;

        public enum Refs
        {
            PlaybackHeading,
            PlaybackDescription,
            AutoDelayLabel,
            AutoDelaySlider,
            AutoDelayValue,
            SpeedLabel,
            SpeedChoice_0,
            SpeedChoiceLabel_0,
            SpeedChoice_1,
            SpeedChoiceLabel_1,
            SpeedChoice_2,
            SpeedChoiceLabel_2,
            PlaybackHelp,
        }

        public sealed class Model
        {
            public float AutoDelay;
            public IReadOnlyList<float> SpeedChoices;
            public float SpeedUpMultiplier;
            public Func<float, string> FormatSeconds;
        }

        public event Action<float> AutoDelayChanged;
        public event Action<float> SpeedChoiceClicked;

        private readonly KedButton[] _choices = new KedButton[SpeedChoiceCount];
        private readonly float[] _choiceValues = new float[SpeedChoiceCount];
        private KedSliderField _autoDelay;
        private Func<float, string> _formatSeconds = s => s.ToString("0.0");

        protected override void OnInitialize()
        {
            // 0.1초 단위 — 슬라이더는 정수(5–50)로 두고 10으로 나눈다.
            _autoDelay = new KedSliderField(
                View.Component<Slider>(Refs.AutoDelaySlider),
                View.Text(Refs.AutoDelayValue),
                KedSettingRanges.MinAutoDelay * 10f, KedSettingRanges.MaxAutoDelay * 10f, wholeNumbers: true,
                format: v => _formatSeconds(v / 10f),
                changed: v => AutoDelayChanged?.Invoke(v / 10f));

            Refs[] choices = { Refs.SpeedChoice_0, Refs.SpeedChoice_1, Refs.SpeedChoice_2 };

            for (int i = 0; i < choices.Length; i++)
            {
                int index = i;
                Button button = View.Button(choices[i]);
                KedUIUtil.OnClick(button, () => SpeedChoiceClicked?.Invoke(_choiceValues[index]));

                if (button != null)
                    button.TryGetComponent(out _choices[i]);
            }
        }

        public void Present(Model model)
        {
            if (model.FormatSeconds != null)
                _formatSeconds = model.FormatSeconds;

            _autoDelay.Set(model.AutoDelay * 10f);

            for (int i = 0; i < _choices.Length; i++)
            {
                bool has = model.SpeedChoices != null && i < model.SpeedChoices.Count;
                KedButton choice = _choices[i];
                if (choice == null)
                    continue;

                choice.SetVisible(has);
                if (!has)
                    continue;

                _choiceValues[i] = model.SpeedChoices[i];
                choice.SetLabel(FormatMultiplier(model.SpeedChoices[i]));
                choice.SetSelectedLook(UnityEngine.Mathf.Approximately(model.SpeedChoices[i], model.SpeedUpMultiplier));
            }
        }

        public Selectable FirstSelectable => _autoDelay?.Slider;

        private static string FormatMultiplier(float value) =>
            (UnityEngine.Mathf.Approximately(value, UnityEngine.Mathf.Round(value))
                ? value.ToString("0")
                : value.ToString("0.0")) + "×";
    }
}
