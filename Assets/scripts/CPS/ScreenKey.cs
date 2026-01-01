using System;

[Serializable]
public struct ScreenKey : IEquatable<ScreenKey>
{
    public string value;
    public string Value => value;

    public ScreenKey(string value)
    {
        this.value = value;
    }

    public bool Equals(ScreenKey other) => value == other.value;
    public override bool Equals(object obj) => obj is ScreenKey other && Equals(other);
    public override int GetHashCode() => value != null ? value.GetHashCode() : 0;
    public override string ToString() => value;
}