using System;
using System.Collections.Generic;
using UnityEngine;

public class UIScreen : MonoBehaviour
{
    private Dictionary<string, RectTransform> _slots;

    public void Build(UIBinder binder, UIScreenSpec spec)
    {
        List<string> required = spec.slots.ConvertAll(s => s.slotName);
        _slots = binder.BuildSlots(transform, required);
    }

    public RectTransform GetSlot(string slotName)
    {
        if (!_slots.TryGetValue(slotName, out RectTransform slot))
        {
            Debug.LogWarning($"[UIScreen] Slot '{slotName}' not found.", this);
            return null;
        }

        return slot;
    }
    
    #region 동적 재구성/재배치 용
    
    private Dictionary<string, WidgetHandle> _widgetsByNameTag = new();

    internal void SetWidgets(Dictionary<string, WidgetHandle> map)
        => _widgetsByNameTag = map;

    public WidgetHandle GetWidgetHandle(string nameTag)
    {
        if (string.IsNullOrWhiteSpace(nameTag))
            return null;

        if (!_widgetsByNameTag.TryGetValue(nameTag, out var handle) || handle == null)
        {
            Debug.LogWarning($"[UIScreen] WidgetHandle not found for nameTag='{nameTag}'", this);
            return null;
        }

        return handle;
    }
    #endregion
    
    public void Open() => gameObject.SetActive(true);
    public void Close() => Destroy(gameObject);
    
    
    #region 레거시
    // 핸들을 통하는 대신, 직접 Screen 내부의 특정 위젯을 찾고 싶을 때 사용하면 어떨까 싶어서 남겨둔 레거시 코드.
    // 특정 화면의 커스텀 로직이 필요한 "Presenter/Controller"를 빠르게 만들 때,
    // 혹은 3rd-party / 레거시 UI 코드와의 임시 브리지로 쓰거나,
    // 기획 / 연출 용 미니 스크립트 테스트 시, 빠르게 컴포넌트만 가져오기 위해 사용.
    // * 그렇지만 이것 들도 그냥 "GetWidgetHandle" + "handle.Text"로도 무조건 할 수 있음. *
    //예: var text = screen.GetWidgetDirect<TMP_Text>("GoldText");
    // text.text = gold.ToString();
    
    /// <summary>
    /// Component(TMP_Text, Image 등)를 바로 얻고 싶을 때 사용.
    /// var text = screen.GetWidget<TMP_Text>("ScoreText");
    /// </summary>
    public T GetWidgetDirect<T>(string nameTag) where T : Component
    {
        WidgetHandle handle = GetWidgetHandle(nameTag);
        if (handle == null)
            return null;

        var component = handle.GameObject.GetComponentInChildren<T>(includeInactive: true);
        if (component != null)
            return component;

        Debug.LogWarning(
            $"[UIScreen] Widget '{nameTag}' (GameObject='{handle.GameObject.name}') " +
            $"does not contain component of type {typeof(T).Name}", this);
        return null;
    }
    #endregion
}
