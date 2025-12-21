using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIScreenSpecAsset", menuName = "Scriptable Objects/UIScreenSpecAsset")]
public sealed class UIScreenSpecAsset : ScriptableObject
{
    public UIScreenSpec spec = new UIScreenSpec();
}

[Serializable]
public class UIScreenSpec
{
    public string screenId;
    
    public ThemeSpec baseTheme;          // nullable
    public LayoutPatchSpec baseLayout;   // nullable

    public UIVariantRule[] variants;     // nullable
    
    
    public string name;                  // 디스플레이용 이름
    public GameObject templatePrefab;    // 실제 UI 프리팹

    // 슬롯 이름 -> WidgetSpec 리스트
    public List<SlotSpec> slots = new();
}

[Serializable]
public class SlotSpec
{
    public string slotName; // "Header" / "Body" / "Footer"
    public List<WidgetSpec> widgets = new();
}

[Serializable]
public sealed class WidgetSpec
{
    public WidgetType widgetType;
    public string text;
    public string onClickRoute;
}

public enum WidgetType { Text, Button }