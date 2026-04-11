using System;
using UnityEngine;

[CreateAssetMenu(fileName = "UiTheme", menuName = "DarkVillage/UI Theme")]
public sealed class UiTheme : ScriptableObject
{
    private static readonly string[] PreferredBodyResourceFonts =
    {
        "Fonts/Songti",
        "Fonts/Baskerville"
    };

    private static readonly string[] PreferredDisplayResourceFonts =
    {
        "Fonts/Baskerville",
        "Fonts/Songti"
    };

    private static readonly string[] PreferredBodyFonts =
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
        "Times New Roman",
        "Georgia",
        "Palatino"
    };

    private static readonly string[] PreferredDisplayFonts =
    {
        "EB Garamond",
        "Libre Baskerville",
        "Cormorant Garamond",
        "Baskerville",
        "Source Han Serif SC",
        "Source Han Serif CN",
        "Noto Serif CJK SC",
        "Songti SC",
        "STSong",
        "SimSun",
        "Times New Roman",
        "Georgia",
        "Palatino"
    };

    [Header("Fonts")]
    [SerializeField] private Font bodyFont;
    [SerializeField] private Font displayFont;

    [Header("Chrome Sprites")]
    [SerializeField] private Sprite dialogueFrameSprite;
    [SerializeField] private Sprite portraitFrameSprite;
    [SerializeField] private Sprite choicePanelSprite;
    [SerializeField] private Sprite interactionPromptSprite;
    [SerializeField] private Sprite keycapBadgeSprite;
    [SerializeField] private Sprite combatPanelSprite;
    [SerializeField] private Sprite combatEmberGlowSprite;
    [SerializeField] private Sprite markerChipSprite;
    [SerializeField] private Sprite modalFrameSprite;
    [SerializeField] private Sprite primaryButtonSprite;

    [Header("Base Colors")]
    [SerializeField] private Color charcoal = new Color32(0x0D, 0x10, 0x14, 0xFF);
    [SerializeField] private Color slate = new Color32(0x1E, 0x23, 0x2A, 0xFF);
    [SerializeField] private Color primaryText = new Color32(0xEE, 0xE8, 0xDD, 0xFF);
    [SerializeField] private Color secondaryText = new Color32(0xBF, 0xC3, 0xC6, 0xFF);
    [SerializeField] private Color brass = new Color32(0xC8, 0xA5, 0x6E, 0xFF);
    [SerializeField] private Color dimBrass = new Color32(0x8C, 0x73, 0x50, 0xFF);
    [SerializeField] private Color moss = new Color32(0x51, 0x65, 0x46, 0xFF);
    [SerializeField] private Color ember = new Color32(0xA9, 0x4E, 0x3D, 0xFF);
    [SerializeField] private Color fogShadow = new Color(0f, 0f, 0f, 0.42f);

    [Header("Panel Colors")]
    [SerializeField] private Color panelOuter = new Color(0.05f, 0.06f, 0.08f, 0.9f);
    [SerializeField] private Color panelInner = new Color(0.12f, 0.14f, 0.17f, 0.82f);
    [SerializeField] private Color choiceIdle = new Color(0.15f, 0.17f, 0.2f, 0.5f);
    [SerializeField] private Color choiceSelected = new Color(0.79f, 0.67f, 0.45f, 0.38f);

    [Header("Animation")]
    [SerializeField] private float fastFadeDuration = 0.16f;

    public Font BodyFont => bodyFont != null ? bodyFont : LoadBodyFont();
    public Font DisplayFont => displayFont != null ? displayFont : LoadDisplayFont(bodyFont != null ? bodyFont : LoadBodyFont());
    public Sprite DialogueFrameSprite => dialogueFrameSprite != null ? dialogueFrameSprite : TryLoadResourceSprite("UI/Slices/DialogueFrame");
    public Sprite PortraitFrameSprite => portraitFrameSprite != null ? portraitFrameSprite : TryLoadResourceSprite("UI/Slices/PortraitFrame");
    public Sprite ChoicePanelSprite => choicePanelSprite != null ? choicePanelSprite : TryLoadResourceSprite("UI/Slices/ChoicePanel");
    public Sprite InteractionPromptSprite => interactionPromptSprite != null ? interactionPromptSprite : TryLoadResourceSprite("UI/Slices/InteractionPrompt");
    public Sprite KeycapBadgeSprite => keycapBadgeSprite != null ? keycapBadgeSprite : TryLoadResourceSprite("UI/Slices/KeycapBadge");
    public Sprite CombatPanelSprite => combatPanelSprite != null ? combatPanelSprite : TryLoadResourceSprite("UI/Slices/CombatPanel");
    public Sprite CombatEmberGlowSprite => combatEmberGlowSprite != null ? combatEmberGlowSprite : TryLoadResourceSprite("UI/Slices/CombatEmberGlow");
    public Sprite MarkerChipSprite => markerChipSprite != null ? markerChipSprite : TryLoadResourceSprite("UI/Slices/MarkerChip");
    public Sprite ModalFrameSprite => modalFrameSprite != null ? modalFrameSprite : TryLoadResourceSprite("UI/Slices/ModalFrame");
    public Sprite PrimaryButtonSprite => primaryButtonSprite != null ? primaryButtonSprite : TryLoadResourceSprite("UI/Slices/PrimaryButton");

    public Color WaxWhiteText => primaryText;
    public Color WaxWhiteTextSoft => new Color(primaryText.r, primaryText.g, primaryText.b, 0.9f);
    public Color SootBlackBase => new Color(0.1f, 0.1f, 0.12f, 0.95f);
    public Color MutedBrass => brass;
    public Color MossGreen => moss;
    public Color EmberRed => ember;
    public Color Charcoal => charcoal;
    public Color Slate => slate;
    public Color PrimaryText => primaryText;
    public Color SecondaryText => secondaryText;
    public Color Brass => brass;
    public Color DimBrass => dimBrass;
    public Color Moss => moss;
    public Color Ember => ember;
    public Color FogShadow => fogShadow;
    public Color PanelOuter => panelOuter;
    public Color PanelInner => panelInner;
    public Color ChoiceIdle => choiceIdle;
    public Color ChoiceSelected => choiceSelected;
    public float FastFadeDuration => Mathf.Max(0.01f, fastFadeDuration);

    public void EnsureRuntimeDefaults()
    {
        if (bodyFont == null)
        {
            bodyFont = LoadBodyFont();
        }

        if (displayFont == null)
        {
            displayFont = LoadDisplayFont(bodyFont);
        }
    }

    public static UiTheme CreateRuntimeDefault()
    {
        UiTheme theme = CreateInstance<UiTheme>();
        theme.hideFlags = HideFlags.HideAndDontSave;
        theme.EnsureRuntimeDefaults();
        return theme;
    }

    private static Font LoadBodyFont()
    {
        Font projectFont = TryLoadResourceFont(PreferredBodyResourceFonts);
        if (projectFont != null)
        {
            return projectFont;
        }

        Font font = TryCreateDynamicOsFont(PreferredBodyFonts);
        if (font != null)
        {
            return font;
        }

        return LoadBuiltinFallback();
    }

    private static Font LoadDisplayFont(Font bodyFallback)
    {
        Font projectFont = TryLoadResourceFont(PreferredDisplayResourceFonts);
        if (projectFont != null)
        {
            return projectFont;
        }

        Font font = TryCreateDynamicOsFont(PreferredDisplayFonts);
        if (font != null)
        {
            return font;
        }

        return bodyFallback != null ? bodyFallback : LoadBuiltinFallback();
    }

    private static Font LoadBuiltinFallback()
    {
        Font font = TryLoadBuiltinFont("LegacyRuntime.ttf");

        if (font != null)
        {
            return font;
        }

        return Font.CreateDynamicFontFromOSFont("Arial", 16);
    }

    private static Font TryLoadResourceFont(string[] resourcePaths)
    {
        for (int i = 0; i < resourcePaths.Length; i++)
        {
            Font font = Resources.Load<Font>(resourcePaths[i]);
            if (font != null)
            {
                return font;
            }
        }

        return null;
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

    private static Sprite TryLoadResourceSprite(string resourcePath)
    {
        Sprite directSprite = Resources.Load<Sprite>(resourcePath);
        if (directSprite != null)
        {
            return directSprite;
        }

        int splitIndex = resourcePath.LastIndexOf('/');
        if (splitIndex < 0)
        {
            return null;
        }

        string folderPath = resourcePath.Substring(0, splitIndex);
        string assetName = resourcePath.Substring(splitIndex + 1);
        Sprite[] sprites = Resources.LoadAll<Sprite>(folderPath);
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite == null)
            {
                continue;
            }

            if (sprite.name.Equals(assetName, StringComparison.OrdinalIgnoreCase)
                || sprite.name.StartsWith($"{assetName}_b", StringComparison.OrdinalIgnoreCase))
            {
                return sprite;
            }
        }

        return null;
    }
}
