using UnityEngine;

public class WidgetFactory
{
    private readonly GameObject _textPrefab;
    private readonly GameObject _buttonPrefab;
    
    public WidgetFactory(GameObject textPrefab, GameObject buttonPrefab)
    {
        _textPrefab = textPrefab;
        _buttonPrefab = buttonPrefab;
    }
    
    public MonoBehaviour Create(WidgetSpec spec, Transform parent)
    {
        switch (spec.widgetType)
        {
            case "text":
            {
                GameObject go = Object.Instantiate(_textPrefab, parent);
                TextWidget textWidget  = go.GetComponent<TextWidget>();
                textWidget.SetText(spec.text);
                return textWidget;
            }
            case "button":
            {
                GameObject go = Object.Instantiate(_buttonPrefab, parent);
                ButtonWidget buttonWidget  = go.GetComponent<ButtonWidget>();
                buttonWidget.SetLabel(spec.text);
                return buttonWidget;
            }
            default:
                Debug.LogError($"Unknown widgetType: {spec.widgetType}");
                return null;
        }
    }
}
