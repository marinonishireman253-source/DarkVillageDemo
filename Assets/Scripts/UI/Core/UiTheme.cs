using System;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "UiTheme", menuName = "DarkVillage/UI Theme")]
public sealed class UiTheme : ScriptableObject
{
    private const string DefaultBodyFontResourcePath = "Fonts/TMP/Hiragino Sans GB UI Body SDF";
    private const string DefaultDisplayFontResourcePath = "Fonts/TMP/Hiragino Sans GB UI Display SDF";

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset bodyFontAsset;
    [SerializeField] private TMP_FontAsset displayFontAsset;

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

    public TMP_FontAsset BodyFont => ResolveBodyFont();
    public TMP_FontAsset DisplayFont => ResolveDisplayFont();
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
        if (!IsUsableFontAsset(bodyFontAsset))
        {
            bodyFontAsset = LoadBodyFont();
        }

        if (!IsUsableFontAsset(displayFontAsset))
        {
            displayFontAsset = LoadDisplayFont(bodyFontAsset);
        }
    }

    public static UiTheme CreateRuntimeDefault()
    {
        UiTheme theme = CreateInstance<UiTheme>();
        theme.hideFlags = HideFlags.HideAndDontSave;
        theme.EnsureRuntimeDefaults();
        return theme;
    }

    private static TMP_FontAsset LoadBodyFont()
    {
        TMP_FontAsset body = LoadUiFontAsset(DefaultBodyFontResourcePath, "body");
        if (IsUsableFontAsset(body))
        {
            return body;
        }

        TMP_FontAsset display = LoadUiFontAsset(DefaultDisplayFontResourcePath, "display");
        if (IsUsableFontAsset(display))
        {
            Debug.LogWarning("[UiTheme] Body TMP font is unusable. Falling back to display TMP font.");
            return display;
        }

        return body;
    }

    private static TMP_FontAsset LoadDisplayFont(TMP_FontAsset bodyFallback)
    {
        TMP_FontAsset displayFont = LoadUiFontAsset(DefaultDisplayFontResourcePath, "display");
        return IsUsableFontAsset(displayFont) ? displayFont : bodyFallback;
    }

    private TMP_FontAsset ResolveBodyFont()
    {
        if (IsUsableFontAsset(bodyFontAsset))
        {
            return bodyFontAsset;
        }

        bodyFontAsset = LoadBodyFont();
        return bodyFontAsset;
    }

    private TMP_FontAsset ResolveDisplayFont()
    {
        if (IsUsableFontAsset(displayFontAsset))
        {
            return displayFontAsset;
        }

        displayFontAsset = LoadDisplayFont(ResolveBodyFont());
        return displayFontAsset;
    }

    private static TMP_FontAsset LoadUiFontAsset(string resourcePath, string role)
    {
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>(resourcePath);
        if (fontAsset != null)
        {
            return fontAsset;
        }

        if (TMP_Settings.defaultFontAsset != null)
        {
            Debug.LogError($"[UiTheme] Missing fixed {role} TMP font at Resources/{resourcePath}. Falling back to TMP default font.");
            return TMP_Settings.defaultFontAsset;
        }

        Debug.LogError($"[UiTheme] Missing fixed {role} TMP font at Resources/{resourcePath}, and TMP default font is also unavailable.");
        return null;
    }

    private static bool IsUsableFontAsset(TMP_FontAsset fontAsset)
    {
        return fontAsset != null
            && fontAsset.atlasTextures != null
            && fontAsset.atlasTextures.Length > 0
            && fontAsset.atlasTextures[0] != null;
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
