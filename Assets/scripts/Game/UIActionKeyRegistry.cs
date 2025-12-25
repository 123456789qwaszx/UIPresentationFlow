using System.Collections.Generic;

public static class UIActionKeys
{
    public static readonly UIActionKey Gold =
        UIActionKeyRegistry.Get("ui/gold");

    public static readonly UIActionKey Hp =
        UIActionKeyRegistry.Get("ui/hp");

    public static readonly UIActionKey Gem =
        UIActionKeyRegistry.Get("ui/gem");
}

public static class UIActionKeyRegistry
{
    private static readonly Dictionary<string, UIActionKey> _cache = new();

    public static UIActionKey Get(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return UIActionKey.None;

        raw = raw.Trim();

        if (_cache.TryGetValue(raw, out UIActionKey key))
            return key;

        key = new UIActionKey(raw);
        _cache[raw] = key;
        return key;
    }
}
