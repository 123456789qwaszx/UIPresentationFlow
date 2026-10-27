using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

// 화면 묶음 — 화면 하나의 코드 · 프리팹 · 그림 · Presentation을 한 폴더에 둔다(docs/AUTHORING.md "화면 묶음").
//
//   {screens}/{묶음}/Scripts/        화면 · 위젯 스크립트
//   {screens}/{묶음}/Prefabs/        빌더가 만든 화면 프리팹(산출물)
//   {screens}/{묶음}/Presentation/   {화면 키}Presentation.asset
//   {screens}/{묶음}/Sprites/        그 화면만 쓰는 그림
//   {screens}/{묶음}/Data/           그 화면에 맞춘 데이터
//   {screens}/{묶음}/Editor/         빌드 명세(+ 에디터 asmdef를 가리키는 .asmref)
//   {common}/                        여러 화면이 같이 쓰는 것
//
// 뿌리 경로와 "묶음 밖에서 가리켜도 되는 곳"은 앱이 코드 상수로 넘긴다. 하위 폴더 이름은 이 규칙으로 고정한다.
// 화면 스크립트가 묶음 안에 없으면 생성물은 앱이 정한 대체 폴더로 간다 — 묶음을 쓰지 않는 앱도 같은 도구를 쓴다.
public sealed class UIScreenUnits
{
    private readonly string[] _allowedOutside;

    // allowedOutside: 묶음이 가리켜도 되는 묶음 밖의 폴더(이 프레임워크, 게임 쪽 프레임워크 …).
    // Packages/와 내장 리소스는 늘 된다.
    public UIScreenUnits(string screensDir, string commonDir, params string[] allowedOutside)
    {
        ScreensDir = screensDir.TrimEnd('/');
        CommonDir = commonDir.TrimEnd('/');
        _allowedOutside = Array.ConvertAll(allowedOutside ?? Array.Empty<string>(), dir => dir.TrimEnd('/'));
    }

    public string ScreensDir { get; }
    public string CommonDir { get; }

    #region 생성물 자리

    // 화면 스크립트가 {screens}/{묶음}/Scripts/ 바로 아래에 있으면 그 묶음 폴더, 아니면 null.
    public string UnitOf(Type viewType)
    {
        string script = ScriptPath(viewType);
        if (script == null || !script.StartsWith(ScreensDir + "/", StringComparison.Ordinal))
            return null;

        string dir = Path.GetDirectoryName(script)?.Replace('\\', '/');
        if (dir == null || !dir.EndsWith("/Scripts", StringComparison.Ordinal))
            return null;

        return dir.Substring(0, dir.Length - "/Scripts".Length);
    }

    public string PrefabPath(Type viewType, string name, string fallbackDir)
    {
        string unit = UnitOf(viewType);
        return unit != null ? $"{unit}/Prefabs/{name}.prefab" : $"{fallbackDir}/{name}.prefab";
    }

    public string PresentationPath(Type viewType, string key, string fallbackDir)
    {
        string unit = UnitOf(viewType);
        return unit != null ? $"{unit}/Presentation/{key}Presentation.asset" : $"{fallbackDir}/{key}.asset";
    }

    public string DataPath(Type viewType, string fileName, string fallbackDir)
    {
        string unit = UnitOf(viewType);
        return unit != null ? $"{unit}/Data/{fileName}" : $"{fallbackDir}/{fileName}";
    }

    #endregion

    #region 감사

    public string[] Units() =>
        AssetDatabase.IsValidFolder(ScreensDir) ? AssetDatabase.GetSubFolders(ScreensDir) : Array.Empty<string>();

    // 묶음마다: 안의 에셋이 직접 가리키는 것이 제 묶음 · 공용 · 허용한 곳 · 패키지 · 내장 리소스뿐인가.
    // 다른 묶음이나 뿌리의 다른 폴더를 가리키면 problems에 올린다. 돌려주는 값은 감사한 묶음 수.
    public int Audit(List<string> problems)
    {
        string[] units = Units();

        foreach (string unit in units)
        {
            foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { unit }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                    continue;

                foreach (string dependency in AssetDatabase.GetDependencies(path, false))
                {
                    if (!IsAllowed(unit, dependency))
                        problems.Add($"묶음 밖을 가리킨다: {path} → {dependency}");
                }
            }
        }

        return units.Length;
    }

    private bool IsAllowed(string unit, string dependency)
    {
        if (!dependency.StartsWith("Assets/", StringComparison.Ordinal))
            return true;

        if (dependency.StartsWith(unit + "/", StringComparison.Ordinal) ||
            dependency.StartsWith(CommonDir + "/", StringComparison.Ordinal))
            return true;

        foreach (string dir in _allowedOutside)
        {
            if (dependency.StartsWith(dir + "/", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    #endregion

    private static string ScriptPath(Type type)
    {
        if (type == null)
            return null;

        foreach (string guid in AssetDatabase.FindAssets($"t:MonoScript {type.Name}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script != null && script.GetClass() == type)
                return path;
        }

        return null;
    }
}
