using UnityEngine;

public class UIBootStrap : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject textWidgetPrefab;
    public GameObject buttonWidgetPrefab;
    
    [Header("Root")]
    public Transform uiRoot;

    [SerializeField]private UIScreenCatalog catalog;
    
    private void Awake()
    {
        if (uiRoot == null) uiRoot = transform;
        if (catalog == null) catalog = FindFirstObjectByType<UIScreenCatalog>();

        
        UIBinder binder = new ();
        UIPatchApplier patcher = new ();
        WidgetFactory widgets = new (textWidgetPrefab, buttonWidgetPrefab);
        UIComposer composer = new (widgets);

        UIResolver resolver = new (catalog);
        UIScreenFactory factory  = new (uiRoot, binder, patcher, composer);
        UIRouter router   = new (resolver, factory);
    }
}
