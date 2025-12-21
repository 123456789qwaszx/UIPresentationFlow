using UnityEngine;

public sealed class UISlot : MonoBehaviour
{
    [Tooltip("Slot identifier. e.g. Header / Body / Footer")]
    public string id;

    [Tooltip("Optional: If assigned, this transform will be used as the slot root instead of this object.")]
    public RectTransform target;
}