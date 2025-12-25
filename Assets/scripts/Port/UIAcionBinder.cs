using System;
using UnityEngine;

public sealed class UiActionBinder : IUiActionBinder
{
    private readonly Func<IHudView> _getHudView;

    public UiActionBinder(Func<IHudView> getHudView)
    {
        _getHudView = getHudView;
    }

    public void Bind(ButtonWidget button, string route)
    {
        switch (route)
        {
            case "ui/gold":
                Debug.Log("Connect ui/gold");
                button.SetOnClick(() =>
                {
                    _getHudView()?.SetGold(444);
                });
                break;
            
            case "ui/hp":
                Debug.Log("Connect ui/hp");
                button.SetOnClick(() =>
                {
                    _getHudView()?.SetHp(555, 666);
                });
                break;
            
            case "ui/gem":
                Debug.Log("Connect ui/gem");
                button.SetOnClick(() =>
                {
                    _getHudView()?.SetGem(777);
                });
                break;

            default:
                Debug.LogWarning($"[UiActionBinder] Unknown ui action route='{route}'");
                break;
        }
    }
}