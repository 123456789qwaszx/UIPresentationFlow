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
        foreach (WidgetLayoutPatch widgetPatch in _layout.widgets)
        {
            string nameTag = (widgetPatch.nameTag ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(nameTag))
            {
                Debug.Log("Empty or Null LayoutPatch");
                continue;
            }

            WidgetHandle handle = screen.GetWidgetHandle(nameTag);
            if (handle == null)
            {
                Debug.LogWarning(
                    $"[LayoutSpecPatch] WidgetHandle not found for nameTag='{nameTag}' on screen '{screen.name}'.",
                    screen);
                continue;
            }

            ApplyWidgetPatch(handle, widgetPatch);
        }
    }

    private static void ApplyWidgetPatch(WidgetHandle handle, WidgetLayoutPatch patch)
    {
        if (handle.RectTransform == null)
        {
            Debug.LogWarning(
                $"[LayoutSpecPatch] Widget '{patch.nameTag}' has no RectTransform (GameObject='{handle.GameObject.name}').",
                handle.GameObject);
            return;
        }

        RectTransform rect = handle.RectTransform;

        // Active
        if (patch.overrideActive)
        {
            handle.SetActive(patch.active);
        }

        RectTransformPatch rectPatch = patch.rect;
        if (rectPatch == null)
            return;

        // Anchors
        if (rectPatch.overrideAnchors)
        {
            rect.anchorMin = rectPatch.anchorMin;
            rect.anchorMax = rectPatch.anchorMax;
        }

        // Pivot
        if (rectPatch.overridePivot)
        {
            rect.pivot = rectPatch.pivot;
        }

        // Position
        if (rectPatch.overrideAnchoredPosition)
        {
            rect.anchoredPosition = rectPatch.anchoredPosition;
        }

        // Size
        if (rectPatch.overrideSizeDelta)
        {
            rect.sizeDelta = rectPatch.sizeDelta;
        }
    }
}
