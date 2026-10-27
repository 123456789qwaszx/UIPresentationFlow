using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 그림을 이름(확장자 뺀 파일 이름)으로 찾는다.
// 명세가 경로가 아니라 이름을 쓰면, 그림을 공용과 화면 전용 사이에서 옮겨도 명세를 고치지 않는다.
//
// 같은 이름이 둘 이상이면 그 이름으로는 찾지 않는다(어느 쪽인지 이름만으로 정해지지 않는다).
// 겹침을 모두 문제로 볼지는 쓰는 쪽이 정한다 — ReportDuplicates. 테마 변형처럼 일부러 같은 이름을 두는
// 폴더(블랙홀의 Art/GUI/Dark)는 exclude로 뺀다.
public sealed class UISpriteIndex
{
    private readonly Dictionary<string, string> _paths;
    private readonly Dictionary<string, List<string>> _duplicates;
    private readonly Dictionary<string, Sprite> _loaded = new(StringComparer.Ordinal);

    private UISpriteIndex(Dictionary<string, string> paths, Dictionary<string, List<string>> duplicates)
    {
        _paths = paths;
        _duplicates = duplicates;
    }

    public IReadOnlyDictionary<string, string> Paths => _paths;

    public IReadOnlyDictionary<string, List<string>> Duplicates => _duplicates;

    // folders 아래(하위 폴더 포함)의 PNG. exclude 아래는 뺀다.
    public static UISpriteIndex Build(IEnumerable<string> folders, IEnumerable<string> exclude = null)
    {
        var excluded = new List<string>();
        if (exclude != null)
        {
            foreach (string folder in exclude)
                excluded.Add(folder.TrimEnd('/') + "/");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var found = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        // 폴더를 하나도 넘기지 않으면 FindAssets는 프로젝트 전체를 본다 — 없는 폴더뿐이면 빈 색인.
        string[] roots = Existing(folders);
        string[] guids = roots.Length > 0 ? AssetDatabase.FindAssets("t:Texture2D", roots) : Array.Empty<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || !seen.Add(path))
                continue;

            if (excluded.Exists(prefix => path.StartsWith(prefix, StringComparison.Ordinal)))
                continue;

            string name = Path.GetFileNameWithoutExtension(path);
            if (!found.TryGetValue(name, out List<string> paths))
                found[name] = paths = new List<string>();

            paths.Add(path);
        }

        var index = new Dictionary<string, string>(StringComparer.Ordinal);
        var duplicates = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (KeyValuePair<string, List<string>> pair in found)
        {
            if (pair.Value.Count == 1)
                index[pair.Key] = pair.Value[0];
            else
                duplicates[pair.Key] = pair.Value;
        }

        return new UISpriteIndex(index, duplicates);
    }

    public bool TryGetPath(string name, out string path) => _paths.TryGetValue(name, out path);

    // 이름의 스프라이트. 없거나 이름이 겹치면 problems에 올리고 null.
    public Sprite Load(string name, List<string> problems)
    {
        if (_loaded.TryGetValue(name, out Sprite cached))
            return cached;

        Sprite sprite = null;

        if (_paths.TryGetValue(name, out string path))
        {
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                problems?.Add($"스프라이트 없음(Texture Type이 Sprite가 아니다?): {path}");
        }
        else if (_duplicates.TryGetValue(name, out List<string> paths))
        {
            problems?.Add($"그림 이름이 겹친다 '{name}': {string.Join(", ", paths)}");
        }
        else
        {
            problems?.Add($"그림 없음: {name}.png");
        }

        _loaded[name] = sprite;
        return sprite;
    }

    public void ReportDuplicates(List<string> problems)
    {
        foreach (KeyValuePair<string, List<string>> pair in _duplicates)
            problems.Add($"그림 이름이 겹친다 '{pair.Key}': {string.Join(", ", pair.Value)}");
    }

    private static string[] Existing(IEnumerable<string> folders)
    {
        var result = new List<string>();
        foreach (string folder in folders)
        {
            if (AssetDatabase.IsValidFolder(folder))
                result.Add(folder);
        }

        return result.ToArray();
    }
}
