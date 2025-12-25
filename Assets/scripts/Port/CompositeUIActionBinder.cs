using UnityEngine;

public sealed class CompositeUiActionBinder : IUiActionBinder
{
    readonly IUiActionBinder[] _binders;

    public CompositeUiActionBinder(params IUiActionBinder[] binders)
    {
        _binders = binders;
    }

    public bool TryBind(ButtonWidget sourceButton, UIActionKey actionKey)
    {
        foreach (IUiActionBinder binder in _binders)
            if (binder.TryBind(sourceButton, actionKey))
                return true;

        Debug.LogWarning($"Unhandled action: {actionKey}");
        return false;
    }
}