using System.Collections.Generic;

public static class UIActionKeyRegistry
{
    private static readonly Dictionary<string, UIActionKey> _cache = new();

    public static UIActionKey Get(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return UIActionKey.None;

        raw = raw.Trim();

        if (_cache.TryGetValue(raw, out var key))
            return key;

        key = new UIActionKey(raw);
        _cache[raw] = key;
        return key;
    }
}
