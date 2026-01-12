using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "UI/Widget Preset Catalog", fileName = "WidgetPresetCatalog")]
public sealed class WidgetPresetCatalog : ScriptableObject
{
    public List<WidgetPreset> presets = new();
}

[Serializable]
public struct WidgetPreset
{
    public string id;

    public WidgetRectMode rectMode;
    public Vector2 anchorMin;
    public Vector2 anchorMax;
    public Vector2 pivot;
    public Vector2 anchoredPosition;
    public Vector2 sizeDelta;
}