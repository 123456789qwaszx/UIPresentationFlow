using System;
using System.Collections.Generic;
using UnityEngine;

public class UIScreen : MonoBehaviour
{
    private Dictionary<string, RectTransform> _slots;
    private Dictionary<string, WidgetHandle> _widgetsByNameTag;

    public WidgetHandle GetWidgetHandle(string nameTag)
    {
        if (string.IsNullOrWhiteSpace(nameTag))
            return null;

        if (_widgetsByNameTag == null ||
            !_widgetsByNameTag.TryGetValue(nameTag, out WidgetHandle handle) ||
            handle == null)
        {
            Debug.LogWarning($"[UIScreen] WidgetHandle not found for nameTag='{nameTag}'", this);
            return null;
        }

        return handle;
    }
    
    public IEnumerable<WidgetHandle> GetAllWidgets()
    {
        if (_widgetsByNameTag == null)
            yield break;

        foreach (var kv in _widgetsByNameTag)
        {
            if (kv.Value != null)
                yield return kv.Value;
        }
    }

    public RectTransform GetSlot(string slotName)
    {
        if (_slots == null ||
            !_slots.TryGetValue(slotName, out RectTransform slot) ||
            slot == null)
        {
            Debug.LogWarning($"[UIScreen] Slot '{slotName}' not found.", this);
            return null;
        }

        return slot;
    }

    // 루트 슬롯만 템플릿 의존, 자식 슬롯은 Slot 위젯에서 동적 생성
    public void BuildSlotMap(UISlotBinder binder, UIScreenSpec spec)
    {
        if (binder == null || spec == null)
        {
            _slots = new Dictionary<string, RectTransform>(StringComparer.Ordinal);
            return;
        }

        // 🔹 템플릿에 실제로 필요하다고 보는 슬롯들만 추린다 (루트 슬롯들)
        List<string> required = BuildRequiredTemplateSlotIds(spec);

        // 🔹 strict:false → 예외는 절대 안 던지고, 없는 건 그냥 Warn + 무시
        _slots = binder.BindSlots(transform, required, strict: false);
    }

    private static List<string> BuildRequiredTemplateSlotIds(UIScreenSpec spec)
    {
        var required = new List<string>();
        if (spec == null || spec.slots == null)
            return required;

        // 모든 Slot 위젯이 참조하는 slotId 모으기 (자식 슬롯 이름들)
        var childSet = new HashSet<string>(StringComparer.Ordinal);

        foreach (var slot in spec.slots)
        {
            if (slot == null || slot.widgets == null) continue;

            foreach (var w in slot.widgets)
            {
                if (w == null) continue;
                if (w.widgetType != WidgetType.Slot) continue;

                string id = (w.slotId ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(id))
                    childSet.Add(id);
            }
        }

        // SlotSpec 중에서 "어떤 Slot 위젯에서도 slotId로 참조되지 않는 것"만 루트로 간주
        foreach (var slot in spec.slots)
        {
            if (slot == null) continue;

            string name = (slot.slotName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(name)) continue;

            if (childSet.Contains(name))
                continue; // 자식 슬롯 → 템플릿에 없어도 됨

            if (!required.Contains(name))
                required.Add(name);
        }

        return required;
    }

    internal void SetWidgets(Dictionary<string, WidgetHandle> map)
    {
        _widgetsByNameTag = map;
    }

    
    
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
