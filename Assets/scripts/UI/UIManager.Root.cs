using System;

public sealed partial class UIManager
{
    private UIPresentationSpec _currentRootPresentation;

    public T SwitchRoot<T>(
        UIPresentationSpec presentation,
        Action<T> afterPresented = null)
        where T : UIBase, IUIRoot
    {
        if (presentation == null)
            throw new ArgumentNullException(nameof(presentation));

        T root = Require<T>("Root");
        bool sameRoot = CurrentRoot == root;

        if (CurrentRoot != null && !sameRoot)
            SetVisible(CurrentRoot, false);

        CurrentRoot = root;
        _currentRootPresentation = presentation;

        Mount(root, _rootLayer);

        // Presentation identity is independent from View identity.
        // Therefore same Root + different Presentation must still resolve/patch.
        ApplyPresentation(root, presentation, UnityDisplayContextProvider.GetCurrent());
        SetVisible(root, true);

        afterPresented?.Invoke(root);
        return root;
    }

    public bool ReapplyCurrentRoot(in DisplayContext display)
    {
        if (CurrentRoot == null || _currentRootPresentation == null)
            return false;

        ApplyPresentation(CurrentRoot, _currentRootPresentation, display);
        return true;
    }
}