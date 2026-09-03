public sealed class UIRouter
{
    private readonly UIResolver              _resolver;
    private readonly UIScreenFactory         _factory;

    public UIScreen        CurrentScreen { get; private set; }
    public ScreenKey       CurrentKey    { get; private set; }

    // Inputs and outputs of the most recent Show(), for tracing and preview.
    public DisplayContext  LastDisplay   { get; private set; }
    public UIResolveResult LastResult    { get; private set; }

    public UIRouter(UIResolver resolver, UIScreenFactory factory)
    {
        _resolver = resolver;
        _factory  = factory;
    }

    public UIScreen Show(ScreenKey key)
    {
        DisplayContext  display = UnityDisplayContextProvider.GetCurrent();
        UIResolveResult result  = _resolver.Resolve(key, display);
        LastDisplay = display;
        LastResult  = result;

        UIScreen screen = _factory.Create(result);
        if (screen == null)
            return null;

        screen.gameObject.name = key.ToString();

        if (CurrentScreen != null)
            UnityEngine.Object.Destroy(CurrentScreen.gameObject);

        CurrentScreen = screen;
        CurrentKey    = key;
        return screen;
    }
}