using UnityEngine;

namespace BlackHole.Unity
{
    // 데모의 UI 소리: 버튼 올림·누름(ButtonFeedback.Sounds)과 화면 전환. 음량은 설정의 전체 음량 × 효과음 음량이고, 바꾸면 바로 따른다.
    // 게임의 SoundPlayer·UISounds에서 UI 효과음만 남긴 것이다(BGM·전투 소리는 없다).
    public sealed class DemoButtonSounds : MonoBehaviour, IButtonSounds
    {
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip _hover;
        [SerializeField] private AudioClip _click;
        [SerializeField] private AudioClip _switchScreen;

        private GameSettings _settings;

        private void Reset() => _source = GetComponent<AudioSource>();

        private void OnDestroy()
        {
            if (_settings != null)
                _settings.Changed -= HandleSettingChanged;
        }

        // 설정의 음량을 따른다. 부트스트랩이 시작할 때 한 번 부른다.
        public void Bind(GameSettings settings)
        {
            if (_settings != null)
                _settings.Changed -= HandleSettingChanged;

            _settings = settings;
            _settings.Changed += HandleSettingChanged;
            ApplyVolume();
        }

        public void PlayHover() => Play(_hover);
        public void PlayClick() => Play(_click);
        public void PlaySwitchScreen() => Play(_switchScreen);

        private void Play(AudioClip clip)
        {
            if (clip != null && _source != null)
                _source.PlayOneShot(clip);
        }

        private void HandleSettingChanged(string id)
        {
            if (id == GameSettings.MasterVolume || id == GameSettings.EffectsVolume)
                ApplyVolume();
        }

        private void ApplyVolume()
        {
            if (_source != null && _settings != null)
                _source.volume = _settings.LevelOf(GameSettings.MasterVolume) * _settings.LevelOf(GameSettings.EffectsVolume);
        }
    }
}
