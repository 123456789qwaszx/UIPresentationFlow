using UnityEngine;
using Object = UnityEngine.Object;

// Materializes resolved presentation data into a concrete UIBase root:
//   Instantiate -> initialize typed refs -> apply presentation patches.
//
// UIScreen is no longer part of the runtime application boundary.
// Every presentation prefab must expose a concrete UIBase<TRefs> on its root.
public sealed class UIScreenFactory
{
    private readonly Transform _uiRoot;
    private readonly UIPatchApplier _patcher;

    public UIScreenFactory(Transform uiRoot, UIPatchApplier patcher)
    {
        _uiRoot = uiRoot;
        _patcher = patcher;
    }

    public UIBase Create(UIResolveResult result)
    {
        if (result == null)
            return null;

        ResolvedUIScreen resolved = result.Resolved;
        GameObject prefab = resolved.Prefab;

        if (prefab == null)
        {
            Debug.LogWarning(
                $"[UIScreenFactory] Resolved prefab is null. screen={resolved.ScreenKey}");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, _uiRoot);

        UIBase root = go.GetComponent<UIBase>();
        if (root == null)
        {
            Debug.LogError(
                $"[UIScreenFactory] Prefab '{prefab.name}' must have a concrete UIBase<TRefs> component on its root. screen={resolved.ScreenKey}");
            Object.Destroy(go);
            return null;
        }

        if (root is not IUIPresentationRefProvider refs)
        {
            Debug.LogError(
                $"[UIScreenFactory] UI root '{root.GetType().Name}' must implement {nameof(IUIPresentationRefProvider)}. Use UIBase<TRefs>. screen={resolved.ScreenKey}",
                root);
            Object.Destroy(go);
            return null;
        }

        // Awake normally initializes active prefab instances.
        // Explicitly ensuring it also makes the factory contract deterministic
        // for inactive prefabs and tests.
        root.EnsureInitialized();

        _patcher.Apply(refs, result.Patches);
        return root;
    }
}