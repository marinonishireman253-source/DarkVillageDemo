using System;
using UnityEditor;
using UnityEngine;

public sealed class UiPortraitTextureImporter : AssetPostprocessor
{
    private const string PortraitFolder = "Assets/Resources/Characters/Sagiri/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(PortraitFolder, StringComparison.Ordinal))
        {
            return;
        }

        if (assetImporter is not TextureImporter importer)
        {
            return;
        }

        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.streamingMipmaps = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.isReadable = false;
    }
}
