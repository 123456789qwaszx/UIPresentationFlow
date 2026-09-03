using TMPro;
using UnityEngine;

public sealed class PresentationMetadataVisualTest : MonoBehaviour
{
    [SerializeField] private TitleUIRoot titleRoot;

    private void Start()
    {
        IUIPresentationRefProvider provider = titleRoot;

        foreach (string refId in provider.TextTargetIds)
        {
            if (!provider.TryGetText(refId, out TMP_Text text))
            {
                Debug.LogWarning($"Text not found: {refId}");
                continue;
            }

            if (!provider.TryGetTextRole(refId, out UITextRole role))
            {
                Debug.LogWarning($"TextRole not found: {refId}");
                continue;
            }

            Debug.Log($"{refId} -> {role}");

            switch (role)
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