using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class UIImageBindingBuilder
{
    private const string RootPath = "Assets/UI/ImageBindings";

    public static UIImageBindingSet Build(
        UIBase view,
        string themeId)
    {
        if (view == null)
            throw new ArgumentNullException(nameof(view));

        if (string.IsNullOrWhiteSpace(themeId))
            throw new ArgumentException(
                "Theme id is required.",
                nameof(themeId));

        themeId = themeId.Trim();

        List<UIImageBindingEntry> entries =
            UIImageBindingScanner.Scan(view);

        string viewName = view.GetType().Name;
        string themePath = $"{RootPath}/{themeId}";
        string viewPath = $"{themePath}/{viewName}";
        string assetPath =
            $"{viewPath}/{viewName}.ImageBinding.asset";

        EnsureFolder(RootPath);
        EnsureFolder(themePath);
        EnsureFolder(viewPath);

        UIImageBindingSet existing =
            AssetDatabase.LoadAssetAtPath<UIImageBindingSet>(assetPath);

        if (existing != null)
        {
            Debug.LogWarning(
                $"[ImageBindingBuilder] Binding already exists: {assetPath}",
                existing);

            return existing;
        }

        UIImageBindingSet binding =
            ScriptableObject.CreateInstance<UIImageBindingSet>();

        binding.viewId =
            view.GetType().FullName ?? viewName;

        binding.images = entries;

        AssetDatabase.CreateAsset(binding, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[ImageBindingBuilder] Created '{themeId}/{viewName}' " +
            $"with {entries.Count} image binding(s).",
            binding);

        return binding;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent =
            path.Substring(0, path.LastIndexOf('/'));

        string folderName =
            path.Substring(path.LastIndexOf('/') + 1);

        EnsureFolder(parent);

        AssetDatabase.CreateFolder(
            parent,
            folderName);
    }
}