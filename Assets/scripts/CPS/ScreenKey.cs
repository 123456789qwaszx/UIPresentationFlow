using System;

public static class UIScreenKeys
{
    public static readonly ScreenKey Home  = new("home");
    public static readonly ScreenKey Shop  = new("shop");
}

[Serializable]
public readonly struct ScreenKey : IEquatable<ScreenKey>
{
    public readonly string Value;

    public ScreenKey(string value)
    {
        Value = value;
    }

    public bool Equals(ScreenKey other) => Value == other.Value;
    public override bool Equals(object obj) => obj is ScreenKey other && Equals(other);
    public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
    public override string ToString() => Value;
}