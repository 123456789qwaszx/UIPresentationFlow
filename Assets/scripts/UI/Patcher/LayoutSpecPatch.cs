using UnityEngine;

public sealed class LayoutSpecPatch : IUIPatch
{
    private readonly LayoutPatchSpec _layout;

    public LayoutSpecPatch(LayoutPatchSpec layout)
    {
        _layout = layout;
    }

    public void Apply(UIScreen screen)
    {
        if (_layout == null || screen == null)
            return;

        IUIPresentationRefProvider refs =
            screen.GetComponent<IUIPresentationRefProvider>();

        if (refs == null)
        {
            Debug.LogWarning(
                $"[LayoutSpecPatch] No {nameof(IUIPresentationRefProvider)} found on screen '{screen.name}'.",
                screen);
            return;
        }

        foreach (WidgetLayoutPatch widgetPatch in _layout.widgets)
        {
            if (widgetPatch == null)
                continue;

            string refId = (widgetPatch.refId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(refId))
            {
                Debug.LogWarning(
                    "[LayoutSpecPatch] Empty refId ignored.",
                    screen);
                continue;
            }

            if (!refs.TryGetRect(refId, out RectTransform rect) || rect == null)
            {
                Debug.LogWarning(
                    $"[LayoutSpecPatch] RectTransform not found for refId='{refId}' on screen '{screen.name}'.",
                    screen);
                continue;
            }

            ApplyWidgetPatch(rect, widgetPatch);
        }
    }

    private static void ApplyWidgetPatch(
        RectTransform rect,
        WidgetLayoutPatch patch)
    {
        if (patch.overrideActive)
            rect.gameObject.SetActive(patch.active);

        RectTransformPatch rectPatch = patch.rect;
        if (rectPatch == null)
            return;

        if (rectPatch.overrideAnchors)
        {
            rect.anchorMin = rectPatch.anchorMin;
            rect.anchorMax = rectPatch.anchorMax;
        }

        if (rectPatch.overridePivot)
            rect.pivot = rectPatch.pivot;

        if (rectPatch.overrideAnchoredPosition)
            rect.anchoredPosition = rectPatch.anchoredPosition;

        if (rectPatch.overrideSizeDelta)
            rect.sizeDelta = rectPatch.sizeDelta;
    }
}