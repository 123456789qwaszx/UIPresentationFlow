using UnityEngine;

// 빌드 명세가 짓는 노드 하나: RectTransform과 그 노드의 시안 사각형.
//
// 시안 좌표는 왼쪽 위가 원점이고 아래로 +y다 — 디자인 도구의 숫자 그대로. 자식도 시안의 절대 좌표로 적고,
// UINodes가 부모의 시안 사각형을 빼서 Unity 앵커 좌표로 바꾼다. 그래서 명세의 숫자가 시안의 숫자와 같다.
// 시안 판의 크기는 게임마다 다르다(ked 1600×900, 블랙홀 1920×1080). 화면 뿌리를 만들 때 판 사각형을 넘긴다.
public sealed class UINode
{
    public readonly RectTransform Rect;
    public readonly Rect Design;

    public UINode(RectTransform rect, Rect design)
    {
        Rect = rect;
        Design = design;
    }

    public GameObject Go => Rect.gameObject;
}

// 앵커 아홉 자리. Unity 규칙(x는 오른쪽, y는 위쪽으로 0–1).
public static class UIAnchor
{
    public static readonly Vector2 TopLeft = new(0f, 1f);
    public static readonly Vector2 TopCenter = new(0.5f, 1f);
    public static readonly Vector2 TopRight = new(1f, 1f);
    public static readonly Vector2 MiddleLeft = new(0f, 0.5f);
    public static readonly Vector2 Center = new(0.5f, 0.5f);
    public static readonly Vector2 MiddleRight = new(1f, 0.5f);
    public static readonly Vector2 BottomLeft = new(0f, 0f);
    public static readonly Vector2 BottomCenter = new(0.5f, 0f);
    public static readonly Vector2 BottomRight = new(1f, 0f);
}
