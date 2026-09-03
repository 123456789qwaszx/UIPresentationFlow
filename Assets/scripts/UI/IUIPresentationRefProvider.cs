using System.Collections.Generic;
using TMPro;
using UnityEngine;

public interface IUIPresentationRefProvider
{
    IReadOnlyList<string> TextTargetIds { get; }

    bool TryGetRect(string refId, out RectTransform rect);
    bool TryGetText(string refId, out TMP_Text text);
    bool TryGetTextRole(string refId, out UITextRole role);
}