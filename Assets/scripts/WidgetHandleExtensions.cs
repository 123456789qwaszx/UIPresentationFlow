using UnityEngine;

public static class WidgetHandleExtensions
{
    public static void SetText(this WidgetHandle h, string text)
    {
        if (h.Text == null) return;
        h.Text.text = text;
    }

    public static void BindClick(this WidgetHandle h, System.Action onClick)
    {
        if (h.Button == null) return;
        h.Button.onClick.RemoveAllListeners();
        if (onClick != null)
            h.Button.onClick.AddListener(() => onClick());
    }

    public static void SetAlpha(this WidgetHandle h, float alpha)
    {
        if (h.CanvasGroup == null) return;
        h.CanvasGroup.alpha = alpha;
    }

    public static void MoveTo(this WidgetHandle h, Vector2 anchoredPos)
    {
        if (h.RectTransform == null) return;
        h.RectTransform.anchoredPosition = anchoredPos;
    }
}