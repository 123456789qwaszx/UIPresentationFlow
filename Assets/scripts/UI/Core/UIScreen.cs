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
}