using UnityEngine;

public sealed class WidgetRectApplier
{
    public void Apply(RectTransform rect, WidgetSpec spec)
    {
        switch (spec.rectMode)
        {
            case WidgetRectMode.UseSlotLayout:
                // 아무 것도 하지 않음 (Slot / LayoutGroup에 맡김)
                break;

            case WidgetRectMode.OverrideInSlot:
                ApplyOverride(rect, spec);
                break;

            default:
                Debug.LogWarning(
                    $"[WidgetRectApplier] Unsupported rectMode={spec.rectMode} on '{rect.name}'");
                break;
        }
    }

    private void ApplyOverride(RectTransform rect, WidgetSpec spec)
    {
        rect.anchorMin = spec.anchorMin;
        rect.anchorMax = spec.anchorMax;
        rect.pivot     = spec.pivot;

        rect.anchoredPosition = spec.anchoredPosition;
        rect.sizeDelta        = spec.sizeDelta;
    }
}