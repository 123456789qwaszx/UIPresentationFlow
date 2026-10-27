using System.Collections.Generic;
using TMPro;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// TMP 글꼴 자산. 시안의 Noto Sans KR은 가변 글꼴(wght 100–900)이고 기본 인스턴스가 Thin이라 TMP가 그대로 쓰면
    /// 가는 글자가 된다(IMPLEMENTATION §3). 그래서 같은 글꼴에서 뽑은 정적 굵기 둘(Medium 500 · Bold 700, OFL 그대로)을 쓴다.
    /// 둘 다 동적 SDF — 한국어 글자는 쓰는 대로 아틀라스에 들어간다. Bold는 Medium의 굵기 표(700)에 걸어
    /// 글자 스타일 Bold가 가짜 굵게가 아니라 Bold 글꼴로 그려지게 한다.
    /// 만들기 · 비우기 · 굵기 표는 UIPF의 UIFontAssets(Editor/UI/Authoring)가 하고, 여기는 KedUI의 글꼴과 자리만 둔다.
    /// </summary>
    public static class KedUIFonts
    {
        // 원본 TTF와 그것으로 만든 SDF 자산을 한 폴더에 둔다 — 여러 화면이 같이 쓰는 Common의 몫.
        public static string FontDir => KedUIUnits.CommonDir + "/Fonts";
        public static string OutputDir => FontDir;
        public static string MediumAssetPath => OutputDir + "/NotoSansKR-Medium SDF.asset";
        public static string BoldAssetPath => OutputDir + "/NotoSansKR-Bold SDF.asset";

        public static TMP_FontAsset Ensure(List<string> problems)
        {
            UIPrefabs.EnsureFolder(OutputDir);

            TMP_FontAsset bold = UIFontAssets.EnsureDynamic(FontDir + "/NotoSansKR-Bold.ttf", BoldAssetPath, problems);
            TMP_FontAsset medium = UIFontAssets.EnsureDynamic(FontDir + "/NotoSansKR-Medium.ttf", MediumAssetPath, problems);

            if (medium != null && bold != null)
                UIFontAssets.SetWeightTypeface(medium, 700, bold, problems);

            return medium;
        }

        /// <summary>
        /// 빌드 끝에 두 동적 글꼴이 그동안 채운 글자와 아틀라스를 비운다 — 커밋되는 글꼴 자산을 작게(8MB → 몇 KB).
        /// 실행 중 쓰는 글자는 다시 채워진다.
        /// </summary>
        public static void ClearDynamicData(List<string> problems)
        {
            UIFontAssets.ClearDynamicData(MediumAssetPath, problems);
            UIFontAssets.ClearDynamicData(BoldAssetPath, problems);
        }
    }
}
