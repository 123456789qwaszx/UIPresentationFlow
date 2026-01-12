using System.Collections.Generic;
using UnityEngine;

public class UIComposer
{
    private readonly WidgetFactory _factory;
    private readonly WidgetRectApplier _rectApplier;

    public UIComposer(WidgetFactory factory, WidgetRectApplier rectApplier)
    {
        _factory = factory;
        _rectApplier = rectApplier;
    }

    public void Compose(UIScreen screen, UIScreenSpec screenSpec)
    {
        if (screen == null || screenSpec == null)
        {
            Debug.LogWarning("[UIComposer] screen or screenSpec is null");
            return;
        }

        // ---- 1) slotName -> SlotSpec 빠른 lookup 테이블 ----
        var slotLookup = BuildSlotLookup(screenSpec);

        // ---- 2) 전체 WidgetHandle 캐시 (nameTag 기준) ----
        var widgetsByNameTag = new Dictionary<string, WidgetHandle>();

        // ---- 3) UISlot 트리를 BFS로 순회 ----
        var visited = new HashSet<UISlot>();
        var queue   = new Queue<UISlot>();

        // 템플릿 프리팹 안에 이미 존재하는 모든 UISlot을 시작점으로
        foreach (var slot in screen.GetComponentsInChildren<UISlot>(includeInactive: true))
        {
            EnqueueSlot(slot, queue, visited);
        }

        while (queue.Count > 0)
        {
            UISlot slot = queue.Dequeue();
            if (slot == null) continue;

            string slotId = slot.Id; // UISlot.Id 프로퍼티 (Trim 포함)
            if (string.IsNullOrEmpty(slotId))
                continue;

            // 이 슬롯에 대응하는 SlotSpec이 없으면 스킵
            if (!slotLookup.TryGetValue(slotId, out SlotSpec slotSpec) || slotSpec == null)
                continue;

            Transform parent = slot.target != null ? slot.target : slot.transform;

            // 🔹 이 슬롯 안에 들어갈 Widget들을 만든다
            if (slotSpec.widgets != null)
            {
                foreach (var widgetSpec in slotSpec.widgets)
                {
                    if (widgetSpec == null || widgetSpec.disabled)
                        continue;

                    // 실제 Widget 생성
                    WidgetHandle widget = _factory.Create(widgetSpec, parent);
                    if (widget == null) continue;

                    // Rect 설정 (Slot타입은 WidgetFactory 내부에서 처리했다면 스킵 가능)
                    if (widget.RectTransform != null && widgetSpec.widgetType != WidgetType.Slot)
                    {
                        _rectApplier.Apply(widget.RectTransform, widgetSpec);
                    }

                    // nameTag 캐싱
                    string tag = (widgetSpec.nameTag ?? string.Empty).Trim();
                    if (!string.IsNullOrEmpty(tag))
                    {
                        if (!widgetsByNameTag.TryAdd(tag, widget))
                        {
                            Debug.LogWarning($"[UIComposer] Duplicate widget nameTag='{tag}'");
                        }
                    }

                    // 🔹 새로 생성된 Widget 안에 있는 UISlot들도 큐에 추가 (Slot 위젯 포함)
                    foreach (var nestedSlot in widget.GameObject.GetComponentsInChildren<UISlot>(includeInactive: true))
                    {
                        EnqueueSlot(nestedSlot, queue, visited);
                    }
                }
            }
        }

        // 최종적으로 UIScreen에 Widget 맵 전달
        screen.SetWidgets(widgetsByNameTag);
    }

    // slotName -> SlotSpec 맵 구성
    private static Dictionary<string, SlotSpec> BuildSlotLookup(UIScreenSpec screenSpec)
    {
        var dict = new Dictionary<string, SlotSpec>();

        if (screenSpec.slots == null)
            return dict;

        foreach (var slot in screenSpec.slots)
        {
            if (slot == null) continue;
            string name = slot.slotName;
            if (string.IsNullOrWhiteSpace(name)) continue;

            name = name.Trim();
            if (!dict.ContainsKey(name))
                dict.Add(name, slot);
            else
                Debug.LogWarning($"[UIComposer] Duplicate SlotSpec.slotName '{name}'");
        }

        return dict;
    }

    private static void EnqueueSlot(UISlot slot, Queue<UISlot> queue, HashSet<UISlot> visited)
    {
        if (slot == null || visited.Contains(slot))
            return;

        visited.Add(slot);
        queue.Enqueue(slot);
    }
}
