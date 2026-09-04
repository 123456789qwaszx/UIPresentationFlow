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
        AddBinding(root,
            r => r.LobbyClicked += HandleLobbyClicked,
            r => r.LobbyClicked -= HandleLobbyClicked);

        AddBinding(root,
            r => r.StartClicked += HandleStartClicked,
            r => r.StartClicked -= HandleStartClicked);
    }
    
    private void HandleLobbyClicked()
    {
        GoToAdaptiveDemoUIRoot();
    }

    private void HandleStartClicked()
    {
    }
}