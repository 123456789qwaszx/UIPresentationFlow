using System;
using System.Collections.Generic;

public sealed partial class UIManager
{
    private sealed class PageState
    {
        public UIBase Page;
        public UIPresentationSpec Presentation;
    }

    private readonly Dictionary<UIBase, PageState> _pagesByOwner = new();

    public TPage SwitchPage<TPage>(
        UIBase owner,
        UIPresentationSpec presentation)
        where TPage : UIBase, IUIPage
    {
        if (owner == null)
            throw new ArgumentNullException(nameof(owner));

        if (presentation == null)
            throw new ArgumentNullException(nameof(presentation));

        if (owner is not IUIPageOwner pageOwner)
        {
            throw new InvalidOperationException(
                $"[UIManager] View '{owner.GetType().Name}' does not support Pages.");
        }

        TPage page = Require<TPage>();

        ValidatePageOwner(page, pageOwner);

        return page;
    }

    private static void ValidatePageOwner(
        UIBase page,
        IUIPageOwner owner)
    {
        if (page.transform.parent == owner.PageRoot)
            return;

        throw new InvalidOperationException(
            $"[UIManager] Page '{page.GetType().Name}' must be a child of " +
            $"'{owner.PageRoot.name}'.");
    }
}