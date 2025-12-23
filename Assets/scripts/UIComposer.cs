using UnityEngine;

public class UIComposer
{
    private readonly WidgetFactory _factory;

    public UIComposer(WidgetFactory factory)
    {
        _factory = factory;
    }

    public void Compose(UIScreen screen, UIScreenSpec screenSpec, UIRouter router)
    {
        foreach (SlotSpec slotSpec in screenSpec.slots)
        {
            RectTransform slot = screen.GetSlot(slotSpec.slotName);

            DestroyChildren(slot);

            foreach (WidgetSpec widgetSpec in slotSpec.widgets)
            {
                MonoBehaviour widget = _factory.Create(widgetSpec, slot);
                
                if (widgetSpec.rectMode == WidgetRectMode.OverrideInSlot)
                {
                    ApplyRectFromSpec((RectTransform)widget.transform, widgetSpec);
                }
            }
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
