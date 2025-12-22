using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonWidget : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;

    public void SetLabel(string labelText)
    {
        if (label == null && !TryAutoBindLabel())
            return;
        
        label.text = labelText;
    }

    public void SetOnClick(Action action)
    {
        if (button == null && !TryAutoBindButton())
            return;
        
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => action?.Invoke());
    }
    
    
    private bool TryAutoBindButton()
    {
        button = GetComponent<Button>();

        if (button == null)
            button = GetComponentInChildren<Button>(includeInactive: true);

        if (button != null)
        {
            Debug.LogWarning(
                $"[ButtonWidget] Auto-Add Button on '{name}'. " +
                "Consider wiring 'button' field in the prefab for better control.");
            return true;
        }

        Debug.LogError(
            $"[ButtonWidget] No Button found on '{name}'. " +
            "Set 'button' field in inspector or ensure there is a Button child.");
        return false;
    }

    private bool TryAutoBindLabel()
    {
        if (label != null)
            return true;

        if (button != null)
            label = button.GetComponentInChildren<TMP_Text>(includeInactive: true);

        if (label == null)
            label = GetComponentInChildren<TMP_Text>(includeInactive: true);

        if (label != null)
        {
            Debug.LogWarning(
                $"[ButtonWidget] Auto-Add TMP_Text label on '{name}'. " +
                "Consider wiring 'label' field in the prefab for better performance.");
            return true;
        }

        Debug.LogError(
            $"[ButtonWidget] No TMP_Text (label) found on '{name}'. " +
            "Set 'label' in inspector or ensure there is a TMP_Text child.");
        return false;
    }
}
