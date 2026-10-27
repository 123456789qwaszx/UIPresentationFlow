using UnityEngine;

// 시안 좌표로 노드를 놓는다(UINode). 노드의 모양(그림 · 글자 · 버튼)은 정하지 않는다 — 그것은 게임의 부품 키트 몫이다.
//
//   Root     부모 없는 뿌리(화면 · 위젯 프리팹). 부모(층 · PageRoot)를 꽉 채운다
//   Node     앵커 한 점에 고정. 시안 판 크기에서 정확히 시안 자리에 온다
//   Fill     부모를 꽉 채운다(안쪽 여백)
//   Stretch  시안 사각형을 네 변 앵커로 늘인다(부모가 커지면 같이 커진다)
//   StretchX 가로로만 늘인다
public static class UINodes
{
    // 화면(View) 프리팹의 뿌리 + 화면 컴포넌트. design은 부모가 차지하는 시안 사각형(보통 판 전체).
    // 뿌리에 다른 컴포넌트(CanvasGroup · 딤 Image)를 화면보다 먼저 붙여야 하면 Root를 쓰고 직접 붙인다.
    public static UINode View<T>(string name, Rect design) where T : Component
    {
        UINode node = Root(name, design);
        node.Go.AddComponent<T>();
        return node;
    }

    public static UINode Root(string name, Rect design)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");

        var rect = (RectTransform)go.transform;
        StretchToParent(rect);

        return new UINode(rect, design);
    }

    public static UINode Node(UINode parent, string name, Rect design)
        => Node(parent, name, design, UIAnchor.TopLeft, UIAnchor.TopLeft);

    // 기준점(pivot)도 앵커와 같은 점.
    public static UINode Node(UINode parent, string name, Rect design, Vector2 anchor)
        => Node(parent, name, design, anchor, anchor);

    // 앵커와 기준점이 다를 때 — 예: 버튼 아래 변에 매단 글자(앵커는 아래 가운데, 기준점은 위 가운데).
    public static UINode Node(UINode parent, string name, Rect design, Vector2 anchor, Vector2 pivot)
    {
        RectTransform rect = NewRect(parent, name);
        Place(rect, design, parent.Design, anchor, pivot);
        return new UINode(rect, design);
    }

    // 안쪽 여백은 왼 · 위 · 오른 · 아래(음수면 밖으로).
    public static UINode Fill(UINode parent, string name, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
    {
        RectTransform rect = NewRect(parent, name);
        StretchToParent(rect);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);

        Rect d = parent.Design;
        return new UINode(rect, new Rect(d.x + left, d.y + top, d.width - left - right, d.height - top - bottom));
    }

    public static UINode Stretch(UINode parent, string name, Rect design)
    {
        RectTransform rect = NewRect(parent, name);
        Rect p = parent.Design;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(design.x - p.x, (p.y + p.height) - (design.y + design.height));
        rect.offsetMax = new Vector2((design.x + design.width) - (p.x + p.width), -(design.y - p.y));

        return new UINode(rect, design);
    }

    // 세로는 anchorY 한 줄에 고정한다(1 = 위, 0.5 = 가운데, 0 = 아래).
    public static UINode StretchX(UINode parent, string name, Rect design, float anchorY)
    {
        RectTransform rect = NewRect(parent, name);
        Rect p = parent.Design;

        rect.anchorMin = new Vector2(0f, anchorY);
        rect.anchorMax = new Vector2(1f, anchorY);
        rect.pivot = new Vector2(0.5f, anchorY);

        float left = design.x - p.x;
        float right = (p.x + p.width) - (design.x + design.width);
        float pivotDown = design.y + design.height * (1f - anchorY);
        float anchorDown = p.y + p.height * (1f - anchorY);

        rect.sizeDelta = new Vector2(-(left + right), design.height);
        rect.anchoredPosition = new Vector2((left - right) * 0.5f, -(pivotDown - anchorDown));

        return new UINode(rect, design);
    }

    public static void Place(RectTransform rect, Rect design, Rect parent, Vector2 anchor)
        => Place(rect, design, parent, anchor, anchor);

    public static void Place(RectTransform rect, Rect design, Rect parent, Vector2 anchor, Vector2 pivot)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = new Vector2(design.width, design.height);

        // 시안은 아래로 +y, Unity는 위로 +y — 세로는 "위에서 잰 거리"로 계산하고 부호를 뒤집는다.
        float px = design.x + design.width * pivot.x;
        float pyDown = design.y + design.height * (1f - pivot.y);
        float qx = parent.x + parent.width * anchor.x;
        float qyDown = parent.y + parent.height * (1f - anchor.y);

        rect.anchoredPosition = new Vector2(px - qx, -(pyDown - qyDown));
    }

    // 자리는 두고 기준점만 가운데로 — 누름 연출(ButtonAnimation, localScale)이 가운데로 줄어들게.
    public static void CenterPivot(RectTransform rect)
    {
        Vector2 center = new(0.5f, 0.5f);
        Vector2 delta = center - rect.pivot;

        rect.pivot = center;
        rect.anchoredPosition += new Vector2(delta.x * rect.sizeDelta.x, delta.y * rect.sizeDelta.y);
    }

    private static RectTransform NewRect(UINode parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");

        var rect = (RectTransform)go.transform;
        rect.SetParent(parent.Rect, false);
        return rect;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
