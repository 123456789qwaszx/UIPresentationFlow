using System;
using UnityEngine;

// 실험 배정 값 · Variant 강제 지정의 id. Variant 조건(VariantCondition.experimentVariantId)에 에셋으로 저장된다 —
// 그래서 값 칸은 Unity가 직렬화하는 비공개 칸이고(readonly 칸은 직렬화되지 않는다), 밖에는 읽기만 내준다.
[Serializable]
public struct VariantId : IEquatable<VariantId>
{
    [SerializeField] private string value;

    public VariantId(string value)
    {
        this.value = value ?? string.Empty;
    }

    public string Value => value ?? string.Empty;

    public bool IsValid => !string.IsNullOrEmpty(value);

    public bool Equals(VariantId other)
        => string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object obj)
        => obj is VariantId other && Equals(other);

    public override int GetHashCode()
        => Value.GetHashCode();

    public static bool operator ==(VariantId left, VariantId right)
        => left.Equals(right);

    public static bool operator !=(VariantId left, VariantId right)
        => !left.Equals(right);

    public override string ToString() => Value;

    public static implicit operator VariantId(string value)
        => new VariantId(value);

    public static implicit operator string(VariantId id)
        => id.Value;
}
