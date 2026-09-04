using UnityEngine;
using Object = UnityEngine.Object;

// Temporary R9 migration bridge.
// View creation still lives here until UIManager replaces Router/Factory.
// Presentation resolution no longer selects the prefab.
public sealed class UIScreenFactory
{
    private readonly Transform _uiRoot;
    private readonly UIPatchApplier _patcher;

    public UIScreenFactory(Transform uiRoot, UIPatchApplier patcher)
    {
        _uiRoot = uiRoot;
        _patcher = patcher;
    }

    public UIBase Create(GameObject prefab, UIResolveResult result)
    {
        if (prefab == null || result == null)
            return null;

        string presentationId = result.Resolved?.PresentationId ?? "(unknown)";

        GameObject go = Object.Instantiate(prefab, _uiRoot);

        UIBase root = go.GetComponent<UIBase>();
        if (root == null)
        {
            Debug.LogError(
                $"[UIScreenFactory] Prefab '{prefab.name}' must have a concrete " +
                $"UIBase<TRefs> component on its root. presentation={presentationId}");
            Object.Destroy(go);
            return null;
        }

        if (root is not IUIPresentationRefProvider refs)
        {
            Debug.LogError(
                $"[UIScreenFactory] UI root '{root.GetType().Name}' must implement " +
                $"{nameof(IUIPresentationRefProvider)}. Use UIBase<TRefs>. " +
                $"presentation={presentationId}",
                root);
            Object.Destroy(go);
            return null;
        }

        root.EnsureInitialized();
        _patcher.Apply(refs, result.Patches);
        return root;
    }
}