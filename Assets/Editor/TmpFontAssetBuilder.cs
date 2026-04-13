using System.IO;
using System.Text;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class TmpFontAssetBuilder
{
    private const int PreferredSamplingPointSize = 384;
    private const int PreferredAtlasPadding = 10;
    private const int PreferredAtlasSize = 8192;
    private const string HiraginoSourceFontPath = "Assets/Resources/Fonts/HiraginoSansGB.ttc";
    private const string BodyFontAssetPath = "Assets/Resources/Fonts/TMP/Hiragino Sans GB UI Body SDF.asset";
    private const string DisplayFontAssetPath = "Assets/Resources/Fonts/TMP/Hiragino Sans GB UI Display SDF.asset";
    private static readonly string[] ProjectTextExtensions =
    {
        ".cs",
        ".txt",
        ".md",
        ".json",
        ".yaml",
        ".yml",
        ".asset",
        ".unity",
        ".prefab",
        ".uss",
        ".uxml"
    };

    private const string CoreUiCharacters =
        "0123456789" +
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
        "abcdefghijklmnopqrstuvwxyz" +
        "，。！？；：、（）【】《》〈〉“”‘’—…·+-=%/\\\\|_#@&*~^$<>{}[]'\"`" +
        "伊尔萨恩人物背包状态目标探索记录当前任务生命近战攻击封印烛台灰烬客厅誓牌裂痕客厅像被烧毁过却又被谁按原样拼了回来这间不像更像是有人把整层的灰都按在了它上面一路往右边延过去第一盏大概就在前面继续关闭选择回应交互检查拾取离开进入返回确认取消";

    [MenuItem("Tools/DarkVillage/Build TMP Fonts")]
    public static void BuildRuntimeFonts()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        BuildFontAsset(HiraginoSourceFontPath, BodyFontAssetPath, "Hiragino Sans GB UI Body SDF");
        BuildFontAsset(HiraginoSourceFontPath, DisplayFontAssetPath, "Hiragino Sans GB UI Display SDF");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TmpFontAssetBuilder] Hiragino Sans GB TMP fonts rebuilt.");
    }

    private static void BuildFontAsset(string sourceFontPath, string targetAssetPath, string fontAssetName)
    {
        AssetDatabase.ImportAsset(sourceFontPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourceFontPath);
        if (sourceFont == null)
        {
            throw new FileNotFoundException($"Could not load font at path: {sourceFontPath}");
        }

        string directory = Path.GetDirectoryName(targetAssetPath)?.Replace("\\", "/");
        if (!string.IsNullOrWhiteSpace(directory) && !AssetDatabase.IsValidFolder(directory))
        {
            CreateFolderRecursive(directory);
        }

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetAssetPath) != null)
        {
            AssetDatabase.DeleteAsset(targetAssetPath);
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            PreferredSamplingPointSize,
            PreferredAtlasPadding,
            GlyphRenderMode.SDF16,
            PreferredAtlasSize,
            PreferredAtlasSize,
            AtlasPopulationMode.Dynamic,
            true);

        fontAsset.name = fontAssetName;
        fontAsset.isMultiAtlasTexturesEnabled = true;
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        AssetDatabase.CreateAsset(fontAsset, targetAssetPath);

        string prebakedCharacters = CollectPrebakedCharacters();
        if (!string.IsNullOrEmpty(prebakedCharacters))
        {
            fontAsset.TryAddCharacters(prebakedCharacters, out string missingCharacters);
            if (!string.IsNullOrEmpty(missingCharacters))
            {
                Debug.LogWarning($"[TmpFontAssetBuilder] Missing {missingCharacters.Length} prebaked characters for '{fontAssetName}'. Dynamic fallback will cover the remainder.");
            }
        }

        AddSubAssetIfNeeded(fontAsset.material, fontAsset, $"{fontAsset.name} Material");

        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                Texture2D atlasTexture = fontAsset.atlasTextures[i];
                AddSubAssetIfNeeded(atlasTexture, fontAsset, $"{fontAsset.name} Atlas {i}");
            }
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
    }

    private static string CollectPrebakedCharacters()
    {
        HashSet<char> seenCharacters = new HashSet<char>();
        StringBuilder builder = new StringBuilder(CoreUiCharacters.Length + 2048);
        AppendUniqueCharacters(builder, seenCharacters, CoreUiCharacters);
        AppendUniqueCharacters(builder, seenCharacters, "\n\r\t ");

        CollectProjectCharacters(builder, seenCharacters, "Assets");
        CollectProjectCharacters(builder, seenCharacters, "Docs");
        CollectFileCharacters(builder, seenCharacters, "PROJECT_PROGRESS.txt");

        return builder.ToString();
    }

    private static void CollectProjectCharacters(StringBuilder builder, HashSet<char> seenCharacters, string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            return;
        }

        string[] files = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string extension = Path.GetExtension(files[i]);
            if (!ShouldScanTextFile(extension))
            {
                continue;
            }

            CollectFileCharacters(builder, seenCharacters, files[i]);
        }
    }

    private static void CollectFileCharacters(StringBuilder builder, HashSet<char> seenCharacters, string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        string content;
        try
        {
            content = File.ReadAllText(filePath);
        }
        catch (IOException)
        {
            return;
        }

        AppendUniqueCharacters(builder, seenCharacters, content);
    }

    private static bool ShouldScanTextFile(string extension)
    {
        for (int i = 0; i < ProjectTextExtensions.Length; i++)
        {
            if (extension.Equals(ProjectTextExtensions[i], System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void AppendUniqueCharacters(StringBuilder builder, HashSet<char> seenCharacters, string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
            {
                continue;
            }

            if (!seenCharacters.Add(c))
            {
                continue;
            }

            builder.Append(c);
        }
    }

    private static void AddSubAssetIfNeeded(Object subAsset, Object owner, string name)
    {
        if (subAsset == null || owner == null)
        {
            return;
        }

        subAsset.name = name;
        string subAssetPath = AssetDatabase.GetAssetPath(subAsset);
        string ownerPath = AssetDatabase.GetAssetPath(owner);

        if (string.IsNullOrEmpty(subAssetPath))
        {
            AssetDatabase.AddObjectToAsset(subAsset, owner);
        }
        else if (subAssetPath != ownerPath)
        {
            Object duplicated = Object.Instantiate(subAsset);
            duplicated.name = name;
            AssetDatabase.AddObjectToAsset(duplicated, owner);
        }
    }

    private static void CreateFolderRecursive(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }
}
