using System;
using TMPro;

// TODO:게임 플레이 UI 표면 층이라, ?.보단 Helper를 추가하는게 좋을 것 같긴한데, 완성하고 다듬을 것.
public sealed class HudPresenter : IHudView
{
    private readonly Func<UIScreen> _getScreen;

    public HudPresenter(Func<UIScreen> getScreen)
    {
        _getScreen = getScreen;
    }

    private const string GoldTextKey = "tag00";
    private const string HpTextKey   = "tag01";
    private const string GemTextKey  = "tag02";

    public void SetGold(int value)
    {
        UIScreen screen = _getScreen();
        WidgetHandle handle = screen?.GetWidgetHandle(GoldTextKey);
        TMP_Text text = handle?.Text;

        if (text != null)
            text.text = value.ToString();
    }

    public void SetHp(int current, int max)
    {
        UIScreen screen = _getScreen();
        WidgetHandle handle = screen?.GetWidgetHandle(HpTextKey);
        TMP_Text text = handle?.Text;

        if (text != null)
            text.text = $"{current}/{max}";
    }
    
    public void SetGem(int value)
    {
        UIScreen screen = _getScreen();
        WidgetHandle handle = screen?.GetWidgetHandle(GemTextKey);
        TMP_Text text = handle?.Text;

        if (text != null)
            text.text = value.ToString();
    }
}