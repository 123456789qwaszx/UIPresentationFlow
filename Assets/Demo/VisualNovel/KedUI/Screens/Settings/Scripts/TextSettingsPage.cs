using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 11 설정 · 대사 탭. 글자 크기 · 글자 출력 속도 · 대사창 불투명도. 미리보기는 고른 속도로 글자를 찍고
    /// 고른 불투명도로 바탕을 칠한다(글자 크기는 UIPF가 미리보기 글자에도 곱한다 — Styled 등록).
    /// </summary>
    [UIViewKey("Ked.TextSettingsPage")]
    public sealed class TextSettingsPage : UIPage<TextSettingsPage.Refs>
    {
        private const float PreviewHoldSeconds = 1.2f;

        public enum Refs
        {
            TextHeading,
            TextDescription,
            TextLabel_0,
            TextSlider_0,
            TextValue_0,
            TextLabel_1,
            TextSlider_1,
            TextValue_1,
            TextLabel_2,
            TextSlider_2,
            TextValue_2,
            TextPreviewCaption,
            TextPreviewSurface,
            TextPreviewText,
        }

        public sealed class Model
        {
            public float TextScale;
            public float TypeSpeed;
            public float BoxOpacity;
        }

        public event Action<float> TextScaleChanged;
        public event Action<float> TypeSpeedChanged;
        public event Action<float> BoxOpacityChanged;

        private KedSliderField _scale;
        private KedSliderField _speed;
        private KedSliderField _opacity;

        private float _typeSpeed = 30f;
        private float _typed;
        private float _hold;

        protected override void OnInitialize()
        {
            _scale = new KedSliderField(
                View.Component<Slider>(Refs.TextSlider_0),
                View.Text(Refs.TextValue_0),
                KedSettingRanges.MinTextScale * 100f, KedSettingRanges.MaxTextScale * 100f, wholeNumbers: true,
                format: v => v.ToString("0") + "%",
                changed: v => TextScaleChanged?.Invoke(v / 100f));

            _speed = new KedSliderField(
                View.Component<Slider>(Refs.TextSlider_1),
                View.Text(Refs.TextValue_1),
                KedSettingRanges.MinTypeSpeed, KedSettingRanges.MaxTypeSpeed, wholeNumbers: true,
                format: v => v.ToString("0"),
                changed: v =>
                {
                    _typeSpeed = v;
                    RestartPreview();
                    TypeSpeedChanged?.Invoke(v);
                });

            _opacity = new KedSliderField(
                View.Component<Slider>(Refs.TextSlider_2),
                View.Text(Refs.TextValue_2),
                KedSettingRanges.MinBoxOpacity * 100f, 100f, wholeNumbers: true,
                format: v => v.ToString("0") + "%",
                changed: v =>
                {
                    ApplyPreviewOpacity(v / 100f);
                    BoxOpacityChanged?.Invoke(v / 100f);
                });

            Styled.AddText(View.Text(Refs.TextPreviewText), UITextRole.Body, UITextSizePolicy.Prefab);
        }

        public void Present(Model model)
        {
            _scale.Set(model.TextScale * 100f);
            _speed.Set(model.TypeSpeed);
            _opacity.Set(model.BoxOpacity * 100f);

            _typeSpeed = model.TypeSpeed;
            ApplyPreviewOpacity(model.BoxOpacity);
            RestartPreview();
        }

        public Selectable FirstSelectable => _scale?.Slider;

        private void Update()
        {
            TMP_Text preview = View.Text(Refs.TextPreviewText);
            if (preview == null)
                return;

            int total = preview.textInfo != null ? preview.textInfo.characterCount : 0;
            if (total <= 0)
            {
                preview.ForceMeshUpdate();
                total = preview.textInfo.characterCount;
            }

            if (_typed < total)
            {
                _typed += Mathf.Max(1f, _typeSpeed) * Time.unscaledDeltaTime;
                preview.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(_typed));
                return;
            }

            _hold += Time.unscaledDeltaTime;
            if (_hold >= PreviewHoldSeconds)
                RestartPreview();
        }

        private void RestartPreview()
        {
            _typed = 0f;
            _hold = 0f;

            TMP_Text preview = View.Text(Refs.TextPreviewText);
            if (preview != null)
                preview.maxVisibleCharacters = 0;
        }

        private void ApplyPreviewOpacity(float opacity)
        {
            Image surface = View.Image(Refs.TextPreviewSurface);
            if (surface == null)
                return;

            Color color = surface.color;
            color.a = Mathf.Clamp01(opacity);
            surface.color = color;
        }
    }
}
