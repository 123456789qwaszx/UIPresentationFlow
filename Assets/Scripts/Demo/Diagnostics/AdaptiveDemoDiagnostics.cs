using System;
using UnityEngine;

// Demo-only bridge between UI presentation state
// and the on-screen diagnostics label.
public sealed class AdaptiveDemoDiagnostics : MonoBehaviour
{
    [SerializeField]
    private DisplayInfoLabel displayInfoLabel;

    private UIManager _ui;

    private UIResolveResult _lastResult;
    private DisplayContext _lastDisplay;

    public void Initialize(UIManager ui)
    {
        _ui = ui;

        Refresh();
    }

    private void LateUpdate()
    {
        if (_ui == null ||
            _ui.LastResult?.Resolved == null)
        {
            return;
        }

        if (_lastResult == _ui.LastResult &&
            _lastDisplay == _ui.LastDisplay)
        {
            return;
        }

        Refresh();
    }

    private void Refresh()
    {
        if (_ui?.LastResult?.Resolved == null ||
            displayInfoLabel == null)
        {
            return;
        }

        _lastResult = _ui.LastResult;
        _lastDisplay = _ui.LastDisplay;

        displayInfoLabel.Set(
            _ui.LastDisplay,
            _ui.LastResult.Resolved);
    }
}