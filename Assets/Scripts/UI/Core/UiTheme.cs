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
    [SerializeField] private Color charcoal = default;
    [SerializeField] private Color slate = default;
    [SerializeField] private Color primaryText = default;
    [SerializeField] private Color secondaryText = default;
    [SerializeField] private Color brass = default;
    [SerializeField] private Color dimBrass = default;
    [SerializeField] private Color moss = default;
    [SerializeField] private Color ember = default;
    [SerializeField] private Color fogShadow = default;

    [Header("Panel Colors")]
    [SerializeField] private Color panelOuter = default;
    [SerializeField] private Color panelInner = default;
    [SerializeField] private Color choiceIdle = default;
    [SerializeField] private Color choiceSelected = default;

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
        if (charcoal == default) charcoal = UiColors.Charcoal;
        if (slate == default) slate = UiColors.Slate;
        if (primaryText == default) primaryText = UiColors.TextPrimary;
        if (secondaryText == default) secondaryText = UiColors.TextSecondary;
        if (brass == default) brass = UiColors.Brass;
        if (dimBrass == default) dimBrass = UiColors.DimBrass;
        if (moss == default) moss = UiColors.Moss;
        if (ember == default) ember = UiColors.Ember;
        if (fogShadow == default) fogShadow = UiColors.FogShadow;
        if (panelOuter == default) panelOuter = UiColors.PanelOuter;
        if (panelInner == default) panelInner = UiColors.PanelInner;
        if (choiceIdle == default) choiceIdle = UiColors.ChoiceIdle;
        if (choiceSelected == default) choiceSelected = UiColors.ChoiceSelected;

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
