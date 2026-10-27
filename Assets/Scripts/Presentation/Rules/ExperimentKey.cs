using System;
using UnityEngine;

public static class UIExperiments
{
    public static readonly ExperimentKey HomeLayoutTest
        = new ExperimentKey("HomeLayoutTest");
}

// 실험 이름. Variant 조건(VariantCondition.experimentKey)에 에셋으로 저장된다 — 값 칸은 Unity가 직렬화하는
// 비공개 칸이고(readonly 칸은 직렬화되지 않는다), 밖에는 읽기만 내준다.
[Serializable]
public struct ExperimentKey : IEquatable<ExperimentKey>
{
    [SerializeField] private string value;

    public ExperimentKey(string value)
    {
        this.value = value ?? string.Empty;
    }

    public string Value => value ?? string.Empty;

    public bool IsValid => !string.IsNullOrEmpty(value);

    public override string ToString() => Value;

    public bool Equals(ExperimentKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object obj) => obj is ExperimentKey other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();

    public static implicit operator ExperimentKey(string value) => new(value);
    public static implicit operator string(ExperimentKey key) => key.Value;
}
