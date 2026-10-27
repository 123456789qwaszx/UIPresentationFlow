using System;
using Ked.Samples.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VisualNovelDemo
{
    /// <summary>
    /// 선택지 항목 하나. 재생 화면의 선택지 상자(PlayOptionsBox.ItemContainer)에 데모 흐름이 만든다.
    /// ked에서는 이 자리를 본체의 VNOptionItem이 차지한다 — 화면 묶음은 항목의 종류를 모른다.
    /// 모양은 같은 키트로 짓는다(빌드 명세 VNDemoOptionItemSpec): 올림 · 포커스 = 청록 테두리, 누름 = 크기 연출(ButtonFeedback).
    /// 고르기는 Button.onClick에 묶는다 — 포인터 클릭과 키보드 · 패드의 Submit이 같은 길로 온다(KedUIUtil.OnClick).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VNDemoOptionItem : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject selectionIndicator;

        private bool _hovered;
        private bool _selected;
        private bool _bound;
        private Action _onChosen;

        public Button Button => button;

        public void Bind(string text, Action onChosen)
        {
            _onChosen = onChosen;

            if (label != null)
                label.text = text ?? string.Empty;

            if (!_bound && button != null)
            {
                _bound = true;
                ButtonFeedback.Bind(button);
                KedUIUtil.OnClick(button, () => _onChosen?.Invoke());
            }

            Refresh();
        }

        public void OnSelect(BaseEventData eventData)
        {
            _selected = true;
            Refresh();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
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

        private void OnDisable()
        {
            _hovered = false;
            _selected = false;
            Refresh();
        }

        private void Refresh()
        {
            if (selectionIndicator != null)
                selectionIndicator.SetActive(_hovered || _selected);
        }
    }
}
