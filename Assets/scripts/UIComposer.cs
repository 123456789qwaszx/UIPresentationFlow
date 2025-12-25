using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class UIComposer
{
    private readonly WidgetFactory _factory;
    private readonly IUiActionBinder _actionBinder;

    public UIComposer(WidgetFactory factory, IUiActionBinder actionBinder)
    {
        _factory = factory;
        _actionBinder = actionBinder;
    }

    public void Compose(UIScreen screen, UIScreenSpec screenSpec, UIRouter router)
    {
        var widgetMap = new Dictionary<string, MonoBehaviour>();
        
        foreach (SlotSpec slotSpec in screenSpec.slots)
        {
            RectTransform slot = screen.GetSlot(slotSpec.slotName);
            DestroyChildren(slot);

            foreach (WidgetSpec widgetSpec in slotSpec.widgets)
            {
                MonoBehaviour widget = _factory.Create(widgetSpec, slot);
                
                // 위치 조정
                if (widgetSpec.rectMode == WidgetRectMode.OverrideInSlot)
                {
                    ApplyRectFromSpec((RectTransform)widget.transform, widgetSpec);
                }
                
                // 버튼 연결
                BindButtonIfNeeded(widgetSpec, widget as ButtonWidget, router);

                // 위젯 캐싱
                string tag = (widgetSpec.nameTag ?? string.Empty).Trim();
                if (!widgetMap.TryAdd(tag, widget))
                {
                    Debug.LogWarning($"[UIComposer] Duplicate widget nameTag='{tag}'");
                }
            }
        }
        
        screen.SetWidgets(widgetMap);
    }
    
    private void BindButtonIfNeeded(WidgetSpec spec, ButtonWidget button, UIRouter router)
    {
        if (button == null) return;
        if (string.IsNullOrEmpty(spec.onClickRoute)) return;

        string route = spec.onClickRoute;

        if (route.StartsWith("ui/", StringComparison.Ordinal))
        {
            // UI/게임 로직용 route → 포트에 위임
            _actionBinder?.Bind(button, route);
        }
        else
        {
            // 나머지는 기존처럼 UI 라우팅
            button.SetOnClick(() =>
            {
                router.Navigate(new UIRequest(route));
            });
        }
    }
    
    private void DestroyChildren(RectTransform slot)
    {
        for (int i = slot.childCount - 1; i >= 0; i--)
            Object.Destroy(slot.GetChild(i).gameObject);
    }
    
    private void ApplyRectFromSpec(RectTransform rect, WidgetSpec spec)
    {
        rect.anchorMin = spec.anchorMin;
        rect.anchorMax = spec.anchorMax;
        rect.pivot     = spec.pivot;
        rect.anchoredPosition = spec.anchoredPosition;
        rect.sizeDelta        = spec.sizeDelta;
    }
}
