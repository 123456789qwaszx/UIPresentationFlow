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

            // 2) Panels (필요하면 유지)
            var images = screen.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.name.Contains("Panel"))
                    img.color = _theme.panelColor;
            }

            // 3) Text : WidgetHandle + TextRole 기반
            foreach (var widget in screen.GetAllWidgets())
            {
                if (widget.Text == null)
                    continue;

                widget.Text.font = _theme.mainFont;

                switch (widget.TextRole)
                {
                    case UITextRole.Title:
                        widget.Text.fontSize = _theme.titleSize;
                        widget.Text.color    = _theme.textMainColor;
                        break;
                    case UITextRole.Body:
                        widget.Text.fontSize = _theme.bodySize;
                        widget.Text.color    = _theme.textMainColor;
                        break;
                    case UITextRole.Caption:
                        widget.Text.fontSize = _theme.captionSize;
                        widget.Text.color    = _theme.textWeakColor;
                        break;
                }
            }
        }
    }