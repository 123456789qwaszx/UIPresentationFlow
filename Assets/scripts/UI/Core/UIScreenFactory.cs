using System;
using UnityEngine;
using Object = UnityEngine.Object;

// Materializes a ResolvedUIScreen into a live UIScreen:
//   Instantiate -> register authored tags -> apply patches
// Patches run last so every target exists when they look it up.
public class UIScreenFactory
{
    private readonly Transform      _uiRoot;
    private readonly UIPatchApplier _patcher;
    private readonly bool           _strict;

    public UIScreenFactory(Transform uiRoot, UIPatchApplier patcher, bool strict = true)
    {
        _uiRoot  = uiRoot  ?? throw new ArgumentNullException(nameof(uiRoot));
        _patcher = patcher ?? throw new ArgumentNullException(nameof(patcher));
        _strict  = strict;
    }

    public UIScreen Create(UIResolveResult result)
    {
        ResolvedUIScreen resolved = result.Resolved;

        GameObject prefab = resolved.Prefab;
        if (prefab == null)
        {
            string message = $"[UIScreenFactory] Resolved prefab is null. screen={resolved.ScreenKey}";
            if (_strict)
                throw new InvalidOperationException(message);
            Debug.LogWarning(message);
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

        screen.RegisterAuthoredWidgets();
        _patcher.Apply(screen, result.Patches);

        return screen;
    }
}
