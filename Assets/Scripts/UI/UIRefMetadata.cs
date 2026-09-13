using System;
using System.Collections.Generic;
using System.Reflection;

public enum UITextRole
{
    Title,
    Body,
    Caption,
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class UIRefTextRoleAttribute : Attribute
{
    public UITextRole Role { get; }

    public UIRefTextRoleAttribute(UITextRole role)
    {
        Role = role;
    }
}

public readonly struct UIRefMetadata
{
    public bool HasTextRole { get; }
    public UITextRole TextRole { get; }

    internal UIRefMetadata(UITextRole textRole)
    {
        HasTextRole = true;
        TextRole = textRole;
    }
}

public static class UIRefMetadataCache<TRefs>
    where TRefs : struct, Enum
{
    private static readonly Dictionary<string, TRefs> KeysById;
    private static readonly Dictionary<TRefs, UIRefMetadata> MetadataByKey;
    private static readonly IReadOnlyList<string> TextTargetIdList;

    static UIRefMetadataCache()
    {
        string[] refIds = Enum.GetNames(typeof(TRefs));

        KeysById = new Dictionary<string, TRefs>(
            refIds.Length,
            StringComparer.Ordinal);

        MetadataByKey = new Dictionary<TRefs, UIRefMetadata>();
        var textTargetIds = new List<string>();

        foreach (string refId in refIds)
        {
            if (!Enum.TryParse(refId, out TRefs key))
                continue;

            KeysById.Add(refId, key);

            FieldInfo field = typeof(TRefs).GetField(
                refId,
                BindingFlags.Public | BindingFlags.Static);

            UIRefTextRoleAttribute textRole =
                field?.GetCustomAttribute<UIRefTextRoleAttribute>(inherit: false);

            if (textRole == null)
                continue;

            MetadataByKey[key] = new UIRefMetadata(textRole.Role);
            textTargetIds.Add(refId);
        }

        TextTargetIdList = textTargetIds.AsReadOnly();
    }

    public static IReadOnlyList<string> TextTargetIds => TextTargetIdList;

    public static bool TryGetKey(string refId, out TRefs key)
    {
        if (string.IsNullOrEmpty(refId))
        {
            key = default;
            return false;
        }

        return KeysById.TryGetValue(refId, out key);
    }

    public static bool TryGetMetadata(TRefs key, out UIRefMetadata metadata)
        => MetadataByKey.TryGetValue(key, out metadata);

    public static bool TryGetTextRole(TRefs key, out UITextRole role)
    {
        if (MetadataByKey.TryGetValue(key, out UIRefMetadata metadata)
            && metadata.HasTextRole)
        {
            role = metadata.TextRole;
            return true;
        }

        role = default;
        return false;
    }

    public static bool TryGetTextRole(string refId, out UITextRole role)
    {
        if (!TryGetKey(refId, out TRefs key))
        {
            role = default;
            return false;
        }

        return TryGetTextRole(key, out role);
    }
}