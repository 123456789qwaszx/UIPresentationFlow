using TMPro;
using UnityEngine;

// Theme now depends only on the presentation-ref capability.
// Text membership comes from Ref metadata, not hierarchy/tag scanning.
public sealed class ThemeSpecPatch : IUIPatch
{
    private readonly ThemeSpec _theme;

    public ThemeSpecPatch(ThemeSpec theme)
    {
        _theme = theme;
    }

    public void Apply(IUIPresentationRefProvider refs)
    {
        if (_theme == null || refs == null)
            return;

        string targetName = refs.GetType().Name;

        foreach (string refId in refs.TextTargetIds)
        {
            if (!refs.TryGetText(refId, out TMP_Text text) || text == null)
            {
                Debug.LogWarning(
                    $"[ThemeSpecPatch] TMP_Text not found for text refId='{refId}' on '{targetName}'.");
                continue;
            }

            if (!refs.TryGetTextRole(refId, out UITextRole role))
            {
                Debug.LogWarning(
                    $"[ThemeSpecPatch] UITextRole not found for text refId='{refId}' on '{targetName}'.");
                continue;
            }

            ApplyTextTheme(text, role);
        }
    }

    private void ApplyTextTheme(TMP_Text text, UITextRole role)
    {
        if (_theme.mainFont != null)
            text.font = _theme.mainFont;

        switch (role)
        {
            case UITextRole.Title:
                text.fontSize = _theme.titleSize;
                text.color = _theme.textMainColor;
                break;

            case UITextRole.Body:
                text.fontSize = _theme.bodySize;
                text.color = _theme.textMainColor;
                break;

            case UITextRole.Caption:
                text.fontSize = _theme.captionSize;
                text.color = _theme.textWeakColor;
                break;
        }
    }
}