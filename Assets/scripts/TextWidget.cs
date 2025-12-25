using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class TextWidget : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    public void SetText(string labelText)
    {
        if (text == null && !TryAutoBindLabel())
            return;
        
        text.text = labelText;
    }
    
    
    private bool TryAutoBindLabel()
    {
        text = GetComponent<TMP_Text>();

        if (text == null)
            text = GetComponentInChildren<TMP_Text>(includeInactive: true);

        if (text != null)
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
