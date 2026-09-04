public sealed partial class VNScreenBindings
{
    private void GoToTitle()
    {
        UI.SwitchRoot<TitleUIRoot>(
            _titlePresentation, 
            root => 
            { 
                BindMain(root, ApplyBindings); 
            });
    }

    private void ApplyBindings(TitleUIRoot root)
    {
    }
}