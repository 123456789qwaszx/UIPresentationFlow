using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 재생 화면의 선택지 상자 — 딤과 상자의 보이기 · 입력만 맡는다(시안 03). 항목은 앱이 ItemContainer에 만든다
    /// (선택 가능 · 불가 조건도 그쪽이 정한다). 재생 런타임은 모른다.
    /// 재생 쪽 계약(ked 본체의 선택지 상자 — 페이드)은 앱이 이것 위에 구현한다(ked: App/Runtime/Play/KedOptionsBoxView).
    /// </summary>
    public sealed class PlayOptionsBox
    {
        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly RectTransform _itemContainer;

        public PlayOptionsBox(RectTransform root, CanvasGroup group, RectTransform itemContainer)
        {
            _root = root;
            _group = group;
            _itemContainer = itemContainer;
        }

        public RectTransform Root => _root;
        public CanvasGroup Group => _group;
        public RectTransform ItemContainer => _itemContainer;

        public bool IsShowing => _group != null && _group.alpha > 0f;

        public void ResetTransform()
        {
            if (_root == null)
                return;

            _root.localPosition = Vector3.zero;
            _root.anchoredPosition = Vector2.zero;
        }

        public void SetVisibleImmediate(bool visible)
        {
            KedUIUtil.SetActive(_root, true);
            SetShown(visible);
        }

        public void SetInputEnabled(bool enabled)
        {
            if (_group == null)
                return;

            _group.interactable = enabled;
            _group.blocksRaycasts = enabled;
        }

        public void PrepareHidden()
        {
            KedUIUtil.SetActive(_root, true);
            SetShown(false);
        }

        /// <summary>보이기와 입력을 함께 바꾼다.</summary>
        public void SetShown(bool visible)
        {
            if (_group == null)
                return;

            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }
    }
}
