using UnityEngine;

[DisallowMultipleComponent]
public sealed class UISlot : MonoBehaviour
{
    public string id;

    public RectTransform target;

    public string Id => (id ?? string.Empty).Trim();

    private void Reset()
    {
        // 컴포넌트를 처음 붙였을 때 한 번 호출
        if (string.IsNullOrWhiteSpace(id))
            id = gameObject.name;

        if (target == null)
            target = GetComponent<RectTransform>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 공백 정리
        if (id != null)
            id = id.Trim();

        // target 비어 있으면 자기 RectTransform 자동 할당
        if (target == null)
            target = GetComponent<RectTransform>();
    }
#endif
}