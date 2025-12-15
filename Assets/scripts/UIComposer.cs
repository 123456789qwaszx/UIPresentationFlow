using UnityEngine;

public class UIComposer
{
    private readonly WidgetFactory _widgets;

    public UIComposer(WidgetFactory widgets)
    {
        _widgets = widgets;
    }

    public void Compose(UIScreen screen, UIScreenSpec spec, UIRouter router)
    {
        foreach (var keyValuePair in spec.slotWidgets)
        {
            string slotName = keyValuePair.Key;
            RectTransform slot = screen.GetSlot(slotName);

            for (int i = slot.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(slot.GetChild(i).gameObject);
            }

            foreach (WidgetSpec widgetSpec in keyValuePair.Value)
            {
                MonoBehaviour widget = _widgets.Create(widgetSpec, slot);
            }
        }
    }
}
