using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIResolveTrace
{
    private readonly List<string> _lines = new();
    public void Add(string message) => _lines.Add(message);
    public string Dump() => "[Trace]\n- " + string.Join("\n- ", _lines);
}

public class UIRequest
{
    public string route;
    public object payload;

    public UIRequest(string route, object payload)
    {
        this.route = route;
        this.payload = payload;
    }
}

public sealed class UIResolveResult
{
    public UIScreenSpec Spec { get; }
    public List<IUIPatch> Patches { get; }
    public UIResolveTrace Trace { get; }

    public UIResolveResult(UIScreenSpec spec, List<IUIPatch> patches, UIResolveTrace trace)
    {
        this.Spec = spec;
        this.Patches = patches;
        this.Trace = trace;
    }
}

public class UIResolver
{
    private readonly UIScreenCatalog _catalog;

    public UIResolver(UIScreenCatalog catalog)
    {
        _catalog = catalog;
    }

    public UIResolveResult Resolve(ScreenKey screenKey, UIRequest request)
    {
        UIResolveTrace trace = new UIResolveTrace();
        trace.Add($"Resolve ScreenKey = {screenKey}");
        trace.Add($"Payload = {(request.payload != null ? request.payload.ToString() : "null")}");

        UIScreenSpec baseSpec = _catalog.GetScreenSpec(screenKey);
        trace.Add($"Picked base spec = {baseSpec.name}");

        List<IUIPatch> patches = new();

        ThemeId theme = screenKey switch
        {
            ScreenKey.Shop => ThemeId.Dark,
            ScreenKey.Home => ThemeId.Light,
            _ => ThemeId.Light
        };

        patches.Add(new ThemePatch(theme));
        trace.Add($"Applied ThemePatch({theme})");

        return new UIResolveResult(baseSpec, patches, trace);
    }
}