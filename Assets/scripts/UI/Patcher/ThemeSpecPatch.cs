public enum UITextRole
{
    Title,
    Body,
    Caption,
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

        // 텍스트 전용 패치
        foreach (var widget in screen.GetAllWidgets())
        {
            var text = widget.Text;
            if (text == null)
                continue;

            // 1) 폰트: 지정되어 있으면 통일, 아니면 건드리지 않기
            if (_theme.mainFont != null)
                text.font = _theme.mainFont;

            // 2) 역할별 크기 + 색
            switch (widget.TextRole)
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

            // 3) Alignment / FontStyle 는 여기서 건드리지 않는다 (레이아웃/연출 영역)
        }
    }
}