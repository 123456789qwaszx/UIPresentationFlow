using UnityEngine;

public class UIOpener
{
    private readonly UIRouter _router;
    private readonly IHudView _hud;

    public UIOpener(UIRouter router, IHudView hud)
    {
        _router = router;
        _hud    = hud;
    }

    public void Open(UIActionKey action, object payload = null)
    {
        _router.Navigate(new UIRequest(action, payload));
    }
    
    public void SetGold(int goldValue)
        => _hud.SetGold(goldValue);
    
    public void SetHp(int currentHp, int maxHp)
        => _hud.SetHp(currentHp, maxHp);
    
    public void SetGem(int gemValue)
        => _hud.SetGem(gemValue);
}
