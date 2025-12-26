using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WidgetHandle : IWidget
{
    public WidgetType Type { get; }
    public string NameTag { get; }

    public GameObject GameObject { get; }
    public RectTransform RectTransform { get; }

    // “능력” 후보들
    public Button Button { get; }
    public TMP_Text Text { get; }
    public Image Image { get; }
    public CanvasGroup CanvasGroup { get; }

    public WidgetHandle(WidgetType type, string nameTag, GameObject go)
    {
        Type        = type;
        NameTag     = nameTag;
        GameObject  = go;
        RectTransform = go.GetComponent<RectTransform>();

        Button      = go.GetComponentInChildren<Button>();
        Text        = go.GetComponentInChildren<TMP_Text>();
        Image       = go.GetComponentInChildren<Image>();
        CanvasGroup = go.GetComponent<CanvasGroup>(); // 없으면 null 유지
    }

    public void SetActive(bool active) => GameObject.SetActive(active);
}