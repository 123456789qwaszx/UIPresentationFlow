using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R = Ked.Samples.UI.BacklogPanel.Refs;
using W = Ked.Samples.UI.BacklogRow.Refs;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// 08 백로그 — Design/08_backlog.png. 줄은 ScrollRect 안의 VerticalLayoutGroup에 쌓인다(줄 높이는 글자에 맞춰 자란다).
    /// 줄 견본(LogRowTemplate)은 숨겨 두고 화면이 복제한다.
    /// </summary>
    public static class BacklogSpec
    {
        public static GameObject Build(KedUIKit kit)
        {
            UINode view = kit.NewView<BacklogPanel>(nameof(BacklogPanel));

            UINode dim = kit.Fill(view, nameof(R.LogDim));
            kit.Solid(dim, KedColors.Dim, raycast: true);

            UINode safe = kit.Fill(view, nameof(R.SafeAreaRoot));
            UINode frame = kit.Frame(safe, "LogFrame");

            UINode panel = kit.Node(frame, nameof(R.LogPanel), 80, 58, 1440, 790);
            kit.Image(panel, "panel_backlog", raycast: true);

            kit.Label(frame, nameof(R.BacklogTitle), "대화 기록", 118, 77, 900, 46, 30, KedColors.White, bold: true);
            kit.IconButton(frame, nameof(R.BacklogClose), "close", new Rect(1430, 76, 44, 44), padding: 11f);

            UINode scroll = kit.Node(frame, nameof(R.LogScroll), 110, 150, 1340, 605);
            kit.Clear(scroll);
            ScrollRect scrollRect = scroll.Go.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;
            scrollRect.inertia = true;

            UINode viewport = kit.Fill(scroll, nameof(R.LogViewport));
            viewport.Go.AddComponent<RectMask2D>();

            UINode content = kit.Node(viewport, nameof(R.LogContent), new Rect(110, 150, 1340, 0), UIAnchor.TopLeft);
            content.Rect.anchorMin = new Vector2(0f, 1f);
            content.Rect.anchorMax = new Vector2(1f, 1f);
            content.Rect.pivot = new Vector2(0.5f, 1f);
            content.Rect.sizeDelta = Vector2.zero;
            content.Rect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = content.Go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.Go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport.Rect;
            scrollRect.content = content.Rect;

            BuildRow(kit, content);

            kit.Label(frame, nameof(R.LogEmpty), "아직 기록이 없습니다.", 145, 180, 1100, 40, 21, KedColors.Muted);
            kit.Label(frame, nameof(R.BacklogHint), "되감기 버튼으로 그 대사로 돌아갑니다. 이미 지난 장면에서는 그 지점부터 새 회차가 시작됩니다.",
                143, 774, 1200, 34, 18, KedColors.Muted);

            return view.Go;
        }

        // 줄: [화자 180 · 대사 985 · 이동 46], 지금 줄은 옅은 청록 바탕, 아래는 구분선.
        private static void BuildRow(KedUIKit kit, UINode content)
        {
            Rect d = new(120, 165, 1320, 98);
            UINode row = kit.Node(content, nameof(R.LogRowTemplate), d);
            row.Go.AddComponent<BacklogRow>();

            HorizontalLayoutGroup layout = row.Go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(25, 32, 7, 20);
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            LayoutElement rowElement = row.Go.AddComponent<LayoutElement>();
            rowElement.minHeight = 86f;

            UINode highlight = kit.Fill(row, nameof(W.RowHighlight));
            kit.Surface(highlight, KedColors.CurrentRow, radius: 9f);
            highlight.Go.AddComponent<LayoutElement>().ignoreLayout = true;
            highlight.Go.SetActive(false);

            UINode speaker = kit.Node(row, nameof(W.RowSpeaker), 145, 172, 180, 38);
            kit.Text(speaker, "", 21, KedColors.SpeakerTeal, TextAlignmentOptions.TopLeft, bold: true,
                overflow: TextOverflowModes.Ellipsis);
            Fixed(speaker.Go, 180f, null);

            UINode text = kit.Node(row, nameof(W.RowText), 345, 172, 985, 66);
            kit.Text(text, "", 23, KedColors.Navy, TextAlignmentOptions.TopLeft, wrap: true, lineSpacing: 6f);
            Fixed(text.Go, 985f, null);

            KedButton jump = kit.IconButton(row, nameof(W.RowJump), "rollback", new Rect(1352, 176, 46, 46), padding: 12f);
            Fixed(jump.gameObject, 46f, 46f);
            // 이동 버튼 위에서 시작한 끌기도 목록을 끈다 — UIPF UI_EventHandler가 끌기를 부모 ScrollRect에 넘긴다.

            UINode divider = kit.StretchX(row, nameof(W.RowDivider), new Rect(145, 258, 1230, 1), 0f);
            divider.Rect.anchoredPosition = new Vector2(divider.Rect.anchoredPosition.x, 1f);
            kit.Solid(divider, KedColors.Divider);
            divider.Go.AddComponent<LayoutElement>().ignoreLayout = true;

            row.Go.SetActive(false);
        }

        private static void Fixed(GameObject go, float width, float? height)
        {
            if (go == null)
                return;

            LayoutElement element = go.AddComponent<LayoutElement>();
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = 0f;

            if (height.HasValue)
            {
                element.minHeight = height.Value;
                element.preferredHeight = height.Value;
            }
        }
    }
}
