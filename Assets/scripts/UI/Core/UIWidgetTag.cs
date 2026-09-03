using System.Collections.Generic;
using UnityEngine;

// Marks an authored widget as a stable presentation target.
//
// LayoutSpecPatch and ThemeSpecPatch find widgets through this tag — never
// through GameObject names or hierarchy paths — so a designer can restructure
// the prefab without breaking any layout variant.
//
// Convention (M3 §9): the tag is unique within a screen and names the
// widget's role, not its variant: "PrimaryContent", never "PrimaryContent_Wide".
[DisallowMultipleComponent]
public sealed class UIWidgetTag : MonoBehaviour
{
    public string nameTag;
    public UITextRole textRole = UITextRole.Body;

    public string NameTag => (nameTag ?? string.Empty).Trim();

    public WidgetHandle CreateHandle() => new WidgetHandle(NameTag, gameObject, textRole);

    // Every tagged descendant of `root`, including inactive ones (a variant may
    // re-activate them). Empty tags are skipped with a warning.
    public static List<WidgetHandle> CollectHandles(Transform root)
    {
        var handles = new List<WidgetHandle>();

        foreach (UIWidgetTag tag in root.GetComponentsInChildren<UIWidgetTag>(includeInactive: true))
        {
            if (string.IsNullOrEmpty(tag.NameTag))
            {
                Debug.LogWarning($"[UIWidgetTag] Empty nameTag on '{tag.gameObject.name}'; ignored.", tag);
                continue;
            }

            handles.Add(tag.CreateHandle());
        }

        return handles;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        if (string.IsNullOrWhiteSpace(nameTag))
            nameTag = gameObject.name;
    }

    private void OnValidate()
    {
        if (nameTag != null)
            nameTag = nameTag.Trim();
    }
#endif
}
