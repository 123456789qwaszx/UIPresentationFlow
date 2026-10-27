using System;
using TMPro;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 슬라이더 하나와 그 값 글자. 값을 넣을 때는 콜백을 부르지 않는다(SetValueWithoutNotify) —
    /// 화면을 새로 그리는 일이 설정을 다시 저장하는 고리가 되지 않게(시안 §8).
    /// </summary>
    internal sealed class KedSliderField
    {
        private readonly Slider _slider;
        private readonly TMP_Text _value;
        private readonly Func<float, string> _format;

        public KedSliderField(
            Slider slider,
            TMP_Text value,
            float min,
            float max,
            bool wholeNumbers,
            Func<float, string> format,
            Action<float> changed)
        {
            _slider = slider;
            _value = value;
            _format = format ?? (v => v.ToString("0"));

            if (_slider == null)
                return;

            _slider.minValue = min;
            _slider.maxValue = max;
            _slider.wholeNumbers = wholeNumbers;

            _slider.onValueChanged.AddListener(v =>
            {
                ShowValue(v);
                changed?.Invoke(v);
            });
        }

        public Slider Slider => _slider;

        public void Set(float value)
        {
            if (_slider != null)
                _slider.SetValueWithoutNotify(value);

            ShowValue(_slider != null ? _slider.value : value);
        }

        public void Redraw()
        {
            if (_slider != null)
                ShowValue(_slider.value);
        }

        private void ShowValue(float value)
        {
            KedUIUtil.SetText(_value, _format(value));
        }
    }
}
