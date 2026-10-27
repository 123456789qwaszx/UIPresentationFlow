using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Panel이 열린 동안의 입력.
//
// Root 막기: Panel이 하나라도 있으면 지금 Root의 CanvasGroup.interactable을 끈다 — 가려진 Panel과 같은 규칙.
// 그러지 않으면 딤이 포인터는 막아도 방향키·Enter·패드가 아래 Root의 버튼에 닿는다.
// blocksRaycasts와 투명도는 건드리지 않는다. 딤 없는 Panel 바깥을 누른 포인터가 Root를 지나 아래(게임 화면)로
// 빠지지 않게 하려는 것이다. Root의 버튼은 Selectable.IsInteractable()이 부모 CanvasGroup을 따르므로 눌리지 않는다
// (ButtonFeedback도 클릭 전에 이것을 본다). 쌓기 · 꺼내기 · Root 바꾸기 때마다 맞추므로, 앱이 Root를 따로 막으려면
// 그 뒤에 다시 막는다. 아래 화면도 계속 조작되어야 하는 창은 Panel이 아니라 Overlay에 둔다.
//
// 포커스 되돌리기: Panel을 쌓을 때 그 순간의 선택(EventSystem)을 기억했다가, 꺼낼 때 그것이 아직 보이고
// 막혀 있지 않으면 되돌린다. 전부 꺼내면 맨 처음 Panel 전의 선택으로, 아래 Panel로 돌아가면(PushPanel의 되돌아가기)
// 그 Panel 안의 선택으로. 되돌릴 수 없으면(사라졌거나 막혔으면) 닫힌 Panel 안의 것이 선택으로 남지 않게만 비운다 —
// 대신 무엇을 고를지는 앱이 정한다.
public sealed partial class UIManager
{
    // Panel → 그 Panel을 쌓던 순간의 선택.
    private readonly Dictionary<UIBase, GameObject> _selectionBeforePanel = new();

    private bool _blockRootUnderPanels = true;

    // Panel이 열린 동안 Root를 막는다(CanvasGroup.interactable). 기본 켬.
    public bool BlockRootUnderPanels
    {
        get => _blockRootUnderPanels;
        set
        {
            _blockRootUnderPanels = value;
            ApplyRootInputState();
        }
    }

    private void ApplyRootInputState()
    {
        if (CurrentRoot == null)
            return;

        bool blocked = _blockRootUnderPanels && HasPanel;

        if (!CurrentRoot.TryGetComponent(out CanvasGroup group))
        {
            if (!blocked)
                return;

            group = CurrentRoot.gameObject.AddComponent<CanvasGroup>();
        }

        group.interactable = !blocked;
    }

    private void RememberSelection(UIBase panel)
    {
        EventSystem eventSystem = EventSystem.current;
        _selectionBeforePanel[panel] = eventSystem != null ? eventSystem.currentSelectedGameObject : null;
    }

    private GameObject TakeSelection(UIBase panel)
    {
        if (panel == null || !_selectionBeforePanel.TryGetValue(panel, out GameObject selection))
            return null;

        _selectionBeforePanel.Remove(panel);
        return selection;
    }

    // Root 막기를 맞춘 뒤에 부른다 — 막힌 동안에는 Selectable이 되돌릴 수 없게 보인다.
    private static void RestoreSelection(GameObject selection)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return;

        bool usable = selection != null
                      && selection.activeInHierarchy
                      && (!selection.TryGetComponent(out Selectable selectable) || selectable.IsInteractable());

        if (usable)
        {
            eventSystem.SetSelectedGameObject(selection);
            return;
        }

        // 닫힌 Panel 안의 것이 선택된 채로 남으면 마우스를 쓰기 전까지 키 입력이 먹지 않는다.
        GameObject current = eventSystem.currentSelectedGameObject;
        if (current != null && !current.activeInHierarchy)
            eventSystem.SetSelectedGameObject(null);
    }
}
