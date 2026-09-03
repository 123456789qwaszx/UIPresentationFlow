using System;

public enum UITextRole
{
    Title,
    Body,
    Caption,
}

// Presentation metadata attached to a typed UI ref.
// The ref keeps identity; the attribute adds only semantic role information.
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class UIRefTextRoleAttribute : Attribute
{
    public UITextRole Role { get; }

    public UIRefTextRoleAttribute(UITextRole role)
    {
        Role = role;
    }
}