using UnityEngine;

public sealed partial class VNScreenBindings
{
    private SystemMenuPanel _systemMenuPanel;

    private void OpenSaveMenu()
    {
        OpenSystemMenu(showSavePage: true);
    }

    private void OpenLoadMenu()
    {
        OpenSystemMenu(showSavePage: false);
    }

    private void OpenSystemMenu(bool showSavePage)
    {
        UI.PushPanel<SystemMenuPanel>(
            _demoMenuPresentation,
            afterPresented: panel =>
            {
                _systemMenuPanel = panel;

                BindView(panel, ApplyBindings);

                if (showSavePage)
                    OpenSavePage();
                else
                    OpenLoadPage();
            },
            afterClosed: Unbind);
    }

    private void ApplyBindings(SystemMenuPanel panel)
    {
        AddBinding(panel,
            p => p.SaveClicked += OpenSavePage,
            p => p.SaveClicked -= OpenSavePage);

        AddBinding(panel,
            p => p.LoadClicked += OpenLoadPage,
            p => p.LoadClicked -= OpenLoadPage);

        AddBinding(panel,
            p => p.CloseClicked += CloseSystemMenu,
            p => p.CloseClicked -= CloseSystemMenu);
    }

    private void OpenSavePage()
    {
        if (_systemMenuPanel == null)
            return;

        UI.SwitchPage<SavePage>(
            _systemMenuPanel,
            _demoPageAPresentation,
            afterPresented: page =>
            {
                BindView(page, ApplyBindings);
            },
            afterClosed: Unbind);
    }

    private void OpenLoadPage()
    {
        if (_systemMenuPanel == null)
            return;

        UI.SwitchPage<LoadPage>(
            _systemMenuPanel,
            _demoPageBPresentation,
            afterPresented: page =>
            {
                BindView(page, ApplyBindings);
            },
            afterClosed: Unbind);
    }

    private void ApplyBindings(SavePage page)
    {
        AddBinding(page,
            p => p.SlotClicked += HandleSaveSlotClicked,
            p => p.SlotClicked -= HandleSaveSlotClicked);
    }

    private void ApplyBindings(LoadPage page)
    {
        AddBinding(page,
            p => p.SlotClicked += HandleLoadSlotClicked,
            p => p.SlotClicked -= HandleLoadSlotClicked);
    }

    private void HandleSaveSlotClicked(int slotIndex)
    {
        Debug.Log($"[Demo] Save slot {slotIndex}");
    }

    private void HandleLoadSlotClicked(int slotIndex)
    {
        Debug.Log($"[Demo] Load slot {slotIndex}");
    }

    private void CloseSystemMenu()
    {
        _systemMenuPanel = null;
        ClosePanel();
    }
}
