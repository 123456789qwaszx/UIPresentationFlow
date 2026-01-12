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

    
    
   #region 프리팹전용 우회로
    // nameTag -> Transform (해당 nameTag를 가진 GameObject의 루트)
    private Dictionary<string, Transform> _directWidgetCache;

    /// <summary>
    /// Component(TMP_Text, Image 등)를 바로 얻고 싶을 때 사용. 주로 Rig프리팹을 UI에 오버라이드 했을 경우 사용.
    /// 정식 위젯(WidgetHandle) 시스템과는 완전히 별개의 우회로이며,
    /// UIScreen 트리에서 nameTag와 동일한 GameObject.name을 가진 노드를
    /// 한 번 찾아 캐싱한 뒤, 그 밑에서 T 컴포넌트를 찾는다.
    /// </summary>
    public T GetWidgetDirect<T>(string nameTag) where T : Component
    {
        if (string.IsNullOrWhiteSpace(nameTag))
            return null;

        // 1) 우회 전용 캐시에서 먼저 시도
        if (_directWidgetCache != null &&
            _directWidgetCache.TryGetValue(nameTag, out Transform cachedRoot) &&
            cachedRoot != null)
        {
            var cachedComponent = cachedRoot.GetComponentInChildren<T>(includeInactive: true);
            if (cachedComponent != null)
                return cachedComponent;

            Debug.LogWarning(
                $"[UIScreen] GetWidgetDirect<{typeof(T).Name}>: cached GameObject='{cachedRoot.name}' " +
                $"does not contain component of type {typeof(T).Name} (nameTag='{nameTag}').", this);
            return null;
        }

        // 2) 캐시에 없으면 트리 전체를 돌며 GameObject.name 으로 한 번만 탐색
        Transform found = FindChildByName(transform, nameTag);
        if (found == null)
        {
            Debug.LogWarning(
                $"[UIScreen] GetWidgetDirect<{typeof(T).Name}>: GameObject with nameTag='{nameTag}' not found.",
                this);
            return null;
        }

        // 캐시에 저장 (다음 호출부터는 트리 탐색 생략)
        _directWidgetCache ??= new Dictionary<string, Transform>(StringComparer.Ordinal);
        _directWidgetCache[nameTag] = found;

        var comp = found.GetComponentInChildren<T>(includeInactive: true);
        if (comp != null)
            return comp;

        Debug.LogWarning(
            $"[UIScreen] GetWidgetDirect<{typeof(T).Name}>: GameObject='{found.name}' " +
            $"does not contain component of type {typeof(T).Name} (nameTag='{nameTag}').", this);
        return null;
    }

    /// <summary>
    /// Transform 트리 전체를 돌면서 이름으로 찾는 간단한 DFS 유틸.
    /// GetWidgetDirect 전용 우회로이며, GetWidgetHandle에는 영향을 주지 않는다.
    /// </summary>
    private static Transform FindChildByName(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            var found = FindChildByName(child, name);
            if (found != null)
                return found;
        }

        return null;
    }
    #endregion
}