using UnityEngine;
using Object = UnityEngine.Object;

public class WidgetFactory
{
    private readonly GameObject _textPrefab;
    private readonly GameObject _buttonPrefab;
    private readonly GameObject _imagePrefab;
    private readonly GameObject _togglePrefab;
    private readonly GameObject _sliderPrefab;
    private readonly GameObject _gameObjectPrefab;

    private readonly IUiActionBinder _actionBinder;
    private readonly bool _strict;
    
    public WidgetFactory(
        GameObject textPrefab,
        GameObject buttonPrefab,
        GameObject imagePrefab,
        GameObject togglePrefab,
        GameObject sliderPrefab,
        GameObject gameObjectPrefab,
        IUiActionBinder actionBinder,
        bool strictMode = false)
    {
        _textPrefab       = textPrefab;
        _buttonPrefab     = buttonPrefab;
        _imagePrefab      = imagePrefab;
        _togglePrefab     = togglePrefab;
        _sliderPrefab     = sliderPrefab;
        _gameObjectPrefab = gameObjectPrefab;

        _actionBinder = actionBinder;
        _strict       = strictMode;
    }
    
    public WidgetHandle Create(WidgetSpec spec, Transform parent)
    {
        GameObject go = Object.Instantiate(ResolvePrefab(spec), parent);
        if(_strict) go.gameObject.name = spec.nameTag;
        
        var handle = new WidgetHandle(spec.widgetType, spec.nameTag, go);
        
        // ---- 공통 텍스트 세팅 (Text / Button / Toggle 라벨 등) ----
        if (!string.IsNullOrEmpty(spec.text) && handle.Text != null)
        {
            handle.Text.text = spec.text;
        }
        
        // ---- Image 옵션 적용 ----
        if (handle.Image != null)
        {
            if (spec.imageSprite != null)
                handle.Image.sprite = spec.imageSprite;

            handle.Image.color = spec.imageColor;

            if (spec.imageSetNativeSize)
                handle.Image.SetNativeSize();
        }
        
        // ---- Toggle 옵션 적용 ----
        if (handle.Toggle != null)
        {
            handle.Toggle.isOn        = spec.toggleInitialValue;
            handle.Toggle.interactable = spec.toggleInteractable;
        }
        
        // ---- Slider 옵션 적용 ----
        if (handle.Slider != null)
        {
            handle.Slider.minValue     = spec.sliderMin;
            handle.Slider.maxValue     = spec.sliderMax;
            handle.Slider.wholeNumbers = spec.sliderWholeNumbers;

            float v = spec.sliderInitialValue;
            if (spec.sliderMin < spec.sliderMax)
                v = Mathf.Clamp(v, spec.sliderMin, spec.sliderMax);

            handle.Slider.value = v;
        }
        
        // ---- 액션 바인딩 ----
        if (!string.IsNullOrEmpty(spec.onClickRoute))
        {
            BindActionIfNeeded(spec, handle);
        }

        return handle;
    }
    
    
    private void BindActionIfNeeded(WidgetSpec spec, WidgetHandle widget)
    {
        UIActionKey key = UIActionKeyRegistry.Get(spec.onClickRoute);
        _actionBinder?.TryBind(widget, key);
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
            case WidgetType.Image:
                return _imagePrefab;
            case WidgetType.Toggle:
                return _togglePrefab;
            case WidgetType.Slider:
                return _sliderPrefab;
            case WidgetType.GameObject:
                return _gameObjectPrefab;
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
