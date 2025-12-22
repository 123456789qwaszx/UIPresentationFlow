using UnityEngine;

public class WidgetFactory
{
    private readonly GameObject _textPrefab;
    private readonly GameObject _buttonPrefab;
    private readonly bool _strict;
    
    public WidgetFactory(GameObject textPrefab, GameObject buttonPrefab, bool strictMode = false)
    {
        _textPrefab  = textPrefab;
        _buttonPrefab = buttonPrefab;
        _strict  = strictMode;
    }
    
    public MonoBehaviour Create(WidgetSpec spec, Transform parent)
    {
        GameObject prefab = ResolvePrefab(spec);
        GameObject go = Object.Instantiate(prefab, parent);
        
        switch (spec.widgetType)
        {
            case WidgetType.Text:
            {
                TextWidget textWidget = ResolveWidgetComponent<TextWidget>(go, prefab, spec);
                if (textWidget == null)
                    return null;

                textWidget.SetText(spec.text);
                return textWidget;
            }
            case WidgetType.Button:
            {
                ButtonWidget buttonWidget = ResolveWidgetComponent<ButtonWidget>(go, prefab, spec);
                if (buttonWidget == null)
                    return null;

                buttonWidget.SetLabel(spec.text);
                //TODO buttonWidget.SetOnClick 는 나중에 Router랑 연결
                return buttonWidget;
            }
            default:
                Debug.LogError($"[WidgetFactory] Unknown widgetType: {spec.widgetType}");
                return null;
        }
    }
    
    private GameObject ResolvePrefab(WidgetSpec spec)
    {
        if (spec.prefabOverride != null)
            return spec.prefabOverride;

        switch (spec.widgetType)
        {
            case WidgetType.Text:
                return _textPrefab;
            case WidgetType.Button:
                return _buttonPrefab;
            default:
                Debug.LogError($"[WidgetFactory] Unsupported widgetType for prefab resolution: {spec.widgetType}");
                return null;
        }
    }

    
    private T ResolveWidgetComponent<T>(GameObject go, GameObject prefab, WidgetSpec spec)
        where T : MonoBehaviour
    {
        T widget = go.GetComponent<T>();
        if (widget != null)
            return widget;

        if (_strict)
        {
            Debug.LogError(
                $"[WidgetFactory] (STRICT) Prefab '{prefab.name}' " +
                $"must have {typeof(T).Name} for widgetType={spec.widgetType}.");
            return null;
        }
        
        widget = go.GetComponentInChildren<T>(includeInactive: true);
        if (widget != null)
        {
            Debug.LogWarning(
                $"[WidgetFactory] Auto-bound {typeof(T).Name} from children of instance '{go.name}'. " +
                "Consider wiring it directly on the prefab for better control.");
            return widget;
        }

        widget = go.AddComponent<T>();
        Debug.LogWarning(
            $"[WidgetFactory] Prefab '{(prefab != null ? prefab.name : "null")}' has no {typeof(T).Name} " +
            $"for widgetType={spec.widgetType}. " +
            $"Auto-added {typeof(T).Name} on instance '{go.name}'. " +
            "Consider adding it to the prefab for better performance.");

        return widget;
    }
}
