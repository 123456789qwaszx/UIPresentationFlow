using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RectTransformPatch
{
    [Header("Anchors")]
    public bool   overrideAnchors;
    public Vector2 anchorMin;
    public Vector2 anchorMax;

    [Header("Pivot")]
    public bool   overridePivot;
    public Vector2 pivot;

    [Header("Position")]
    public bool   overrideAnchoredPosition;
    public Vector2 anchoredPosition;

    [Header("Size")]
    public bool   overrideSizeDelta;
    public Vector2 sizeDelta;
}

[Serializable]
public sealed class WidgetLayoutPatch
{
    [Tooltip("UIScreen.WidgetHandle.NameTag 와 일치해야 합니다.")]
    public string nameTag;

    [Header("Active")]
    public bool overrideActive;
    public bool active = true;

    [Header("RectTransform")]
    public RectTransformPatch rect = new RectTransformPatch();
}

[CreateAssetMenu(menuName = "UI/LayoutPatchSpec")]
public sealed class LayoutPatchSpec : ScriptableObject
{
    [Tooltip("이 레이아웃 패치가 적용할 위젯 목록 (nameTag 기준)")]
    public List<WidgetLayoutPatch> widgets = new();

    // 나중에 SafeArea 같은 전역 옵션도 여기 추가 가능
    // public bool useSafeArea;

    public void BuildPatches(List<IUIPatch> patches)
    {
        patches.Add(new LayoutSpecPatch(this));
    }
}