#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One-shot generator for the M3 canonical screen and everything needed to
// run it: the authored prefab (tagged with UIWidgetTag), the Wide/Compact
// LayoutPatchSpec assets, the UIScreenSpecAsset, the catalog and the demo
// scene. Output is ordinary, hand-editable Unity data; re-running resets it.
//
// Editor:    Tools > UI > Generate Adaptive UI Demo
// Headless:  Unity -batchmode -projectPath <p> -executeMethod AdaptiveDemoAssetGenerator.Generate -quit
public static class AdaptiveDemoAssetGenerator
{
    public const string RootFolder = "Assets/Demo/AdaptiveUI";
    public const string ScenePath  = "Assets/Scenes/AdaptiveUIDemo.unity";
    public const string ScreenKey  = "adaptive_demo";

    // Reference resolution the base layout is authored against (16:9).
    public static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);

    // CanvasScaler match = 1 (height): canvas height is always 1080 units,
    // width = 1080 * aspect. Aspect changes become a single horizontal axis.
    public const float MatchWidthOrHeight = 1f;

    // ---- palette ----
    private static readonly Color Bg      = new Color(0.11f, 0.13f, 0.17f);
    private static readonly Color Bar     = new Color(0.17f, 0.22f, 0.30f);
    private static readonly Color PanelColor = new Color(0.23f, 0.29f, 0.39f);
    private static readonly Color Side    = new Color(0.27f, 0.34f, 0.48f);
    private static readonly Color Accent  = new Color(0.36f, 0.55f, 0.94f);
    private static readonly Color Ink     = new Color(0.95f, 0.96f, 0.98f);
    private static readonly Color InkWeak = new Color(0.75f, 0.78f, 0.84f);

    [MenuItem("Tools/UI/Generate Adaptive UI Demo")]
    public static void Generate()
    {
        EnsureFolder(RootFolder);

        GameObject       prefab  = CreateScreenPrefab($"{RootFolder}/AdaptiveDemoScreen.prefab");
        LayoutPatchSpec  wide    = CreateWideLayout($"{RootFolder}/Layout_Wide.asset");
        LayoutPatchSpec  compact = CreateCompactLayout($"{RootFolder}/Layout_Compact.asset");
        UIScreenSpecAsset spec   = CreateSpec($"{RootFolder}/AdaptiveDemoScreen.asset", prefab, wide, compact);
        UIScreenCatalog  catalog = CreateCatalog($"{RootFolder}/AdaptiveDemoCatalog.asset", spec);

        AssetDatabase.SaveAssets();
        CreateScene(ScenePath, catalog);
        AssetDatabase.Refresh();

        Debug.Log($"[AdaptiveDemo] Generated {RootFolder}/* and {ScenePath}. Open the scene and press Play.");
    }

    // ------------------------------------------------------------------ prefab

    // Base = 16:9 intent, authored in a 1920x1080 canvas (y up, center origin).
    //
    //   Header          top bar, stretch, h110
    //   PrimaryContent  centered panel, 1140x640 at x=-190   -> spans -760..380
    //   SideInfo        300x640 at x=560                     -> spans  410..710
    //   BottomControls  bottom bar, stretch, h100
    //   LeftAction      260x90, 40px from bottom-left corner (y=130)
    //   RightAction     260x90, 40px from bottom-right corner
    //   Background      full-bleed
    private static GameObject CreateScreenPrefab(string path)
    {
        var root = new GameObject("AdaptiveDemoScreen", typeof(RectTransform), typeof(UIScreen));
        Stretch(root.GetComponent<RectTransform>());

        // Background
        Image background = Panel("Background", root.transform, Bg);
        Stretch(background.rectTransform);
        Tag(background.gameObject, "Background");

        // Header
        Image header = Panel("Header", root.transform, Bar);
        Place(header.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(0, 110));
        Tag(header.gameObject, "Header", UITextRole.Title);
        Label("Title", header.transform, "Adaptive UI Demo", 44, TextAlignmentOptions.MidlineLeft, Ink, new Vector4(32, 0, 32, 0));

        // Primary content
        Image primary = Panel("PrimaryContent", root.transform, PanelColor);
        Place(primary.rectTransform, Center, Center, Center, new Vector2(-190, 20), new Vector2(1140, 640));
        Tag(primary.gameObject, "PrimaryContent", UITextRole.Body);
        Label("Label", primary.transform, "Primary Content", 40, TextAlignmentOptions.Center, Ink, Vector4.zero);

        // Side info
        Image side = Panel("SideInfo", root.transform, Side);
        Place(side.rectTransform, Center, Center, Center, new Vector2(560, 20), new Vector2(300, 640));
        Tag(side.gameObject, "SideInfo", UITextRole.Caption);
        Label("Label", side.transform, "Side Info", 30, TextAlignmentOptions.Center, InkWeak, Vector4.zero);

        // Bottom controls (+ display info readout)
        Image bottom = Panel("BottomControls", root.transform, Bar);
        Place(bottom.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(0, 100));
        Tag(bottom.gameObject, "BottomControls", UITextRole.Caption);
        bottom.gameObject.AddComponent<DisplayInfoLabel>();
        Label("Info", bottom.transform, "display info", 26, TextAlignmentOptions.Center, InkWeak, new Vector4(32, 0, 32, 0));

        // Actions
        Button left = ButtonWidget("LeftAction", root.transform, "Left Action");
        Place(left.image.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(40, 130), new Vector2(260, 90));
        Tag(left.gameObject, "LeftAction");

        Button right = ButtonWidget("RightAction", root.transform, "Right Action");
        Place(right.image.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 130), new Vector2(260, 90));
        Tag(right.gameObject, "RightAction");

        GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return asset;
    }

    // ------------------------------------------------------------------ layouts

    // Wide (18:9 .. 21:9, canvas 2160..2520 wide): use some of the extra width,
    // but keep the actions tracking the content instead of hugging far edges.
    private static LayoutPatchSpec CreateWideLayout(string path)
        => CreateOrUpdate<LayoutPatchSpec>(path, layout =>
        {
            layout.widgets.Clear();
            layout.widgets.Add(RectPatch("PrimaryContent", position: new Vector2(-250, 20), size: new Vector2(1400, 640)));
            layout.widgets.Add(RectPatch("SideInfo",       position: new Vector2(700, 20)));
            layout.widgets.Add(RectPatch("LeftAction",     anchors: (new Vector2(0.5f, 0), new Vector2(0.5f, 0)), position: new Vector2(-1040, 130)));
            layout.widgets.Add(RectPatch("RightAction",    anchors: (new Vector2(0.5f, 0), new Vector2(0.5f, 0)), position: new Vector2(1040, 130)));
        });

    // Compact (< 16:10, canvas ~1440 wide at 4:3): the base content would
    // overflow the left edge, so drop the side panel and re-center content.
    private static LayoutPatchSpec CreateCompactLayout(string path)
        => CreateOrUpdate<LayoutPatchSpec>(path, layout =>
        {
            layout.widgets.Clear();
            layout.widgets.Add(new WidgetLayoutPatch { nameTag = "SideInfo", overrideActive = true, active = false });
            layout.widgets.Add(RectPatch("PrimaryContent", position: new Vector2(0, 20), size: new Vector2(1240, 640)));
        });

    private static WidgetLayoutPatch RectPatch(
        string nameTag,
        (Vector2 min, Vector2 max)? anchors = null,
        Vector2? position = null,
        Vector2? size = null)
    {
        var patch = new WidgetLayoutPatch { nameTag = nameTag };
        if (anchors.HasValue)
        {
            patch.rect.overrideAnchors = true;
            patch.rect.anchorMin = anchors.Value.min;
            patch.rect.anchorMax = anchors.Value.max;
        }
        if (position.HasValue)
        {
            patch.rect.overrideAnchoredPosition = true;
            patch.rect.anchoredPosition = position.Value;
        }
        if (size.HasValue)
        {
            patch.rect.overrideSizeDelta = true;
            patch.rect.sizeDelta = size.Value;
        }
        return patch;
    }

    // ------------------------------------------------------------------ spec / catalog

    private static UIScreenSpecAsset CreateSpec(string path, GameObject prefab, LayoutPatchSpec wide, LayoutPatchSpec compact)
        => CreateOrUpdate<UIScreenSpecAsset>(path, asset =>
        {
            asset.spec.screenKey      = new ScreenKey(ScreenKey);
            asset.spec.templatePrefab = prefab;
            asset.spec.baseTheme      = null;
            asset.spec.baseLayout     = null;   // base = prefab as authored (16:9)
            asset.spec.variants = new[]
            {
                ClassRule("Wide",      DisplayLayoutClass.Wide,      wide),
                ClassRule("UltraWide", DisplayLayoutClass.UltraWide, wide),      // same asset: no real difference yet
                ClassRule("Compact",   DisplayLayoutClass.Compact,   compact),
            };
        });

    private static UIVariantRule ClassRule(string id, DisplayLayoutClass cls, LayoutPatchSpec layout)
        => new UIVariantRule
        {
            variantId      = id,
            priority       = 100,
            condition      = new VariantCondition { useLayoutClass = true, layoutClass = cls },
            overrideLayout = layout,
        };

    private static UIScreenCatalog CreateCatalog(string path, UIScreenSpecAsset spec)
        => CreateOrUpdate<UIScreenCatalog>(path, catalog =>
        {
            catalog.entries.Clear();
            catalog.entries.Add(new UIScreenCatalog.ScreenEntry { screenKey = spec.spec.screenKey, specAsset = spec });
        });

    // ------------------------------------------------------------------ scene

    private static void CreateScene(string path, UIScreenCatalog catalog)
    {
        Scene previous = SceneManager.GetActiveScene();
        Scene scene    = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        try
        {
            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic    = true;
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = Bg;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = MatchWidthOrHeight;

            var uiRoot = new GameObject("UIRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            uiRoot.SetParent(canvasGo.transform, false);
            Stretch(uiRoot);

            var installerGo = new GameObject("UIPresentationInstaller", typeof(UIPresentationInstaller));
            var so = new SerializedObject(installerGo.GetComponent<UIPresentationInstaller>());
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.FindProperty("uiRoot").objectReferenceValue  = uiRoot;
            so.FindProperty("initialScreenKey").stringValue = ScreenKey;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, path);
        }
        finally
        {
            if (previous.IsValid())
                SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    // ------------------------------------------------------------------ helpers

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    private static void Stretch(RectTransform rt)
        => Place(rt, Vector2.zero, Vector2.one, Center, Vector2.zero, Vector2.zero);

    private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta        = size;
    }

    private static Image Panel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color         = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Label(string name, Transform parent, string text, float size, TextAlignmentOptions align, Color color, Vector4 margin)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>());
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text          = text;
        tmp.fontSize      = size;
        tmp.alignment     = align;
        tmp.color         = color;
        tmp.margin        = margin;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button ButtonWidget(string name, Transform parent, string label)
    {
        Image image = Panel(name, parent, Accent);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Label("Label", image.transform, label, 30, TextAlignmentOptions.Center, Ink, Vector4.zero);
        return button;
    }

    private static void Tag(GameObject go, string nameTag, UITextRole role = UITextRole.Body)
    {
        var tag = go.AddComponent<UIWidgetTag>();
        tag.nameTag  = nameTag;
        tag.textRole = role;
    }

    private static T CreateOrUpdate<T>(string path, Action<T> configure) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, path);
        }
        else
        {
            configure(asset);
            EditorUtility.SetDirty(asset);
        }
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
