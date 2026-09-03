using System;
using UnityEngine;

[Serializable]
public class UIVariantRule
{
    public string variantId; // e.g. "Shop_Layout_B"
    public int priority;

    public VariantCondition condition;

    public GameObject overridePrefab;      // Replaces the screen prefab
    public ThemeSpec overrideTheme;        // Overrides the theme
    public LayoutPatchSpec overrideLayout; // Overrides layout / locale-specific settings
}