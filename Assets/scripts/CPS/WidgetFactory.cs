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
    private readonly GameObject _slotPrefab;

    private readonly IUiActionBinder _actionBinder;
    private readonly bool _strict;
    
    public WidgetFactory(
        GameObject textPrefab,
        GameObject buttonPrefab,
        GameObject imagePrefab,
        GameObject togglePrefab,
        GameObject sliderPrefab,
        GameObject gameObjectPrefab,
        GameObject slotPrefab,
        IUiActionBinder actionBinder,
        bool strictMode = false
    )
    {
        _textPrefab       = textPrefab;
        _buttonPrefab     = buttonPrefab;
        _imagePrefab      = imagePrefab;
        _togglePrefab     = togglePrefab;
        _sliderPrefab     = sliderPrefab;
        _gameObjectPrefab = gameObjectPrefab;
        _slotPrefab       = slotPrefab;

        _actionBinder = actionBinder;
        _strict       = strictMode;
    }
    
    public WidgetHandle Create(WidgetSpec spec, Transform parent)
    {
        GameObject prefab = ResolvePrefab(spec);
        if (prefab == null)
        {
            Debug.LogError($"[WidgetFactory] Prefab is null for widgetType={spec.widgetType}, nameTag='{spec.nameTag}'");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, parent);

        if (_strict && !string.IsNullOrEmpty(spec.nameTag))
            go.name = spec.nameTag;

        // ---- Slot 위젯인 경우: UISlot 생성/보정 ----
        if (spec.widgetType == WidgetType.Slot)
        {
            // RectTransform 확보
            var rt = go.GetComponent<RectTransform>();
            if (rt == null)
                rt = go.AddComponent<RectTransform>();

            // UISlot 확보
            var slot = go.GetComponent<UISlot>();
            if (slot == null)
                slot = go.AddComponent<UISlot>();

            // Slot id는 일단 nameTag를 기준으로 사용 (nameTag가 비어있으면 GameObject 이름)
            string id = !string.IsNullOrWhiteSpace(spec.slotId)
                ? spec.slotId.Trim()
                : go.name;

            slot.id = id;

            // target 비어있으면 자기 RectTransform 할당
            if (slot.target == null)
                slot.target = rt;

            // RectMode OverrideInSlot 일 때만 값 적용
            ApplyRectOverrideIfNeeded(rt, spec);

            // Slot은 실제로 Text/Image/버튼이 아니니까, 나머지 옵션은 건너뛰고 핸들만 반환
            var slotHandle = new WidgetHandle(spec.widgetType, spec.nameTag, go);
            return slotHandle;
        }

        // ---- 일반 위젯 핸들 생성 ----
        var handle = new WidgetHandle(spec.widgetType, spec.nameTag, go);

        // RectMode가 OverrideInSlot이면 RectTransform 보정
        var rectTransform = go.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            ApplyRectOverrideIfNeeded(rectTransform, spec);
        }

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
            handle.Toggle.isOn         = spec.toggleInitialValue;
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

            case WidgetType.Slot:
                // Slot 전용 프리팹이 있으면 사용, 없으면 GameObject 프리팹으로 대체
                return _slotPrefab != null ? _slotPrefab : _gameObjectPrefab;

            default:
                Debug.LogError($"[WidgetFactory] Unsupported widgetType for prefab resolution: {spec.widgetType}");
                return null;
        }
    }

    // rectMode == OverrideInSlot 일 때만 anchor/size/position 적용
    private static void ApplyRectOverrideIfNeeded(RectTransform rt, WidgetSpec spec)
    {
        if (rt == null)
            return;

        if (spec.rectMode != WidgetRectMode.OverrideInSlot)
            return;

        rt.anchorMin = spec.anchorMin;
        rt.anchorMax = spec.anchorMax;
        rt.pivot     = spec.pivot;
        rt.anchoredPosition = spec.anchoredPosition;
        rt.sizeDelta = spec.sizeDelta;
    }
}
