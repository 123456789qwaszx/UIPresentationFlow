using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// Ked UI 시안(Docs/IMPLEMENTATION.md §3)의 색. 빌더가 프리팹을 만들 때와 화면이 상태를 그릴 때 같은 값을 쓴다.
    /// 그림(PNG)에 이미 칠해진 색은 여기 없다 — 코드가 칠하는 글자·단색 도형·상태 표시만.
    /// </summary>
    public static class KedColors
    {
        public static readonly Color Navy = Hex(0x29476C);
        public static readonly Color Ivory = Hex(0xF8F8F1);
        public static readonly Color Turquoise = Hex(0x13BDC5);
        public static readonly Color Muted = Hex(0x718598);
        public static readonly Color Divider = Hex(0xD6E2E8);
        public static readonly Color White = Color.white;

        // 딤: #142B45, 투명도 시안 그대로(메뉴·백로그·팝업 aa, 선택지 40).
        public static readonly Color Dim = Hex(0x142B45, 0xAA);
        public static readonly Color ChoiceDim = Hex(0x142B45, 0x40);

        public static readonly Color QuickSurface = Hex(0xF8F8F1, 0xEF);
        public static readonly Color SurfaceLine = Hex(0xC8DCE3);
        public static readonly Color SpeakerTeal = Hex(0x079AA5);
        public static readonly Color CurrentRow = Hex(0xDFF4F4);
        public static readonly Color PreviewCaption = Hex(0x17334B, 0xD9);
        public static readonly Color PreviewSub = Hex(0xD6F0F2);
        public static readonly Color TopGradient = Hex(0x13263B, 0x55);
        public static readonly Color ActiveSurface = Hex(0x13BDC5, 0x40);

        // 막힌 버튼의 투명도(시안 .45–.55).
        public const float DisabledAlpha = 0.5f;

        public static Color Hex(int rgb, int alpha = 0xFF)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                alpha / 255f);
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
