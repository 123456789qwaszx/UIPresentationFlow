public sealed partial class VNScreenBindings
{
    private void GoToAlbum()
    {
        UI.SwitchRoot<AlbumUIRoot>(
            _titlePresentation,
            afterPresented: root =>
            {
                BindView(root, ApplyBindings);
            },
            afterClosed: Unbind);
    }

    private void ApplyBindings(AlbumUIRoot root)
    {
        AddBinding(root,
            r => r.BackClicked += HandleAlbumBackClicked,
            r => r.BackClicked -= HandleAlbumBackClicked);
    }

    private void HandleAlbumBackClicked()
    {
        GoToTitle();
    }
}
