using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BlackHole.EditorTools
{
    // 블랙홀 화면의 부품 모양 — 그림 · 글꼴 · 버튼 꼴. 자리 계산 · 저장 · 검사는 UIPF(Editor/UI/Authoring)가 맡는다.
    // 값은 게임에서 가져온 손 프리팹과 같게 둔다(명세로 다시 지어도 같은 화면이 되게).
    internal sealed class BlackHoleScreenKit
    {
        public const string Root = "Assets/Demo/BlackHole";

        // 시안 판 = UI Canvas의 기준 해상도(1920×1080, Expand). 씬 빌더(BlackHoleDemoSceneBuilder)와 같다.
        public static readonly Rect Screen = new(0f, 0f, 1920f, 1080f);

        // 글자와 딤의 먹색.
        public static readonly Color Ink = new(0.125f, 0.129f, 0.141f, 1f);

        // 기본 글꼴은 영어 판. 언어에 맞는 글꼴은 실행 중 UIFontSet(Presentation)이 건다.
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        private readonly UISpriteIndex _sprites;

        public BlackHoleScreenKit()
        {
            // 밝은 판 그림만 이름으로 찾는다. Dark/에는 같은 이름의 어두운 판이 있고, 실행 중 ImageBindings가 바꾼다.
            _sprites = UISpriteIndex.Build(new[] { Root + "/Art/GUI" }, new[] { Root + "/Art/GUI/Dark" });

            Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (Font == null)
                Problems.Add("글꼴 없음: " + FontPath);
        }

        public TMP_FontAsset Font { get; }

        public List<string> Problems { get; } = new();

        public Sprite Sprite(string name) => _sprites.Load(name, Problems);

        // 화면 전체를 덮는 딤. 아래 화면을 누르지 못하게 막는다.
        public Image Dim(UINode node, float alpha)
        {
            Image image = node.Go.AddComponent<Image>();
            image.sprite = null;
            image.color = new Color(Ink.r, Ink.g, Ink.b, alpha);
            image.raycastTarget = true;
            return image;
        }

        public Image Image(
            UINode node,
            string sprite,
            Image.Type type = UnityEngine.UI.Image.Type.Simple,
            float pixelsPerUnitMultiplier = 1f,
            bool preserveAspect = false)
        {
            Image image = node.Go.AddComponent<Image>();
            image.sprite = Sprite(sprite);
            image.type = type;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            image.preserveAspect = preserveAspect;
            image.color = Color.white;
            image.raycastTarget = true;
            return image;
        }

        // 가운데 맞춤 한 줄 글자. autoSizeMin을 주면 칸에 넘칠 때 그 크기까지 줄인다(번역이 길어도 한 줄).
        public TextMeshProUGUI Text(UINode node, string text, float size, float characterSpacing, float? autoSizeMin = null)
        {
            TextMeshProUGUI tmp = node.Go.AddComponent<TextMeshProUGUI>();

            if (Font != null)
            {
                tmp.font = Font;
                tmp.fontSharedMaterial = Font.material;
            }

            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = Ink;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.characterSpacing = characterSpacing;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.richText = true;
            tmp.raycastTarget = false;
            tmp.margin = Vector4.zero;

            if (autoSizeMin.HasValue)
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = autoSizeMin.Value;
                tmp.fontSizeMax = size;
            }

            return tmp;
        }

        // 그림 버튼: 단추(Button · UI_EventHandler · ButtonAnimation) + 그림 자식(imageName).
        // 올림 · 누름 · 막힘은 Sprite Swap — {icon}_normal · _hover · _pressed · _disabled.
        // 크기 연출(ButtonAnimation)은 기본값(올림 1.06, 누름 0.93)이다.
        public Button IconButton(UINode parent, string name, string imageName, string icon, Rect design, out UINode node)
        {
            node = UINodes.Node(parent, name, design, UIAnchor.Center);

            Button button = node.Go.AddComponent<Button>();
            node.Go.AddComponent<UI_EventHandler>();
            node.Go.AddComponent<ButtonAnimation>();

            UINode imageNode = UINodes.Fill(node, imageName);
            Image image = Image(imageNode, icon + "_normal", preserveAspect: true);

            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                highlightedSprite = Sprite(icon + "_hover"),
                pressedSprite = Sprite(icon + "_pressed"),
                selectedSprite = null,
                disabledSprite = Sprite(icon + "_disabled"),
            };

            return button;
        }
    }
}
