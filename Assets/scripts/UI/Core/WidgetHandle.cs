using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A resolved reference to one tagged widget. Patches address widgets through
// this, by NameTag, so they never depend on hierarchy or GameObject names.
public sealed class WidgetHandle
{
    public string NameTag { get; }

    public GameObject    GameObject    { get; }
    public RectTransform RectTransform { get; }

    public Button      Button      { get; }
    public TMP_Text    Text        { get; }
    public Image       Image       { get; }
    public Toggle      Toggle      { get; }
    public Slider      Slider      { get; }
    public CanvasGroup CanvasGroup { get; }

    public UITextRole TextRole { get; }

    public WidgetHandle(string nameTag, GameObject go, UITextRole textRole = UITextRole.Body)
    {
        NameTag       = nameTag;
        GameObject    = go;
        RectTransform = go.GetComponent<RectTransform>();

        Button      = go.GetComponentInChildren<Button>(true);
        Text        = go.GetComponentInChildren<TMP_Text>(true);
        Image       = go.GetComponentInChildren<Image>(true);
        Toggle      = go.GetComponentInChildren<Toggle>(true);
        Slider      = go.GetComponentInChildren<Slider>(true);
        CanvasGroup = go.GetComponent<CanvasGroup>();

        TextRole = textRole;
    }

    public void SetActive(bool active) => GameObject.SetActive(active);
}
