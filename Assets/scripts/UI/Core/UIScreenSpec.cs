using System;
using UnityEngine;

// Authored description of one screen: which prefab, which base theme/layout,
// and the rules that swap them per context. The prefab is the content;
// this spec only says how the content adapts.
[Serializable]
public class UIScreenSpec
{
    public ScreenKey screenKey;

    // Authored screen prefab. Must carry a UIScreen component; widgets that
    // patches target must carry a UIWidgetTag.
    public GameObject templatePrefab;

    public ThemeSpec       baseTheme;    // nullable
    public LayoutPatchSpec baseLayout;   // nullable: base = prefab as authored
    public UIVariantRule[] variants;     // nullable
}
