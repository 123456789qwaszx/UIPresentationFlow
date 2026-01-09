using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum UITextRole
{
    Title,
    Body,
    Caption,
}

public sealed class UITextRoleTag : MonoBehaviour
{
    public UITextRole role;
}


public class ThemeSpecPatch : IUIPatch
{
    private readonly ThemeSpec _theme;

    public ThemeSpecPatch(ThemeSpec theme)
    {
        _theme = theme;
    }

    public void Apply(UIScreen screen)
    {
        if (_theme == null || screen == null)
            return;

        // 1) Background
        Image rootImage = screen.GetComponent<Image>();
        if (rootImage != null)
            rootImage.color = _theme.backgroundColor;

        // 2) Panels (optional: by tag / layer / name)
        var images = screen.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            // 예: 이름 규칙이나 커스텀 컴포넌트로 필터링
            // 여기서는 간단히 "Panel"이름을 가진 애만
            if (img.gameObject.name.Contains("Panel"))
                img.color = _theme.panelColor;
        }

        // 3) Text
        TMP_Text[] texts = screen.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            var roleTag = text.GetComponent<UITextRoleTag>();
            var role    = roleTag != null ? roleTag.role : UITextRole.Body;

            text.font = _theme.mainFont;

            switch (role)
            {
                case UITextRole.Title:
                    text.fontSize = _theme.titleSize;
                    text.color    = _theme.textMainColor;
                    break;
                case UITextRole.Body:
                    text.fontSize = _theme.bodySize;
                    text.color    = _theme.textMainColor;
                    break;
                case UITextRole.Caption:
                    text.fontSize = _theme.captionSize;
                    text.color    = _theme.textWeakColor;
                    break;
            }
        }
    }
}
