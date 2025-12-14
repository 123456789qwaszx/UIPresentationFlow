using UnityEngine;

public class UIScreenFactory : MonoBehaviour
{
    private readonly Transform _uiRoot;
    private readonly UIPatchApplier _patcher;
    
    public UIScreenFactory(Transform uiRoot, UIPatchApplier patcher)
    {
        _uiRoot   = uiRoot;
        _patcher  = patcher;
    }

    public UIScreen Create(UIResolveResult result, UIRouter router)
    {
        GameObject go = Instantiate(result.Spec.templatePrefab, _uiRoot);
        UIScreen screen = go.GetComponent<UIScreen>();
        return screen;
    }
}
