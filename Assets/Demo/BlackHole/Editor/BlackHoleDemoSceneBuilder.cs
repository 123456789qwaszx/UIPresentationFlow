using System.Collections.Generic;
using System.IO;
using System.Text;
using BlackHole.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace BlackHole.EditorTools
{
    // 메뉴 UI Demo > Build BlackHole Demo Scene: 블랙홀 키우기 UI 데모 씬을 코드로 만든다. 다시 누르면 같은 씬을 새로 만든다.
    // 1. 글꼴: Fonts의 TTF로 정적 TMP Font Asset을 굽는다. 글자는 데모가 보이는 것만 — 글자 표(BlackHoleDemoText.csv)의 번역 칸,
    //    설정 선택지(언어 이름 "한국어" 등), ASCII. 플레이해도 에셋이 바뀌지 않는다(동적 글꼴은 쓴 글자를 에셋에 저장해 변경으로 잡힌다).
    //    글자 표를 고치면 이 메뉴를 다시 누른다
    // 2. UIFontSet: en-US = LiberationSans, ko-KR = 제목 나눔휴먼 · 본문 넥슨 Lv1 고딕
    //    TMP Settings의 대체 글꼴(Fallback Font Assets) 맨 앞에 넥슨 Lv1 고딕을 둔다 — 영어 화면에서도 "한국어" 같은 한글이 보인다(게임과 같은 설정)
    // 3. 씬: 카메라(+AudioListener), EventSystem(Input System), UI Canvas(1920×1080, Expand) 아래 Root·Panel·Overlay 층과 화면 프리팹 7개,
    //    부트스트랩(BlackHoleDemoBootstrap)과 소리(DemoButtonSounds). 참조는 모두 여기서 연결한다
    // 게임의 GameScene에서 UI Canvas 부분과 같은 구성이다.
    internal static class BlackHoleDemoSceneBuilder
    {
        private const string Root = "Assets/Demo/BlackHole";
        private const string ScenePath = Root + "/Scenes/BlackHoleDemo.unity";
        private const string FontSetPath = Root + "/Theme/UIFontSet.asset";
        private const string TextsPath = Root + "/Localization/BlackHoleDemoText.csv";
        private const string EnglishFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        // 정적 글꼴의 샘플 크기·여백·아틀라스. 데모 글자(약 300자)가 1024 한 장에 들어가는 크기다. 넘치면 다음 장을 만든다.
        private const int SamplingPointSize = 48;
        private const int AtlasPadding = 5;
        private const int AtlasSize = 1024;

        private static readonly string[] RootScreens = { "TitleScreen" };
        private static readonly string[] PanelScreens = { "ModeSelectPanel", "SettingsPanel", "PausePanel", "ConfirmPanel" };
        private static readonly string[] OverlayScreens = { "NoticeOverlay", "EndingOverlay" };

        [MenuItem("UI Demo/Build BlackHole Demo Scene")]
        private static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            UIFontSet fonts = BuildFontSet();
            if (fonts == null)
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();
            CreateEventSystem();

            RectTransform canvas = CreateCanvas();
            RectTransform rootLayer = CreateLayer("RootLayer", canvas);
            RectTransform panelLayer = CreateLayer("PanelLayer", canvas);
            RectTransform overlayLayer = CreateLayer("OverlayLayer", canvas);

            var views = new System.Collections.Generic.List<UIBase>();
            foreach (string name in RootScreens) views.Add(Instantiate(name, rootLayer));
            foreach (string name in PanelScreens) views.Add(Instantiate(name, panelLayer));
            foreach (string name in OverlayScreens) views.Add(Instantiate(name, overlayLayer));

            if (views.Contains(null))
                return;

            var demo = new GameObject("BlackHoleDemo");
            DemoButtonSounds sounds = CreateSounds(demo);
            BlackHoleDemoBootstrap bootstrap = demo.AddComponent<BlackHoleDemoBootstrap>();

            var so = new SerializedObject(bootstrap);
            so.FindProperty("_rootLayer").objectReferenceValue = rootLayer;
            so.FindProperty("_panelLayer").objectReferenceValue = panelLayer;
            so.FindProperty("_overlayLayer").objectReferenceValue = overlayLayer;

            SerializedProperty viewList = so.FindProperty("_views");
            viewList.arraySize = views.Count;
            for (int i = 0; i < views.Count; i++)
                viewList.GetArrayElementAtIndex(i).objectReferenceValue = views[i];

            SerializedProperty presentations = so.FindProperty("_presentations");
            SetPresentation(presentations, "Title", "TitleScreen");
            SetPresentation(presentations, "ModeSelect", "ModeSelectPanel");
            SetPresentation(presentations, "Settings", "SettingsPanel");
            SetPresentation(presentations, "Pause", "PausePanel");
            SetPresentation(presentations, "Confirm", "ConfirmPanel");
            SetPresentation(presentations, "Notice", "NoticeOverlay");
            SetPresentation(presentations, "Ending", "EndingOverlay");

            so.FindProperty("_fonts").objectReferenceValue = fonts;
            so.FindProperty("_palette").objectReferenceValue = Load<UIPalette>(Root + "/Theme/UIPalette.asset");
            so.FindProperty("_sprites").objectReferenceValue = Load<UISpriteSet>(Root + "/Theme/UISpriteSet.asset");
            so.FindProperty("_texts").objectReferenceValue = Load<TextAsset>(TextsPath);
            so.FindProperty("_transitionLook").objectReferenceValue = Load<ScreenTransitionLook>(Root + "/Looks/ScreenTransitionLook.asset");
            so.FindProperty("_sounds").objectReferenceValue = sounds;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[데모] {ScenePath}를 만들었다. 플레이해서 확인한다.", AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
        }

        // 언어별 글꼴. 한국어 글꼴은 Fonts의 TTF로 구운 정적 TMP Font Asset이다(FONTS_NOTICE.txt).
        private static UIFontSet BuildFontSet()
        {
            TMP_FontAsset english = Load<TMP_FontAsset>(EnglishFontPath);
            TextAsset texts = Load<TextAsset>(TextsPath);

            if (english == null || texts == null)
                return null;

            string characters = DemoCharacters(texts.text);
            TMP_FontAsset koreanBody = StaticFontAsset(Root + "/Fonts/NEXONLv1GothicRegular.ttf", characters);
            TMP_FontAsset koreanTitle = StaticFontAsset(Root + "/Fonts/NanumHumanBold.ttf", characters);

            if (koreanBody == null || koreanTitle == null)
                return null;

            UIFontSet set = AssetDatabase.LoadAssetAtPath<UIFontSet>(FontSetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<UIFontSet>();
                AssetDatabase.CreateAsset(set, FontSetPath);
            }

            set.locales = new[]
            {
                new UIFontSet.LocaleFonts { localeId = "en-US", title = english, body = english, caption = english },
                new UIFontSet.LocaleFonts { localeId = "ko-KR", title = koreanTitle, body = koreanBody, caption = koreanBody },
            };

            EditorUtility.SetDirty(set);
            SetGlobalFallback(koreanBody);
            AssetDatabase.SaveAssets();
            return set;
        }

        // 데모가 보이는 글자: 글자 표의 번역 칸(키 칸·# 메모 줄 제외), 설정 선택지, ASCII.
        private static string DemoCharacters(string table)
        {
            var characters = new HashSet<char>();

            for (char c = ' '; c <= '~'; c++)
                characters.Add(c);

            foreach (string[] row in Csv.Parse(table))
            {
                if (row.Length == 0 || row[0].StartsWith("#"))
                    continue;

                for (int i = 1; i < row.Length; i++)
                    characters.UnionWith(row[i]);
            }

            foreach (GameSettings.Definition definition in GameSettings.Definitions)
            {
                characters.UnionWith(definition.Label);

                foreach (string option in definition.Options)
                    characters.UnionWith(option);
            }

            characters.RemoveWhere(char.IsControl);

            var text = new StringBuilder(characters.Count);
            foreach (char c in characters)
                text.Append(c);

            return text.ToString();
        }

        // TTF 옆의 "{이름} SDF.asset"에 정적 글꼴을 굽는다. 같은 설정으로 만든 것이 있으면 글자만 다시 채운다(GUID 유지).
        private static TMP_FontAsset StaticFontAsset(string ttfPath, string characters)
        {
            string assetPath = Path.ChangeExtension(ttfPath, null) + " SDF.asset";
            Font font = Load<Font>(ttfPath);
            if (font == null)
                return null;

            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);

            if (asset != null && (asset.faceInfo.pointSize != SamplingPointSize || asset.atlasPadding != AtlasPadding || asset.atlasWidth != AtlasSize))
            {
                AssetDatabase.DeleteAsset(assetPath);
                asset = null;
            }

            if (asset == null)
            {
                asset = TMP_FontAsset.CreateFontAsset(
                    font, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic);
                asset.name = Path.GetFileNameWithoutExtension(assetPath);

                AssetDatabase.CreateAsset(asset, assetPath);
                asset.material.name = asset.name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            else
            {
                asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                asset.ClearFontAssetData();
            }

            if (!asset.TryAddCharacters(characters, out string missing))
                Debug.LogWarning($"[데모] {font.name}에 없는 글자: {missing}", font);

            asset.atlasPopulationMode = AtlasPopulationMode.Static;

            for (int i = 0; i < asset.atlasTextures.Length; i++)
            {
                Texture2D atlas = asset.atlasTextures[i];
                atlas.name = $"{asset.name} Atlas{(i == 0 ? "" : " " + i)}";

                if (!AssetDatabase.Contains(atlas))
                    AssetDatabase.AddObjectToAsset(atlas, asset);
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        // TMP Settings의 대체 글꼴 맨 앞에 font를 둔다. 글꼴(과 그 글꼴의 대체 목록)에 없는 글자는 여기서 찾는다.
        private static void SetGlobalFallback(TMP_FontAsset font)
        {
            TMP_Settings settings = Load<TMP_Settings>(TmpSettingsPath);
            if (settings == null)
                return;

            var so = new SerializedObject(settings);
            SerializedProperty list = so.FindProperty("m_fallbackFontAssets");

            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                Object item = list.GetArrayElementAtIndex(i).objectReferenceValue;
                if (item == null || item == font)
                    list.DeleteArrayElementAtIndex(i);
            }

            list.InsertArrayElementAtIndex(0);
            list.GetArrayElementAtIndex(0).objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateCamera()
        {
            // 소리(DemoButtonSounds)를 들으려면 씬에 AudioListener가 하나 있어야 한다. 카메라에 둔다.
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
        }

        private static void CreateEventSystem() =>
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        private static RectTransform CreateCanvas()
        {
            var go = new GameObject("UI Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            return (RectTransform)go.transform;
        }

        private static RectTransform CreateLayer(string name, RectTransform canvas)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = canvas.gameObject.layer;

            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static UIBase Instantiate(string screen, Transform layer)
        {
            GameObject prefab = Load<GameObject>($"{Root}/Prefabs/{screen}.prefab");
            if (prefab == null)
                return null;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, layer);
            UIBase view = instance.GetComponent<UIBase>();

            if (view == null)
                Debug.LogError($"[데모] {screen} 프리팹의 루트에 화면 컴포넌트(UIBase)가 없다.", prefab);

            return view;
        }

        private static DemoButtonSounds CreateSounds(GameObject owner)
        {
            var source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;

            DemoButtonSounds sounds = owner.AddComponent<DemoButtonSounds>();
            var so = new SerializedObject(sounds);
            so.FindProperty("_source").objectReferenceValue = source;
            so.FindProperty("_hover").objectReferenceValue = Load<AudioClip>(Root + "/Sounds/Hover.mp3");
            so.FindProperty("_click").objectReferenceValue = Load<AudioClip>(Root + "/Sounds/Click.mp3");
            so.FindProperty("_switchScreen").objectReferenceValue = Load<AudioClip>(Root + "/Sounds/SwitchingScreen.mp3");
            so.ApplyModifiedPropertiesWithoutUndo();
            return sounds;
        }

        private static void SetPresentation(SerializedProperty presentations, string field, string screen) =>
            presentations.FindPropertyRelative(field).objectReferenceValue =
                Load<UIPresentationSpec>($"{Root}/Presentations/{screen}/{screen}.Presentation.asset");

        private static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogError($"[데모] {path}를 찾지 못했다.");
            return asset;
        }
    }
}
