using System;

// 값 객체의 필수조건
//1. 불변성
//2, 명확한 동등성 정의
//3. 생성 경로 제한
//4. Null Object 제공
//5. 디버깅 친화성

// 위 다섯 개가 갖춰짐으로써, Key/Identifier로서는 완성됨.
// 만약 의미분기, 파싱로직 등 확장을 한다면, 이 타입 바깥에서 할 것.
public readonly struct UIActionKey : IEquatable<UIActionKey>
{
    internal readonly string Value;

    internal UIActionKey(string value)
    {
        Value = value;
    }

    public bool Equals(UIActionKey other)
        => string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object obj)
        => obj is UIActionKey other && Equals(other);

    public override int GetHashCode()
        => Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0;

    public override string ToString()
        => Value ?? "<None>";

    public static bool operator ==(UIActionKey left, UIActionKey right)
        => left.Equals(right);

    public static bool operator !=(UIActionKey left, UIActionKey right)
        => !left.Equals(right);

    public static readonly UIActionKey None = default;
}