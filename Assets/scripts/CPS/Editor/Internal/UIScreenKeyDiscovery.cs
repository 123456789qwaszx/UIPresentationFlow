#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;

public static class UIScreenKeyDiscovery
{
    private static List<ScreenKey> _cached;

    public static IReadOnlyList<ScreenKey> All
    {
        get
        {
            if (_cached == null)
                BuildCache();
            return _cached;
        }
    }

    private static void BuildCache()
    {
        _cached = new List<ScreenKey>();

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in asm.GetTypes())
            {
                foreach (var field in type.GetFields(
                             BindingFlags.Public |
                             BindingFlags.NonPublic |
                             BindingFlags.Static))
                {
                    if (!field.IsDefined(typeof(UIScreenKeyAttribute), false))
                        continue;

                    if (field.FieldType == typeof(ScreenKey))
                    {
                        var key = (ScreenKey)field.GetValue(null);
                        _cached.Add(key);
                    }
                }
            }
        }
    }
}
#endif