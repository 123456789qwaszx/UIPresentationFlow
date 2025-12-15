using System.Collections.Generic;
using UnityEngine;

public static class UIScreenCatalogDefaults
{
    /// <summary>
    /// 런타임/에디터 어디서든 호출해서
    /// 카탈로그에 기본 화면 스펙을 채워 넣는 헬퍼.
    /// 실제 에셋에 저장하려면 에디터에서 호출해야 함.
    /// </summary>
    public static void FillWithDefaultScreens(
        UIScreenCatalog catalog,
        GameObject templatePrefab)
    {
        if (catalog == null)
        {
            Debug.LogError("Catalog is null.");
            return;
        }

        catalog.entries.Clear();

        // Home
        var homeSpec = new UIScreenSpec
        {
            name = "Home",
            templatePrefab = templatePrefab,
            slots = new List<SlotSpec>
            {
                new SlotSpec
                {
                    slotName = "Header",
                    widgets = new List<WidgetSpec>
                    {
                        new WidgetSpec
                        {
                            widgetType = WidgetType.Text,
                            text = "HOME"
                        }
                    }
                },
                new SlotSpec
                {
                    slotName = "Body",
                    widgets = new List<WidgetSpec>
                    {
                        new WidgetSpec
                        {
                            widgetType = WidgetType.Text,
                            text = "Welcome!"
                        }
                    }
                },
                new SlotSpec
                {
                    slotName = "Footer",
                    widgets = new List<WidgetSpec>
                    {
                        new WidgetSpec
                        {
                            widgetType = WidgetType.Button,
                            text = "Go Shop",
                            onClickRoute = "shop"
                        }
                    }
                }
            }
        };

        catalog.entries.Add(new UIScreenCatalog.Entry
        {
            key = ScreenKey.Home,
            spec = homeSpec
        });

        // Shop
        var shopSpec = new UIScreenSpec
        {
            name = "Shop",
            templatePrefab = templatePrefab,
            slots = new List<SlotSpec>
            {
                new SlotSpec
                {
                    slotName = "Header",
                    widgets = new List<WidgetSpec>
                    {
                        new WidgetSpec
                        {
                            widgetType = WidgetType.Text,
                            text = "SHOP"
                        }
                    }
                },
                new SlotSpec
                {
                    slotName = "Body",
                    widgets = new List<WidgetSpec>
                    {
                        new WidgetSpec
                        {
                            widgetType = WidgetType.Text,
                            text = "Buy something..."
                        }
                    }
                },
                new SlotSpec
                {
                    slotName = "Footer",
                    widgets = new List<WidgetSpec>
                    {
                        new WidgetSpec
                        {
                            widgetType = WidgetType.Button,
                            text = "Back Home",
                            onClickRoute = "home"
                        }
                    }
                }
            }
        };

        catalog.entries.Add(new UIScreenCatalog.Entry
        {
            key = ScreenKey.Shop,
            spec = shopSpec
        });

        // 캐시 재빌드
        typeof(UIScreenCatalog)
            .GetMethod("BuildCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(catalog, null);
    }
}
