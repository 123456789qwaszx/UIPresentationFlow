using System.Collections.Generic;
using UnityEngine;

public sealed class UIImageBindingSet : ScriptableObject
{
    // Concrete UI View type의 FullName.
    // 예: "TitleUIRoot"
    // namespace가 있다면 "Game.UI.TitleUIRoot"
    public string viewId;

    public List<UIImageBindingEntry> images = new();
}