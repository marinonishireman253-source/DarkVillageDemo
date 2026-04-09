using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public sealed class SagiriSpriteSheetImporter : AssetPostprocessor
{
    private const string TargetAssetPath = "Assets/Art/Characters/SagiriSpriteSheet.png";
    private const string AutoSliceGuardKey = "SagiriSpriteSheetImporter.AutoSliceInProgress";
    private const string RuntimeSpriteFolder = "Assets/Resources/Characters/SagiriRuntime/";
    private const string RuntimeVfxFolder = "Assets/Resources/Vfx/";
    private const int PixelsPerUnit = 64;
    private const int MinimumSpriteSize = 16;
    private const int ExtrudeSize = 0;

    private void OnPreprocessTexture()
    {
        bool isTargetSheet = assetPath.Equals(TargetAssetPath, StringComparison.Ordinal);
        bool isRuntimeSprite = assetPath.StartsWith(RuntimeSpriteFolder, StringComparison.Ordinal);
        bool isRuntimeVfx = assetPath.StartsWith(RuntimeVfxFolder, StringComparison.Ordinal);

        if (!isTargetSheet && !isRuntimeSprite && !isRuntimeVfx)
        {
            return;
        }

        if (assetImporter is not TextureImporter importer)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = isTargetSheet
            ? SpriteImportMode.Multiple
            : SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = true;

        if (isRuntimeSprite)
        {
            importer.spritePivot = new Vector2(0.5f, 0f);
        }

        ApplySpriteTextureSettings(importer, isRuntimeSprite);
    }

    private void OnPostprocessTexture(Texture2D texture)
    {
        if (!assetPath.Equals(TargetAssetPath, StringComparison.Ordinal) || texture == null)
        {
            return;
        }

        if (SessionState.GetBool(AutoSliceGuardKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoSliceGuardKey, true);
        EditorApplication.delayCall += () =>
        {
            try
            {
                ConfigureAndSliceSagiriSheet();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                SessionState.EraseBool(AutoSliceGuardKey);
            }
        };
    }

    [MenuItem("Tools/Sprites/Configure And Slice Sagiri Sheet")]
    private static void ConfigureAndSliceSagiriSheet()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TargetAssetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Could not find sprite sheet at '{TargetAssetPath}'.");
            return;
        }

        bool changed = false;
        changed |= importer.textureType != TextureImporterType.Sprite;
        importer.textureType = TextureImporterType.Sprite;

        changed |= importer.spriteImportMode != SpriteImportMode.Multiple;
        importer.spriteImportMode = SpriteImportMode.Multiple;

        changed |= Math.Abs(importer.spritePixelsPerUnit - PixelsPerUnit) > 0.01f;
        importer.spritePixelsPerUnit = PixelsPerUnit;

        changed |= importer.filterMode != FilterMode.Point;
        importer.filterMode = FilterMode.Point;

        changed |= importer.textureCompression != TextureImporterCompression.Uncompressed;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        changed |= !importer.alphaIsTransparency;
        importer.alphaIsTransparency = true;

        changed |= importer.mipmapEnabled;
        importer.mipmapEnabled = false;

        changed |= !importer.isReadable;
        importer.isReadable = true;

        ApplySpriteTextureSettings(importer, false);

        if (changed)
        {
            importer.SaveAndReimport();
            importer = AssetImporter.GetAtPath(TargetAssetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Texture importer vanished for '{TargetAssetPath}'.");
                return;
            }
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TargetAssetPath);
        if (texture == null)
        {
            Debug.LogError($"Could not load texture at '{TargetAssetPath}'.");
            return;
        }

        Rect[] rects = GenerateAutomaticRects(texture);
        if (rects == null || rects.Length == 0)
        {
            Debug.LogError("Automatic sprite slicing returned no rectangles.");
            return;
        }

        SpriteMetaData[] sprites = BuildSpriteMetaData(rects);
#pragma warning disable 618
        importer.spritesheet = sprites;
#pragma warning restore 618
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.isReadable = true;
        importer.SaveAndReimport();

        EditorGUIUtility.PingObject(texture);
        Debug.Log($"Configured and sliced '{TargetAssetPath}' into {sprites.Length} sprites.");
    }

    [MenuItem("Tools/Sprites/Configure And Slice Sagiri Sheet", true)]
    private static bool ValidateConfigureAndSliceSagiriSheet()
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(TargetAssetPath) != null;
    }

    private static Rect[] GenerateAutomaticRects(Texture2D texture)
    {
        Type utilityType = Type.GetType("UnityEditorInternal.InternalSpriteUtility, UnityEditor");
        if (utilityType == null)
        {
            throw new InvalidOperationException("Could not resolve UnityEditorInternal.InternalSpriteUtility.");
        }

        MethodInfo method = utilityType
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(candidate =>
                candidate.Name == "GenerateAutomaticSpriteRectangles" &&
                !candidate.Name.EndsWith("_Injected", StringComparison.Ordinal));

        if (method == null)
        {
            throw new MissingMethodException("Could not find InternalSpriteUtility.GenerateAutomaticSpriteRectangles.");
        }

        ParameterInfo[] parameters = method.GetParameters();
        object[] args;
        if (parameters.Length == 3)
        {
            args = new object[] { texture, MinimumSpriteSize, ExtrudeSize };
        }
        else if (parameters.Length == 2)
        {
            args = new object[] { texture, MinimumSpriteSize };
        }
        else
        {
            throw new NotSupportedException($"Unexpected GenerateAutomaticSpriteRectangles signature with {parameters.Length} parameters.");
        }

        object result = method.Invoke(null, args);
        return result as Rect[];
    }

    private static void ApplySpriteTextureSettings(TextureImporter importer, bool alignToBottomCenter)
    {
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMode = importer.spriteImportMode == SpriteImportMode.Multiple ? (int)SpriteImportMode.Multiple : (int)SpriteImportMode.Single;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = alignToBottomCenter ? (int)SpriteAlignment.BottomCenter : (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);
    }

    private static SpriteMetaData[] BuildSpriteMetaData(IReadOnlyList<Rect> rects)
    {
        List<Rect> ordered = OrderRects(rects);
        SpriteMetaData[] sprites = new SpriteMetaData[ordered.Count];
        for (int i = 0; i < ordered.Count; i++)
        {
            sprites[i] = new SpriteMetaData
            {
                name = $"Sagiri_{i:D2}",
                rect = ordered[i],
                alignment = (int)SpriteAlignment.BottomCenter,
                pivot = new Vector2(0.5f, 0f)
            };
        }

        return sprites;
    }

    private static List<Rect> OrderRects(IReadOnlyList<Rect> rects)
    {
        List<Rect> sortedByTop = rects
            .OrderByDescending(rect => rect.yMax)
            .ThenBy(rect => rect.xMin)
            .ToList();

        if (sortedByTop.Count <= 1)
        {
            return sortedByTop;
        }

        float averageHeight = sortedByTop.Average(rect => rect.height);
        float rowThreshold = Mathf.Max(averageHeight * 0.4f, 8f);

        List<List<Rect>> rows = new List<List<Rect>>();
        foreach (Rect rect in sortedByTop)
        {
            List<Rect> row = rows.FirstOrDefault(existing => Mathf.Abs(existing[0].center.y - rect.center.y) <= rowThreshold);
            if (row == null)
            {
                row = new List<Rect>();
                rows.Add(row);
            }

            row.Add(rect);
        }

        return rows
            .OrderByDescending(row => row.Average(rect => rect.center.y))
            .SelectMany(row => row.OrderBy(rect => rect.xMin))
            .ToList();
    }
}
