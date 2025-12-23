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
    public string nameTag;
    public string text;
    public string onClickRoute;
    public GameObject prefabOverride;
    
    public WidgetRectMode rectMode = WidgetRectMode.PrefabDefault;

    // 아래 값들은 rectMode == OverrideInSlot일 때만 사용
    public Vector2 anchorMin   = new Vector2(0.5f, 0.5f);
    public Vector2 anchorMax   = new Vector2(0.5f, 0.5f);
    public Vector2 pivot       = new Vector2(0.5f, 0.5f);
    public Vector2 anchoredPosition = Vector2.zero;
    public Vector2 sizeDelta   = new Vector2(300f, 80f);
}

public enum WidgetType { Text, Button }

public enum WidgetRectMode
{
    PrefabDefault,   // 프리팹/슬롯 LayoutGroup에 맡김 (기본)
    OverrideInSlot   // 이 슬롯 안에서만 위치/크기를 직접 지정
}