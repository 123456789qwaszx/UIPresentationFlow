using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "UI/ThemeSpec")]
public class ThemeSpec : ScriptableObject
{
    [Header("Base Colors")]
    public Color textMainColor   = Color.white;
    public Color textWeakColor   = Color.gray;

    [Header("Typography")]
    public TMP_FontAsset mainFont;
    public int titleSize   = 28;
    public int bodySize    = 16;
    public int captionSize = 13;

    // Fonts, button styles, etc. can be added later if necessary
    public void BuildPatches(System.Collections.Generic.List<IUIPatch> patches)
    {
        patches.Add(new ThemeSpecPatch(this));
    }
}