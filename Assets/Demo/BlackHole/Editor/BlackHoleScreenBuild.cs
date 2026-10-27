using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BlackHole.EditorTools
{
    // 메뉴 UI Demo > Build BlackHole Screens: 빌드 명세가 있는 화면의 프리팹을 짓는다. 지금은 일시 정지 창 하나다.
    // 같은 자리에 덮어쓰므로 GUID가 그대로 — 씬(BlackHoleDemoSceneBuilder)과 Presentation의 참조가 이어진다.
    // 저장한 뒤 데모의 프리팹 전체와 Presentation을 UIPF 검사(UIScreenValidation)로 본다 — 손 프리팹도 같은 그물을 지난다.
    //
    // 데모 폴더는 화면 묶음 구조가 아니다(Prefabs/ · Presentations/ · Scripts/Screens/가 따로).
    // 그래도 같은 도구를 쓴다 — 자리를 직접 적을 뿐이다.
    internal static class BlackHoleScreenBuild
    {
        private const string PrefabsDir = BlackHoleScreenKit.Root + "/Prefabs";
        private const string PresentationsDir = BlackHoleScreenKit.Root + "/Presentations";

        [MenuItem("UI Demo/Build BlackHole Screens")]
        private static void BuildFromMenu()
        {
            bool ok = Build(out string report);

            if (ok)
                Debug.Log(report);
            else
                Debug.LogError(report);

            EditorUtility.DisplayDialog(
                "UI Demo",
                ok ? "화면을 지었습니다. 콘솔에 보고가 있습니다." : "문제가 있습니다. 콘솔의 보고를 확인하세요.",
                "확인");
        }

        public static bool Build(out string report)
        {
            var problems = new List<string>();
            var built = new List<string>();

            if (!UIPrefabs.CanBuild(out string reason))
            {
                report = "[블랙홀 화면] 빌드하지 않았다 — " + reason;
                return false;
            }

            string checkedLine = null;

            try
            {
                var kit = new BlackHoleScreenKit();

                Save(PausePanelSpec.Build(kit), PrefabsDir + "/PausePanel.prefab", built);

                problems.AddRange(kit.Problems);
                AssetDatabase.SaveAssets();

                checkedLine = UIScreenValidation.Run(new[] { PrefabsDir, PresentationsDir }, problems);
            }
            catch (Exception error)
            {
                problems.Add("빌드 중단: " + error);
            }

            var lines = new List<string> { "만든 것:" };
            foreach (string path in built)
                lines.Add("  " + path);

            report = UIScreenValidation.Report("[블랙홀 화면]", checkedLine ?? "—", problems) + string.Join("\n", lines);
            return problems.Count == 0;
        }

        private static void Save(GameObject go, string path, List<string> built)
        {
            UIPrefabs.Save(go, path);
            built.Add(path);
        }
    }
}
