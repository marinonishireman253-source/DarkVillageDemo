using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UiFactory
{
    private static Texture2D s_WhiteTexture;
    private static Sprite s_WhiteSprite;
    private static Font s_BuiltinFont;
    private static readonly Dictionary<int, TMP_FontAsset> s_TmpFontAssets = new Dictionary<int, TMP_FontAsset>();
    private static readonly string[] s_PreferredSystemFonts =
    {
        "Source Han Serif SC",
        "Source Han Serif CN",
        "Noto Serif CJK SC",
        "Songti SC",
        "STSong",
        "SimSun",
        "Songti TC",
        "PMingLiU",
        "EB Garamond",
        "Libre Baskerville",
        "Baskerville",
        "Times New Roman",
        "Georgia",
        "Palatino"
    };

    public static Canvas CreateLayerCanvas(Transform parent, string name, int sortingOrder)
    {
        GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);

        RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
        Stretch(rectTransform);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    public static RectTransform CreateRect(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta,
        Vector2 anchoredPosition)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rectTransform = go.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = pivot;
        rectTransform.sizeDelta = sizeDelta;
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.localScale = Vector3.one;
        return rectTransform;
    }

    public static Image CreateImage(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta,
        Vector2 anchoredPosition,
        Color color)
    {
        RectTransform rectTransform = CreateRect(name, parent, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.sprite = GetWhiteSprite();
        image.type = Image.Type.Sliced;
        image.color = color;
        return image;
    }

    public static void ApplySprite(Image image, Sprite sprite, Image.Type imageType = Image.Type.Sliced, bool preserveAspect = false)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite != null ? sprite : GetWhiteSprite();
        image.type = imageType;
        image.preserveAspect = preserveAspect;
    }

    public static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta,
        Vector2 anchoredPosition,
        Font font,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor alignment,
        Color color,
        bool richText = false)
    {
        RectTransform rectTransform = CreateRect(name, parent, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
        TextMeshProUGUI text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = GetOrCreateTmpFontAsset(font != null ? font : GetBuiltinFont());
        text.fontSize = fontSize;
        text.fontStyle = MapFontStyle(fontStyle);
        text.alignment = MapAlignment(alignment);
        text.color = color;
        text.richText = richText;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.extraPadding = true;
        text.isTextObjectScaleStatic = true;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
        ApplyTextMaterial(text, fontSize >= 20 || fontStyle == FontStyle.Bold || fontStyle == FontStyle.BoldAndItalic);
        return text;
    }

    public static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = target.AddComponent<CanvasGroup>();
        }

        return group;
    }

    public static Font GetBuiltinFont()
    {
        if (s_BuiltinFont != null)
        {
            return s_BuiltinFont;
        }

        s_BuiltinFont = TryCreateDynamicOsFont(s_PreferredSystemFonts);
        if (s_BuiltinFont == null)
        {
            s_BuiltinFont = TryLoadBuiltinFont("LegacyRuntime.ttf");
        }

        if (s_BuiltinFont == null)
        {
            s_BuiltinFont = Font.CreateDynamicFontFromOSFont("Arial", 16);
        }

        return s_BuiltinFont;
    }

    public static TMP_FontAsset GetOrCreateDefaultTmpFont(Font font = null)
    {
        return GetOrCreateTmpFontAsset(font != null ? font : GetBuiltinFont());
    }

    public static void RefreshTextMaterial(TMP_Text text, bool emphasize)
    {
        ApplyTextMaterial(text, emphasize);
    }

    public static void Stretch(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static Sprite GetWhiteSprite()
    {
        if (s_WhiteSprite != null)
        {
            return s_WhiteSprite;
        }

        if (s_WhiteTexture == null)
        {
            s_WhiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            s_WhiteTexture.SetPixel(0, 0, Color.white);
            s_WhiteTexture.Apply(false, true);
        }

        s_WhiteSprite = Sprite.Create(s_WhiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
        s_WhiteSprite.hideFlags = HideFlags.HideAndDontSave;
        return s_WhiteSprite;
    }

    private static Font TryLoadBuiltinFont(string resourceName)
    {
        try
        {
            return Resources.GetBuiltinResource<Font>(resourceName);
        }
        catch (System.ArgumentException)
        {
            return null;
        }
    }

    private static Font TryCreateDynamicOsFont(string[] fontNames)
    {
        try
        {
            return Font.CreateDynamicFontFromOSFont(fontNames, 16);
        }
        catch (System.Exception)
        {
            return null;
        }
    }

    private static TMP_FontAsset GetOrCreateTmpFontAsset(Font font)
    {
        Font sourceFont = font != null ? font : GetBuiltinFont();
        int key = sourceFont != null ? sourceFont.GetInstanceID() : 0;
        if (key != 0 && s_TmpFontAssets.TryGetValue(key, out TMP_FontAsset cachedAsset) && cachedAsset != null)
        {
            return cachedAsset;
        }

        TMP_FontAsset fontAsset = null;
        if (sourceFont != null)
        {
            try
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 8, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            }
            catch (System.Exception)
            {
                fontAsset = null;
            }
        }

        if (fontAsset == null)
        {
            fontAsset = TMP_Settings.defaultFontAsset;
        }

        if (fontAsset != null)
        {
            fontAsset.hideFlags = HideFlags.HideAndDontSave;
            if (key != 0)
            {
                s_TmpFontAssets[key] = fontAsset;
            }
        }

        return fontAsset;
    }

    private static TextAlignmentOptions MapAlignment(TextAnchor alignment)
    {
        switch (alignment)
        {
            case TextAnchor.UpperLeft:
                return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter:
                return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight:
                return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft:
                return TextAlignmentOptions.MidlineLeft;
            case TextAnchor.MiddleCenter:
                return TextAlignmentOptions.Midline;
            case TextAnchor.MiddleRight:
                return TextAlignmentOptions.MidlineRight;
            case TextAnchor.LowerLeft:
                return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter:
                return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight:
                return TextAlignmentOptions.BottomRight;
            default:
                return TextAlignmentOptions.TopLeft;
        }
    }

    private static FontStyles MapFontStyle(FontStyle fontStyle)
    {
        switch (fontStyle)
        {
            case FontStyle.Bold:
                return FontStyles.Bold;
            case FontStyle.Italic:
                return FontStyles.Italic;
            case FontStyle.BoldAndItalic:
                return FontStyles.Bold | FontStyles.Italic;
            default:
                return FontStyles.Normal;
        }
    }

    private static void ApplyTextMaterial(TMP_Text text, bool emphasize)
    {
        if (text == null || text.fontSharedMaterial == null)
        {
            return;
        }

        Material runtimeMaterial = new Material(text.fontSharedMaterial)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        runtimeMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
        runtimeMaterial.DisableKeyword(ShaderUtilities.Keyword_Underlay);

        if (runtimeMaterial.HasProperty(ShaderUtilities.ID_FaceDilate))
        {
            runtimeMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, emphasize ? 0.045f : 0.018f);
        }

        if (runtimeMaterial.HasProperty(ShaderUtilities.ID_OutlineWidth))
        {
            runtimeMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, emphasize ? 0.06f : 0.03f);
        }

        if (runtimeMaterial.HasProperty(ShaderUtilities.ID_OutlineColor))
        {
            runtimeMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.03f, 0.04f, 0.05f, emphasize ? 0.72f : 0.42f));
        }

        if (runtimeMaterial.HasProperty(ShaderUtilities.ID_UnderlayColor))
        {
            runtimeMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0f));
        }

        if (runtimeMaterial.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
        {
            runtimeMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        }

        if (runtimeMaterial.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
        {
            runtimeMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
        }

        if (runtimeMaterial.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
        {
            runtimeMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
        }

        text.fontSharedMaterial = runtimeMaterial;
    }
}
