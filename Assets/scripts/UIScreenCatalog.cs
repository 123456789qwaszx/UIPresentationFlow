using System.Collections.Generic;
using UnityEngine;

public class UIScreenSpec
{
    public string name;
    public GameObject templatePrefab;
    public Dictionary<string, List<WidgetSpec>> slotWidgets = new();
}

public sealed class WidgetSpec
{
    public string widgetType;   // "text" or "button"
    public string text;         // label
    public string onClickRoute; // 버튼이면 라우트
}

public enum ScreenKey { Home, Shop }

public class UIScreenCatalog : MonoBehaviour
{
    [SerializeField] private GameObject defaultTemplatePrefab;

    private readonly Dictionary<ScreenKey, UIScreenSpec> _screenSpecsByKey = new();
    private void Awake()
    {
        RegisterScreenSpecs(defaultTemplatePrefab);
    }

    private void RegisterScreenSpecs(GameObject templatePrefab)
    {
        _screenSpecsByKey[ScreenKey.Home] = new UIScreenSpec()
        {
            name = "Home",
            templatePrefab = templatePrefab,
            slotWidgets = new Dictionary<string, List<WidgetSpec>>()
            {
                ["Header"] = new () { new WidgetSpec { widgetType="text", text="HOME" } },
                ["Body"]   = new() { new WidgetSpec { widgetType="text", text="Welcome!" } },
                ["Footer"] = new() { new WidgetSpec { widgetType="button", text="Go Shop", onClickRoute="shop" } },
            }
        };

        _screenSpecsByKey[ScreenKey.Shop] = new UIScreenSpec
        {
            name = "Shop",
            templatePrefab = templatePrefab,
            slotWidgets = new Dictionary<string, List<WidgetSpec>>()
            {
                ["Header"] = new() { new WidgetSpec { widgetType="text", text="SHOP" } },
                ["Body"]   = new() { new WidgetSpec { widgetType="text", text="Buy something..." } },
                ["Footer"] = new() { new WidgetSpec { widgetType="button", text="Back Home", onClickRoute="home" } },
            }
        };
    }
    
    public UIScreenSpec GetScreenSpec(ScreenKey key)
    {
        return _screenSpecsByKey[key];
    }
}
