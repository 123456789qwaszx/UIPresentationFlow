using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;


public class TextWidget : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    public void SetText(string labelText) => label.text = labelText;
}

public class ButtonWidget : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    public void SetLabel(string labelText) => label.text = labelText;
    public void SetOnClick(Action action)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => action?.Invoke());
    }
}

public class WidgetFactory : MonoBehaviour
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
                GameObject go = Instantiate(_textPrefab, parent);
                TextWidget textWidget  = go.GetComponent<TextWidget>();
                textWidget.SetText(spec.text);
                return textWidget;
            }
            case "button":
            {
                GameObject go = Instantiate(_buttonPrefab, parent);
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
