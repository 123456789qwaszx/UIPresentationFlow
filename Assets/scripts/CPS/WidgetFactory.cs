using UnityEngine;

public class WidgetFactory
{
    private readonly GameObject _textPrefab;
    private readonly GameObject _buttonPrefab;
    private readonly IUiActionBinder _actionBinder;
    private readonly bool _strict;
    
    public WidgetFactory(GameObject textPrefab, GameObject buttonPrefab, IUiActionBinder actionBinder, bool strictMode = false)
    {
        _textPrefab  = textPrefab;
        _buttonPrefab = buttonPrefab;
        _actionBinder = actionBinder;
        _strict  = strictMode;
    }
    
    public WidgetHandle Create(WidgetSpec spec, Transform parent)
    {
        GameObject prefab = ResolvePrefab(spec);
        GameObject go = Object.Instantiate(prefab, parent);
        
        var handle = new WidgetHandle(spec.widgetType, spec.nameTag, go);
        
        if (!string.IsNullOrEmpty(spec.text))
        {
            if (handle.Text != null)
                handle.Text.text = spec.text;
        }
        
        if (!string.IsNullOrEmpty(spec.onClickRoute))
        {
            BindActionIfNeeded(spec, handle);
        }

        return handle;
        
        //
        // switch (spec.widgetType)
        // {
        //     case WidgetType.Text:
        //     {
        //         TextWidget textWidget = ResolveWidgetComponent<TextWidget>(go, prefab, spec);
        //         textWidget.SetText(spec.text);
        //         return textWidget;
        //     }
        //     case WidgetType.Button:
        //     {
        //         ButtonWidget buttonWidget = ResolveWidgetComponent<ButtonWidget>(go, prefab, spec);
        //         buttonWidget.SetLabel(spec.text);
        //         BindActionIfNeeded(spec, buttonWidget);
        //         return buttonWidget;
        //     }
        //     default:
        //         Debug.LogError($"[WidgetFactory] Unknown widgetType: {spec.widgetType}");
        //         return null;
        // }
    }
    
    
    private void BindActionIfNeeded(WidgetSpec spec, WidgetHandle widget)
    {
        UIActionKey key = UIActionKeyRegistry.Get(spec.onClickRoute);
        _actionBinder.TryBind(widget, key);
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
