using System;
using System.Collections.Generic;
using UnityEngine;

// UI subsystem facade.
//
// It owns execution order and lifecycle, not presentation policy:
//   View lookup -> lifecycle -> resolve -> restore authored baseline -> patch.
public sealed partial class UIManager
{
    private readonly Dictionary<Type, UIBase> _views = new();
    private readonly Stack<UIBase> _panelStack = new();

    private readonly Transform _rootLayer;
    private readonly Transform _panelLayer;
    private readonly UIResolver _resolver;
    private readonly UIPresentationApplier _presentationApplier;

    public UIBase CurrentRoot { get; private set; }
    public bool HasPanel => _panelStack.Count > 0;

    public DisplayContext LastDisplay { get; private set; }
    public UIResolveResult LastResult { get; private set; }

    public UIManager(
        Transform rootLayer,
        Transform panelLayer,
        UIResolver resolver,
        UIPresentationApplier presentationApplier)
    {
        _rootLayer = rootLayer;
        _panelLayer = panelLayer;
        _resolver = resolver;
        _presentationApplier = presentationApplier;
    }

    public void Register(UIBase view)
    {
        if (view == null)
            throw new ArgumentNullException(nameof(view));

        Type type = view.GetType();
        if (_views.TryGetValue(type, out UIBase existing) && existing != view)
        {
            throw new InvalidOperationException(
                $"[UIManager] Duplicate View type '{type.Name}'. " +
                "One concrete View type must identify one registered View instance.");
        }

        view.EnsureInitialized();
        _views[type] = view;
    }

    public T GetUI<T>() where T : UIBase
    {
        return _views.TryGetValue(typeof(T), out UIBase view)
            ? (T)view
            : null;
    }

    private T Require<T>() where T : UIBase
    {
        T view = GetUI<T>();
        if (view != null)
            return view;

        throw new InvalidOperationException(
            $"[UIManager] View '{typeof(T).Name}' is not registered.");
    }

    private static void SetVisible(UIBase view, bool visible)
    {
        if (view.gameObject.activeSelf != visible)
            view.gameObject.SetActive(visible);
    }

    private static void Mount(UIBase view, Transform layer)
    {
        if (view.transform.parent != layer)
            view.transform.SetParent(layer, worldPositionStays: false);

        view.transform.SetAsLastSibling();
    }
}