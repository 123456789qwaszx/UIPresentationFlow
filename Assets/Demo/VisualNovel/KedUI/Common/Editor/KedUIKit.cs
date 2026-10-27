using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Ked.Samples.UI.Editor
{
    public enum KedButtonStyle
    {
        Primary,
        Secondary,
        Danger,
    }

    /// <summary>
    /// 화면 빌드 명세(Screens/*/Editor)가 쓰는 부품 상자 — KedUI의 부품 모양. 시안의 부품 규칙(Docs/IMPLEMENTATION.md §4·§8)을 한곳에 둔다:
    /// Simple/Sliced는 asset-manifest.json대로, 아이콘은 비율 유지, 버튼은 UIPF ButtonAnimation(누름 0.98)과
    /// KedButton(포커스 테두리·막힘 투명도), 글자는 모두 TMP(Noto Sans KR).
    /// 노드 자리(시안 좌표 → 앵커) · 그림 찾기 · 직렬화 칸 쓰기는 UIPF(Editor/UI/Authoring)의 것을 쓴다 — UINodes · UISpriteIndex · UISerialized.
    /// 시안 판은 1600×900.
    /// </summary>
    public sealed class KedUIKit
    {
        public static string Root => KedUIUnits.Root;
        public static readonly Rect Screen = new(0f, 0f, 1600f, 900f);

        [Serializable]
        private sealed class Manifest
        {
            public ManifestEntry[] assets;
        }

        [Serializable]
        private sealed class ManifestEntry
        {
            public string name;
            public int width;
            public int height;
            public string imageType;
            public int[] border;
        }

        private readonly Dictionary<string, ManifestEntry> _manifest = new(StringComparer.Ordinal);
        private readonly UISpriteIndex _art;

        public TMP_FontAsset Font { get; }
        public List<string> Problems { get; } = new();

        public KedUIKit(TMP_FontAsset font)
        {
            Font = font;

            // 그림은 이름으로 찾는다 — Art/ · Common/Sprites/ · 묶음의 Sprites/ 어디에 있어도 된다(KedUIUnits).
            _art = KedUIUnits.Art(Problems);

            string manifestPath = Root + "/asset-manifest.json";
            if (!File.Exists(manifestPath))
            {
                Problems.Add($"manifest 없음: {manifestPath}");
                return;
            }

            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (manifest?.assets == null)
            {
                Problems.Add("manifest에 assets가 없다");
                return;
            }

            foreach (ManifestEntry entry in manifest.assets)
                _manifest[entry.name] = entry;
        }

        #region 그림

        public Sprite Sprite(string name) => _art.Load(name, Problems);

        public bool IsSliced(string name) =>
            _manifest.TryGetValue(name, out ManifestEntry entry) &&
            string.Equals(entry.imageType, "Sliced", StringComparison.OrdinalIgnoreCase);

        public int SourceHeight(string name) =>
            _manifest.TryGetValue(name, out ManifestEntry entry) ? entry.height : 0;

        #endregion

        #region 노드

        /// <summary>화면(View) 프리팹의 뿌리. 부모(층 · PageRoot)를 꽉 채운다. design은 부모가 차지하는 시안 사각형.</summary>
        public UINode NewView<T>(string name, Rect? design = null) where T : Component
        {
            UINode node = UINodes.Root(name, design ?? Screen);
            node.Go.AddComponent<CanvasGroup>();
            node.Go.AddComponent<T>();
            return node;
        }

        /// <summary>
        /// 화면 크기 1600×900 판. 가운데에 고정한다 — 캔버스 비율 맞춤이 Expand라 어떤 화면비에서도 판 전체가 보인다.
        /// 배경·딤은 판 밖(화면 전체)에, 내용은 판 안에 둔다.
        /// </summary>
        public UINode Frame(UINode parent, string name = "Frame")
            => Node(parent, name, Screen, UIAnchor.Center);

        /// <summary>배경 그림: 화면 전체를 비율 유지로 덮는다(넘치는 쪽은 잘린다).</summary>
        public Image Backdrop(UINode parent, string name, string sprite, bool raycast = true)
        {
            UINode node = Fill(parent, name);
            Image image = Image(node, sprite, raycast: raycast);

            AspectRatioFitter fitter = node.Go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = image.sprite != null
                ? image.sprite.rect.width / Mathf.Max(1f, image.sprite.rect.height)
                : 16f / 9f;

            return image;
        }

        /// <summary>앵커 한 점에 고정한 노드. anchor는 Unity 규칙(x 오른쪽, y 위쪽 0–1). 시안 위치는 1600×900에서 정확히 같다.</summary>
        public UINode Node(UINode parent, string name, Rect design, Vector2 anchor)
            => UINodes.Node(parent, name, design, anchor);

        public UINode Node(UINode parent, string name, Rect design)
            => Node(parent, name, design, UIAnchor.TopLeft);

        public UINode Node(UINode parent, string name, float x, float y, float w, float h)
            => Node(parent, name, new Rect(x, y, w, h), UIAnchor.TopLeft);

        public UINode Node(UINode parent, string name, float x, float y, float w, float h, Vector2 anchor)
            => Node(parent, name, new Rect(x, y, w, h), anchor);

        /// <summary>부모를 꽉 채운다. inset은 안쪽 여백(왼·위·오른·아래, 음수면 밖으로).</summary>
        public UINode Fill(UINode parent, string name, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
            => UINodes.Fill(parent, name, left, top, right, bottom);

        /// <summary>부모 안의 시안 사각형을 네 변 앵커로 늘인다(부모가 커지면 같이 커진다).</summary>
        public UINode Stretch(UINode parent, string name, Rect design)
            => UINodes.Stretch(parent, name, design);

        /// <summary>가로로만 늘인다(위·아래·가운데 한 줄에 고정). anchorY는 1=위, 0.5=가운데, 0=아래.</summary>
        public UINode StretchX(UINode parent, string name, Rect design, float anchorY)
            => UINodes.StretchX(parent, name, design, anchorY);

        #endregion

        #region 그래픽

        /// <summary>제공된 PNG 하나. Image Type은 manifest(Simple/Sliced)대로.</summary>
        public Image Image(UINode node, string sprite, Color? color = null, bool raycast = false, bool preserveAspect = false, float ppuMultiplier = 1f)
        {
            Image image = node.Go.AddComponent<Image>();
            image.sprite = Sprite(sprite);
            image.type = IsSliced(sprite) ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = ppuMultiplier;
            image.preserveAspect = preserveAspect;
            image.color = color ?? Color.white;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>단색 사각형(딤·구분선) — pixel_white.</summary>
        public Image Solid(UINode node, Color color, bool raycast = false)
        {
            return Image(node, "pixel_white", color, raycast);
        }

        /// <summary>둥근 면(surface_white, 9분할). radius는 시안 px.</summary>
        public Image Surface(UINode node, Color color, float radius = 14f, bool raycast = false)
        {
            return Image(node, "surface_white", color, raycast, ppuMultiplier: 14f / Mathf.Max(1f, radius));
        }

        /// <summary>테두리(outline_white, 9분할). 선 굵기는 시안 px.</summary>
        public Image Outline(UINode node, Color color, float thickness = 3f)
        {
            return Image(node, "outline_white", color, ppuMultiplier: 3f / Mathf.Max(0.5f, thickness));
        }

        /// <summary>가로로 늘이는 레일(slider·toggle 트랙) — 그림 높이를 노드 높이에 맞춰 9분할 경계를 줄인다.</summary>
        public Image Rail(UINode node, string sprite, bool raycast = false)
        {
            float source = SourceHeight(sprite);
            float multiplier = source > 0f ? source / Mathf.Max(1f, node.Design.height) : 1f;
            return Image(node, sprite, raycast: raycast, ppuMultiplier: multiplier);
        }

        public Image Clear(UINode node, bool raycast = true)
        {
            Image image = node.Go.AddComponent<Image>();
            image.sprite = null;
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = raycast;
            return image;
        }

        #endregion

        #region 글자

        public TextMeshProUGUI Text(
            UINode node,
            string text,
            float size,
            Color color,
            TextAlignmentOptions alignment,
            bool bold = false,
            bool wrap = false,
            TextOverflowModes overflow = TextOverflowModes.Overflow,
            float lineSpacing = 0f,
            float characterSpacing = 0f)
        {
            TextMeshProUGUI tmp = node.Go.AddComponent<TextMeshProUGUI>();

            if (Font != null)
            {
                tmp.font = Font;
                tmp.fontSharedMaterial = Font.material;
            }

            tmp.text = text ?? string.Empty;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = overflow;
            tmp.lineSpacing = lineSpacing;
            tmp.characterSpacing = characterSpacing;
            tmp.raycastTarget = false;
            tmp.richText = true;
            tmp.margin = Vector4.zero;

            return tmp;
        }

        public TextMeshProUGUI Label(
            UINode parent,
            string name,
            string text,
            float x, float y, float w, float h,
            float size,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left,
            bool bold = false,
            bool wrap = false,
            TextOverflowModes overflow = TextOverflowModes.Overflow,
            Vector2? anchor = null)
        {
            UINode node = Node(parent, name, new Rect(x, y, w, h), anchor ?? UIAnchor.TopLeft);
            return Text(node, text, size, color, alignment, bold, wrap, overflow);
        }

        #endregion

        #region 버튼

        /// <summary>
        /// 글자 버튼(button_primary · button_secondary · button_danger). choice=true면 "선택됨" 그림이 primary다
        /// (설정의 배속·화면 모드처럼 하나를 고르는 버튼).
        /// </summary>
        public KedButton TextButton(
            UINode parent,
            string name,
            string labelName,
            string text,
            Rect design,
            KedButtonStyle style,
            float fontSize = 23f,
            Vector2? anchor = null,
            bool choice = false)
        {
            UINode node = Node(parent, name, design, anchor ?? UIAnchor.TopLeft);
            CenterPivot(node.Rect);

            string sprite = style switch
            {
                KedButtonStyle.Primary => "button_primary",
                KedButtonStyle.Danger => "button_danger",
                _ => "button_secondary",
            };

            Image background = Image(node, sprite, raycast: true);
            Color labelColor = style == KedButtonStyle.Secondary ? KedColors.Navy : KedColors.White;

            Button button = AddButton(node, background);
            GameObject ring = FocusRing(node, name + "Focus");

            UINode labelNode = Fill(node, labelName, 22f, 0f, 22f, 4f);
            TextMeshProUGUI label = Text(labelNode, text, fontSize, labelColor, TextAlignmentOptions.Center, bold: true,
                overflow: TextOverflowModes.Ellipsis);

            return AddLook(node, button, background, ring, label,
                normalSprite: background.sprite,
                hoverSprite: null,
                selectedSprite: choice ? Sprite("button_primary") : null,
                backgroundNormal: Color.white,
                backgroundSelected: Color.white,
                labelNormal: labelColor,
                labelSelected: KedColors.White);
        }

        /// <summary>
        /// 원형 자리의 아이콘 버튼(시안 icon()): 상아색 둥근 바탕 위에 글리프. 누르는 칸은 바탕 전체(52–58px),
        /// 글리프는 안쪽 여백만큼 작다(24–30px). 켜짐(자동·빨리 넘기기)은 바탕이 청록으로.
        /// </summary>
        public KedButton IconButton(
            UINode parent,
            string name,
            string icon,
            Rect design,
            float padding = 14f,
            Vector2? anchor = null,
            bool withBase = true)
        {
            UINode node = Node(parent, name, design, anchor ?? UIAnchor.TopLeft);
            CenterPivot(node.Rect);

            UINode baseNode = Fill(node, name + "Base");
            Image hit = withBase
                ? Surface(baseNode, KedColors.QuickSurface, radius: 16f, raycast: true)
                : Clear(baseNode);

            UINode iconNode = Fill(node, name + "Icon", padding, padding, padding, padding);
            Image(iconNode, "icon_" + icon, preserveAspect: true);

            Button button = AddButton(node, hit);
            GameObject ring = FocusRing(node, name + "Focus");

            return AddLook(node, button, hit, ring, null,
                normalSprite: null,
                hoverSprite: null,
                selectedSprite: null,
                backgroundNormal: withBase ? KedColors.QuickSurface : new Color(1f, 1f, 1f, 0f),
                backgroundSelected: KedColors.ActiveSurface,
                labelNormal: KedColors.Navy,
                labelSelected: KedColors.Navy);
        }

        /// <summary>설정의 카테고리 탭(tab_normal · tab_hover · tab_selected). 상태가 바뀌어도 사각형은 같다.</summary>
        public KedButton Tab(UINode parent, string name, string textName, string text, Rect design, Rect textDesign)
        {
            UINode node = Node(parent, name, design, UIAnchor.TopLeft);
            CenterPivot(node.Rect);
            Image background = Image(node, "tab_normal", raycast: true);

            Button button = AddButton(node, background);
            GameObject ring = FocusRing(node, name + "Focus");

            UINode labelNode = Node(node, textName, textDesign, UIAnchor.TopLeft);
            TextMeshProUGUI label = Text(labelNode, text, 22f, KedColors.Navy, TextAlignmentOptions.Center, bold: true);

            return AddLook(node, button, background, ring, label,
                normalSprite: Sprite("tab_normal"),
                hoverSprite: Sprite("tab_hover"),
                selectedSprite: Sprite("tab_selected"),
                backgroundNormal: Color.white,
                backgroundSelected: Color.white,
                labelNormal: KedColors.Navy,
                labelSelected: KedColors.White);
        }

        /// <summary>그림 한 장이 곧 버튼(슬롯 카드). 부모를 꽉 채우고, 안의 내용은 이 노드 아래에 둔다(누름 연출이 함께 움직인다).</summary>
        public UINode ImageButton(UINode parent, string name, string sprite)
        {
            UINode node = Fill(parent, name);
            Image background = Image(node, sprite, raycast: true);

            Button button = AddButton(node, background);
            GameObject ring = FocusRing(node, name + "Focus");

            AddLook(node, button, background, ring, null,
                normalSprite: background.sprite,
                hoverSprite: null,
                selectedSprite: null,
                backgroundNormal: Color.white,
                backgroundSelected: Color.white,
                labelNormal: KedColors.Navy,
                labelSelected: KedColors.Navy);

            return node;
        }

        /// <summary>바탕 없는 글자 버튼(타이틀의 "종료"). 누르는 칸은 노드 전체.</summary>
        public KedButton LinkButton(UINode parent, string name, string labelName, string text, Rect design, float fontSize, Color color)
        {
            UINode node = Node(parent, name, design, UIAnchor.TopLeft);
            CenterPivot(node.Rect);
            Image hit = Clear(node);

            Button button = AddButton(node, hit);
            GameObject ring = FocusRing(node, name + "Focus");

            UINode labelNode = Fill(node, labelName, 10f, 0f, 0f, 0f);
            TextMeshProUGUI label = Text(labelNode, text, fontSize, color, TextAlignmentOptions.Left);

            return AddLook(node, button, hit, ring, label,
                normalSprite: null,
                hoverSprite: null,
                selectedSprite: null,
                backgroundNormal: new Color(1f, 1f, 1f, 0f),
                backgroundSelected: new Color(1f, 1f, 1f, 0f),
                labelNormal: color,
                labelSelected: color);
        }

        // 버튼은 가운데를 기준점으로 — 누름 연출(ButtonAnimation, localScale)이 가운데로 줄어들게. 자리는 그대로 둔다.
        private static void CenterPivot(RectTransform rect) => UINodes.CenterPivot(rect);

        private Button AddButton(UINode node, Graphic target)
        {
            Button button = node.Go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = target;

            node.Go.AddComponent<CanvasGroup>();

            // 누름 0.98 · 올림 크기 변화 없음(시안 §8 — 올림은 테두리로 보인다).
            ButtonAnimation animation = node.Go.AddComponent<ButtonAnimation>();
            UISerialized.SetFloat(animation, "_hoverScale", 1f);
            UISerialized.SetFloat(animation, "_pressScale", 0.98f);

            return button;
        }

        private GameObject FocusRing(UINode node, string name)
        {
            UINode ring = Fill(node, name, -5f, -5f, -5f, -5f);
            Outline(ring, KedColors.Turquoise, 3f);
            ring.Go.SetActive(false);
            return ring.Go;
        }

        private KedButton AddLook(
            UINode node,
            Button button,
            Graphic background,
            GameObject ring,
            TMP_Text label,
            Sprite normalSprite,
            Sprite hoverSprite,
            Sprite selectedSprite,
            Color backgroundNormal,
            Color backgroundSelected,
            Color labelNormal,
            Color labelSelected)
        {
            KedButton look = node.Go.AddComponent<KedButton>();

            var so = new SerializedObject(look);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("background").objectReferenceValue = background as Image;
            so.FindProperty("focusRing").objectReferenceValue = ring;
            so.FindProperty("canvasGroup").objectReferenceValue = node.Go.GetComponent<CanvasGroup>();
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("normalSprite").objectReferenceValue = normalSprite;
            so.FindProperty("hoverSprite").objectReferenceValue = hoverSprite;
            so.FindProperty("selectedSprite").objectReferenceValue = selectedSprite;
            so.FindProperty("backgroundNormal").colorValue = backgroundNormal;
            so.FindProperty("backgroundSelected").colorValue = backgroundSelected;
            so.FindProperty("labelNormal").colorValue = labelNormal;
            so.FindProperty("labelSelected").colorValue = labelSelected;
            so.ApplyModifiedPropertiesWithoutUndo();

            return look;
        }

        #endregion

        #region 슬라이더 · 스위치

        /// <summary>
        /// 슬라이더: 트랙(slider_track) · 채움(slider_fill) · 손잡이(slider_thumb) 따로(시안 §8).
        /// Handle Slide Area를 트랙과 같게 두어 손잡이 중심이 레일 양 끝에서 멈춘다.
        /// track은 레일의 시안 사각형(높이 16), 누르는 칸은 손잡이 높이(32)만큼.
        /// </summary>
        public Slider Slider(UINode parent, string name, string partPrefix, Rect track, float thumbSize = 32f)
        {
            Rect hit = new(track.x, track.y + track.height * 0.5f - thumbSize * 0.5f, track.width, thumbSize);
            UINode node = Node(parent, name, hit, UIAnchor.TopLeft);
            Clear(node, raycast: true);

            UINode trackNode = Node(node, partPrefix + "Track", track, UIAnchor.MiddleLeft);
            StretchHorizontally(trackNode.Rect, track.height);
            Rail(trackNode, "slider_track");

            UINode fillArea = Node(node, partPrefix + "FillArea", track, UIAnchor.MiddleLeft);
            StretchHorizontally(fillArea.Rect, track.height);

            UINode fill = Fill(fillArea, partPrefix + "Fill");
            Rail(fill, "slider_fill");
            fill.Rect.anchorMax = new Vector2(0.5f, 1f);

            // 손잡이: 슬라이더가 가로 앵커를 값으로 옮긴다. 세로는 손잡이 칸(32)을 꽉 채운다 — Unity 기본 슬라이더와 같은 꼴.
            UINode handleArea = Fill(node, partPrefix + "HandleArea");
            UINode thumb = Node(handleArea, partPrefix + "Thumb",
                new Rect(track.x - thumbSize * 0.5f, hit.y, thumbSize, thumbSize), UIAnchor.MiddleLeft);
            thumb.Rect.anchorMin = new Vector2(0f, 0f);
            thumb.Rect.anchorMax = new Vector2(0f, 1f);
            thumb.Rect.pivot = new Vector2(0.5f, 0.5f);
            thumb.Rect.sizeDelta = new Vector2(thumbSize, 0f);
            thumb.Rect.anchoredPosition = Vector2.zero;
            Image thumbImage = Image(thumb, "slider_thumb", raycast: true, preserveAspect: true);

            Slider slider = node.Go.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.targetGraphic = thumbImage;
            slider.fillRect = fill.Rect;
            slider.handleRect = thumb.Rect;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.wholeNumbers = true;
            slider.SetValueWithoutNotify(50f);

            return slider;
        }

        private static void StretchHorizontally(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>켜고 끄는 스위치(toggle_track_off/on + toggle_thumb). 손잡이 중심은 트랙 왼쪽 끝에서 20 / 70.</summary>
        public Toggle Switch(UINode parent, string name, string partPrefix, Rect track, float thumbSize = 34f)
        {
            UINode node = Node(parent, name, track, UIAnchor.TopLeft);

            UINode trackNode = Fill(node, partPrefix + "Track");
            Image trackImage = Rail(trackNode, "toggle_track_off", raycast: true);

            float inset = (track.height - thumbSize) * 0.5f;
            float offX = inset + thumbSize * 0.5f;
            float onX = track.width - offX;

            UINode thumb = Node(node, partPrefix + "Thumb",
                new Rect(track.x + inset, track.y + inset, thumbSize, thumbSize), UIAnchor.MiddleLeft);
            thumb.Rect.pivot = new Vector2(0.5f, 0.5f);
            thumb.Rect.anchoredPosition = new Vector2(offX, 0f);
            Image(thumb, "toggle_thumb", preserveAspect: true);

            Toggle toggle = node.Go.AddComponent<Toggle>();
            toggle.transition = Selectable.Transition.None;
            toggle.targetGraphic = trackImage;
            toggle.graphic = null;
            toggle.isOn = false;

            KedToggleSwitch look = node.Go.AddComponent<KedToggleSwitch>();
            var so = new SerializedObject(look);
            so.FindProperty("toggle").objectReferenceValue = toggle;
            so.FindProperty("track").objectReferenceValue = trackImage;
            so.FindProperty("thumb").objectReferenceValue = thumb.Rect;
            so.FindProperty("offSprite").objectReferenceValue = Sprite("toggle_track_off");
            so.FindProperty("onSprite").objectReferenceValue = Sprite("toggle_track_on");
            so.FindProperty("thumbOffX").floatValue = offX;
            so.FindProperty("thumbOnX").floatValue = onX;
            so.ApplyModifiedPropertiesWithoutUndo();

            return toggle;
        }

        #endregion
    }
}
