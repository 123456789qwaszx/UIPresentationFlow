using System;
using UnityEngine;

[Serializable]
public sealed class UIImageBindingEntry
{
    public string refId;

    // 마지막으로 UI Prefab을 Scan했을 때의 authored Sprite.
    // Theme Sync 시 사용자가 직접 바꾼 Sprite인지 판별하는 기준.
    public Sprite baseSprite;

    // 이 Theme에서 실제 사용할 Sprite.
    public Sprite sprite;
}