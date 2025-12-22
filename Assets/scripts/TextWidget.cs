using TMPro;
using UnityEngine;

public class TextWidget : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    public void SetText(string labelText)
    {
        if (label == null && !TryAutoBindLabel())
            return;
        
        label.text = labelText;
    }
    
    
    private bool TryAutoBindLabel()
    {
        label = GetComponent<TMP_Text>();

        if (label == null)
            label = GetComponentInChildren<TMP_Text>(includeInactive: true);

        if (label != null)
        {
            Debug.LogWarning(
                $"[TextWidget] Auto-bound TMP_Text on '{name}'. " +
                "Consider wiring 'label' field in the prefab for better control.");
            return true;
        }

        Debug.LogWarning(
            $"[TextWidget] No TMP_Text found on '{name}'. " +
            "Set 'label' in inspector or add a TMP_Text child.");
        return false;
    }
}
