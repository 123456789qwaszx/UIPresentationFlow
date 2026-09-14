public sealed partial class VNScreenBindings
{
    private void OpenDemoConfirmPanel()
    {
        UI.PushPanel<DemoConfirmPanel>(
            _demoConfirmPresentation,
            afterPresented: panel =>
            {
                BindView(panel, ApplyBindings);
            },
            afterClosed: Unbind);
    }

    private void ApplyBindings(DemoConfirmPanel panel)
    {
        AddBinding(panel,
            p => p.CloseClicked += HandleDemoConfirmCloseClicked,
            p => p.CloseClicked -= HandleDemoConfirmCloseClicked);
    }

    private void HandleDemoConfirmCloseClicked()
    {
        ClosePanel();
    }
}