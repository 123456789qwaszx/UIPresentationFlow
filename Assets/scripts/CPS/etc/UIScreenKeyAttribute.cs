#if UNITY_EDITOR
using System;

[AttributeUsage(AttributeTargets.Field)]
public sealed class UIScreenKeyAttribute : Attribute
{
}
#endif