using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class UIComposer
{
    private readonly WidgetFactory _factory;
    private readonly WidgetRectApplier _rectApplier;

    public UIComposer(WidgetFactory factory, WidgetRectApplier rectApplier)
    {
        _factory = factory;
        _rectApplier = rectApplier;
    }

    public void Compose(UIScreen screen, UIScreenSpec screenSpec)
    {
        var widgetMap = new Dictionary<string, WidgetHandle>();
        
        foreach (SlotSpec slotSpec in screenSpec.slots)
        {
            RectTransform slot = screen.GetSlot(slotSpec.slotName);
            DestroyChildren(slot);

            foreach (WidgetSpec widgetSpec in slotSpec.widgets)
            {
                WidgetHandle widget = _factory.Create(widgetSpec, slot);
                
                _rectApplier.Apply(widget.RectTransform, widgetSpec);
                
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
    
    private void DestroyChildren(RectTransform slot)
    {
        for (int i = slot.childCount - 1; i >= 0; i--)
            Object.Destroy(slot.GetChild(i).gameObject);
    }
}
