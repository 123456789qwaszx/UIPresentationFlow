using System;
using System.Collections.Generic;

public sealed class UIActionBinder : IUiActionBinder
{
    private readonly Func<IHudView> _getHudView;
    private readonly Dictionary<UIActionKey, Action<ButtonWidget>> _bindings;

    public UIActionBinder(Func<IHudView> getHudView)
    {
        _getHudView = getHudView;

        _bindings = new Dictionary<UIActionKey, Action<ButtonWidget>>
        {
            {
                UIActionKeys.Gold,
                buttonWidget =>
                {
                    void OnClick()
                    {
                        _getHudView()?.SetGold(444);
                    }

                    buttonWidget.SetOnClick(OnClick);
                }
            },
            {
                UIActionKeys.Hp,
                buttonWidget =>
                {
                    void OnClick()
                    {
                        _getHudView()?.SetHp(555, 666);
                    }
                    
                    buttonWidget.SetOnClick(OnClick);
                }
            },
            {
                UIActionKeys.Gem,
                buttonWidget =>
                {
                    void OnClick()
                    {
                        _getHudView()?.SetGem(777);
                    }
                    
                    buttonWidget.SetOnClick(OnClick);
                }
            },
        };
    }

    // 게임 기능을 엮기위한 Bind
    public bool TryBind(ButtonWidget sourceButton, UIActionKey actionKey)
    {
        if (actionKey == UIActionKey.None)
            return false;
        
        if(!_bindings.TryGetValue(actionKey, out Action<ButtonWidget> bindingRule))
            return false;

        bindingRule(sourceButton);
        return true;
    }
}