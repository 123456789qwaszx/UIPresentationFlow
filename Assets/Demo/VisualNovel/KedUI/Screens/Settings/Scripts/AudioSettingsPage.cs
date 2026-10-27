using System;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>07 설정 · 음량 탭. UIPF Page(SettingsPanel의 PageRoot 아래). 값은 0–100으로 보이고 0–1로 넘긴다.</summary>
    public sealed class AudioSettingsPage : UIPage<AudioSettingsPage.Refs>
    {
        public enum Channel
        {
            Master = 0,
            Bgm = 1,
            Sfx = 2,
            Voice = 3,
        }

        public enum Refs
        {
            AudioHeading,
            AudioDescription,
            AudioLabel_0,
            AudioSlider_0,
            AudioValue_0,
            AudioLabel_1,
            AudioSlider_1,
            AudioValue_1,
            AudioLabel_2,
            AudioSlider_2,
            AudioValue_2,
            AudioLabel_3,
            AudioSlider_3,
            AudioValue_3,
            MuteLabel,
            MuteToggle,
        }

        public sealed class Model
        {
            public float Master;
            public float Bgm;
            public float Sfx;
            public float Voice;
            public bool Muted;
        }

        public event Action<Channel, float> VolumeChanged;
        public event Action<bool> MuteChanged;

        private readonly KedSliderField[] _fields = new KedSliderField[4];
        private KedToggleSwitch _mute;

        protected override void OnInitialize()
        {
            Refs[] sliders = { Refs.AudioSlider_0, Refs.AudioSlider_1, Refs.AudioSlider_2, Refs.AudioSlider_3 };
            Refs[] values = { Refs.AudioValue_0, Refs.AudioValue_1, Refs.AudioValue_2, Refs.AudioValue_3 };

            for (int i = 0; i < sliders.Length; i++)
            {
                Channel channel = (Channel)i;

                _fields[i] = new KedSliderField(
                    View.Component<Slider>(sliders[i]),
                    View.Text(values[i]),
                    0f, 100f, wholeNumbers: true,
                    format: v => v.ToString("0"),
                    changed: v => VolumeChanged?.Invoke(channel, v / 100f));
            }

            _mute = View.Component<KedToggleSwitch>(Refs.MuteToggle);
            Toggle toggle = View.Component<Toggle>(Refs.MuteToggle);
            if (toggle != null)
                toggle.onValueChanged.AddListener(on => MuteChanged?.Invoke(on));
        }

        public void Present(Model model)
        {
            _fields[(int)Channel.Master]?.Set(model.Master * 100f);
            _fields[(int)Channel.Bgm]?.Set(model.Bgm * 100f);
            _fields[(int)Channel.Sfx]?.Set(model.Sfx * 100f);
            _fields[(int)Channel.Voice]?.Set(model.Voice * 100f);
            _mute?.SetWithoutNotify(model.Muted);
        }

        public Selectable FirstSelectable => _fields[0]?.Slider;
    }
}
