using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R = Ked.Samples.UI.TitleScreen.Refs;

namespace Ked.Samples.UI.Editor
{
    /// <summary>01 타이틀 — Design/01_title.png · layouts.json "title".</summary>
    public static class TitleSpec
    {
        public static GameObject Build(KedUIKit kit)
        {
            UINode view = kit.NewView<TitleScreen>(nameof(TitleScreen));
            kit.Backdrop(view, nameof(R.Background), "background_title");

            UINode safe = kit.Fill(view, nameof(R.SafeAreaRoot));
            UINode frame = kit.Frame(safe, nameof(R.Frame));

            TextMeshProUGUI brand = kit.Label(frame, nameof(R.Brand), "KED", 110, 100, 500, 128, 90, KedColors.Navy, bold: true);
            brand.characterSpacing = 11f;

            TextMeshProUGUI subtitle = kit.Label(frame, nameof(R.BrandSubtitle), "PRESENTATION RUNTIME", 116, 213, 650, 35, 18, KedColors.Navy, bold: true);
            subtitle.characterSpacing = 16f;

            kit.Label(frame, nameof(R.Tagline), "이야기가 화면이 되는 순간", 114, 277, 650, 60, 32, KedColors.Navy, bold: true);
            kit.Label(frame, nameof(R.CurrentExample), "현재 예제  /  —", 116, 345, 600, 36, 20, KedColors.Muted);

            // 버튼 열: 예제에 따라 불러오기가 빠진다 — 세로 레이아웃이 자리를 메운다.
            UINode column = kit.Node(frame, nameof(R.MenuColumn), 110, 422, 390, 444);
            VerticalLayoutGroup layout = column.Go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            kit.TextButton(column, nameof(R.NewGame), nameof(R.NewGameLabel), "처음부터", new Rect(110, 422, 390, 68), KedButtonStyle.Primary);
            kit.TextButton(column, nameof(R.Continue), nameof(R.ContinueLabel), "이어하기", new Rect(110, 504, 390, 68), KedButtonStyle.Secondary);
            kit.TextButton(column, nameof(R.Load), nameof(R.LoadLabel), "불러오기", new Rect(110, 586, 390, 68), KedButtonStyle.Secondary);
            kit.TextButton(column, nameof(R.SwitchExample), nameof(R.SwitchExampleLabel), "다른 예제로", new Rect(110, 668, 390, 68), KedButtonStyle.Secondary);
            kit.TextButton(column, nameof(R.OpenSettings), nameof(R.OpenSettingsLabel), "설정", new Rect(110, 750, 390, 68), KedButtonStyle.Secondary);
            kit.LinkButton(column, nameof(R.Quit), nameof(R.QuitLabel), "종료", new Rect(110, 832, 140, 32), 19, KedColors.Muted);

            // 예제 카드: 둥근 틀 안에 실제 예제 배경(런타임에 호스트가 준다)과 이름 띠.
            UINode preview = kit.Node(frame, nameof(R.ScenePreview), 700, 169, 780, 486);
            kit.Surface(preview, KedColors.Hex(0xE4F6F6), radius: 16f);

            UINode mask = kit.Node(preview, "PreviewMask", 703, 172, 774, 480);
            kit.Surface(mask, KedColors.Divider, radius: 13f);
            mask.Go.AddComponent<Mask>().showMaskGraphic = true;

            UINode scene = kit.Fill(mask, nameof(R.PreviewScene));
            Image sceneImage = kit.Clear(scene, raycast: false);
            sceneImage.color = KedColors.Divider;
            AspectRatioFitter fitter = scene.Go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;

            UINode caption = kit.Node(mask, nameof(R.PreviewCaption), 703, 550, 774, 102);
            kit.Solid(caption, KedColors.PreviewCaption);

            kit.Label(mask, nameof(R.PreviewTitle), "—", 737, 565, 600, 42, 30, KedColors.White, bold: true);
            kit.Label(mask, nameof(R.PreviewSub), "—", 739, 612, 700, 26, 17, KedColors.PreviewSub);

            kit.Label(frame, nameof(R.BuildLabel), "Ked Presentation Runtime · UI 샘플", 1010, 827, 480, 28, 16, KedColors.Muted,
                TextAlignmentOptions.Right);

            return view.Go;
        }
    }
}
