using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class WidgetHandle : IWidget
{
    public WidgetType Type { get; }
    public string NameTag { get; }

    public GameObject GameObject { get; }
    public RectTransform RectTransform { get; }

    public Button Button { get; }
    public TMP_Text Text { get; }
    public Image Image { get; }
    public Toggle Toggle { get; }
    public Slider Slider { get; }
    public CanvasGroup CanvasGroup { get; }

    public WidgetHandle(WidgetType type, string nameTag, GameObject go)
    {
        Type         = type;
        NameTag      = nameTag;
        GameObject   = go;
        RectTransform = go.GetComponent<RectTransform>();

        Button      = go.GetComponentInChildren<Button>();
        Text        = go.GetComponentInChildren<TMP_Text>();
        Image       = go.GetComponentInChildren<Image>();
        Toggle      = go.GetComponentInChildren<Toggle>();
        Slider      = go.GetComponentInChildren<Slider>();
        CanvasGroup = go.GetComponent<CanvasGroup>();
    }

    public void SetActive(bool active) => GameObject.SetActive(active);
}