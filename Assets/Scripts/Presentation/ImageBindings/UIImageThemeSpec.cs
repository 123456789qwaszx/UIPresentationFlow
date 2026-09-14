using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "UIImageTheme",
    menuName = "UI/Image Theme")]
public sealed class UIImageThemeSpec : ScriptableObject
{
    public string themeId;

    public List<UIImageBindingSet> bindings = new();
}