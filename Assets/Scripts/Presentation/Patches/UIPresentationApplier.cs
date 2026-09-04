using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class UIPresentationApplier
{
    private readonly Dictionary<UIBase, UIPresentationBaseline> _baselines = new();

    public void Apply(UIBase view, UIResolveResult result)
    {
        UIPresentationBaseline baseline = 
            GetBaseline(view);

        // Remove only the visual properties owned by the previously applied Presentation.
        baseline.RestoreOwned(view);

        // Capture the authored value the first time a property enters Presentation ownership,
        // then mark the properties owned by this Presentation.
        baseline.CaptureAndOwn(view, result.Resolved);

        foreach (IUIPatch patch in result.Patches)
            patch?.Apply(view);
    }

    private UIPresentationBaseline GetBaseline(UIBase view)
    {
        if (_baselines.TryGetValue(view, out UIPresentationBaseline baseline))
            return baseline;
        
        baseline = new UIPresentationBaseline();

        _baselines.Add(view, baseline);

        return baseline;
    }

    private sealed class UIPresentationBaseline
    {
        private readonly Dictionary<string, RectBaseline> _rects = new();
        private readonly Dictionary<string, TextBaseline> _texts = new();

        public void RestoreOwned(IUIPresentationRefProvider refs)
        {
            foreach (KeyValuePair<string, RectBaseline> pair in _rects)
            {
                if (refs.TryGetRect(pair.Key, out RectTransform rect) && rect != null)
                    pair.Value.RestoreOwned(rect);
                else
                    pair.Value.ReleaseOwnership();
            }

            foreach (KeyValuePair<string, TextBaseline> pair in _texts)
            {
                if (refs.TryGetText(pair.Key, out TMP_Text text) && text != null)
                    pair.Value.RestoreOwned(text);
                else
                    pair.Value.ReleaseOwnership();
            }
        }

        public void CaptureAndOwn(IUIPresentationRefProvider refs, ResolvedUIPresentation resolved)
        {
            CaptureLayout(refs, resolved?.Layout);
            CaptureTheme(refs, resolved?.Theme);
        }

        private void CaptureLayout(IUIPresentationRefProvider refs, LayoutPatchSpec layout)
        {
            if (layout?.widgets == null)
                return;

            foreach (WidgetLayoutPatch widget in layout.widgets)
            {
                if (widget == null)
                    continue;

                string refId = (widget.refId ?? string.Empty).Trim();

                if (string.IsNullOrEmpty(refId))
                    continue;

                if (!refs.TryGetRect(refId, out RectTransform rect) || rect == null)
                    continue;

                if (!_rects.TryGetValue(refId, out RectBaseline baseline))
                {
                    baseline = new RectBaseline();
                    _rects.Add(refId, baseline);
                }

                baseline.CaptureAndOwn(rect, widget);
            }
        }

        private void CaptureTheme(IUIPresentationRefProvider refs, ThemeSpec theme)
        {
            if (theme == null)
                return;

            foreach (string refId in refs.TextTargetIds)
            {
                if (!refs.TryGetTextRole(refId, out _))
                    continue;
                
                if (!refs.TryGetText(refId, out TMP_Text text) || text == null)
                    continue;
                
                if (!_texts.TryGetValue(refId, out TextBaseline baseline))
                {
                    baseline = new TextBaseline();
                    _texts.Add(refId, baseline);
                }

                baseline.CaptureAndOwn(text, theme);
            }
        }
    }

    private sealed class RectBaseline
    {
        private bool _hasActive;
        private bool _active;
        private bool _ownsActive;

        private bool _hasAnchors;
        private Vector2 _anchorMin;
        private Vector2 _anchorMax;
        private bool _ownsAnchors;

        private bool _hasPivot;
        private Vector2 _pivot;
        private bool _ownsPivot;

        private bool _hasAnchoredPosition;
        private Vector2 _anchoredPosition;
        private bool _ownsAnchoredPosition;

        private bool _hasSizeDelta;
        private Vector2 _sizeDelta;
        private bool _ownsSizeDelta;

        public void CaptureAndOwn(RectTransform rect, WidgetLayoutPatch patch)
        {
            if (patch.overrideActive)
            {
                if (!_hasActive)
                {
                    _active = rect.gameObject.activeSelf;
                    _hasActive = true;
                }

                _ownsActive = true;
            }

            RectTransformPatch rectPatch = patch.rect;
            
            if (rectPatch == null)
                return;

            if (rectPatch.overrideAnchors)
            {
                if (!_hasAnchors)
                {
                    _anchorMin = rect.anchorMin;
                    _anchorMax = rect.anchorMax;

                    _hasAnchors = true;
                }

                _ownsAnchors = true;
            }

            if (rectPatch.overridePivot)
            {
                if (!_hasPivot)
                {
                    _pivot = rect.pivot;
                    _hasPivot = true;
                }

                _ownsPivot = true;
            }

            if (rectPatch.overrideAnchoredPosition)
            {
                if (!_hasAnchoredPosition)
                {
                    _anchoredPosition = rect.anchoredPosition;
                    _hasAnchoredPosition = true;
                }

                _ownsAnchoredPosition = true;
            }

            if (rectPatch.overrideSizeDelta)
            {
                if (!_hasSizeDelta)
                {
                    _sizeDelta = rect.sizeDelta;
                    _hasSizeDelta = true;
                }

                _ownsSizeDelta = true;
            }
        }

        public void RestoreOwned(
            RectTransform rect)
        {
            if (_ownsAnchors && _hasAnchors)
            {
                rect.anchorMin = _anchorMin;
                rect.anchorMax = _anchorMax;
            }

            if (_ownsPivot && _hasPivot)
                rect.pivot = _pivot;

            if (_ownsAnchoredPosition && _hasAnchoredPosition)
                rect.anchoredPosition = _anchoredPosition;

            if (_ownsSizeDelta && _hasSizeDelta)
                rect.sizeDelta = _sizeDelta;
            
            if (_ownsActive && _hasActive)
                rect.gameObject.SetActive(_active);
            
            ReleaseOwnership();
        }

        public void ReleaseOwnership()
        {
            _ownsActive = false;
            _ownsAnchors = false;
            _ownsPivot = false;
            _ownsAnchoredPosition = false;
            _ownsSizeDelta = false;
        }
    }

    private sealed class TextBaseline
    {
        private bool _hasFont;
        private TMP_FontAsset _font;
        private bool _ownsFont;

        private bool _hasFontSize;
        private float _fontSize;
        private bool _ownsFontSize;

        private bool _hasColor;
        private Color _color;
        private bool _ownsColor;

        public void CaptureAndOwn(TMP_Text text, ThemeSpec theme)
        {
            // ThemeSpecPatch only changes font when
            // mainFont has explicitly been authored.
            if (theme.mainFont != null)
            {
                if (!_hasFont)
                {
                    _font = text.font;
                    _hasFont = true;
                }

                _ownsFont = true;
            }

            if (!_hasFontSize)
            {
                _fontSize = text.fontSize;
                _hasFontSize = true;
            }

            _ownsFontSize = true;

            if (!_hasColor)
            {
                _color = text.color;
                _hasColor = true;
            }

            _ownsColor = true;
        }

        public void RestoreOwned(TMP_Text text)
        {
            if (_ownsFont && _hasFont)
                text.font = _font;

            if (_ownsFontSize && _hasFontSize)
                text.fontSize = _fontSize;

            if (_ownsColor && _hasColor)
                text.color = _color;

            ReleaseOwnership();
        }

        public void ReleaseOwnership()
        {
            _ownsFont = false;
            _ownsFontSize = false;
            _ownsColor = false;
        }
    }
}