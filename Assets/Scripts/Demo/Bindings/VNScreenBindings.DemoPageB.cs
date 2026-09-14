public sealed partial class VNScreenBindings
{
    private void OpenDemoPageB()
    {
        DemoMenuPanel owner = UI.GetUI<DemoMenuPanel>();

        UI.SwitchPage<DemoPageB>(
            owner,
            _demoPageBPresentation,
            afterPresented: page =>
            {
                BindView(page, ApplyBindings);
            },
            afterClosed: Unbind);
    }

    private void ApplyBindings(DemoPageB page)
    {
        AddBinding(page,
            p => p.ConfirmClicked += HandleDemoPageBConfirmClicked,
            p => p.ConfirmClicked -= HandleDemoPageBConfirmClicked);
    }

    private void HandleDemoPageBConfirmClicked()
    {
        OpenDemoPageA();
    }
}
