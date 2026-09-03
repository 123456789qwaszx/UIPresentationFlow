using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    public WidgetHandle CreateHandle()
        => new WidgetHandle(InferType(gameObject), NameTag, gameObject, textRole);

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

    private static WidgetType InferType(GameObject go)
    {
        if (go.GetComponent<Button>() != null) return WidgetType.Button;
        if (go.GetComponent<Toggle>() != null) return WidgetType.Toggle;
        if (go.GetComponent<Slider>() != null) return WidgetType.Slider;
        if (go.GetComponent<TMP_Text>() != null) return WidgetType.Text;
        if (go.GetComponent<Image>() != null) return WidgetType.Image;
        return WidgetType.GameObject;
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
