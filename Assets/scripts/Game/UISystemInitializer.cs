public static class UISystemInitializer
{
    public static void InitAll(UIScreenCatalog catalog)
    {
        UIRouteKeyRegistry.Init(catalog);
        catalog.BuildCache();
        //UIActionKeyRegistry.Init();
    }
}