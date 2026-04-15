using System.IO;
using System.Text;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class TmpFontAssetBuilder
{
    private const int PreferredSamplingPointSize = 120;
    private const int PreferredAtlasPadding = 9;
    private const int PreferredAtlasSize = 2048;
    private const string HiraginoSourceFontPath = "Assets/Resources/Fonts/HiraginoSansGB.ttc";
    private const string BodyFontAssetPath = "Assets/Resources/Fonts/TMP/Hiragino Sans GB UI Body SDF.asset";
    private const string DisplayFontAssetPath = "Assets/Resources/Fonts/TMP/Hiragino Sans GB UI Display SDF.asset";
    private const string CoreUiCharacters =
        "0123456789" +
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
        "abcdefghijklmnopqrstuvwxyz" +
        ".,!?;:()[]<>+-=%/\\\\|_#@&*~^$'\"`";
    private static readonly string[] CharacterSourceExtensions =
    {
        ".cs",
        ".asset",
        ".prefab",
        ".unity",
        ".txt",
        ".json",
        ".md",
        ".yarn"
    };
    private static readonly char[] RequiredValidationCharacters = { '你', '灰', '客', '厅', '选', '择', '继', '续', '生', '命' };

    [MenuItem("Tools/DarkVillage/Build TMP Fonts")]
    public static void BuildRuntimeFonts()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string prebakedCharacters = CollectPrebakedCharacters();
        EnsureFontAsset(HiraginoSourceFontPath, BodyFontAssetPath, "Hiragino Sans GB UI Body SDF", prebakedCharacters);
        EnsureFontAsset(HiraginoSourceFontPath, DisplayFontAssetPath, "Hiragino Sans GB UI Display SDF", prebakedCharacters);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TmpFontAssetBuilder] Hiragino Sans GB TMP fonts rebuilt.");
    }

    [MenuItem("Tools/DarkVillage/Report TMP Missing Characters")]
    public static void ReportMissingCharacters()
    {
        AssetDatabase.ImportAsset(HiraginoSourceFontPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(HiraginoSourceFontPath);
        if (sourceFont == null)
        {
            throw new FileNotFoundException($"Could not load font at path: {HiraginoSourceFontPath}");
        }

        string characters = CollectPrebakedCharacters();
        FontEngine.InitializeFontEngine();
        FontEngineError loadResult = FontEngine.LoadFontFace(sourceFont, PreferredSamplingPointSize);
        if (loadResult != FontEngineError.Success)
        {
            throw new IOException($"Could not load font face for {sourceFont.name}: {loadResult}");
        }

        StringBuilder missingCharacters = new StringBuilder();
        StringBuilder missingCodePoints = new StringBuilder();
        for (int i = 0; i < characters.Length; i++)
        {
            char character = characters[i];
            if (FontEngine.TryGetGlyphIndex(character, out uint glyphIndex) && glyphIndex != 0)
            {
                continue;
            }

            missingCharacters.Append(character);
            if (missingCodePoints.Length > 0)
            {
                missingCodePoints.Append(", ");
            }

            missingCodePoints.Append("U+");
            missingCodePoints.Append(((int)character).ToString("X4"));
        }

        Debug.Log($"[TmpFontAssetBuilder] Missing characters ({missingCharacters.Length}): {missingCharacters}");
        Debug.Log($"[TmpFontAssetBuilder] Missing code points: {missingCodePoints}");
    }

    private static void EnsureFontAsset(string sourceFontPath, string targetAssetPath, string fontAssetName, string prebakedCharacters)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetAssetPath);
        if (IsReadyFontAsset(existing))
        {
            SetClearDynamicDataOnBuild(existing, false);
            return;
        }

        BuildFontAsset(sourceFontPath, targetAssetPath, fontAssetName, prebakedCharacters);
    }

    private static void BuildFontAsset(string sourceFontPath, string targetAssetPath, string fontAssetName, string prebakedCharacters)
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
            GlyphRenderMode.SDFAA,
            PreferredAtlasSize,
            PreferredAtlasSize,
            AtlasPopulationMode.Dynamic,
            true);

        fontAsset.name = fontAssetName;
        fontAsset.isMultiAtlasTexturesEnabled = true;
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        SetClearDynamicDataOnBuild(fontAsset, false);
        AssetDatabase.CreateAsset(fontAsset, targetAssetPath);

        if (!string.IsNullOrEmpty(prebakedCharacters))
        {
            fontAsset.TryAddCharacters(prebakedCharacters, out string missingCharacters);
            if (!string.IsNullOrEmpty(missingCharacters))
            {
                Debug.LogWarning($"[TmpFontAssetBuilder] Missing {missingCharacters.Length} prebaked characters for '{fontAssetName}'. Dynamic fallback will cover the remainder.");
            }
        }

        Material material = AddSubAssetIfNeeded(fontAsset.material, fontAsset, $"{fontAsset.name} Material") as Material;
        if (material != null)
        {
            fontAsset.material = material;
            EditorUtility.SetDirty(material);
        }

        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                Texture2D atlasTexture = fontAsset.atlasTextures[i];
                Texture2D localAtlasTexture = AddSubAssetIfNeeded(atlasTexture, fontAsset, $"{fontAsset.name} Atlas {i}") as Texture2D;
                if (localAtlasTexture != null)
                {
                    fontAsset.atlasTextures[i] = localAtlasTexture;
                    EditorUtility.SetDirty(localAtlasTexture);
                }
            }
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(targetAssetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
    }

    private static string CollectPrebakedCharacters()
    {
        HashSet<char> seenCharacters = new HashSet<char>();
        StringBuilder builder = new StringBuilder(4096);
        AppendUniqueCharacters(builder, seenCharacters, CoreUiCharacters);
        AppendUniqueCharacters(builder, seenCharacters, "\n\r\t ");
        AppendProjectCharacters(builder, seenCharacters);

        return builder.ToString();
    }

    private static void AppendProjectCharacters(StringBuilder builder, HashSet<char> seenCharacters)
    {
        string[] assetPaths = AssetDatabase.GetAllAssetPaths();
        for (int i = 0; i < assetPaths.Length; i++)
        {
            string assetPath = assetPaths[i];
            if (!assetPath.StartsWith("Assets/", System.StringComparison.Ordinal))
            {
                continue;
            }

            if (!HasSupportedTextExtension(assetPath))
            {
                continue;
            }

            if (assetPath.Contains("/Fonts/TMP/"))
            {
                continue;
            }

            string fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            string content;
            try
            {
                content = File.ReadAllText(fullPath);
            }
            catch (IOException)
            {
                continue;
            }

            AppendUsefulProjectCharacters(builder, seenCharacters, content);
        }
    }

    private static bool HasSupportedTextExtension(string assetPath)
    {
        string extension = Path.GetExtension(assetPath);
        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        for (int i = 0; i < CharacterSourceExtensions.Length; i++)
        {
            if (extension.Equals(CharacterSourceExtensions[i], System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void AppendUsefulProjectCharacters(StringBuilder builder, HashSet<char> seenCharacters, string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];
            if (!ShouldIncludeProjectCharacter(c))
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

    private static bool ShouldIncludeProjectCharacter(char c)
    {
        if (char.IsControl(c))
        {
            return c == '\n' || c == '\r' || c == '\t';
        }

        if (c <= 0x007F)
        {
            return !char.IsSurrogate(c);
        }

        return c >= 0x2E80 && c <= 0x9FFF
            || c >= 0xF900 && c <= 0xFAFF
            || c >= 0xFF00 && c <= 0xFFEF;
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

    private static Object AddSubAssetIfNeeded(Object subAsset, Object owner, string name)
    {
        if (subAsset == null || owner == null)
        {
            return null;
        }

        subAsset.name = name;
        string subAssetPath = AssetDatabase.GetAssetPath(subAsset);
        string ownerPath = AssetDatabase.GetAssetPath(owner);

        if (string.IsNullOrEmpty(subAssetPath))
        {
            AssetDatabase.AddObjectToAsset(subAsset, owner);
            return subAsset;
        }

        if (subAssetPath != ownerPath)
        {
            Object duplicated = Object.Instantiate(subAsset);
            duplicated.name = name;
            AssetDatabase.AddObjectToAsset(duplicated, owner);
            return duplicated;
        }

        return subAsset;
    }

    private static bool IsReadyFontAsset(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null
            || fontAsset.material == null
            || fontAsset.atlasTextures == null
            || fontAsset.atlasTextures.Length == 0
            || fontAsset.atlasTextures[0] == null)
        {
            return false;
        }

        if (fontAsset.characterTable == null || fontAsset.characterTable.Count < 64)
        {
            return false;
        }

        for (int i = 0; i < RequiredValidationCharacters.Length; i++)
        {
            if (!fontAsset.HasCharacter(RequiredValidationCharacters[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static void SetClearDynamicDataOnBuild(TMP_FontAsset fontAsset, bool value)
    {
        if (fontAsset == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(fontAsset);
        SerializedProperty property = serializedObject.FindProperty("m_ClearDynamicDataOnBuild");
        if (property == null)
        {
            return;
        }

        if (property.boolValue == value)
        {
            return;
        }

        property.boolValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(fontAsset);
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
