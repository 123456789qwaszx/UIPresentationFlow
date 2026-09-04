public sealed partial class UIManager
{
    private void ApplyPresentation(
        UIBase view,
        UIPresentationSpec presentation,
        in DisplayContext display)
    {
        view.EnsureInitialized();

        UIResolveResult result = 
            _resolver.Resolve(presentation, display);

        _presentationApplier.Apply(view, result);

        LastDisplay = display;
        LastResult = result;
    }
}