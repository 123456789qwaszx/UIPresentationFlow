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

    public UIScreenFactory(Transform uiRoot, UIPatchApplier patcher)
    {
        _uiRoot  = uiRoot;
        _patcher = patcher;
    }

    public UIScreen Create(UIResolveResult result)
    {
        ResolvedUIScreen resolved = result.Resolved;

        GameObject prefab = resolved.Prefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[UIScreenFactory] Resolved prefab is null. screen={resolved.ScreenKey}");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, _uiRoot);
        UIScreen screen = go.GetComponent<UIScreen>();
        if (screen == null)
        {
            Debug.LogError($"[UIScreenFactory] Prefab '{prefab.name}' must have a {nameof(UIScreen)} component. screen={resolved.ScreenKey}");
            Object.Destroy(go);
            return null;
        }

        screen.RegisterAuthoredWidgets();
        _patcher.Apply(screen, result.Patches);

        return screen;
    }
}