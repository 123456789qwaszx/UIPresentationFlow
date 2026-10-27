using System;
using System.Collections.Generic;
using System.Linq;
using Ked.Samples.UI;
using Ked.Samples.UI.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace VisualNovelDemo.EditorTools
{
    /// <summary>
    /// 메뉴 UI Demo > Build Visual Novel Demo — ked의 VN UI 묶음(KedUI 복사본)으로 화면 프리팹을 짓고 데모 씬을 만든다.
    /// 다시 누르면 같은 자리에 다시 짓는다(GUID 유지).
    ///
    ///   0. 그림 임포트 설정(묶음의 KedUIArtImporter — asset-manifest.json대로. 들이지 않은 Art/의 그림은 건너뛴다)
    ///   1. TMP 글꼴(묶음의 Noto Sans KR, 동적 SDF)
    ///   2. 화면 프리팹 12개 — 묶음의 빌드 명세(Screens/*/Editor)를 그대로 부른다. 자리는 묶음 규칙(Screens/*/Prefabs)
    ///   3. 데모 선택지 항목(VNDemoOptionItemSpec) · UIPF 글꼴 묶음 · 화면별 Presentation(+ 재생 화면의 21:9 Variant)
    ///   4. 씬 VisualNovelDemo — 카메라, EventSystem(Input System), 캔버스(1600×900, Expand) 아래 무대 그림과
    ///      Root · Panel · Overlay 층, 화면 프리팹, VNDemoHost(참조는 모두 여기서 잇는다). 빌드 설정의 씬 목록 맨 앞에 둔다
    ///   5. 동적 글꼴 비우기, 묶음 검사(KedUIValidate — Refs · Presentation · 묶음 감사 · 그림 이름)
    ///
    /// 화면 묶음은 ked가 원본이고 여기는 복사본이다(Documentation~/vendoring.md). 명세 · 키트 · 그림은 고치지 않는다 —
    /// 고칠 일이 있으면 ked에서 고치고 다시 들인다. 프리팹 · 글꼴 SDF는 이 프로젝트의 Unity로 다시 짓는 산출물이다.
    /// 화면 목록과 순서, 씬, 선택지 항목, 글자 표는 데모(앱)의 몫이라 이 저장소가 원본이다.
    /// </summary>
    internal static class VNDemoBuild
    {
        private const string AppDir = "Assets/Demo/VisualNovel/App";
        private const string ScenePath = AppDir + "/Scenes/VisualNovelDemo.unity";
        private const string OptionItemPath = AppDir + "/Prefabs/VNDemoOptionItem.prefab";
        private const string TextsPath = AppDir + "/Texts/VNDemoTexts.csv";
        private const string FallbackScreensDir = AppDir + "/Generated";
        private const string StageSprite = "background_title";

        // 화면 목록 — 등록 · Presentation 순서. Page는 주인(설정 창)보다 먼저 짓는다.
        private static readonly Type[] ViewTypes =
        {
            typeof(TitleScreen),
            typeof(PlayScreen),
            typeof(SystemMenuPanel),
            typeof(SaveLoadPanel),
            typeof(BacklogPanel),
            typeof(SettingsPanel),
            typeof(AudioSettingsPage),
            typeof(TextSettingsPage),
            typeof(PlaybackSettingsPage),
            typeof(DisplaySettingsPage),
            typeof(ConfirmPanel),
            typeof(NoticePanel),
        };

        [MenuItem("UI Demo/Build Visual Novel Demo")]
        private static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            bool ok = Build(out string report);

            if (ok)
                Debug.Log(report);
            else
                Debug.LogError(report);

            EditorUtility.DisplayDialog(
                "UI Demo",
                ok ? "비주얼 노벨 데모를 지었습니다. 콘솔에 보고가 있습니다." : "문제가 있습니다. 콘솔의 보고를 확인하세요.",
                "확인");
        }

        public static bool Build(out string report)
        {
            var problems = new List<string>();
            var built = new List<string>();

            if (!UIPrefabs.CanBuild(out string reason))
            {
                report = "[VN 데모] 빌드하지 않았다 — " + reason;
                return false;
            }

            string checkedLine = null;

            try
            {
                EditorUtility.DisplayProgressBar("UI Demo", "그림 임포트 설정", 0.05f);
                KedUIArtImporter.Apply();

                EditorUtility.DisplayProgressBar("UI Demo", "TMP 글꼴", 0.15f);
                TMP_FontAsset font = KedUIFonts.Ensure(problems);
                var kit = new KedUIKit(font);

                EditorUtility.DisplayProgressBar("UI Demo", "화면", 0.3f);

                GameObject optionItem = Save(VNDemoOptionItemSpec.Build(kit), OptionItemPath, built);

                GameObject[] pages =
                {
                    SaveScreen(SettingsSpec.AudioPage(kit), built),
                    SaveScreen(SettingsSpec.TextPage(kit), built),
                    SaveScreen(SettingsSpec.PlaybackPage(kit), built),
                    SaveScreen(SettingsSpec.DisplayPage(kit), built),
                };

                GameObject[] roots =
                {
                    SaveScreen(TitleSpec.Build(kit), built),
                    SaveScreen(PlaySpec.Build(kit), built),
                };

                GameObject[] panels =
                {
                    SaveScreen(MenuSpec.Build(kit), built),
                    SaveScreen(SaveLoadSpec.Build(kit), built),
                    SaveScreen(BacklogSpec.Build(kit), built),
                    SaveScreen(SettingsSpec.Build(kit, pages), built),
                    SaveScreen(ModalSpec.Confirm(kit), built),
                    SaveScreen(ModalSpec.Notice(kit), built),
                };

                EditorUtility.DisplayProgressBar("UI Demo", "데이터 자산", 0.7f);

                UIFontSet fontSet = FontSet(font);
                UIPresentationSpec[] presentations = Presentations();

                TextAsset texts = AssetDatabase.LoadAssetAtPath<TextAsset>(TextsPath);
                if (texts == null)
                    problems.Add($"글자 표 없음: {TextsPath}");

                Sprite stage = kit.Sprite(StageSprite);
                problems.AddRange(kit.Problems);

                // 화면을 세우며 채운 글자를 비운 채로 저장한다 — 커밋되는 글꼴 자산을 작게 둔다.
                KedUIFonts.ClearDynamicData(problems);
                AssetDatabase.SaveAssets();

                EditorUtility.DisplayProgressBar("UI Demo", "씬", 0.85f);
                BuildScene(roots, panels, fontSet, texts, presentations, optionItem, stage);
                built.Add(ScenePath);

                EditorUtility.DisplayProgressBar("UI Demo", "검사", 0.95f);
                checkedLine = KedUIValidate.Run(problems);
            }
            catch (Exception error)
            {
                problems.Add("빌드 중단: " + error);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            problems = problems.Distinct().ToList();

            report = UIScreenValidation.Report("[VN 데모]", checkedLine ?? "—", problems) +
                     "만든 것:\n  " + string.Join("\n  ", built);
            return problems.Count == 0;
        }

        #region 데이터 자산

        // UIPF 공용 글꼴 묶음 — 언어 줄마다 같은 글꼴(Noto Sans KR은 라틴 글자도 갖는다). 묶음의 Common/Fonts에 있다.
        private static UIFontSet FontSet(TMP_FontAsset font)
        {
            string path = KedUIFonts.FontDir + "/KedUIFontSet.asset";
            UIFontSet set = AssetDatabase.LoadAssetAtPath<UIFontSet>(path);

            if (set == null)
            {
                set = ScriptableObject.CreateInstance<UIFontSet>();
                AssetDatabase.CreateAsset(set, path);
            }

            set.locales = new[]
            {
                new UIFontSet.LocaleFonts { localeId = "ko-KR", body = font },
                new UIFontSet.LocaleFonts { localeId = "en-US", body = font },
            };

            EditorUtility.SetDirty(set);
            return set;
        }

        // 화면마다 Presentation 하나 — 없을 때만 만든다(묶음의 Presentation 폴더). 재생 화면의 화면비 Variant도 없을 때만.
        private static UIPresentationSpec[] Presentations()
        {
            var specs = new List<UIPresentationSpec>(ViewTypes.Length);

            foreach (Type type in ViewTypes)
            {
                string key = UIViewKey.Of(type);
                string path = KedUIUnits.Rules.PresentationPath(type, key, FallbackScreensDir);
                UIPresentationSpec spec = UIPresentationAssets.Ensure(type, path);

                if (type == typeof(PlayScreen))
                    PlaySpec.EnsureVariants(spec);

                specs.Add(spec);
            }

            return specs.ToArray();
        }

        #endregion

        #region 씬

        private static void BuildScene(
            GameObject[] roots,
            GameObject[] panels,
            UIFontSet fontSet,
            TextAsset texts,
            UIPresentationSpec[] presentations,
            GameObject optionItem,
            Sprite stage)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            Camera cam = camera.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);
            cam.orthographic = true;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var canvasGo = new GameObject("VisualNovelDemoCanvas", typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // 시안 1600×900. Expand — 어떤 화면비에서도 1600×900 판 전체가 들어온다(남는 쪽은 가장자리로 늘어난다).
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 100f;

            canvasGo.AddComponent<GraphicRaycaster>();
            VNDemoHost host = canvasGo.AddComponent<VNDemoHost>();
            canvasGo.AddComponent<UIDisplayRefreshDriver>();

            // 무대 — 재생 화면은 투명하다(ked에서는 연출 무대가 그린다). 데모는 그림 한 장을 깐다.
            Stage(canvasGo.transform, stage);

            RectTransform rootLayer = Layer(canvasGo.transform, "RootLayer");
            RectTransform panelLayer = Layer(canvasGo.transform, "PanelLayer");
            RectTransform overlayLayer = Layer(canvasGo.transform, "OverlayLayer");

            foreach (GameObject prefab in roots)
                Place(prefab, rootLayer);

            foreach (GameObject prefab in panels)
                Place(prefab, panelLayer);

            var so = new SerializedObject(host);
            so.FindProperty("rootLayer").objectReferenceValue = rootLayer;
            so.FindProperty("panelLayer").objectReferenceValue = panelLayer;
            so.FindProperty("overlayLayer").objectReferenceValue = overlayLayer;
            so.FindProperty("fontSet").objectReferenceValue = fontSet;
            so.FindProperty("texts").objectReferenceValue = texts;
            so.FindProperty("optionItemPrefab").objectReferenceValue =
                optionItem != null ? optionItem.GetComponent<VNDemoOptionItem>() : null;
            so.FindProperty("examplePreview").objectReferenceValue = stage;

            SerializedProperty list = so.FindProperty("presentations");
            list.arraySize = presentations.Length;
            for (int i = 0; i < presentations.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = presentations[i];

            so.ApplyModifiedPropertiesWithoutUndo();

            UIPrefabs.EnsureFolder(UIPrefabs.DirectoryOf(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
        }

        // 플레이어 빌드의 첫 씬 — 이미 목록에 있으면 그대로 둔다(순서 · 켜짐을 사람이 바꿨을 수 있다).
        private static void AddToBuildSettings(string path)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Any(scene => scene.path == path))
                return;

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) }.Concat(scenes).ToArray();
        }

        private static void Stage(Transform canvas, Sprite sprite)
        {
            var go = new GameObject("Stage", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");

            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;

            // 화면 전체를 비율 유지로 덮는다(넘치는 쪽은 잘린다).
            AspectRatioFitter fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite != null ? sprite.rect.width / Mathf.Max(1f, sprite.rect.height) : 16f / 9f;
        }

        private static RectTransform Layer(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");

            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static void Place(GameObject prefab, RectTransform layer)
        {
            if (prefab == null)
                return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(layer, false);
            instance.SetActive(false);
        }

        #endregion

        #region 저장

        private static GameObject SaveScreen(GameObject go, List<string> built)
        {
            UIBase view = go.GetComponent<UIBase>();
            string path = KedUIUnits.Rules.PrefabPath(view != null ? view.GetType() : null, go.name, FallbackScreensDir);
            return Save(go, path, built);
        }

        private static GameObject Save(GameObject go, string path, List<string> built)
        {
            GameObject prefab = UIPrefabs.Save(go, path);
            built.Add(path);
            return prefab;
        }

        #endregion
    }
}
