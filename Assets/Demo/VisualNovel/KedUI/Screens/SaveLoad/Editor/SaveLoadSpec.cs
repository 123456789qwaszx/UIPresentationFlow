using TMPro;
using UnityEngine;
using UnityEngine.UI;
using C = Ked.Samples.UI.SaveSlotCard.Refs;
using R = Ked.Samples.UI.SaveLoadPanel.Refs;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// 05 저장 · 06 불러오기 — Design/05_save.png · 06_load.png. 한 프리팹, 두 모드(글자만 다르다).
    /// 슬롯 카드 여섯은 GridLayoutGroup(3열, 440×190, 간격 40×48)에 놓인 같은 카드(SaveSlotCard)다.
    /// </summary>
    public static class SaveLoadSpec
    {
        public static GameObject Build(KedUIKit kit)
        {
            UINode view = kit.NewView<SaveLoadPanel>(nameof(SaveLoadPanel));
            kit.Backdrop(view, nameof(R.Background), "background_title");

            UINode safe = kit.Fill(view, nameof(R.SafeAreaRoot));
            UINode frame = kit.Frame(safe, nameof(R.Frame));

            kit.IconButton(frame, nameof(R.Back), "back", new Rect(64, 38, 58, 58), padding: 15f);
            kit.Label(frame, nameof(R.PageTitle), "저장", 144, 37, 700, 56, 36, KedColors.Navy, bold: true);
            kit.Label(frame, nameof(R.PageSubtitle), "현재 장면의 기록을 관리합니다.", 146, 96, 1100, 32, 17, KedColors.Muted);
            kit.IconButton(frame, nameof(R.Close), "close", new Rect(1478, 38, 58, 58), padding: 16f);

            UINode tab = kit.Node(frame, nameof(R.ManualTab), 100, 149, 250, 54);
            kit.Image(tab, "tab_selected");
            kit.Label(frame, nameof(R.ManualLabel), "수동 저장", 115, 157, 210, 35, 21, KedColors.White,
                TextAlignmentOptions.Center, bold: true);

            kit.Label(frame, nameof(R.PageInfo), "PAGE  01 / 01", 1160, 161, 340, 34, 18, KedColors.Muted,
                TextAlignmentOptions.Right);

            UINode grid = kit.Node(frame, nameof(R.SlotGrid), 100, 248, 1400, 428);
            GridLayoutGroup layout = grid.Go.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(440, 190);
            layout.spacing = new Vector2(40, 48);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;

            for (int i = 0; i < SaveLoadPanel.SlotsPerPage; i++)
            {
                int col = i % 3;
                int row = i / 3;
                BuildCard(kit, grid, "Slot_" + (i + 1), new Rect(100 + 480 * col, 248 + 238 * row, 440, 190));
            }

            kit.Label(frame, nameof(R.SlotHint), "선택한 슬롯에 현재 장면을 저장합니다.", 104, 744, 950, 38, 19, KedColors.Muted);

            kit.IconButton(frame, nameof(R.PreviousPage), "back", new Rect(650, 807, 48, 48), padding: 12f);
            kit.Label(frame, nameof(R.CurrentPage), "1", 719, 816, 240, 30, 18, KedColors.Navy,
                TextAlignmentOptions.Center, bold: true);
            kit.IconButton(frame, nameof(R.NextPage), "next", new Rect(934, 807, 48, 48), padding: 12f);

            kit.TextButton(frame, nameof(R.SlotAction), nameof(R.SlotActionLabel), "이 슬롯에 저장",
                new Rect(1180, 785, 320, 70), KedButtonStyle.Primary);

            return view.Go;
        }

        // 카드 하나. 안의 이름은 카드(SaveSlotCard)가 제 안에서만 찾는다.
        private static void BuildCard(KedUIKit kit, UINode grid, string name, Rect d)
        {
            UINode card = kit.Node(grid, name, d);
            card.Go.AddComponent<SaveSlotCard>();

            float x = d.x;
            float y = d.y;

            // 카드 전체가 버튼 — save_slot 그림 그대로(밝은 자리 = 그림 창의 알려진 자리 그림).
            UINode surface = kit.ImageButton(card, nameof(C.CardButton), "save_slot");

            UINode thumb = kit.Node(surface, nameof(C.CardThumbnail), x + 13, y + 12, 414, 100);

            UINode number = kit.Node(thumb, nameof(C.CardNumber), x + 26, y + 21, 64, 28);
            kit.Surface(number, KedColors.Navy, radius: 14f);
            UINode numberText = kit.Fill(number, nameof(C.CardNumberText));
            kit.Text(numberText, "01", 14, KedColors.White, TextAlignmentOptions.Center, bold: true);

            kit.Label(thumb, nameof(C.CardChapter), "", x + 102, y + 22, 310, 26, 15, KedColors.Navy, bold: true,
                overflow: TextOverflowModes.Ellipsis);
            kit.Label(thumb, nameof(C.CardPlayTime), "", x + 200, y + 82, 215, 24, 14, KedColors.Navy,
                TextAlignmentOptions.Right);

            kit.Label(surface, nameof(C.CardTitle), "", x + 24, y + 122, 392, 27, 20, KedColors.Navy, bold: true,
                overflow: TextOverflowModes.Ellipsis);
            kit.Label(surface, nameof(C.CardDate), "", x + 24, y + 154, 360, 24, 14, KedColors.Muted);
            kit.Label(surface, nameof(C.CardEmpty), "빈 슬롯", x + 26, y + 60, 380, 42, 24, KedColors.Muted,
                TextAlignmentOptions.Center);
            kit.Label(surface, nameof(C.CardEmptyHint), "", x + 24, y + 145, 380, 25, 15, KedColors.Muted,
                TextAlignmentOptions.Center);

            UINode selected = kit.Fill(surface, nameof(C.CardSelected), -2f, -2f, -2f, -2f);
            kit.Outline(selected, KedColors.Turquoise, 3f);
            selected.Go.SetActive(false);
        }
    }
}
