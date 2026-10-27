using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 켜고 끄는 스위치의 그림(시안 §8): 트랙은 off/on 그림을 바꾸고 손잡이는 트랙 안에서 좌우로 옮긴다.
    /// 값은 Toggle이 갖는다. 화면은 SetWithoutNotify로 값을 넣어 콜백이 되돌아오지 않게 한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Toggle))]
    public sealed class KedToggleSwitch : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private Image track;
        [SerializeField] private RectTransform thumb;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private Sprite onSprite;
        [Tooltip("손잡이 중심의 가로 위치(트랙 왼쪽 끝 기준). 꺼짐·켜짐.")]
        [SerializeField] private float thumbOffX = 20f;
        [SerializeField] private float thumbOnX = 70f;

        public Toggle Toggle => toggle;

        private void Awake()
        {
            if (toggle == null)
                TryGetComponent(out toggle);

            if (toggle != null)
                toggle.onValueChanged.AddListener(_ => Refresh());

            Refresh();
        }

        public void SetWithoutNotify(bool isOn)
        {
            if (toggle == null)
                return;

            toggle.SetIsOnWithoutNotify(isOn);
            Refresh();
        }

        private void Refresh()
        {
            bool isOn = toggle != null && toggle.isOn;

            if (track != null)
            {
                Sprite sprite = isOn ? onSprite : offSprite;
                if (sprite != null)
                    track.sprite = sprite;
            }

            if (thumb != null)
            {
                Vector2 position = thumb.anchoredPosition;
                position.x = isOn ? thumbOnX : thumbOffX;
                thumb.anchoredPosition = position;
            }
        }
    }
}
