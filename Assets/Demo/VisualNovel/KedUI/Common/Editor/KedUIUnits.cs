using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Ked.Samples.UI.Editor
{
    /// <summary>
    /// KedUI의 자리와 화면 묶음 설정. 규칙(생성물 자리 · 묶음 감사)과 그림을 이름으로 찾는 도구는 UIPF의
    /// UIScreenUnits · UISpriteIndex(Editor/UI/Authoring)이고, 여기는 KedUI가 넘기는 값만 둔다.
    ///
    ///   {뿌리}/Screens/{묶음}/Scripts · Prefabs · Presentation · Sprites · Editor
    ///   {뿌리}/Common/   여러 화면이 같이 쓰는 키트 스크립트 · 그림 · 글꼴
    ///
    /// 뿌리는 런타임 asmdef(Ked.Samples.UI.asmdef)가 있는 폴더다 — 고정 경로를 쓰지 않아, 화면 묶음을 다른 저장소의
    /// 다른 자리로 복사해도(UIPF의 비주얼 노벨 데모) 그대로 돈다.
    /// 묶음은 제 묶음 · Common · UIPF 런타임 코드 · Unity 패키지만 가리킬 수 있다(Audit). 재생 런타임(ked 본체)은 모른다.
    /// </summary>
    public static class KedUIUnits
    {
        private const string FallbackRoot = "Assets/Samples/KedUI";

        private static string _root;
        private static UIScreenUnits _rules;

        public static string Root => _root ??= AsmdefDir("Ked.Samples.UI") ?? FallbackRoot;

        public static string UnitsDir => Root + "/Screens";
        public static string CommonDir => Root + "/Common";

        public static UIScreenUnits Rules => _rules ??= NewRules();

        /// <summary>
        /// KedUI 아래 PNG를 이름으로 — 그림은 Art/ · Common/Sprites/ · 묶음의 Sprites/ 어디에 있어도 된다.
        /// KedUI는 같은 이름을 하나도 두지 않는다 — 겹치면 모두 problems에 올린다.
        /// </summary>
        public static UISpriteIndex Art(List<string> problems)
        {
            UISpriteIndex index = UISpriteIndex.Build(new[] { Root });
            index.ReportDuplicates(problems);
            return index;
        }

        private static UIScreenUnits NewRules()
        {
            // UIPF 코드의 자리도 asmdef로 찾는다(ked는 사본 Assets/UIPresentationFlow/Scripts, UIPF 저장소는 Assets/Scripts).
            string uipf = AsmdefDir("UIPresentationFlow.Runtime");
            return uipf != null
                ? new UIScreenUnits(UnitsDir, CommonDir, uipf)
                : new UIScreenUnits(UnitsDir, CommonDir);
        }

        private static string AsmdefDir(string assemblyName)
        {
            foreach (string guid in AssetDatabase.FindAssets($"{assemblyName} t:AssemblyDefinitionAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == assemblyName)
                    return UIPrefabs.DirectoryOf(path);
            }

            return null;
        }
    }
}
