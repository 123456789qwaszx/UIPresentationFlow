using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed partial class UIManager
{
    private readonly Dictionary<UIBase, UIPresentationBaseline> _baselines = new();

    private void ApplyPresentation(
        UIBase view,
        UIPresentationSpec presentation,
        in DisplayContext display)
    {
        view.EnsureInitialized();

        if (!_baselines.TryGetValue(view, out UIPresentationBaseline baseline))
        {
            baseline = new UIPresentationBaseline();
            _baselines.Add(view, baseline);
        }

        // Restore everything previously touched before resolving a new visual state.
        // This replaces the implicit reset we previously got by destroying and
        // re-instantiating the prefab on every display change.
        baseline.Restore(view);

        UIResolveResult result = _resolver.Resolve(presentation, display);

        baseline.CaptureMissing(view, result.Resolved);
        _patcher.Apply(view, result.Patches);

        LastDisplay = display;
        LastResult = result;
    }

    private sealed class UIPresentationBaseline
    {
        private readonly Dictionary<string, RectState> _rects = new();
        private readonly Dictionary<string, TextState> _texts = new();

        public void CaptureMissing(
            IUIPresentationRefProvider refs,
            ResolvedUIPresentation resolved)
        {
            LayoutPatchSpec layout = resolved?.Layout;
            if (layout?.widgets != null)
            {
                foreach (WidgetLayoutPatch widget in layout.widgets)
                {
                    if (widget == null || string.IsNullOrWhiteSpace(widget.refId))
                        continue;

                    if (_rects.ContainsKey(widget.refId))
                        continue;

                    if (refs.TryGetRect(widget.refId, out RectTransform rect) && rect != null)
                        _rects.Add(widget.refId, RectState.Capture(rect));
                }
            }

            if (resolved?.Theme == null)
                return;

            foreach (string refId in refs.TextTargetIds)
            {
                if (_texts.ContainsKey(refId))
                    continue;

                if (refs.TryGetText(refId, out TMP_Text text) && text != null)
                    _texts.Add(refId, TextState.Capture(text));
            }
        }

        public void Restore(IUIPresentationRefProvider refs)
        {
            foreach (KeyValuePair<string, RectState> pair in _rects)
            {
                if (refs.TryGetRect(pair.Key, out RectTransform rect) && rect != null)
                    pair.Value.Restore(rect);
            }

            foreach (KeyValuePair<string, TextState> pair in _texts)
            {
                if (refs.TryGetText(pair.Key, out TMP_Text text) && text != null)
                    pair.Value.Restore(text);
            }
        }
    }

    private readonly struct RectState
    {
        private readonly bool _active;
        private readonly Vector2 _anchorMin;
        private readonly Vector2 _anchorMax;
        private readonly Vector2 _pivot;
        private readonly Vector2 _anchoredPosition;
        private readonly Vector2 _sizeDelta;

        private RectState(RectTransform rect)
        {
            _active = rect.gameObject.activeSelf;
            _anchorMin = rect.anchorMin;
            _anchorMax = rect.anchorMax;
            _pivot = rect.pivot;
            _anchoredPosition = rect.anchoredPosition;
            _sizeDelta = rect.sizeDelta;
        }

        public static RectState Capture(RectTransform rect) => new(rect);

        public void Restore(RectTransform rect)
        {
            rect.anchorMin = _anchorMin;
            rect.anchorMax = _anchorMax;
            rect.pivot = _pivot;
            rect.anchoredPosition = _anchoredPosition;
            rect.sizeDelta = _sizeDelta;
            rect.gameObject.SetActive(_active);
        }
    }

    private readonly struct TextState
    {
        private readonly TMP_FontAsset _font;
        private readonly float _fontSize;
        private readonly Color _color;

        private TextState(TMP_Text text)
        {
            _font = text.font;
            _fontSize = text.fontSize;
            _color = text.color;
        }

        public static TextState Capture(TMP_Text text) => new(text);

        public void Restore(TMP_Text text)
        {
            text.font = _font;
            text.fontSize = _fontSize;
            text.color = _color;
        }
    }
}