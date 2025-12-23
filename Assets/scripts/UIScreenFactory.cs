using System;
using UnityEngine;
using Object = UnityEngine.Object;

public class UIScreenFactory
{
    private readonly Transform _uiRoot;
    private readonly UIBinder  _binder;
    private readonly UIPatchApplier _patcher;
    private readonly UIComposer _composer;
    private readonly bool _strict;

    public UIScreenFactory(
        Transform uiRoot,
        UIBinder binder,
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

    public UIScreen Create(UIResolveResult result, UIRouter router)
    {
        ResolvedUIScreen resolved = result.Resolved;

        GameObject prefab = resolved.Prefab;
        if (prefab == null)
        {
            if (_strict)throw new InvalidOperationException($"[UIScreenFactory] Resolved prefab is null. screenId={resolved.ScreenId}");
            Debug.LogWarning($"[UIScreenFactory] Resolved prefab is null. screenId={resolved.ScreenId}");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, _uiRoot);
        UIScreen screen = go.GetComponent<UIScreen>();
        if (screen == null)
        {
            if (_strict)
                throw new InvalidOperationException(
                    $"[UIScreenFactory] Prefab '{prefab.name}' must have {nameof(UIScreen)} component. " +
                    $"screenId={resolved.ScreenId}");
            Debug.LogError(
                $"[UIScreenFactory] Prefab '{prefab.name}' must have {nameof(UIScreen)} component. " +
                $"screenId={resolved.ScreenId}");
            Object.Destroy(go);
            return null;
        }

        screen.Build(_binder, resolved.BaseSpec);

        _patcher.Apply(screen, result.Patches);

        _composer.Compose(screen, resolved.BaseSpec, router);

        return screen;
    }
}