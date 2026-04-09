using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class PixelFacadeFactory
{
    public enum FacadeStyle
    {
        GuardOffice,
        Tavern,
        ShutteredInn,
        Apothecary,
        RowResidence,
        RowShop,
        RowQuarantine,
        CollapsedResidence,
        Chapel
    }

    private static readonly Dictionary<string, Material> MaterialCache = new();

    public static GameObject CreateHouse(
        string name,
        Transform parent,
        Vector3 center,
        Vector3 streetForward,
        Vector3 streetRight,
        bool westSide,
        float frontSpan,
        float thickness,
        float height,
        FacadeStyle style,
        float scale = 1f)
    {
        if (parent == null)
        {
            return null;
        }

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, true);
        root.transform.position = center;

        Texture2D externalFacade = LoadFacadeTexture(style);
        if (externalFacade != null)
        {
            CreateSpriteFacadeHouse(root.transform, center, streetForward, streetRight, westSide, frontSpan, thickness, height, style, scale, externalFacade);
            return root;
        }

        CreateGeneratedFacadeHouse(root.transform, center, streetForward, streetRight, westSide, frontSpan, thickness, height, style, scale);
        return root;
    }

    private static void CreateGeneratedFacadeHouse(
        Transform parent,
        Vector3 center,
        Vector3 streetForward,
        Vector3 streetRight,
        bool westSide,
        float frontSpan,
        float thickness,
        float height,
        FacadeStyle style,
        float scale)
    {
        Vector3 streetFacing = westSide ? streetRight : -streetRight;
        Vector3 lateral = streetForward.normalized;
        Vector3 outward = streetFacing.normalized;

        float worldWidth = frontSpan * scale;
        float worldDepth = thickness * scale;
        float worldHeight = height * scale;

        CreatePanel(
            parent,
            "Front",
            center + outward * (worldDepth * 0.5f + 0.06f) + Vector3.up * (worldHeight * 0.5f),
            Quaternion.LookRotation(outward, Vector3.up),
            worldWidth,
            worldHeight,
            GetMaterial($"{style}_front", BuildFrontTexture(style)));

        float sideWidth = Mathf.Max(2f, worldDepth * 0.48f);
        float sideHeight = Mathf.Max(2.8f, worldHeight * 0.88f);
        Material sideMaterial = GetMaterial($"{style}_side", BuildSideTexture(style));

        CreatePanel(
            parent,
            "NorthSide",
            center + lateral * (worldWidth * 0.5f - 0.04f) + Vector3.up * (sideHeight * 0.5f),
            Quaternion.LookRotation(-lateral, Vector3.up),
            sideWidth,
            sideHeight,
            sideMaterial);

        CreatePanel(
            parent,
            "SouthSide",
            center - lateral * (worldWidth * 0.5f - 0.04f) + Vector3.up * (sideHeight * 0.5f),
            Quaternion.LookRotation(lateral, Vector3.up),
            sideWidth,
            sideHeight,
            sideMaterial);

        CreatePanel(
            parent,
            "RoofCap",
            center + outward * (worldDepth * 0.16f) + Vector3.up * (worldHeight + 0.6f * scale),
            Quaternion.LookRotation(outward, Vector3.up),
            worldWidth * 0.92f,
            Mathf.Max(0.75f, worldHeight * 0.16f),
            GetMaterial($"{style}_roofcap", BuildRoofCapTexture(style)));
    }

    private static void CreateSpriteFacadeHouse(
        Transform parent,
        Vector3 center,
        Vector3 streetForward,
        Vector3 streetRight,
        bool westSide,
        float frontSpan,
        float thickness,
        float height,
        FacadeStyle style,
        float scale,
        Texture2D facadeTexture)
    {
        Vector3 streetFacing = westSide ? streetRight : -streetRight;
        Vector3 lateral = streetForward.normalized;
        Vector3 outward = streetFacing.normalized;
        Vector3 facadeFacing = Camera.main != null
            ? -Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized
            : outward;

        if (facadeFacing.sqrMagnitude <= 0.001f)
        {
            facadeFacing = outward;
        }

        float worldWidth = frontSpan * scale;
        float worldDepth = thickness * scale;
        float worldHeight = height * scale;
        float textureAspect = facadeTexture.height > 0 ? facadeTexture.width / (float)facadeTexture.height : 1f;
        float facadeHeight = Mathf.Max(worldHeight * 1.7f, 6.6f * scale);
        float facadeWidth = Mathf.Clamp(facadeHeight * textureAspect, worldWidth * 1.15f, worldWidth * 2.45f);
        Color silhouetteColor = GetSilhouetteColor(style);

        CreateColorPanel(
            parent,
            "FacadeShadow",
            center + outward * (worldDepth * 0.26f) + Vector3.up * (facadeHeight * 0.5f + 0.06f),
            Quaternion.LookRotation(facadeFacing, Vector3.up),
            facadeWidth * 1.02f,
            facadeHeight * 1.02f,
            silhouetteColor);

        CreatePanel(
            parent,
            "Facade",
            center + outward * (worldDepth * 0.5f + 0.18f) + Vector3.up * (facadeHeight * 0.5f),
            Quaternion.LookRotation(facadeFacing, Vector3.up),
            facadeWidth,
            facadeHeight,
            GetMaterial($"{style}_external", facadeTexture));

        CreateColorPanel(
            parent,
            "RoofShadow",
            center + outward * (worldDepth * 0.18f) + Vector3.up * (facadeHeight * 0.92f),
            Quaternion.LookRotation(facadeFacing, Vector3.up),
            facadeWidth * 0.94f,
            Mathf.Max(0.45f, facadeHeight * 0.12f),
            Darken(silhouetteColor, 0.08f));

        CreateColorPanel(
            parent,
            "NorthSlice",
            center + lateral * (facadeWidth * 0.42f) + outward * (worldDepth * 0.12f) + Vector3.up * (facadeHeight * 0.42f),
            Quaternion.LookRotation(-lateral, Vector3.up),
            Mathf.Max(0.4f, worldDepth * 0.2f),
            facadeHeight * 0.82f,
            Darken(silhouetteColor, 0.1f));

        CreateColorPanel(
            parent,
            "SouthSlice",
            center - lateral * (facadeWidth * 0.42f) + outward * (worldDepth * 0.12f) + Vector3.up * (facadeHeight * 0.42f),
            Quaternion.LookRotation(lateral, Vector3.up),
            Mathf.Max(0.4f, worldDepth * 0.2f),
            facadeHeight * 0.82f,
            Darken(silhouetteColor, 0.1f));
    }

    private static void CreateColorPanel(
        Transform parent,
        string name,
        Vector3 position,
        Quaternion rotation,
        float width,
        float height,
        Color color)
    {
        CreatePanel(parent, name, position, rotation, width, height, GetSolidMaterial($"solid_{ColorUtility.ToHtmlStringRGBA(color)}", color));
    }

    private static void CreatePanel(
        Transform parent,
        string name,
        Vector3 position,
        Quaternion rotation,
        float width,
        float height,
        Material material)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;
        quad.transform.SetParent(parent, true);
        quad.transform.position = position;
        quad.transform.rotation = rotation;
        quad.transform.localScale = new Vector3(width, height, 1f);

        Collider collider = quad.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(collider);
            }
            else
            {
                Object.DestroyImmediate(collider);
            }
        }

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private static Material GetMaterial(string key, Texture2D texture)
    {
        if (MaterialCache.TryGetValue(key, out Material cached) && cached != null)
        {
            return cached;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Texture");
        }

        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader)
        {
            name = $"PixelFacade_{key}"
        };

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }
        else if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }
        else
        {
            material.mainTexture = texture;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", Color.white);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", 0f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", 0f);
        }

        if (material.HasProperty("_Cutoff"))
        {
            material.SetFloat("_Cutoff", 0.1f);
        }

        material.enableInstancing = true;
        MaterialCache[key] = material;
        return material;
    }

    private static Material GetSolidMaterial(string key, Color color)
    {
        if (MaterialCache.TryGetValue(key, out Material cached) && cached != null)
        {
            return cached;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader)
        {
            name = $"PixelFacade_{key}"
        };

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", 0f);
        }

        material.enableInstancing = true;
        MaterialCache[key] = material;
        return material;
    }

    private static Texture2D LoadFacadeTexture(FacadeStyle style)
    {
        string resourceName = style switch
        {
            FacadeStyle.GuardOffice => "guard_office",
            FacadeStyle.Tavern => "tavern",
            FacadeStyle.ShutteredInn => "shuttered_inn",
            FacadeStyle.Apothecary => "apothecary",
            FacadeStyle.RowResidence => "row_residence",
            FacadeStyle.RowShop => "row_shop",
            FacadeStyle.RowQuarantine => "row_quarantine",
            FacadeStyle.CollapsedResidence => "collapsed_residence",
            FacadeStyle.Chapel => "chapel",
            _ => null
        };

        return string.IsNullOrWhiteSpace(resourceName)
            ? null
            : Resources.Load<Texture2D>($"Environment2D/Buildings/{resourceName}");
    }

    private static Color GetSilhouetteColor(FacadeStyle style)
    {
        Palette palette = Palette.For(style);
        return Darken(palette.SideWall, 0.03f);
    }

    private static Texture2D BuildFrontTexture(FacadeStyle style)
    {
        int width = style == FacadeStyle.Chapel ? 88 : 72;
        int height = style == FacadeStyle.Chapel ? 116 : 96;
        Texture2D texture = CreateTexture(width, height);
        Palette palette = Palette.For(style);

        DrawRect(texture, 0, 0, width, height, Clear);

        int roofHeight = style == FacadeStyle.CollapsedResidence ? 14 : style == FacadeStyle.Chapel ? 22 : 18;
        int bodyHeight = height - roofHeight - 4;

        DrawRect(texture, 6, 0, width - 12, bodyHeight, palette.Wall);
        DrawRect(texture, 6, 0, width - 12, 5, palette.Shadow);
        DrawRect(texture, 6, bodyHeight - 3, width - 12, 3, palette.Trim);
        DrawRect(texture, 4, bodyHeight, width - 8, roofHeight, palette.Roof);
        DrawRect(texture, 2, bodyHeight + roofHeight - 5, width - 4, 5, palette.RoofShadow);

        for (int x = 10; x < width - 10; x++)
        {
            int lift = Mathf.Max(0, 10 - Mathf.Abs(x - width / 2) / 2);
            DrawRect(texture, x, bodyHeight + roofHeight - 6 + lift, 1, 1, palette.RoofShadow);
        }

        if (style == FacadeStyle.Chapel)
        {
            DrawRect(texture, width / 2 - 8, 8, 16, 28, palette.Door);
            DrawRect(texture, width / 2 - 3, 10, 6, 24, palette.Trim);
            DrawRect(texture, width / 2 - 7, 52, 14, 26, palette.Accent);
            DrawRect(texture, width / 2 - 5, 54, 10, 22, palette.WindowGlow);
            DrawRect(texture, width / 2 - 26, 30, 10, 28, palette.Seal);
            DrawRect(texture, width / 2 + 16, 30, 10, 28, palette.Seal);
        }
        else
        {
            DrawRect(texture, width / 2 - 7, 6, 14, 26, palette.Door);
            DrawRect(texture, width / 2 - 5, 8, 10, 22, palette.DoorInset);

            DrawWindow(texture, 14, 24, 16, 18, palette);
            DrawWindow(texture, width - 30, 24, 16, 18, palette);

            if (style != FacadeStyle.CollapsedResidence)
            {
                DrawWindow(texture, 18, 52, 14, 16, palette);
                DrawWindow(texture, width - 32, 52, 14, 16, palette);
            }
        }

        switch (style)
        {
            case FacadeStyle.GuardOffice:
                DrawRect(texture, width / 2 - 24, 60, 10, 18, palette.Accent);
                DrawRect(texture, width / 2 + 14, 20, 12, 16, palette.Trim);
                break;
            case FacadeStyle.Tavern:
                DrawRect(texture, 10, 36, width - 20, 6, palette.Accent);
                DrawRect(texture, width / 2 + 22, 54, 10, 18, palette.Sign);
                DrawRect(texture, 12, 22, 18, 4, palette.Trim);
                DrawRect(texture, width - 30, 22, 18, 4, palette.Trim);
                break;
            case FacadeStyle.ShutteredInn:
                DrawBoard(texture, 12, 26, 20, 4, palette.Trim);
                DrawBoard(texture, width - 32, 32, 18, 4, palette.Trim);
                DrawBoard(texture, width / 2 - 12, 16, 24, 4, palette.Trim);
                DrawRect(texture, width / 2 + 22, 52, 10, 18, palette.Sign);
                break;
            case FacadeStyle.Apothecary:
                DrawRect(texture, 12, 36, width - 24, 5, palette.Accent);
                DrawRect(texture, width / 2 + 24, 50, 8, 18, palette.Accent);
                DrawRect(texture, width / 2 + 22, 24, 12, 10, palette.WindowGlow);
                break;
            case FacadeStyle.RowShop:
                DrawRect(texture, 10, 34, width - 20, 5, palette.Accent);
                DrawRect(texture, width / 2 + 18, 48, 8, 14, palette.Sign);
                break;
            case FacadeStyle.RowQuarantine:
                DrawBoard(texture, 12, 28, 18, 4, palette.Trim);
                DrawBoard(texture, width - 30, 24, 18, 4, palette.Trim);
                DrawRect(texture, width / 2 - 4, 52, 8, 18, palette.Seal);
                break;
            case FacadeStyle.CollapsedResidence:
                DrawRect(texture, width / 2 + 10, bodyHeight + 3, 18, roofHeight - 3, Clear);
                DrawRect(texture, width / 2 + 2, 48, 22, 12, Clear);
                DrawBoard(texture, width / 2 - 12, 18, 24, 4, palette.Trim);
                DrawRect(texture, width - 26, 8, 18, 10, palette.Shadow);
                break;
        }

        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D BuildSideTexture(FacadeStyle style)
    {
        int width = 28;
        int height = style == FacadeStyle.Chapel ? 108 : 92;
        Texture2D texture = CreateTexture(width, height);
        Palette palette = Palette.For(style);

        DrawRect(texture, 0, 0, width, height, Clear);
        DrawRect(texture, 4, 0, width - 8, height - 16, palette.SideWall);
        DrawRect(texture, 4, 0, width - 8, 5, palette.Shadow);
        DrawRect(texture, 2, height - 16, width - 4, 16, palette.Roof);
        DrawRect(texture, 8, 30, width - 16, 16, palette.WindowGlow);
        DrawRect(texture, 9, 31, width - 18, 14, palette.WindowCore);

        if (style == FacadeStyle.RowQuarantine || style == FacadeStyle.ShutteredInn)
        {
            DrawBoard(texture, 6, 34, width - 12, 3, palette.Trim);
        }

        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D BuildRoofCapTexture(FacadeStyle style)
    {
        Texture2D texture = CreateTexture(72, 18);
        Palette palette = Palette.For(style);
        DrawRect(texture, 0, 0, 72, 18, palette.Roof);
        DrawRect(texture, 0, 0, 72, 4, palette.RoofShadow);
        DrawRect(texture, 0, 14, 72, 4, palette.Trim);
        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D CreateTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"PixelFacade_{width}x{height}"
        };
        return texture;
    }

    private static void DrawWindow(Texture2D texture, int x, int y, int width, int height, Palette palette)
    {
        DrawRect(texture, x, y, width, height, palette.Trim);
        DrawRect(texture, x + 2, y + 2, width - 4, height - 4, palette.WindowGlow);
        DrawRect(texture, x + 4, y + 4, width - 8, height - 8, palette.WindowCore);
    }

    private static void DrawBoard(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        DrawRect(texture, x, y, width, height, color);
        DrawRect(texture, x + 2, y + 1, Mathf.Max(2, width - 4), Mathf.Max(1, height - 2), Darken(color, 0.12f));
    }

    private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        int maxX = Mathf.Min(texture.width, x + width);
        int maxY = Mathf.Min(texture.height, y + height);
        int startX = Mathf.Max(0, x);
        int startY = Mathf.Max(0, y);

        for (int px = startX; px < maxX; px++)
        {
            for (int py = startY; py < maxY; py++)
            {
                texture.SetPixel(px, py, color);
            }
        }
    }

    private static Color Darken(Color color, float amount)
    {
        return new Color(
            Mathf.Clamp01(color.r - amount),
            Mathf.Clamp01(color.g - amount),
            Mathf.Clamp01(color.b - amount),
            color.a);
    }

    private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

    private readonly struct Palette
    {
        public readonly Color Wall;
        public readonly Color SideWall;
        public readonly Color Roof;
        public readonly Color RoofShadow;
        public readonly Color Trim;
        public readonly Color Shadow;
        public readonly Color Door;
        public readonly Color DoorInset;
        public readonly Color WindowGlow;
        public readonly Color WindowCore;
        public readonly Color Accent;
        public readonly Color Sign;
        public readonly Color Seal;

        public Palette(
            Color wall,
            Color sideWall,
            Color roof,
            Color roofShadow,
            Color trim,
            Color shadow,
            Color door,
            Color doorInset,
            Color windowGlow,
            Color windowCore,
            Color accent,
            Color sign,
            Color seal)
        {
            Wall = wall;
            SideWall = sideWall;
            Roof = roof;
            RoofShadow = roofShadow;
            Trim = trim;
            Shadow = shadow;
            Door = door;
            DoorInset = doorInset;
            WindowGlow = windowGlow;
            WindowCore = windowCore;
            Accent = accent;
            Sign = sign;
            Seal = seal;
        }

        public static Palette For(FacadeStyle style)
        {
            switch (style)
            {
                case FacadeStyle.GuardOffice:
                    return new Palette(
                        new Color32(128, 126, 117, 255),
                        new Color32(94, 91, 84, 255),
                        new Color32(79, 72, 80, 255),
                        new Color32(55, 49, 57, 255),
                        new Color32(77, 55, 38, 255),
                        new Color32(78, 71, 70, 255),
                        new Color32(67, 48, 37, 255),
                        new Color32(47, 32, 25, 255),
                        new Color32(210, 224, 234, 255),
                        new Color32(110, 148, 173, 255),
                        new Color32(142, 51, 49, 255),
                        new Color32(156, 114, 65, 255),
                        new Color32(162, 45, 40, 255));
                case FacadeStyle.Tavern:
                    return new Palette(
                        new Color32(158, 138, 114, 255),
                        new Color32(112, 93, 76, 255),
                        new Color32(118, 53, 44, 255),
                        new Color32(77, 34, 31, 255),
                        new Color32(95, 63, 37, 255),
                        new Color32(87, 76, 66, 255),
                        new Color32(77, 48, 30, 255),
                        new Color32(49, 31, 20, 255),
                        new Color32(244, 202, 120, 255),
                        new Color32(143, 90, 43, 255),
                        new Color32(153, 69, 55, 255),
                        new Color32(187, 151, 80, 255),
                        new Color32(164, 58, 46, 255));
                case FacadeStyle.ShutteredInn:
                    return new Palette(
                        new Color32(119, 116, 111, 255),
                        new Color32(85, 82, 78, 255),
                        new Color32(75, 56, 58, 255),
                        new Color32(49, 35, 38, 255),
                        new Color32(81, 58, 39, 255),
                        new Color32(74, 67, 65, 255),
                        new Color32(61, 41, 30, 255),
                        new Color32(42, 28, 22, 255),
                        new Color32(112, 122, 133, 255),
                        new Color32(70, 80, 92, 255),
                        new Color32(109, 60, 53, 255),
                        new Color32(147, 110, 60, 255),
                        new Color32(151, 48, 45, 255));
                case FacadeStyle.Apothecary:
                    return new Palette(
                        new Color32(138, 150, 126, 255),
                        new Color32(93, 104, 84, 255),
                        new Color32(82, 70, 53, 255),
                        new Color32(55, 46, 36, 255),
                        new Color32(92, 73, 45, 255),
                        new Color32(81, 84, 72, 255),
                        new Color32(71, 49, 32, 255),
                        new Color32(44, 30, 18, 255),
                        new Color32(202, 238, 169, 255),
                        new Color32(86, 134, 82, 255),
                        new Color32(88, 128, 72, 255),
                        new Color32(167, 147, 78, 255),
                        new Color32(146, 54, 48, 255));
                case FacadeStyle.RowShop:
                    return new Palette(
                        new Color32(146, 131, 111, 255),
                        new Color32(102, 89, 72, 255),
                        new Color32(104, 64, 49, 255),
                        new Color32(63, 37, 30, 255),
                        new Color32(89, 67, 44, 255),
                        new Color32(82, 72, 62, 255),
                        new Color32(68, 48, 31, 255),
                        new Color32(46, 31, 19, 255),
                        new Color32(237, 220, 176, 255),
                        new Color32(113, 122, 130, 255),
                        new Color32(161, 93, 57, 255),
                        new Color32(184, 144, 77, 255),
                        new Color32(144, 51, 44, 255));
                case FacadeStyle.RowQuarantine:
                    return new Palette(
                        new Color32(118, 112, 108, 255),
                        new Color32(84, 78, 75, 255),
                        new Color32(83, 48, 47, 255),
                        new Color32(53, 30, 31, 255),
                        new Color32(92, 66, 45, 255),
                        new Color32(69, 63, 61, 255),
                        new Color32(64, 42, 32, 255),
                        new Color32(42, 27, 20, 255),
                        new Color32(140, 148, 157, 255),
                        new Color32(80, 90, 98, 255),
                        new Color32(142, 44, 41, 255),
                        new Color32(167, 126, 71, 255),
                        new Color32(181, 55, 50, 255));
                case FacadeStyle.CollapsedResidence:
                    return new Palette(
                        new Color32(128, 121, 114, 255),
                        new Color32(90, 83, 78, 255),
                        new Color32(72, 61, 58, 255),
                        new Color32(47, 39, 38, 255),
                        new Color32(86, 64, 43, 255),
                        new Color32(70, 62, 60, 255),
                        new Color32(63, 41, 28, 255),
                        new Color32(42, 28, 18, 255),
                        new Color32(154, 160, 168, 255),
                        new Color32(90, 98, 106, 255),
                        new Color32(128, 83, 63, 255),
                        new Color32(143, 108, 68, 255),
                        new Color32(163, 60, 52, 255));
                case FacadeStyle.Chapel:
                    return new Palette(
                        new Color32(126, 123, 129, 255),
                        new Color32(88, 85, 92, 255),
                        new Color32(67, 56, 61, 255),
                        new Color32(43, 34, 38, 255),
                        new Color32(84, 68, 55, 255),
                        new Color32(72, 68, 74, 255),
                        new Color32(56, 43, 34, 255),
                        new Color32(39, 30, 24, 255),
                        new Color32(210, 105, 90, 255),
                        new Color32(107, 41, 54, 255),
                        new Color32(152, 52, 48, 255),
                        new Color32(167, 143, 88, 255),
                        new Color32(173, 52, 49, 255));
                default:
                    return new Palette(
                        new Color32(140, 129, 113, 255),
                        new Color32(98, 88, 74, 255),
                        new Color32(92, 62, 49, 255),
                        new Color32(61, 40, 31, 255),
                        new Color32(84, 64, 43, 255),
                        new Color32(79, 71, 62, 255),
                        new Color32(69, 46, 31, 255),
                        new Color32(45, 29, 18, 255),
                        new Color32(211, 224, 235, 255),
                        new Color32(110, 129, 145, 255),
                        new Color32(151, 91, 61, 255),
                        new Color32(175, 138, 78, 255),
                        new Color32(152, 55, 47, 255));
            }
        }
    }
}
