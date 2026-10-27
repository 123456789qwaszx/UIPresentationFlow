using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R = Ked.Samples.UI.PlayScreen.Refs;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// 02 대화 · 03 선택지 — Design/02_dialogue.png · 03_choices.png. 배경 그림(시안 샘플)은 넣지 않는다 —
    /// 이 화면은 투명하고, 무대는 기존 연출 무대가 그린다.
    ///
    /// 화면비: 꼬리표는 왼쪽 위, 퀵메뉴는 오른쪽 위, 대사창은 아래 가운데에 붙는다(Expand 캔버스에서 남는 폭·높이는 가장자리로).
    /// 21:9 이상은 Presentation의 Variant가 꼬리표와 퀵메뉴를 가운데 판의 양 끝으로 옮긴다(EnsureVariants).
    /// </summary>
    public static class PlaySpec
    {
        public const string UltraWideVariant = "UltraWide_HudInBoard";

        private static readonly (string key, string label)[] Quick =
        {
            ("auto", "자동"),
            ("fast_forward", "빨리 넘기기"),
            ("rollback", "되감기"),
            ("backlog", "기록"),
            ("hide_ui", "숨기기"),
            ("menu", "메뉴"),
        };

        public static GameObject Build(KedUIKit kit)
        {
            UINode view = kit.NewView<PlayScreen>(nameof(PlayScreen));

            // 화면 아무 데나 누르면 다음 대사. 키보드 포커스는 받지 않는다(Space는 재생 입력이 따로 읽는다).
            UINode advance = kit.Fill(view, nameof(R.AdvanceArea));
            Image advanceHit = kit.Clear(advance);
            Button advanceButton = advance.Go.AddComponent<Button>();
            advanceButton.transition = Selectable.Transition.None;
            advanceButton.targetGraphic = advanceHit;
            advanceButton.navigation = new Navigation { mode = Navigation.Mode.None };

            UINode safe = kit.Fill(view, nameof(R.SafeAreaRoot));
            UINode hud = kit.Fill(safe, nameof(R.HudRoot));
            hud.Go.AddComponent<CanvasGroup>();

            BuildTopBar(kit, hud);
            BuildDialogue(kit, hud);
            BuildOptions(kit, hud);
            BuildRestore(kit, view);

            return view.Go;
        }

        /// <summary>
        /// 재생 화면 Presentation의 화면비 Variant — 처음 한 번 만드는 데이터(UIPF UIPresentationAssets.EnsureLayoutVariant).
        /// 앱 빌드가 Presentation을 만든 뒤 부른다. 이미 있으면 건드리지 않는다(에셋에서 고친 값이 남는다).
        ///
        /// UltraWide(21:9 이상): 꼬리표와 퀵메뉴를 화면 끝이 아니라 가운데 1600 판의 양 끝에 둔다 — 대사창(1400, 가운데)과
        /// 같은 판에 서게. 기본(화면 끝)은 21:9에서 퀵메뉴가 대사창 오른쪽 끝보다 250 넘게 밖으로 나간다.
        /// </summary>
        public static bool EnsureVariants(UIPresentationSpec spec)
        {
            float half = KedUIKit.Screen.width * 0.5f;

            return UIPresentationAssets.EnsureLayoutVariant(
                spec,
                UltraWideVariant,
                priority: 10,
                new VariantCondition { useLayoutClass = true, layoutClass = DisplayLayoutClass.UltraWide },
                patch =>
                {
                    // 꼬리표: 화면 왼쪽 끝에서 64 → 판 왼쪽 끝에서 64(기준점은 왼쪽 위 그대로).
                    patch.widgets.Add(UIPresentationAssets.MoveWidget(
                        nameof(R.ChapterTag),
                        anchorMin: UIAnchor.TopCenter,
                        anchoredPosition: new Vector2(-half + 64f, -40f)));

                    // 퀵메뉴: 화면 오른쪽 끝에서 52 → 판 오른쪽 끝에서 52(기준점은 오른쪽 위 그대로).
                    patch.widgets.Add(UIPresentationAssets.MoveWidget(
                        nameof(R.QuickMenu),
                        anchorMin: UIAnchor.TopCenter,
                        anchoredPosition: new Vector2(half - 52f, -26f)));
                });
        }

        private static void BuildTopBar(KedUIKit kit, UINode hud)
        {
            UINode gradient = kit.StretchX(hud, nameof(R.TopGradient), new Rect(0, 0, 1600, 130), 1f);
            kit.Solid(gradient, Color.white);
            KedGradient look = gradient.Go.AddComponent<KedGradient>();
            look.SetColors(KedColors.TopGradient, KedColors.WithAlpha(KedColors.TopGradient, 0f));

            // 꼬리표: 글자 길이만큼 늘어나는 남색 알약.
            UINode tag = kit.Node(hud, nameof(R.ChapterTag), 64, 40, 300, 42, UIAnchor.TopLeft);
            kit.Surface(tag, KedColors.Navy, radius: 21f);
            HorizontalLayoutGroup tagLayout = tag.Go.AddComponent<HorizontalLayoutGroup>();
            tagLayout.padding = new RectOffset(18, 18, 6, 6);
            tagLayout.childAlignment = TextAnchor.MiddleLeft;
            tagLayout.childControlWidth = true;
            tagLayout.childControlHeight = true;
            tagLayout.childForceExpandWidth = false;
            tagLayout.childForceExpandHeight = true;
            ContentSizeFitter tagFitter = tag.Go.AddComponent<ContentSizeFitter>();
            tagFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            UINode tagText = kit.Node(tag, nameof(R.ChapterTagText), 82, 46, 264, 30);
            kit.Text(tagText, "—", 16, KedColors.White, TextAlignmentOptions.Left);

            // 퀵메뉴: 상아색 판 위에 아이콘 여섯과 이름.
            UINode quick = kit.Node(hud, nameof(R.QuickMenu), 1000, 26, 548, 96, UIAnchor.TopRight);
            kit.Surface(quick, KedColors.QuickSurface, radius: 18f, raycast: true);

            for (int i = 0; i < Quick.Length; i++)
            {
                (string key, string label) = Quick[i];

                kit.IconButton(quick, "Quick_" + key, key, new Rect(1021 + i * 86, 34, 52, 52), padding: 13f);
                kit.Label(quick, "QuickLabel_" + key, label, 1007 + i * 86, 88, 80, 24, 13, KedColors.Navy,
                    TextAlignmentOptions.Center);
            }
        }

        private static void BuildDialogue(KedUIKit kit, UINode hud)
        {
            // 대사창 판(1600×900, 아래 가운데). 본체 프리셋의 줄·이름 자리는 이 판에 대한 비율이다.
            UINode frame = kit.Node(hud, nameof(R.DialogueFrame), KedUIKit.Screen, UIAnchor.BottomCenter);
            CanvasGroup group = frame.Go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            UINode box = kit.Node(frame, nameof(R.DialogueBox), 100, 644, 1400, 210);
            kit.Image(box, "dialogue_box");

            UINode plate = kit.Node(frame, nameof(R.SpeakerPlate), 130, 605, 332, 58);
            kit.Image(plate, "speaker_nameplate");

            UINode speaker = kit.Node(frame, nameof(R.Speaker), 156, 610, 250, 40);
            kit.Text(speaker, "", 26, KedColors.Navy, TextAlignmentOptions.Center, bold: true);

            UINode line = kit.Node(frame, nameof(R.DialogueText), 156, 693, 1230, 106);
            kit.Text(line, "", 27, KedColors.Navy, TextAlignmentOptions.TopLeft, wrap: true, lineSpacing: 18f);

            UINode marker = kit.Node(frame, nameof(R.NextMarker), 1415, 795, 26, 26);
            kit.Image(marker, "next_marker", preserveAspect: true);
            marker.Go.SetActive(false);

            // 진행 키 안내. 장면 그림이 밝아도 읽히게 어두운 알약 위에 흰 글자.
            UINode hintPlate = kit.Node(frame, "AdvanceHintPlate", 1406, 860, 144, 30);
            kit.Surface(hintPlate, KedColors.Dim, radius: 15f);
            UINode hint = kit.Node(hintPlate, nameof(R.AdvanceHint), 1406, 860, 144, 30);
            kit.Text(hint, "Space  다음", 15, KedColors.White, TextAlignmentOptions.Center);
        }

        private static void BuildOptions(KedUIKit kit, UINode hud)
        {
            UINode root = kit.Fill(hud, nameof(R.OptionsRoot));
            CanvasGroup group = root.Go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            UINode dim = kit.StretchX(root, nameof(R.ChoiceDim), new Rect(0, 126, 1600, 470), 0.5f);
            kit.Solid(dim, KedColors.ChoiceDim, raycast: true);

            // 항목은 재생 쪽이 만든다(KedOptionItem 프리팹, 760×74). 가운데를 기준으로 위아래로 쌓인다.
            UINode container = kit.Node(root, nameof(R.OptionsContainer), new Rect(420, 310, 760, 254), UIAnchor.Center);
            VerticalLayoutGroup layout = container.Go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = container.Go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            container.Go.AddComponent<KedChildWatcher>();
        }

        private static void BuildRestore(KedUIKit kit, UINode view)
        {
            UINode restore = kit.Fill(view, nameof(R.RestoreArea));
            Image hit = kit.Clear(restore);
            Button button = restore.Go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hit;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            UINode plate = kit.Node(restore, "RestoreHintPlate", 560, 836, 480, 36, UIAnchor.BottomCenter);
            kit.Surface(plate, KedColors.Dim, radius: 18f);
            UINode hint = kit.Node(plate, nameof(R.RestoreHint), 560, 836, 480, 36);
            kit.Text(hint, "누르거나 Space · Esc로 다시 보입니다", 16, KedColors.White, TextAlignmentOptions.Center);

            restore.Go.SetActive(false);
        }
    }
}
