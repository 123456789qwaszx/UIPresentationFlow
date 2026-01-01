#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class UIScreenKeyDiscovery
{
    private static List<ScreenKey> _cached;
    //private static bool _hasErrors;

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
        //_hasErrors = false;

        var seenValues = new Dictionary<string, FieldInfo>();

        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
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

                    if (field.FieldType != typeof(ScreenKey))
                        continue;

                    var key = (ScreenKey)field.GetValue(null);
                    if (string.IsNullOrEmpty(key.Value))
                        continue;

                    if (seenValues.TryGetValue(key.Value, out var existing))
                    {
                        Debug.LogError(
                            $"[UIScreenKeyDiscovery] Duplicate ScreenKey value '{key.Value}'.\n" +
                            $"- {existing.DeclaringType.FullName}.{existing.Name}\n" +
                            $"- {type.FullName}.{field.Name}");
                        //_hasErrors = true;
                        continue;
                    }

                    seenValues.Add(key.Value, field);
                    _cached.Add(key);
                }
            }
        }
    }
}
#endif