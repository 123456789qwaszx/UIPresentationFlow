#if UNITY_EDITOR
using UnityEngine;

[System.AttributeUsage(System.AttributeTargets.Field)]
public sealed class UIRouteDefinitionAttribute : System.Attribute
{
}

// 기존: UIRouteKeyAttribute (PropertyAttribute)는 그대로 유지해서
// UIRouteEntry.route 필드 드로어에 사용.
// 이 Attribute는 "이 필드는 route 목록에 포함돼야 한다" 표시용.
#endif