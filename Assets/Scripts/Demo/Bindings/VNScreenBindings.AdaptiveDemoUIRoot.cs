public partial class VNScreenBindings
{
    private void GoToAdaptiveDemoUIRoot()
    {
        UI.SwitchRoot<AdaptiveDemoUIRoot>(
            _titlePresentation, 
            root => 
            { 
                BindMain(root, ApplyBindings); 
            });
    }

    private void ApplyBindings(AdaptiveDemoUIRoot root)
    {
        AddBinding(root,
            r => r.LeftActionClicked += HandleLeftActionClicked,
            r => r.LeftActionClicked -= HandleLeftActionClicked);

        AddBinding(root,
            r => r.RightActionClicked += HandleRightActionClicked,
            r => r.RightActionClicked -= HandleRightActionClicked);
    }
    
    private void HandleLeftActionClicked()
    {
        GoToTitle();
    }

    private void HandleRightActionClicked()
    {
    }
}