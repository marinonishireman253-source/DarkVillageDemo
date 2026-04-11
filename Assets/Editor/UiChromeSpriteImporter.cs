using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class UiChromeSpriteImporter : AssetPostprocessor
{
    private const string UiSliceFolder = "Assets/Resources/UI/Slices/";
    private const int DefaultBorder = 24;
    private const float PixelsPerUnit = 100f;

    private static readonly Dictionary<string, int> BorderPresets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        { "ModalFrame", 80 },
        { "DialogueFrame", 64 },
        { "ChoicePanel", 40 },
        { "PortraitFrame", 48 },
        { "CombatPanel", 56 },
        { "CombatEmberGlow", 48 },
        { "InteractionPrompt", 40 },
        { "KeycapBadge", 18 },
        { "MarkerChip", 20 },
        { "PrimaryButton", 24 }
    };

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(UiSliceFolder, StringComparison.Ordinal))
        {
            return;
        }

        if (assetImporter is not TextureImporter importer)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(settings);

        int border = ResolveBorder(Path.GetFileNameWithoutExtension(assetPath));
        importer.spriteBorder = new Vector4(border, border, border, border);
    }

    private static int ResolveBorder(string assetName)
    {
        int taggedBorder = TryParseTaggedBorder(assetName);
        if (taggedBorder > 0)
        {
            return taggedBorder;
        }

        foreach (KeyValuePair<string, int> preset in BorderPresets)
        {
            if (assetName.StartsWith(preset.Key, StringComparison.OrdinalIgnoreCase))
            {
                return preset.Value;
            }
        }

        return DefaultBorder;
    }

    private static int TryParseTaggedBorder(string assetName)
    {
        int markerIndex = assetName.LastIndexOf("_b", StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0 || markerIndex + 2 >= assetName.Length)
        {
            return -1;
        }

        string borderToken = assetName.Substring(markerIndex + 2);
        return int.TryParse(borderToken, out int border) && border > 0 ? border : -1;
    }
}
