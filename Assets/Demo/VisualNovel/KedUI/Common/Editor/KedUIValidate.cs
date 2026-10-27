using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// Tools/Ked UI/Validate Sample UI — 빌드하지 않고 검사만 한다.
    /// 빌더가 만든 프리팹이든, 손이나 다른 에디터 스크립트로 만든 특수 화면의 프리팹이든 같은 그물을 지난다.
    /// 어느 화면도 이름으로 알지 않는다 — 묶음(Screens/*) 아래에 있는 것을 모두 본다.
    ///
    ///   1. 그림 이름 — KedUI 아래 같은 이름의 PNG가 둘 이상인가(그림은 이름으로 찾는다)
    ///   2. Refs · Presentation — UIPF 검사(UIScreenValidation)를 엄하게: 위젯도 Refs가 모두 있어야 하고,
    ///      presentationId가 비면 안 된다(KedUI는 화면 키를 id로 쓴다)
    ///   3. 묶음 — 묶음 안 에셋이 제 묶음 · Common · UIPF 런타임 코드 · Unity 패키지만 가리키는가(UIScreenUnits.Audit)
    ///   4. 화면 키 — 묶음의 화면 · 위젯은 키를 "Ked."로 고정했는가([UIViewKey]). 묶음은 다른 프로젝트로 옮겨 다니고
    ///      (UIPF의 비주얼 노벨 샘플), 그쪽에도 TitleScreen · SettingsPanel 같은 이름이 흔하다 — 키는 프로젝트에서 하나여야 한다.
    ///      등록하는 화면은 제 묶음의 Presentation이 있고 그 presentationId가 화면 키와 같은가(호스트가 id로 찾는다)
    ///
    /// 빌드(KedUIBuild)도 저장한 뒤 이것을 돌린다.
    /// 배치 모드: -executeMethod Ked.Samples.UI.Editor.KedUIValidate.ValidateBatch (문제가 있으면 종료 코드 1).
    /// </summary>
    public static class KedUIValidate
    {
        [MenuItem("Tools/Ked UI/Validate Sample UI")]
        public static void ValidateFromMenu()
        {
            bool ok = Validate(out string report);

            if (ok)
                Debug.Log(report);
            else
                Debug.LogError(report);

            EditorUtility.DisplayDialog(
                "Ked UI",
                ok ? "검사에서 문제가 없습니다. 콘솔에 보고가 있습니다." : "문제가 있습니다. 콘솔의 보고를 확인하세요.",
                "확인");
        }

        /// <summary>배치 모드용. Unity가 이 프로젝트를 열고 있으면 배치 모드는 프로젝트를 열지 못한다.</summary>
        public static void ValidateBatch()
        {
            bool ok = Validate(out string report);
            Debug.Log(report);

            if (Application.isBatchMode)
                EditorApplication.Exit(ok ? 0 : 1);
        }

        public static bool Validate(out string report)
        {
            var problems = new List<string>();
            string checkedLine = Run(problems);

            report = UIScreenValidation.Report("[Ked UI]", checkedLine, problems);
            return problems.Count == 0;
        }

        /// <summary>검사를 돌려 problems에 더하고, 무엇을 몇 개 봤는지 한 줄로 돌려준다.</summary>
        public static string Run(List<string> problems)
        {
            KedUIUnits.Art(problems);

            string screens = UIScreenValidation.Run(
                new[] { KedUIUnits.UnitsDir },
                problems,
                UIScreenChecks.WidgetRefs | UIScreenChecks.PresentationIds);

            int units = KedUIUnits.Rules.Audit(problems);
            int keys = CheckKeys(problems);

            return $"화면 묶음 {units} · {screens} · 화면 키 {keys}";
        }

        private const string KeyPrefix = "Ked.";

        private static int CheckKeys(List<string> problems)
        {
            Assembly bundle = typeof(TitleScreen).Assembly;
            int count = 0;

            foreach (Type type in TypeCache.GetTypesDerivedFrom<UIBase>())
            {
                if (type.Assembly != bundle || type.IsAbstract || type.IsGenericTypeDefinition)
                    continue;

                count++;
                string key = UIViewKey.Of(type);

                if (!key.StartsWith(KeyPrefix, StringComparison.Ordinal))
                    problems.Add($"화면 키가 '{KeyPrefix}'로 시작하지 않는다: {type.FullName} ('{key}') — [UIViewKey(\"{KeyPrefix}{type.Name}\")]를 붙인다");

                bool registered = typeof(IUIRoot).IsAssignableFrom(type) || typeof(IUIPanel).IsAssignableFrom(type) ||
                                  typeof(IUIPage).IsAssignableFrom(type) || typeof(IUIOverlay).IsAssignableFrom(type);
                if (!registered || KedUIUnits.Rules.UnitOf(type) == null)
                    continue;

                string path = KedUIUnits.Rules.PresentationPath(type, key, null);
                var spec = AssetDatabase.LoadAssetAtPath<UIPresentationSpec>(path);

                if (spec == null)
                    problems.Add($"Presentation 없음: {path} ({type.Name}) — 앱 빌드가 처음 한 번 만든다");
                else if (spec.presentationId != key)
                    problems.Add($"presentationId가 화면 키와 다르다: {path} ('{spec.presentationId}', 키 '{key}')");
            }

            return count;
        }
    }
}
