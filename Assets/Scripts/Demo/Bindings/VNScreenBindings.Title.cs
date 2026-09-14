public sealed partial class VNScreenBindings
{
    private void GoToTitle()
    {
        UI.SwitchRoot<TitleUIRoot>(
            _titlePresentation,
            afterPresented: root =>
            {
                BindView(root, ApplyBindings);
            },
            afterClosed: Unbind);
    }

    private void ApplyBindings(TitleUIRoot root)
    {
        AddBinding(root,
            r => r.StartClicked += HandleStartClicked,
            r => r.StartClicked -= HandleStartClicked);

        AddBinding(root,
            r => r.LoadClicked += HandleLoadClicked,
            r => r.LoadClicked -= HandleLoadClicked);

        AddBinding(root,
            r => r.AlbumClicked += HandleAlbumClicked,
            r => r.AlbumClicked -= HandleAlbumClicked);
    }

    private void HandleStartClicked()
    {
        GoToGameplay();
    }

    private void HandleLoadClicked()
    {
        OpenLoadMenu();
    }

    private void HandleAlbumClicked()
    {
        GoToAlbum();
    }
}
