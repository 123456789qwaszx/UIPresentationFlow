public sealed partial class VNScreenBindings
{
    private void OpenDemoMenuPanel()
    {
        UI.PushPanel<DemoMenuPanel>(
            _demoMenuPresentation,
            afterPresented: panel =>
            {
                BindView(panel, ApplyBindings);
                OpenDemoPageA();
            },
            afterClosed: Unbind);
    }

    private void ApplyBindings(DemoMenuPanel panel)
    {
        AddBinding(panel,
            p => p.CloseClicked += HandleDemoMenuCloseClicked,
            p => p.CloseClicked -= HandleDemoMenuCloseClicked);
    }

    private void HandleDemoMenuCloseClicked()
    {
        ClosePanel();
    }
}
