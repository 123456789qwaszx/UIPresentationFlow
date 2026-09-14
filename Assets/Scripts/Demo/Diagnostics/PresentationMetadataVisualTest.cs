using TMPro;
using UnityEngine;

public sealed class PresentationMetadataVisualTest : MonoBehaviour
{
    [SerializeField] private TitleUIRoot titleRoot;
    [SerializeField] private UITextBindingCatalog textBindings;

    private void Start()
    {
        if (titleRoot == null || textBindings == null)
            return;

        IUIPresentationRefProvider provider = titleRoot;

        if (!textBindings.TryGetBinding(
                titleRoot.GetType(),
                out UITextBindingSet binding)
            || binding?.texts == null)
        {
            Debug.LogWarning("TextBinding not found for TitleUIRoot.", titleRoot);
            return;
        }

        foreach (UITextBindingEntry entry in binding.texts)
        {
            if (entry == null
                || entry.stale
                || entry.role == UITextRole.Unassigned)
            {
                continue;
            }

            if (!provider.TryGetText(entry.refId, out TMP_Text text))
            {
                Debug.LogWarning($"Text not found: {entry.refId}");
                continue;
            }

            Debug.Log($"{entry.refId} -> {entry.role}");

            switch (entry.role)
            {
                case UITextRole.Title:
                    text.fontSize = 60;
                    break;

                case UITextRole.Body:
                    text.fontSize = 30;
                    break;

                case UITextRole.Caption:
                    text.fontSize = 18;
                    break;
            }
        }
    }
}
