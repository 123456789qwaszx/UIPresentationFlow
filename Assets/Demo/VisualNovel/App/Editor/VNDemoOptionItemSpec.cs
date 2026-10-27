using Ked.Samples.UI;
using Ked.Samples.UI.Editor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VisualNovelDemo.EditorTools
{
    /// <summary>
    /// 데모의 선택지 항목(760×74) — ked의 선택지 항목(OptionItemSpec)과 같은 그림 · 자리를 같은 키트로 짓는다.
    /// ked는 본체의 VNOptionItem을 붙이고, 데모는 VNDemoOptionItem(버튼 + 이름 + 포커스 테두리)을 붙인다.
    /// 시안 좌표는 선택지 상자의 첫 칸 자리(420, 310)다 — 항목은 상자의 세로 레이아웃이 쌓는다.
    /// </summary>
    internal static class VNDemoOptionItemSpec
    {
        public static GameObject Build(KedUIKit kit)
        {
            var go = new GameObject(nameof(VNDemoOptionItem), typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 74f);

            var root = new UINode(rect, new Rect(420, 310, 760, 74));

            Image background = kit.Image(root, "choice_normal", raycast: true);

            LayoutElement element = go.AddComponent<LayoutElement>();
            element.preferredWidth = 760f;
            element.preferredHeight = 74f;

            UINode label = kit.Fill(root, "Label", 40f, 6f, 50f, 8f);
            TextMeshProUGUI labelText = kit.Text(label, "", 25, KedColors.Navy, TextAlignmentOptions.Center, bold: true,
                overflow: TextOverflowModes.Ellipsis);

            UINode indicator = kit.Fill(root, "SelectionIndicator", -4f, -4f, -4f, -4f);
            kit.Outline(indicator, KedColors.Turquoise, 3f);
            indicator.Go.SetActive(false);

            Button button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = background;

            // 누름 0.98 · 올림 크기 변화 없음 — 키트의 다른 버튼과 같다(올림은 테두리로 보인다).
            ButtonAnimation animation = go.AddComponent<ButtonAnimation>();
            UISerialized.SetFloat(animation, "_hoverScale", 1f);
            UISerialized.SetFloat(animation, "_pressScale", 0.98f);

            VNDemoOptionItem item = go.AddComponent<VNDemoOptionItem>();
            UISerialized.SetObject(item, "button", button);
            UISerialized.SetObject(item, "label", labelText);
            UISerialized.SetObject(item, "selectionIndicator", indicator.Go);

            return go;
        }
    }
}
