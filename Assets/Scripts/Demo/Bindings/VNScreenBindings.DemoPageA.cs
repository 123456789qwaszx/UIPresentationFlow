public sealed partial class VNScreenBindings
{
    private void OpenDemoPageA()
    {
        DemoMenuPanel owner = UI.GetUI<DemoMenuPanel>();

        UI.SwitchPage<DemoPageA>(
            owner,
            _demoPageAPresentation,
            afterPresented: page =>
            {
                BindView(page, ApplyBindings);
            },
            afterClosed: Unbind);
    }

    private void ApplyBindings(DemoPageA page)
    {
        AddBinding(page,
            p => p.OpenConfirmPanelClicked += HandleOpenConfirmPanelClicked,
            p => p.OpenConfirmPanelClicked -= HandleOpenConfirmPanelClicked);

        AddBinding(page,
            p => p.ConfirmClicked += HandleDemoPageAConfirmClicked,
            p => p.ConfirmClicked -= HandleDemoPageAConfirmClicked);
    }

    private void HandleOpenConfirmPanelClicked()
    {
        OpenDemoConfirmPanel();
    }

    private void HandleDemoPageAConfirmClicked()
    {
        OpenDemoPageB();
    }
}
