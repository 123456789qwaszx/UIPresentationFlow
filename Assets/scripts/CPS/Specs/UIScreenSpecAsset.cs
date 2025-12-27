using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UIScreenSpecAsset", menuName = "Scriptable Objects/UIScreenSpecAsset")]
public sealed class UIScreenSpecAsset : ScriptableObject
{
    public UIScreenSpec spec = new ();
}

[Serializable]
public class UIScreenSpec
{
    public string screenId;
    // 슬롯 이름 -> WidgetSpec 리스트
    public List<SlotSpec> slots = new();
    
    public GameObject templatePrefab;    // 실제 UI 프리팹
    
    public ThemeSpec baseTheme;          // nullable
    public LayoutPatchSpec baseLayout;   // nullable
    public UIVariantRule[] variants;     // nullable
    
    public string name;                  // 에디터 인덱싱용 이름
}

[Serializable]
public class SlotSpec
{
    public string slotName;
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
    
    public WidgetRectMode rectMode = WidgetRectMode.UseSlotLayout;

    // 아래 값들은 rectMode == OverrideInSlot일 때만 사용
    public Vector2 anchorMin   = new Vector2(0.5f, 0.5f);
    public Vector2 anchorMax   = new Vector2(0.5f, 0.5f);
    public Vector2 pivot       = new Vector2(0.5f, 0.5f);
    public Vector2 anchoredPosition = Vector2.zero;
    public Vector2 sizeDelta   = new Vector2(300f, 80f);
    
    // --------- Image 전용 옵션 ---------
    public Sprite imageSprite;                 // null이면 프리팹 기본값
    public Color  imageColor = Color.white;    // 기본 흰색(변경 없으면 그대로)
    public bool   imageSetNativeSize = false;  // 필요하면 true

    // --------- Toggle 전용 옵션 --------
    public bool toggleInitialValue = false;
    public bool toggleInteractable = true;

    // --------- Slider 전용 옵션 --------
    public float sliderMin = 0f;
    public float sliderMax = 1f;
    public float sliderInitialValue = 0.5f;
    public bool  sliderWholeNumbers = false;
}

public enum WidgetType
{
    Text = 0,
    Button = 1,
    Image = 2,
    Toggle = 3,
    Slider = 4,
    
    /// <summary>
    /// 그냥 빈 컨테이너 혹은 임의의 GameObject 프리팹.
    /// (레이아웃용/특수 위젯용으로 사용)
    /// </summary>
    GameObject = 5,
}

public enum WidgetRectMode
{
    UseSlotLayout,   // 프리팹/슬롯 LayoutGroup에 맡김 (기본)
    OverrideInSlot   // 이 슬롯 안에서만 위치/크기를 직접 지정
}