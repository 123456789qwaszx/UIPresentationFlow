using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class UIActionBinder : IUiActionBinder
{
    private readonly Func<IHudView> _getHudView;
    private readonly Dictionary<UIActionKey, Action<WidgetHandle>> _bindings;

    public UIActionBinder(Func<IHudView> getHudView)
    {
        _getHudView = getHudView;

        _bindings = new Dictionary<UIActionKey, Action<WidgetHandle>>
        {
            {
                DefaultActionKeys.Gold,
                widget =>
                {
                    if (widget.Button == null)
                    {
                        Debug.LogWarning(
                            $"[UIActionBinder] actionKey={DefaultActionKeys.Gold} " +
                            $"but widget '{widget.NameTag}' has no Button component.");
                        return;
                    }

                    void OnClick()
                    {
                        _getHudView()?.SetGold(444);
                    }

                    widget.Button.onClick.RemoveAllListeners();
                    widget.Button.onClick.AddListener(OnClick);
                }
            },
            {
                DefaultActionKeys.Hp,
                widget =>
                {
                    if (widget.Button == null)
                    {
                        Debug.LogWarning(
                            $"[UIActionBinder] actionKey={DefaultActionKeys.Hp} " +
                            $"but widget '{widget.NameTag}' has no Button component.");
                        return;
                    }

                    void OnClick()
                    {
                        _getHudView()?.SetHp(555, 666);
                    }

                    widget.Button.onClick.RemoveAllListeners();
                    widget.Button.onClick.AddListener(OnClick);
                }
            },
            {
                DefaultActionKeys.Gem,
                widget =>
                {
                    if (widget.Button == null)
                    {
                        Debug.LogWarning(
                            $"[UIActionBinder] actionKey={DefaultActionKeys.Gem} " +
                            $"but widget '{widget.NameTag}' has no Button component.");
                        return;
                    }

                    void OnClick()
                    {
                        _getHudView()?.SetGem(777);
                    }

                    widget.Button.onClick.RemoveAllListeners();
                    widget.Button.onClick.AddListener(OnClick);
                }
            },
        };
    }

    // 게임 기능을 엮기위한 Bind
    public bool TryBind(WidgetHandle widget, UIActionKey actionKey)
    {
        if (widget == null)
            return false;

        if (actionKey == UIActionKey.None)
            return false;
        
        if (!_bindings.TryGetValue(actionKey, out Action<WidgetHandle> bindingRule))
            return false;

        // 여기서 한 번 더 버튼 존재 체크 (안전장치)
        if (widget.Button == null)
        {
            Debug.LogWarning(
                $"[UIActionBinder] Widget '{widget.NameTag}' has actionKey={actionKey} but no Button component.");
            return false;
        }

        bindingRule(widget);
        return true;
    }
}