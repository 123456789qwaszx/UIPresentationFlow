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

public class UIScreenCatalog : MonoBehaviour
{
    [SerializeField] private GameObject defaultTemplate;
    
    public Dictionary<string, UIScreenSpec> Screens { get; private set; }
    private void Awake()
    {
        LoadScreenSpec(defaultTemplate);
    }

    public void LoadScreenSpec(GameObject templatePrefab)
    {
        Screens = new Dictionary<string, UIScreenSpec>();
        
        Screens["home"] = new UIScreenSpec()
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

        Screens["shop"] = new UIScreenSpec
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
}
