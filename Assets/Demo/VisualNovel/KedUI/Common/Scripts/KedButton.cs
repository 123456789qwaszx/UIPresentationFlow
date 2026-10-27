using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 버튼 하나의 상태 그림. 시안 §8 — 올림·포커스: 청록 테두리(outline_white), 누름: 0.98 크기(UIPF ButtonAnimation),
    /// 막힘: 투명도 0.5와 실제 interactable. 탭·선택지처럼 "선택됨" 그림이 따로 있는 버튼은 SetSelectedLook으로 바꾼다.
    ///
    /// 입력(클릭)은 받지 않는다 — 화면이 Button.onClick에 묶는다. 이것은 보이는 모양만 맡는다.
    /// 누름 크기 연출은 UIPF의 ButtonFeedback/ButtonAnimation을 그대로 쓴다(빌더가 크기 값을 시안에 맞춰 둔다).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KedButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private GameObject focusRing;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text label;

        [Header("Sprites (비우면 그림을 바꾸지 않는다)")]
        [SerializeField] private Sprite normalSprite;
        [SerializeField] private Sprite hoverSprite;
        [SerializeField] private Sprite selectedSprite;

        [Header("Colors")]
        [SerializeField] private Color backgroundNormal = Color.white;
        [SerializeField] private Color backgroundSelected = Color.white;
        [SerializeField] private Color labelNormal = new(0.161f, 0.278f, 0.424f, 1f);
        [SerializeField] private Color labelSelected = Color.white;

        private bool _hovered;
        private bool _focused;
        private bool _selectedLook;
        private bool _lastInteractable = true;
        private bool _lastReachable = true;
        private bool _feedbackBound;

        public Button Button => button;
        public TMP_Text Label => label;
        public bool IsSelectedLook => _selectedLook;

        private void Awake()
        {
            if (button == null)
                TryGetComponent(out button);

            BindFeedback();
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnDisable()
        {
            _hovered = false;
            _focused = false;

            if (focusRing != null)
                focusRing.SetActive(false);
        }

        // 막힘 모양은 버튼 자신의 값(interactable)으로 본다. 부모 CanvasGroup이 막은 경우(가려진 Panel · Panel 아래 Root)는
        // 반투명하게 만들지 않는다 — 그건 "이 버튼을 쓸 수 없다"가 아니라 "지금 위에 다른 화면이 있다"다.
        private void LateUpdate()
        {
            bool own = button == null || button.interactable;
            bool reachable = button == null || button.IsInteractable();

            if (own != _lastInteractable || reachable != _lastReachable)
                Refresh();
        }

        public void SetLabel(string text)
        {
            if (label != null)
                label.text = text ?? string.Empty;
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
                button.interactable = interactable;

            Refresh();
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        /// <summary>선택됨 그림(탭 선택·고른 배속·켜진 자동 진행).</summary>
        public void SetSelectedLook(bool selected)
        {
            if (_selectedLook == selected)
                return;

            _selectedLook = selected;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Refresh();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            Refresh();
        }

        public void OnSelect(BaseEventData eventData)
        {
            _focused = true;
            Refresh();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _focused = false;
            Refresh();
        }

        private void BindFeedback()
        {
            if (_feedbackBound || button == null)
                return;

            _feedbackBound = true;
            // 누름 크기·소리는 UIPF가 정한 길로. 클릭 처리는 화면이 onClick에 따로 묶는다.
            ButtonFeedback.Bind(button);
        }

        private void Refresh()
        {
            bool interactable = button == null || button.interactable;
            bool reachable = button == null || button.IsInteractable();
            _lastInteractable = interactable;
            _lastReachable = reachable;

            if (canvasGroup != null)
                canvasGroup.alpha = interactable ? 1f : KedColors.DisabledAlpha;

            if (focusRing != null)
                focusRing.SetActive(reachable && (_hovered || _focused) && !_selectedLook);

            if (background != null)
            {
                Sprite sprite = _selectedLook && selectedSprite != null
                    ? selectedSprite
                    : reachable && _hovered && hoverSprite != null
                        ? hoverSprite
                        : normalSprite;

                if (sprite != null)
                    background.sprite = sprite;

                background.color = _selectedLook ? backgroundSelected : backgroundNormal;
            }

            if (label != null)
                label.color = _selectedLook ? labelSelected : labelNormal;
        }
    }
}
