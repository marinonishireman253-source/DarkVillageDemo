using System.Collections.Generic;
using UnityEngine;

public static class RuntimeImportedMaterialLibrary
{
    private static readonly Dictionary<string, Material> Cache = new();

    public static void ApplyTheme(string themeKey, GameObject instance)
    {
        if (instance == null || string.IsNullOrWhiteSpace(themeKey))
        {
            return;
        }

        Material material = GetMaterialByKey(themeKey);
        if (material == null)
        {
            return;
        }

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            Material[] replacement = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
            for (int i = 0; i < replacement.Length; i++)
            {
                replacement[i] = material;
            }

            renderer.sharedMaterials = replacement;
        }
    }

    public static void Apply(string resourcePath, GameObject instance)
    {
        if (instance == null || string.IsNullOrWhiteSpace(resourcePath))
        {
            return;
        }

        Material fallbackMaterial = GetMaterialByKey(ResolveMaterialKey(resourcePath));
        if (fallbackMaterial == null)
        {
            return;
        }

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            if (!NeedsFallbackMaterial(renderer))
            {
                continue;
            }

            Material[] replacement = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];
            for (int i = 0; i < replacement.Length; i++)
            {
                replacement[i] = fallbackMaterial;
            }

            renderer.sharedMaterials = replacement;
        }
    }

    private static bool NeedsFallbackMaterial(Renderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        Material[] materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            if (HasTexture(materials[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasTexture(Material material)
    {
        if (material == null)
        {
            return false;
        }

        if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
        {
            return true;
        }

        if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
        {
            return true;
        }

        return material.mainTexture != null;
    }

    private static Material GetMaterialByKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (Cache.TryGetValue(key, out Material cached) && cached != null)
        {
            return cached;
        }

        Material material = CreateMaterial(key);
        if (material != null)
        {
            Cache[key] = material;
        }

        return material;
    }

    private static string ResolveMaterialKey(string resourcePath)
    {
        string path = resourcePath.ToLowerInvariant();

        if (path.Contains("/kenney/graveyardkit/"))
        {
            return "kenney-graveyard";
        }

        if (path.Contains("/kenney/modulardungeon/"))
        {
            return "kenney-dungeon";
        }

        if (path.Contains("/quaternius/fantasyprops/"))
        {
            if (path.Contains("banner") || path.Contains("cloth") || path.Contains("tarp") || path.Contains("bed"))
            {
                return "props-cloth";
            }

            if (path.Contains("lantern") || path.Contains("weapon") || path.Contains("anvil") || path.Contains("metal"))
            {
                return "props-metal";
            }

            return "props-furniture";
        }

        if (path.Contains("/quaternius/medievalvillage/"))
        {
            if (path.Contains("roof"))
            {
                return "village-roof";
            }

            if (path.Contains("unevenbrick") || path.Contains("brick"))
            {
                return "village-brick";
            }

            if (path.Contains("wood") || path.Contains("door") || path.Contains("stairs") || path.Contains("fence") || path.Contains("support") || path.Contains("balcony"))
            {
                return "village-wood";
            }

            if (path.Contains("metalfence"))
            {
                return "village-metal";
            }

            return "village-plaster";
        }

        return null;
    }

    private static Material CreateMaterial(string key)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader)
        {
            name = $"RuntimeImported_{key}"
        };

        switch (key)
        {
            case "village-plaster":
                SetBaseTexture(material, "Imported/Quaternius/MedievalVillage/Textures/T_Plaster_BaseColor", new Color(0.72f, 0.68f, 0.62f), 0.18f);
                break;
            case "village-wood":
                SetBaseTexture(material, "Imported/Quaternius/MedievalVillage/Textures/T_WoodTrim_BaseColor", new Color(0.42f, 0.31f, 0.2f), 0.12f);
                break;
            case "village-brick":
                SetBaseTexture(material, "Imported/Quaternius/MedievalVillage/Textures/T_UnevenBrick_BaseColor", new Color(0.42f, 0.32f, 0.28f), 0.08f);
                break;
            case "village-roof":
                SetBaseTexture(material, "Imported/Quaternius/MedievalVillage/Textures/T_RoundTiles_BaseColor", new Color(0.34f, 0.18f, 0.14f), 0.05f);
                break;
            case "village-metal":
                SetBaseTexture(material, "Imported/Quaternius/MedievalVillage/Textures/T_RockTrim_BaseColor", new Color(0.35f, 0.34f, 0.34f), 0.2f);
                break;
            case "props-furniture":
                SetBaseTexture(material, "Imported/Quaternius/FantasyProps/Textures/T_Trim_Furniture_BaseColor", new Color(0.45f, 0.33f, 0.25f), 0.12f);
                break;
            case "props-cloth":
                SetBaseTexture(material, "Imported/Quaternius/FantasyProps/Textures/T_Trim_Cloth_BaseColor", new Color(0.42f, 0.14f, 0.13f), 0.08f);
                break;
            case "props-metal":
                SetBaseTexture(material, "Imported/Quaternius/FantasyProps/Textures/T_Trim_Metal_BaseColor", new Color(0.46f, 0.42f, 0.38f), 0.25f);
                break;
            case "kenney-graveyard":
                SetBaseTexture(material, "Imported/Kenney/GraveyardKit/Textures/colormap", new Color(0.58f, 0.55f, 0.5f), 0.06f);
                break;
            case "kenney-dungeon":
                SetBaseTexture(material, "Imported/Kenney/ModularDungeon/FBX/Textures/colormap", new Color(0.5f, 0.47f, 0.44f), 0.05f);
                break;
            default:
                return null;
        }

        return material;
    }

    private static void SetBaseTexture(Material material, string texturePath, Color fallbackColor, float smoothness)
    {
        Texture2D texture = Resources.Load<Texture2D>(texturePath);

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }
        else if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", fallbackColor);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", fallbackColor);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }
    }
}
