using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UiFactory
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;
    private const float MinimumUiScale = 0.75f;
    private const float UiScaleStep = 0.25f;
    private const int MinimumAcceptedSamplingPointSize = 224;
    private const int MinimumAcceptedAtlasSize = 8192;
    private const string DefaultBodyFontResourcePath = "Fonts/TMP/Hiragino Sans GB UI Body SDF";
    private const string DefaultDisplayFontResourcePath = "Fonts/TMP/Hiragino Sans GB UI Display SDF";

    private static Texture2D s_WhiteTexture;
    private static Sprite s_WhiteSprite;
    private static TMP_FontAsset s_DefaultBodyFont;
    private static TMP_FontAsset s_DefaultDisplayFont;

    public static Canvas CreateLayerCanvas(Transform parent, string name, int sortingOrder)
    {
        GameObject canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);

        RectTransform rectTransform = canvasObject.GetComponent<RectTransform>();
        Stretch(rectTransform);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // TMP SDF text stays sharper without pixel-perfect snapping on scaled overlay canvases.
        canvas.pixelPerfect = false;
        canvas.sortingOrder = sortingOrder;

        ConfigureCanvasScaler(canvasObject.GetComponent<CanvasScaler>());

        return canvas;
    }

    public static void ConfigureCanvasScaler(CanvasScaler scaler)
    {
        if (scaler == null)
        {
            return;
        }

        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.referencePixelsPerUnit = 100f;
        scaler.dynamicPixelsPerUnit = 2f;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
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
        rectTransform.sizeDelta = RoundVector(sizeDelta);
        rectTransform.anchoredPosition = RoundVector(anchoredPosition);
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
        TMP_FontAsset font,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor alignment,
        Color color,
        bool richText = false)
    {
        RectTransform rectTransform = CreateRect(name, parent, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
        TextMeshProUGUI text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = GetOrCreateDefaultTmpFont(font);
        text.fontSize = fontSize;
        text.fontStyle = MapFontStyle(fontStyle);
        text.alignment = MapAlignment(alignment);
        text.color = color;
        text.richText = richText;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.extraPadding = false;
        text.isTextObjectScaleStatic = true;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
        ApplyTextMaterial(text, fontSize, fontStyle == FontStyle.Bold || fontStyle == FontStyle.BoldAndItalic);
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

    public static TMP_FontAsset GetOrCreateDefaultTmpFont(TMP_FontAsset font = null)
    {
        if (IsUsableFontAsset(font))
        {
            return font;
        }

        TMP_FontAsset fallback = LoadFixedFontAsset(ref s_DefaultBodyFont, DefaultBodyFontResourcePath);
        if (fallback != null)
        {
            return fallback;
        }

        fallback = LoadFixedFontAsset(ref s_DefaultDisplayFont, DefaultDisplayFontResourcePath);
        if (fallback != null)
        {
            return fallback;
        }

        return font != null ? font : TMP_Settings.defaultFontAsset;
    }

    public static void RefreshTextMaterial(TMP_Text text, bool emphasize)
    {
        int fontSize = text != null ? Mathf.Max(1, Mathf.RoundToInt(text.fontSize)) : 0;
        ApplyTextMaterial(text, fontSize, emphasize);
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

    private static Vector2 RoundVector(Vector2 value)
    {
        return new Vector2(Mathf.Round(value.x), Mathf.Round(value.y));
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

    private static TMP_FontAsset LoadFixedFontAsset(ref TMP_FontAsset cache, string resourcePath)
    {
        if (cache != null)
        {
            return IsUsableFontAsset(cache) ? cache : null;
        }

        cache = Resources.Load<TMP_FontAsset>(resourcePath);
        if (!IsUsableFontAsset(cache))
        {
            cache = null;
            return null;
        }

        if (!IsHighQualityFontAsset(cache))
        {
            Debug.LogWarning($"[UiFactory] Fixed TMP font '{cache.name}' is below the expected quality target.");
        }

        return cache;
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

    private static void ApplyTextMaterial(TMP_Text text, int fontSize, bool emphasize)
    {
        if (text == null || text.fontSharedMaterial == null)
        {
            return;
        }

        Material runtimeMaterial = new Material(text.fontSharedMaterial)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        runtimeMaterial.DisableKeyword(ShaderUtilities.Keyword_Outline);

        runtimeMaterial.DisableKeyword(ShaderUtilities.Keyword_Underlay);

        if (runtimeMaterial.HasProperty(ShaderUtilities.ID_FaceDilate))
        {
            runtimeMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
        }

        if (runtimeMaterial.HasProperty(ShaderUtilities.ID_OutlineWidth))
        {
            runtimeMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
        }

        if (runtimeMaterial.HasProperty(ShaderUtilities.ID_OutlineColor))
        {
            runtimeMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.03f, 0.04f, 0.05f, 0f));
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

    private static bool IsHighQualityFontAsset(TMP_FontAsset fontAsset)
    {
        if (!IsUsableFontAsset(fontAsset))
        {
            return false;
        }

        int atlasWidth = fontAsset.atlasWidth;
        int atlasHeight = fontAsset.atlasHeight;
        if ((atlasWidth <= 0 || atlasHeight <= 0) && fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
        {
            atlasWidth = fontAsset.atlasTextures[0].width;
            atlasHeight = fontAsset.atlasTextures[0].height;
        }

        float pointSize = fontAsset.faceInfo.pointSize;
        bool hasLargeAtlas = atlasWidth >= MinimumAcceptedAtlasSize && atlasHeight >= MinimumAcceptedAtlasSize;
        bool hasExpectedSamplingSize = pointSize <= 0f || pointSize >= MinimumAcceptedSamplingPointSize;
        return hasLargeAtlas && hasExpectedSamplingSize;
    }

    private static bool IsUsableFontAsset(TMP_FontAsset fontAsset)
    {
        return fontAsset != null
            && fontAsset.atlasTextures != null
            && fontAsset.atlasTextures.Length > 0
            && fontAsset.atlasTextures[0] != null;
    }
}
