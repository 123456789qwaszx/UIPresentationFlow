using System;

public sealed class HudPresenter : IHudView
{
    private readonly Func<UIScreen> _getScreen;

    public HudPresenter(Func<UIScreen> getScreen)
    {
        _getScreen = getScreen;
    }

    private const string GoldTextKey = "tag00";
    private const string HpTextKey = "tag01";
    private const string GemTextKey = "tag02";

    public void SetGold(int value)
    {
        TextWidget text = _getScreen()?.GetWidget<TextWidget>(GoldTextKey);
        if (text) text.SetText(value.ToString());
    }

    public void SetHp(int current, int max)
    {
        TextWidget text = _getScreen()?.GetWidget<TextWidget>(HpTextKey);
        if (text) text.SetText($"{current}/{max}");
    }
    
    public void SetGem(int value)
    {
        TextWidget text = _getScreen()?.GetWidget<TextWidget>(GemTextKey);
        if (text) text.SetText(value.ToString());
    }
}