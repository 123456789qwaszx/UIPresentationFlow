using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ETouchEvent
{
    PointerUp,
    PointerDown,
    Click,
    LongPressed,
    BeginDrag,
    Drag,
    EndDrag,
    PointerEnter,
    PointerExit,
}

// Adapts Unity EventSystem callbacks into consistent UI interactions.
// Drag and long press suppress click, and starting a drag cancels long press.
//
// Drag confirmation follows EventSystem.pixelDragThreshold.
//
// Pointer enter and exit are always paired: exit is sent at most once per enter,
// and a pointer that is still inside when this object is disabled receives exit then.
//
// Click is not sent while a Selectable on the same object is not interactable
// (Selectable.IsInteractable, which also honors parent CanvasGroups). Other events still arrive.
//
// A drag that nobody here listens to (no BeginDrag/Drag/EndDrag subscriber — e.g. a ButtonFeedback button)
// is passed on to the nearest parent drag handler, such as a ScrollRect. Because this component implements
// the drag interfaces, the EventSystem would otherwise stop the drag here and a list would not scroll when
// the drag starts on one of its buttons. The drag still suppresses the click.
// A drag component on this same object (Slider, Scrollbar, ScrollRect) already takes the drag, so it is not passed on.
public sealed class UI_EventHandler : MonoBehaviour,
    IPointerClickHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IInitializePotentialDragHandler,
    IDragHandler,
    IBeginDragHandler,
    IEndDragHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public Action<PointerEventData> OnClickHandler;
    public Action<PointerEventData> OnPointerDownHandler;
    public Action<PointerEventData> OnPointerUpHandler;
    public Action<PointerEventData> OnDragHandler;
    public Action<PointerEventData> OnBeginDragHandler;
    public Action<PointerEventData> OnEndDragHandler;
    public Action<PointerEventData> OnLongPressHandler;
    public Action<PointerEventData> OnPointerEnterHandler;
    public Action<PointerEventData> OnPointerExitHandler;

    [Header("Long Press")]
    [SerializeField, Min(0f)]
    private float _longPressDuration = 1.0f;

    private bool _isDragging;
    private bool _isDragConfirmed;
    private bool _isLongPressTriggered;
    private bool _isPointerInside;

    private PointerEventData _cachedEventData;
    private PointerEventData _enterEventData;
    // Parent drag handler receiving the drag this object does not use. Set at BeginDrag, cleared at EndDrag.
    private GameObject _dragForwardTarget;
    private Coroutine _longPressCoroutine;
    // Selectable on this object, if any. Looked up once.
    private Selectable _selectable;
    private bool _selectableLookedUp;

    private bool IsClickable()
    {
        if (!_selectableLookedUp)
        {
            _selectableLookedUp = true;
            TryGetComponent(out _selectable);
        }

        return _selectable == null || _selectable.IsInteractable();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isDragConfirmed || _isLongPressTriggered || !IsClickable())
            return;

        OnClickHandler?.Invoke(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StopLongPressCheck();

        _isDragging = false;
        _isDragConfirmed = false;
        _isLongPressTriggered = false;

        _cachedEventData = eventData;

        OnPointerDownHandler?.Invoke(eventData);

        if (OnLongPressHandler != null)
            _longPressCoroutine = StartCoroutine(CheckLongPress());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopLongPressCheck();

        _cachedEventData = null;

        OnPointerUpHandler?.Invoke(eventData);
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        if (!ForwardsDrag)
            return;

        GameObject target = ParentHandler<IInitializePotentialDragHandler>();
        if (target != null)
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.initializePotentialDrag);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _isDragConfirmed = true;
        _cachedEventData = eventData;

        StopLongPressCheck();

        OnBeginDragHandler?.Invoke(eventData);

        _dragForwardTarget = ForwardsDrag ? ParentHandler<IBeginDragHandler>() : null;
        if (_dragForwardTarget != null)
            ExecuteEvents.Execute(_dragForwardTarget, eventData, ExecuteEvents.beginDragHandler);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _cachedEventData = eventData;

        if (_dragForwardTarget != null)
            ExecuteEvents.Execute(_dragForwardTarget, eventData, ExecuteEvents.dragHandler);

        if (!_isDragging || !_isDragConfirmed)
            return;

        OnDragHandler?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isDragConfirmed)
            OnEndDragHandler?.Invoke(eventData);

        EndForwardedDrag(eventData);

        _isDragging = false;
        _isDragConfirmed = false;
        _isLongPressTriggered = false;
        _cachedEventData = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isPointerInside)
            return;

        _isPointerInside = true;
        _enterEventData = eventData;

        OnPointerEnterHandler?.Invoke(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ExitIfInside(eventData);
    }

    // A disabled object receives no exit from the EventSystem, so disabling sends it here.
    private void ExitIfInside(PointerEventData eventData)
    {
        if (!_isPointerInside)
            return;

        _isPointerInside = false;
        _enterEventData = null;

        OnPointerExitHandler?.Invoke(eventData);
    }

    private IEnumerator CheckLongPress()
    {
        yield return new WaitForSecondsRealtime(_longPressDuration);

        if (_cachedEventData != null &&
            !_isLongPressTriggered &&
            !_isDragConfirmed)
        {
            _isLongPressTriggered = true;
            OnLongPressHandler?.Invoke(_cachedEventData);
        }

        _longPressCoroutine = null;
    }

    private static readonly List<IDragHandler> DragHandlersHere = new();

    // Nobody here uses the drag, so it belongs to whatever drags under this object (a list, a pan view).
    private bool ForwardsDrag =>
        OnBeginDragHandler == null && OnDragHandler == null && OnEndDragHandler == null &&
        !OtherDragHandlerHere();

    // The EventSystem sends the drag to every enabled handler on this object. Passing it on as well would
    // move both — a slider in a list would drag the list too.
    private bool OtherDragHandlerHere()
    {
        GetComponents(DragHandlersHere);

        bool found = false;
        foreach (IDragHandler handler in DragHandlersHere)
        {
            if (ReferenceEquals(handler, this))
                continue;

            if (handler is Behaviour behaviour && !behaviour.isActiveAndEnabled)
                continue;

            found = true;
            break;
        }

        DragHandlersHere.Clear();
        return found;
    }

    private GameObject ParentHandler<T>() where T : IEventSystemHandler
    {
        Transform parent = transform.parent;
        return parent != null ? ExecuteEvents.GetEventHandler<T>(parent.gameObject) : null;
    }

    private void EndForwardedDrag(PointerEventData eventData)
    {
        GameObject target = _dragForwardTarget;
        _dragForwardTarget = null;

        if (target != null && eventData != null)
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.endDragHandler);
    }

    private void StopLongPressCheck()
    {
        if (_longPressCoroutine == null)
            return;

        StopCoroutine(_longPressCoroutine);
        _longPressCoroutine = null;
    }

    private void OnDisable()
    {
        ExitIfInside(_enterEventData);
        StopLongPressCheck();

        // Disabled mid-drag: the EventSystem sends no EndDrag here, so end the forwarded drag for the parent.
        EndForwardedDrag(_cachedEventData);

        _isDragging = false;
        _isDragConfirmed = false;
        _isLongPressTriggered = false;
        _cachedEventData = null;
    }
}