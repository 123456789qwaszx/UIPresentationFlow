using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class UIScreenFactory
{
    private readonly Transform _uiRoot;
    private readonly UISlotBinder  _binder;
    private readonly UIPatchApplier _patcher;
    private readonly UIComposer _composer;
    private readonly bool _strict;

    public UIScreenFactory(
        Transform uiRoot,
        UISlotBinder binder,
        UIPatchApplier patcher,
        UIComposer composer,
        bool strict = true)
    {
        _uiRoot   = uiRoot;
        _binder   = binder;
        _patcher  = patcher;
        _composer = composer;
        _strict   = strict;
    }

    public UIScreen Create(UIResolveResult result)
    {
        ResolvedUIScreen resolved = result.Resolved;

        GameObject prefab = resolved.Prefab;
        if (prefab == null)
        {
            if (_strict)throw new InvalidOperationException($"[UIScreenFactory] Resolved prefab is null. screenId={resolved.ScreenKey}");
            Debug.LogWarning($"[UIScreenFactory] Resolved prefab is null. screenId={resolved.ScreenKey}");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, _uiRoot);
        UIScreen screen = go.GetComponent<UIScreen>();
        if (screen == null)
        {
            if (_strict)
                throw new InvalidOperationException(
                    $"[UIScreenFactory] Prefab '{prefab.name}' must have {nameof(UIScreen)} component. " +
                    $"screenId={resolved.ScreenKey}");
            Debug.LogError(
                $"[UIScreenFactory] Prefab '{prefab.name}' must have {nameof(UIScreen)} component. " +
                $"screenId={resolved.ScreenKey}");
            Object.Destroy(go);
            return null;
        }

        screen.BuildSlot(_binder, resolved.BaseSpec);

        _patcher.Apply(screen, result.Patches);

        _composer.Compose(screen, resolved.BaseSpec);

        return screen;
    }
}