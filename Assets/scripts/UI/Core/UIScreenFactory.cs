using System;
using UnityEngine;
using Object = UnityEngine.Object;

// Materializes a ResolvedUIScreen into a live UIScreen:
//   Instantiate -> bind slots -> (compose from SlotSpecs) -> register authored tags -> apply patches
// Patches run last so every target exists when they look it up.
public class UIScreenFactory
{
    private readonly Transform      _uiRoot;
    private readonly UISlotBinder   _binder;
    private readonly UIPatchApplier _patcher;
    private readonly UIComposer     _composer;   // optional: null for authored-prefab screens
    private readonly bool           _strict;

    public UIScreenFactory(
        Transform uiRoot,
        UISlotBinder binder,
        UIPatchApplier patcher,
        UIComposer composer = null,
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
            if (_strict)
                throw new InvalidOperationException($"[UIScreenFactory] Resolved prefab is null. screen={resolved.ScreenKey}");
            Debug.LogWarning($"[UIScreenFactory] Resolved prefab is null. screen={resolved.ScreenKey}");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, _uiRoot);
        UIScreen screen = go.GetComponent<UIScreen>();
        if (screen == null)
        {
            string message = $"[UIScreenFactory] Prefab '{prefab.name}' must have a {nameof(UIScreen)} component. screen={resolved.ScreenKey}";
            if (_strict)
                throw new InvalidOperationException(message);
            Debug.LogError(message);
            Object.Destroy(go);
            return null;
        }

        screen.BuildSlotMap(_binder, resolved.BaseSpec);
        _composer?.Compose(screen, resolved.BaseSpec);
        screen.RegisterAuthoredWidgets();
        _patcher.Apply(screen, result.Patches);

        return screen;
    }
}
