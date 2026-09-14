public sealed partial class VNScreenBindings
{
    private void GoToGameplay()
    {
        UI.SwitchRoot<DemoGameplayRoot>(
            _titlePresentation,
            afterPresented: root =>
            {
                BindView(root, ApplyBindings);
            },
            afterClosed: Unbind);
    }

    private void ApplyBindings(DemoGameplayRoot root)
    {
        AddBinding(root,
            r => r.SaveClicked += HandleGameplaySaveClicked,
            r => r.SaveClicked -= HandleGameplaySaveClicked);

        AddBinding(root,
            r => r.LoadClicked += HandleGameplayLoadClicked,
            r => r.LoadClicked -= HandleGameplayLoadClicked);

        AddBinding(root,
            r => r.TitleClicked += HandleGameplayTitleClicked,
            r => r.TitleClicked -= HandleGameplayTitleClicked);
    }

    private void HandleGameplaySaveClicked()
    {
        OpenSaveMenu();
    }

    private void HandleGameplayLoadClicked()
    {
        OpenLoadMenu();
    }

    private void HandleGameplayTitleClicked()
    {
        GoToTitle();
    }
}
