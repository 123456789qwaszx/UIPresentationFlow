using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 재생 화면의 대사창 — PlayScreen의 Refs 위에서 UI 쪽 일만 한다: 글자 · 이름 넣기, 자리(레이아웃), 글자 배율,
    /// 불투명도, 다음 표시, 보이기. 재생 런타임은 모른다.
    /// 재생 쪽 계약(ked 본체의 대사창 — 연출 프리셋 · 페이드)은 앱이 이것 위에 구현한다(ked: App/Runtime/Play/KedDialogueBoxView).
    ///
    /// 소유권: 줄 · 이름 글자의 자리 · 크기 · 정렬은 앱이 넘기는 레이아웃(PlayDialogueLayout)이 정한다. 넘기지 않으면 프리팹의 자리다.
    /// 그림(박스 · 이름표) · 글꼴 · 불투명도 · 글자 배율은 UI가 정한다.
    /// 이름표 그림은 이름 글자의 자리를 따라간다 — 레이아웃이 이름 자리를 옮겨도 판이 글자를 감싼다.
    /// </summary>
    public sealed class PlayDialogueBox
    {
        // 시안: 이름표 332×58, 이름 글자 250×40 — 글자 둘레의 판 여백(왼쪽·아래·오른쪽·위).
        private static readonly Vector2 PlatePadMin = new(26f, 13f);
        private static readonly Vector2 PlatePadMax = new(56f, 5f);

        private readonly RectTransform _root;
        private readonly CanvasGroup _group;
        private readonly Image _box;
        private readonly RectTransform _plate;
        private readonly Image _plateImage;
        private readonly TMP_Text _line;
        private readonly TMP_Text _name;
        private readonly GameObject _nextMarker;

        private bool _usesName = true;
        private bool _clearNameWhenHidden = true;
        private float _lineBaseSize;
        private float _nameBaseSize;
        private float _textScale = 1f;
        private float _opacity = 1f;

        public PlayDialogueBox(
            RectTransform root,
            CanvasGroup group,
            Image box,
            RectTransform plate,
            TMP_Text line,
            TMP_Text name,
            GameObject nextMarker)
        {
            _root = root;
            _group = group;
            _box = box;
            _plate = plate;
            _plateImage = plate != null ? plate.GetComponent<Image>() : null;
            _line = line;
            _name = name;
            _nextMarker = nextMarker;

            // 레이아웃을 받기 전에는 프리팹의 글자 크기가 기준이다.
            _lineBaseSize = line != null ? line.fontSize : 0f;
            _nameBaseSize = name != null ? name.fontSize : 0f;
        }

        public RectTransform Root => _root;
        public CanvasGroup Group => _group;
        public TMP_Text Line => _line;
        public TMP_Text Name => _name;

        #region 설정 · 테마

        /// <summary>사용자 글자 배율(UIContext.TextScale). 레이아웃(없으면 프리팹)의 크기에 곱한다.</summary>
        public void SetTextScale(float scale)
        {
            float next = scale > 0f ? scale : 1f;
            if (Mathf.Approximately(next, _textScale))
                return;

            _textScale = next;
            ApplyFontSizes();
        }

        /// <summary>대사창 그림의 불투명도(설정 · 대사창 불투명도). 글자는 그대로.</summary>
        public void SetOpacity(float opacity)
        {
            _opacity = Mathf.Clamp01(opacity);
            ApplyOpacity(_box);
            ApplyOpacity(_plateImage);
        }

        public void SetNextMarkerVisible(bool visible)
        {
            KedUIUtil.SetActive(_nextMarker, visible);
        }

        #endregion

        #region 글자 · 자리 · 보이기

        public void ApplyLayout(in PlayDialogueLayout layout)
        {
            ApplyLineLayout(layout);
            ApplyNameLayout(layout);

            _lineBaseSize = layout.lineFontSize;
            _nameBaseSize = layout.nameFontSize;
            ApplyFontSizes();
        }

        public void ResetTransform()
        {
            if (_root == null)
                return;

            _root.localPosition = Vector3.zero;
            _root.anchoredPosition = Vector2.zero;
        }

        /// <summary>줄과 이름을 넣는다. hideCharacters면 한 글자도 보이지 않게 둔다(타자기가 하나씩 연다).</summary>
        public void PrimeText(string text, string characterName, bool hasCharacterName, bool hideCharacters = true)
        {
            SetNextMarkerVisible(false);

            _line.text = text ?? string.Empty;
            _line.maxVisibleCharacters = hideCharacters ? 0 : int.MaxValue;
            _line.ForceMeshUpdate();

            if (_name == null)
                return;

            bool showName = _usesName && hasCharacterName;

            if (showName)
            {
                _name.text = characterName ?? string.Empty;
                SetNameVisible(true);
                return;
            }

            if (_clearNameWhenHidden)
                _name.text = string.Empty;

            SetNameVisible(false);
        }

        public void SetVisibleImmediate(bool visible)
        {
            KedUIUtil.SetActive(_root, true);
            SetShown(visible);
        }

        public void PrepareHidden()
        {
            KedUIUtil.SetActive(_root, true);
            SetShown(false);
        }

        /// <summary>보이기만 바꾼다(알파). 대사창은 입력을 받지 않는다 — 진행 클릭은 아래의 AdvanceArea가 받는다.</summary>
        public void SetShown(bool visible)
        {
            if (_group == null)
                return;

            _group.alpha = visible ? 1f : 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }

        #endregion

        private void ApplyLineLayout(in PlayDialogueLayout layout)
        {
            RectTransform rect = _line.rectTransform;

            rect.anchorMin = layout.lineAnchorMin;
            rect.anchorMax = layout.lineAnchorMax;
            rect.pivot = layout.linePivot;
            rect.anchoredPosition = layout.lineAnchoredPosition;
            rect.sizeDelta = layout.lineSizeDelta;

            _line.alignment = layout.lineAlignment;
            _line.lineSpacing = layout.lineSpacing;
            _line.paragraphSpacing = layout.paragraphSpacing;
            _line.margin = layout.lineMargin;
            _line.overflowMode = layout.lineOverflowMode;
            _line.textWrappingMode = layout.lineTextWrappingMode;
        }

        private void ApplyNameLayout(in PlayDialogueLayout layout)
        {
            _usesName = layout.useName && _name != null;
            _clearNameWhenHidden = layout.clearNameWhenHidden;

            if (_name == null)
                return;

            SetNameVisible(_usesName);

            if (!_usesName)
                return;

            RectTransform rect = _name.rectTransform;

            rect.anchorMin = layout.nameAnchorMin;
            rect.anchorMax = layout.nameAnchorMax;
            rect.pivot = layout.namePivot;
            rect.anchoredPosition = layout.nameAnchoredPosition;
            rect.sizeDelta = layout.nameSizeDelta;

            _name.alignment = layout.nameAlignment;
            _name.margin = layout.nameMargin;
            _name.overflowMode = layout.nameOverflowMode;
            _name.textWrappingMode = layout.nameTextWrappingMode;

            FitPlateToName(rect);
        }

        private void ApplyFontSizes()
        {
            if (_line != null)
                _line.fontSize = _lineBaseSize * _textScale;

            if (_name != null)
                _name.fontSize = _nameBaseSize * _textScale;
        }

        // 이름표 판은 이름 글자와 같은 앵커·자리에 두고 시안의 여백만큼 넓힌다.
        private void FitPlateToName(RectTransform nameRect)
        {
            if (_plate == null)
                return;

            _plate.anchorMin = nameRect.anchorMin;
            _plate.anchorMax = nameRect.anchorMax;
            _plate.pivot = nameRect.pivot;
            _plate.anchoredPosition = nameRect.anchoredPosition;
            _plate.sizeDelta = nameRect.sizeDelta;

            _plate.offsetMin -= PlatePadMin;
            _plate.offsetMax += PlatePadMax;
        }

        private void SetNameVisible(bool visible)
        {
            KedUIUtil.SetActive(_name, visible);
            KedUIUtil.SetActive(_plate, visible);
        }

        private void ApplyOpacity(Image image)
        {
            if (image == null)
                return;

            Color color = image.color;
            color.a = _opacity;
            image.color = color;
        }
    }

    /// <summary>대사창 글자의 자리 · 크기 · 정렬. 앱이 연출 프리셋(ked: 대사창 배치 DB의 한 항목)에서 채워 넘긴다.</summary>
    public struct PlayDialogueLayout
    {
        public Vector2 lineAnchorMin;
        public Vector2 lineAnchorMax;
        public Vector2 linePivot;
        public Vector2 lineAnchoredPosition;
        public Vector2 lineSizeDelta;
        public TextAlignmentOptions lineAlignment;
        public float lineSpacing;
        public float paragraphSpacing;
        public Vector4 lineMargin;
        public TextOverflowModes lineOverflowMode;
        public TextWrappingModes lineTextWrappingMode;
        public float lineFontSize;

        public bool useName;
        public bool clearNameWhenHidden;
        public Vector2 nameAnchorMin;
        public Vector2 nameAnchorMax;
        public Vector2 namePivot;
        public Vector2 nameAnchoredPosition;
        public Vector2 nameSizeDelta;
        public TextAlignmentOptions nameAlignment;
        public Vector4 nameMargin;
        public TextOverflowModes nameOverflowMode;
        public TextWrappingModes nameTextWrappingMode;
        public float nameFontSize;
    }
}
