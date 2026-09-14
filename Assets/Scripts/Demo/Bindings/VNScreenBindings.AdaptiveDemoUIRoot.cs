public partial class VNScreenBindings
{
    private void GoToAdaptiveDemoUIRoot()
    {
        UI.SwitchRoot<AdaptiveDemoUIRoot>(
            _titlePresentation,
            afterPresented: root =>
            {
                BindView(root, ApplyBindings);
            },
            afterClosed: Unbind);
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