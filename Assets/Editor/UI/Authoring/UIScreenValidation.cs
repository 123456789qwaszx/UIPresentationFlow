using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// 화면 프리팹과 Presentation을 검사한다 — 빌드와 따로 돈다.
// 빌더가 만든 프리팹이든, 손이나 다른 에디터 스크립트로 만든 특수 화면이든 같은 그물을 지난다.
//
//   Refs          화면 · 위젯(UIBase<TRefs>)마다 Refs의 이름이 제 아래(자신 포함)에 몇 개인가.
//                 UIBase는 이름으로 찾은 첫 번째를 잡는다.
//                 둘 이상 → 문제. 어느 것이 잡힐지 이름만으로 정해지지 않는다.
//                 없음 → 등록하는 화면(Root · Panel · Page · Overlay)은 문제(UIManager.Register도 경고한다).
//                        위젯은 일부만 쓸 수 있어(한 프리팹, 여러 꼴) 기본으로는 넘어간다.
//   Presentation  오브젝트 이름이 파일 이름과 같은가(다르면 Unity가 경고한다).
//
// 앱이 더 엄하게 볼 수 있다(UIScreenChecks): 위젯도 Refs가 모두 있어야 한다, presentationId가 비면 안 된다
// (비어 있으면 UIContext.PresentationOverrides로 Variant를 고를 수 없고 Trace에 이름이 없다 — 틀린 것은 아니다).
//
// 메뉴 Tools/UI Presentation/Validate Screens는 프로젝트 전체(Assets/)를 본다.
// 앱은 Run에 자기 폴더를 넘기고 묶음 감사(UIScreenUnits.Audit) · 그림 이름(UISpriteIndex)을 더해 쓴다.
// 배치 모드: -executeMethod UIScreenValidation.ValidateBatch (문제가 있으면 종료 코드 1).
// Unity가 그 프로젝트를 열고 있으면 배치 모드는 프로젝트를 열지 못한다.
public static class UIScreenValidation
{
    private const string Title = "[UI 검사]";

    [MenuItem("Tools/UI Presentation/Validate Screens")]
    private static void ValidateFromMenu()
    {
        bool ok = Validate(new[] { "Assets" }, out string report);

        if (ok)
            Debug.Log(report);
        else
            Debug.LogError(report);

        EditorUtility.DisplayDialog(
            "UI Presentation",
            ok ? "검사에서 문제가 없습니다. 콘솔에 보고가 있습니다." : "문제가 있습니다. 콘솔의 보고를 확인하세요.",
            "확인");
    }

    public static void ValidateBatch()
    {
        bool ok = Validate(new[] { "Assets" }, out string report);
        Debug.Log(report);

        if (Application.isBatchMode)
            EditorApplication.Exit(ok ? 0 : 1);
    }

    public static bool Validate(string[] folders, out string report)
    {
        var problems = new List<string>();
        string checkedLine = Run(folders, problems);

        report = Report(Title, checkedLine, problems);
        return problems.Count == 0;
    }

    // folders 아래 프리팹과 Presentation을 검사해 problems에 더하고, 무엇을 몇 개 봤는지 한 줄로 돌려준다.
    public static string Run(string[] folders, List<string> problems, UIScreenChecks strict = UIScreenChecks.None)
    {
        int prefabs = 0;
        int views = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", folders))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab == null)
                continue;

            prefabs++;
            views += CheckRefs(prefab, problems, (strict & UIScreenChecks.WidgetRefs) != 0);
        }

        int presentations = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:UIPresentationSpec", folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var spec = AssetDatabase.LoadAssetAtPath<UIPresentationSpec>(path);
            if (spec == null)
                continue;

            presentations++;
            CheckPresentation(spec, path, problems, (strict & UIScreenChecks.PresentationIds) != 0);
        }

        return $"프리팹 {prefabs}(화면 · 위젯 {views}) · Presentation {presentations}";
    }

    // 프리팹 안의 화면 · 위젯마다 Refs를 검사한다. 돌려주는 값은 검사한 화면 · 위젯 수.
    public static int CheckRefs(GameObject prefab, List<string> problems, bool widgetsNeedAllRefs = false)
    {
        if (prefab == null)
            return 0;

        int checkedViews = 0;

        foreach (UIBase view in prefab.GetComponentsInChildren<UIBase>(true))
        {
            Type refs = UIRefCheck.RefsTypeOf(view.GetType());
            if (refs == null)
                continue;

            checkedViews++;
            bool needsAll = widgetsNeedAllRefs || IsRegistered(view);

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Transform t in view.GetComponentsInChildren<Transform>(true))
                counts[t.name] = counts.TryGetValue(t.name, out int n) ? n + 1 : 1;

            foreach (string name in Enum.GetNames(refs))
            {
                counts.TryGetValue(name, out int count);

                if (count == 0 && needsAll)
                    problems.Add($"{prefab.name}/{view.GetType().Name}: Ref '{name}' 자식이 없다");
                else if (count > 1)
                    problems.Add($"{prefab.name}/{view.GetType().Name}: Ref '{name}' 자식이 {count}개다(첫 번째만 잡힌다)");
            }
        }

        return checkedViews;
    }

    public static void CheckPresentation(UIPresentationSpec spec, string path, List<string> problems, bool idRequired = false)
    {
        if (spec == null)
            return;

        string fileName = Path.GetFileNameWithoutExtension(path);
        if (spec.name != fileName)
            problems.Add($"Presentation 오브젝트 이름이 파일 이름과 다르다: {path} (이름 '{spec.name}')");

        if (idRequired && string.IsNullOrWhiteSpace(spec.presentationId))
            problems.Add($"Presentation의 presentationId가 비었다: {path}");
    }

    public static string Report(string title, string checkedLine, IReadOnlyList<string> problems)
    {
        var sb = new StringBuilder();
        sb.AppendLine(problems.Count == 0 ? $"{title} 검사 통과" : $"{title} 검사 — 문제 {problems.Count}개");
        sb.AppendLine("검사: " + checkedLine);

        foreach (string problem in problems)
            sb.AppendLine("  - " + problem);

        return sb.ToString();
    }

    // UIManager에 등록하는 화면 — Register가 Refs 전체를 확인한다.
    private static bool IsRegistered(UIBase view) =>
        view is IUIRoot || view is IUIPanel || view is IUIPage || view is IUIOverlay;
}

// UIScreenValidation.Run에 더하는 엄한 검사.
[Flags]
public enum UIScreenChecks
{
    None = 0,
    WidgetRefs = 1,       // 위젯도 Refs가 모두 있어야 한다
    PresentationIds = 2,  // presentationId가 비면 안 된다
}
