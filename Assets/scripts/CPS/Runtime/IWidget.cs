using UnityEngine;

public interface IWidget
{
    WidgetType Type { get; }
    string NameTag { get; }

    GameObject GameObject { get; }
    RectTransform RectTransform { get; }

    void SetActive(bool active);
}