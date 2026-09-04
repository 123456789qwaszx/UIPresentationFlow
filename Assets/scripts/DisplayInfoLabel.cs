using TMPro;
using UnityEngine;

// Demo helper: prints what the resolver saw and decided into a text widget.
// Fed by the composition root after each Show(); it never reads Screen itself.
public sealed class DisplayInfoLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text target;

    public void Set(in DisplayContext display, ResolvedUIPresentation resolved)
    {
        if (target == null)
            target = GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (target == null || resolved == null)
            return;

        string layoutClass = DisplayLayoutClassifier.Classify(display).ToString();
        string layout = resolved.Layout != null ? resolved.Layout.name : "Base";
        string variants = resolved.AppliedVariantIds.Count > 0
            ? string.Join(", ", resolved.AppliedVariantIds)
            : "-";

        target.text =
            $"{display.Resolution.x} x {display.Resolution.y}   aspect {display.AspectRatio:F3}   {layoutClass}   " +
            $"|   presentation: {resolved.PresentationId}   |   layout: {layout}   |   variants: {variants}";
    }
}