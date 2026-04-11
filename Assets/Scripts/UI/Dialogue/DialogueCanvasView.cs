using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueCanvasView : MonoBehaviour
{
    private sealed class ChoiceRow
    {
        public GameObject Root { get; set; }
        public Image Background { get; set; }
        public TMP_Text Label { get; set; }
        public Button Button { get; set; }
    }

    [Header("Main Dialogue Panel (Bottom anchored)")]
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private RectTransform dialoguePanelRoot;
    [SerializeField] private Image dialogueBackground;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private Image speakerNamePlate;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private ScrollRect dialogueBodyScroll;

    [Header("Portrait Frame")]
    [SerializeField] private GameObject portraitRoot;
    [SerializeField] private Image portraitFrameDecoration;
    [SerializeField] private Image portraitMatteFill;
    [SerializeField] private Image portraitBackdrop;
    [SerializeField] private RawImage portraitImage;
    [SerializeField] private AspectRatioFitter portraitAspectFitter;
    [SerializeField] private TMP_Text portraitPlaceholderText;
    [SerializeField] private Image portraitNamePlate;
    [SerializeField] private TMP_Text portraitNameText;

    [Header("Choices Panel")]
    [SerializeField] private CanvasGroup choicesCanvasGroup;
    [SerializeField] private RectTransform choicePanelRoot;
    [SerializeField] private Image choicesBackground;
    [SerializeField] private TMP_Text choicesPromptText;
    [SerializeField] private RectTransform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;

    private const float DialoguePanelWidth = 1600f;
    private const float DialoguePanelHeight = 420f;
    private const float DialogueHorizontalPadding = 100f;
    private const float DialogueVerticalPadding = 60f;
    private const float DialogueSpeakerPlateWidth = 360f;
    private const float DialogueSpeakerPlateHeight = 58f;
    private const float DialogueSpeakerGap = 30f;
    private const float PortraitFrameSize = 360f;
    private const float PortraitBrassBorder = 12f;
    private const float PortraitMatteBorder = 20f;

    private readonly List<ChoiceRow> _choiceRows = new List<ChoiceRow>();
    private UiTheme _theme;
    private bool _initialized;

    public void Initialize(UiTheme theme)
    {
        if (_initialized)
        {
            return;
        }

        _theme = theme != null ? theme : UiTheme.CreateRuntimeDefault();
        _theme.EnsureRuntimeDefaults();

        EnsureReferences();
        ApplyTheme();
        HideDialogue();
        HideChoices();
        HidePortrait();

        _initialized = true;
    }

    public void ShowDialogue(string speakerName, string body, string continueHint)
    {
        if (!_initialized || dialogueCanvasGroup == null)
        {
            return;
        }

        SetCanvasGroup(dialogueCanvasGroup, true);
        if (dialoguePanelRoot != null)
        {
            dialoguePanelRoot.gameObject.SetActive(true);
        }

        bool hasSpeaker = !string.IsNullOrWhiteSpace(speakerName);
        if (speakerNamePlate != null)
        {
            speakerNamePlate.gameObject.SetActive(hasSpeaker);
        }

        if (speakerNameText != null)
        {
            string trimmedSpeakerName = speakerName.Trim();
            bool useDisplayFont = ShouldUseDisplayFont(trimmedSpeakerName);
            speakerNameText.font = UiFactory.GetOrCreateDefaultTmpFont(useDisplayFont ? _theme.DisplayFont : _theme.BodyFont);
            speakerNameText.characterSpacing = useDisplayFont ? 5f : 0f;
            speakerNameText.text = hasSpeaker ? FormatSpeakerName(trimmedSpeakerName) : string.Empty;
        }

        if (dialogueText != null)
        {
            dialogueText.text = string.IsNullOrWhiteSpace(body) ? "..." : body.Trim();
        }

        if (hintText != null)
        {
            hintText.text = string.IsNullOrWhiteSpace(continueHint) ? string.Empty : continueHint.Trim();
        }

        RefreshDialogueLayout();
    }

    public void ShowDialogue(string speaker, string text, Sprite portrait = null)
    {
        ShowDialogue(speaker, text, string.Empty);
        if (portrait != null)
        {
            ShowPortrait(portrait.texture, speaker, _theme.Slate);
        }
    }

    public void HideDialogue()
    {
        if (dialogueCanvasGroup == null)
        {
            return;
        }

        SetCanvasGroup(dialogueCanvasGroup, false);
    }

    public void ShowChoices(IReadOnlyList<DialogueChoice> choices, int selectedIndex)
    {
        if (!_initialized || choicesCanvasGroup == null || choicePanelRoot == null || choicesContainer == null)
        {
            return;
        }

        if (choices == null || choices.Count == 0)
        {
            HideChoices();
            return;
        }

        SetCanvasGroup(choicesCanvasGroup, true);
        choicePanelRoot.gameObject.SetActive(true);

        if (choicesPromptText != null)
        {
            choicesPromptText.text = "选择你的回应";
        }

        EnsureChoiceRows(choices.Count);

        float panelHeight = Mathf.Clamp(choices.Count * 54f + 34f, 108f, 248f);
        choicePanelRoot.sizeDelta = new Vector2(choicePanelRoot.sizeDelta.x, panelHeight);

        for (int i = 0; i < _choiceRows.Count; i++)
        {
            bool shouldShow = i < choices.Count;
            _choiceRows[i].Root.SetActive(shouldShow);
            if (!shouldShow)
            {
                continue;
            }

            bool isSelected = i == selectedIndex;
            _choiceRows[i].Background.color = isSelected ? _theme.ChoiceSelected : _theme.ChoiceIdle;
            _choiceRows[i].Label.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
            _choiceRows[i].Label.color = isSelected ? _theme.WaxWhiteText : _theme.SecondaryText;
            _choiceRows[i].Label.text = isSelected
                ? $"> {i + 1}. {choices[i].ChoiceText}"
                : $"  {i + 1}. {choices[i].ChoiceText}";

            if (_choiceRows[i].Button != null)
            {
                _choiceRows[i].Button.onClick.RemoveAllListeners();
                int capturedIndex = i;
                _choiceRows[i].Button.onClick.AddListener(() => DialogueRunner.Instance?.SelectChoice(capturedIndex));
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(choicesContainer);
    }

    public void HideChoices()
    {
        if (choicesCanvasGroup == null)
        {
            return;
        }

        SetCanvasGroup(choicesCanvasGroup, false);
        if (choicePanelRoot != null)
        {
            choicePanelRoot.gameObject.SetActive(false);
        }
    }

    public void ShowPortrait(Texture portraitTexture, string displayName, Color backgroundColor)
    {
        if (!_initialized || portraitRoot == null)
        {
            return;
        }

        portraitRoot.SetActive(true);
        if (portraitFrameDecoration != null)
        {
            portraitFrameDecoration.color = _theme.MutedBrass;
        }

        if (portraitMatteFill != null)
        {
            portraitMatteFill.color = new Color(_theme.Slate.r, _theme.Slate.g, _theme.Slate.b, 0.94f);
        }

        if (portraitBackdrop != null)
        {
            portraitBackdrop.color = backgroundColor.a > 0f ? backgroundColor : _theme.PanelInner;
        }

        if (portraitImage != null)
        {
            portraitImage.texture = portraitTexture;
            portraitImage.gameObject.SetActive(portraitTexture != null);
        }

        if (portraitAspectFitter != null)
        {
            portraitAspectFitter.aspectRatio = portraitTexture != null && portraitTexture.height > 0
                ? portraitTexture.width / (float)portraitTexture.height
                : 1f;
        }

        if (portraitPlaceholderText != null)
        {
            portraitPlaceholderText.gameObject.SetActive(portraitTexture == null);
        }

        bool hasName = !string.IsNullOrWhiteSpace(displayName);
        if (portraitNamePlate != null)
        {
            portraitNamePlate.gameObject.SetActive(hasName);
        }

        if (portraitNameText != null)
        {
            string trimmedDisplayName = hasName ? displayName.Trim() : string.Empty;
            bool useDisplayFont = ShouldUseDisplayFont(trimmedDisplayName);
            portraitNameText.font = UiFactory.GetOrCreateDefaultTmpFont(useDisplayFont ? _theme.DisplayFont : _theme.BodyFont);
            portraitNameText.characterSpacing = useDisplayFont ? 2f : 0f;
            portraitNameText.text = trimmedDisplayName;
        }
    }

    public void HidePortrait()
    {
        if (portraitRoot != null)
        {
            portraitRoot.SetActive(false);
        }
    }

    private void EnsureReferences()
    {
        if (dialogueCanvasGroup != null
            && dialogueText != null
            && speakerNameText != null
            && choicesContainer != null
            && portraitImage != null)
        {
            return;
        }

        BuildRuntimeFallback();
    }

    private void ApplyTheme()
    {
        TMP_FontAsset bodyFont = UiFactory.GetOrCreateDefaultTmpFont(_theme.BodyFont);
        TMP_FontAsset displayFont = UiFactory.GetOrCreateDefaultTmpFont(_theme.DisplayFont);

        if (dialogueText != null)
        {
            dialogueText.font = bodyFont;
            dialogueText.color = _theme.WaxWhiteTextSoft;
            dialogueText.fontSize = 36f;
            dialogueText.lineSpacing = 14f;
            dialogueText.margin = Vector4.zero;
            dialogueText.textWrappingMode = TextWrappingModes.Normal;
            UiFactory.RefreshTextMaterial(dialogueText, false);
        }

        if (speakerNamePlate != null)
        {
            speakerNamePlate.color = new Color(_theme.Charcoal.r, _theme.Charcoal.g, _theme.Charcoal.b, 0.74f);
        }

        if (speakerNameText != null)
        {
            speakerNameText.font = displayFont;
            speakerNameText.fontStyle = FontStyles.Normal;
            speakerNameText.color = _theme.MutedBrass;
            speakerNameText.fontSize = 42f;
            speakerNameText.characterSpacing = 5f;
            UiFactory.RefreshTextMaterial(speakerNameText, true);
        }

        if (hintText != null)
        {
            hintText.font = bodyFont;
            hintText.color = new Color(_theme.SecondaryText.r, _theme.SecondaryText.g, _theme.SecondaryText.b, 0.88f);
            hintText.fontSize = 16f;
            UiFactory.RefreshTextMaterial(hintText, false);
        }

        if (choicesPromptText != null)
        {
            choicesPromptText.font = bodyFont;
            choicesPromptText.color = _theme.SecondaryText;
            UiFactory.RefreshTextMaterial(choicesPromptText, false);
        }

        if (portraitFrameDecoration != null)
        {
            portraitFrameDecoration.color = _theme.MutedBrass;
        }

        if (portraitMatteFill != null)
        {
            portraitMatteFill.color = new Color(_theme.Slate.r, _theme.Slate.g, _theme.Slate.b, 0.94f);
        }

        if (portraitBackdrop != null)
        {
            portraitBackdrop.color = _theme.PanelInner;
        }

        if (portraitNameText != null)
        {
            portraitNameText.font = displayFont;
            portraitNameText.fontStyle = FontStyles.Normal;
            portraitNameText.color = _theme.Charcoal;
            portraitNameText.fontSize = 18f;
            portraitNameText.characterSpacing = 2f;
            UiFactory.RefreshTextMaterial(portraitNameText, true);
        }

        if (portraitPlaceholderText != null)
        {
            portraitPlaceholderText.font = displayFont;
            portraitPlaceholderText.color = _theme.SecondaryText;
            UiFactory.RefreshTextMaterial(portraitPlaceholderText, true);
        }
    }

    private void RefreshDialogueLayout()
    {
        if (dialoguePanelRoot == null || dialogueText == null)
        {
            return;
        }

        dialoguePanelRoot.sizeDelta = new Vector2(DialoguePanelWidth, DialoguePanelHeight);

        LayoutRebuilder.ForceRebuildLayoutImmediate(dialoguePanelRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(dialogueText.rectTransform);
        Canvas.ForceUpdateCanvases();
        if (dialogueBodyScroll != null)
        {
            dialogueBodyScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void EnsureChoiceRows(int count)
    {
        while (_choiceRows.Count < count)
        {
            _choiceRows.Add(CreateChoiceRow(_choiceRows.Count));
        }
    }

    private ChoiceRow CreateChoiceRow(int index)
    {
        if (choiceButtonPrefab != null)
        {
            GameObject instance = Instantiate(choiceButtonPrefab, choicesContainer);
            instance.name = $"ChoiceRow_{index + 1}";

            Image background = instance.GetComponent<Image>();
            if (background == null)
            {
                background = instance.GetComponentInChildren<Image>(true);
            }

            Button button = instance.GetComponent<Button>();
            TMP_Text label = instance.GetComponentInChildren<TMP_Text>(true);

            if (background == null)
            {
                background = instance.AddComponent<Image>();
                background.sprite = null;
            }

            if (button == null)
            {
                button = instance.AddComponent<Button>();
                button.targetGraphic = background;
            }

            if (label == null)
            {
                RectTransform labelRect = UiFactory.CreateRect("Label", instance.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-24f, -12f), Vector2.zero);
                label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.MidlineLeft;
            }

            StyleChoiceLabel(label);

            return new ChoiceRow
            {
                Root = instance,
                Background = background,
                Button = button,
                Label = label
            };
        }

        Image backgroundFallback = UiFactory.CreateImage(
            $"ChoiceRow_{index + 1}",
            choicesContainer,
            new Vector2(0f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 48f),
            Vector2.zero,
            _theme.ChoiceIdle);

        LayoutElement layoutElement = backgroundFallback.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 48f;

        Button fallbackButton = backgroundFallback.gameObject.AddComponent<Button>();
        fallbackButton.targetGraphic = backgroundFallback;

        TMP_Text fallbackLabel = UiFactory.CreateText(
            "Label",
            backgroundFallback.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-24f, -12f),
            Vector2.zero,
            _theme.BodyFont,
            16,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            _theme.SecondaryText);
        StyleChoiceLabel(fallbackLabel);

        return new ChoiceRow
        {
            Root = backgroundFallback.gameObject,
            Background = backgroundFallback,
            Label = fallbackLabel,
            Button = fallbackButton
        };
    }

    private void BuildRuntimeFallback()
    {
        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            UiFactory.Stretch(root);
        }

        dialoguePanelRoot = UiFactory.CreateRect(
            "DialoguePanel",
            transform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(DialoguePanelWidth, DialoguePanelHeight),
            new Vector2(0f, 22f));
        dialogueCanvasGroup = UiFactory.GetOrAddCanvasGroup(dialoguePanelRoot.gameObject);
        CreatePanelShadow(dialoguePanelRoot, new Vector2(18f, -18f), new Vector2(26f, 24f), new Color(0f, 0f, 0f, 0.3f));

        dialogueBackground = UiFactory.CreateImage(
            "DialogueOuter",
            dialoguePanelRoot,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            _theme.SootBlackBase);
        Sprite dialogueFrameSprite = _theme.DialogueFrameSprite;
        if (dialogueFrameSprite != null)
        {
            UiFactory.ApplySprite(dialogueBackground, dialogueFrameSprite);
            dialogueBackground.color = Color.white;
        }

        RectTransform dialogueInner = UiFactory.CreateRect(
            "DialogueInner",
            dialoguePanelRoot,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-10f, -10f),
            Vector2.zero);
        UiFactory.CreateImage("DialogueInnerFill", dialogueInner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, _theme.PanelInner);
        CreateAccentLine(dialogueInner, 16f);
        CreateLeftAccent(dialogueInner, 10f);
        CreateCornerBrackets(dialogueInner, 12f, 16f);

        RectTransform dialogueContent = UiFactory.CreateRect(
            "DialogueContent",
            dialogueInner,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);
        ApplyStretchInsets(dialogueContent, DialogueHorizontalPadding, DialogueVerticalPadding, DialogueHorizontalPadding, DialogueVerticalPadding);

        speakerNamePlate = UiFactory.CreateImage(
            "SpeakerTag",
            dialogueContent,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(DialogueSpeakerPlateWidth, DialogueSpeakerPlateHeight),
            Vector2.zero,
            new Color(_theme.Charcoal.r, _theme.Charcoal.g, _theme.Charcoal.b, 0.74f));

        speakerNameText = UiFactory.CreateText(
            "SpeakerText",
            speakerNamePlate.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-36f, -12f),
            Vector2.zero,
            _theme.DisplayFont,
            42,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            _theme.Charcoal);

        Image bodyViewport = UiFactory.CreateImage(
            "BodyViewport",
            dialogueContent,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            new Color(0f, 0f, 0f, 0.001f));
        ApplyStretchInsets(bodyViewport.rectTransform, 0f, DialogueSpeakerPlateHeight + DialogueSpeakerGap, 0f, 0f);
        bodyViewport.maskable = true;
        Mask bodyMask = bodyViewport.gameObject.AddComponent<Mask>();
        bodyMask.showMaskGraphic = false;

        dialogueBodyScroll = bodyViewport.gameObject.AddComponent<ScrollRect>();
        dialogueBodyScroll.horizontal = false;
        dialogueBodyScroll.movementType = ScrollRect.MovementType.Clamped;
        dialogueBodyScroll.scrollSensitivity = 26f;
        dialogueBodyScroll.viewport = bodyViewport.rectTransform;

        dialogueText = UiFactory.CreateText(
            "BodyText",
            bodyViewport.transform,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            _theme.BodyFont,
            36,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            _theme.WaxWhiteTextSoft,
            true);
        dialogueText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        dialogueBodyScroll.content = dialogueText.rectTransform;

        hintText = UiFactory.CreateText(
            "HintText",
            dialogueInner,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(-(DialogueHorizontalPadding * 2f), 24f),
            new Vector2(0f, 30f),
            _theme.BodyFont,
            16,
            FontStyle.Normal,
            TextAnchor.MiddleRight,
            _theme.SecondaryText);

        choicePanelRoot = UiFactory.CreateRect(
            "ChoicePanel",
            transform,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(520f, 192f),
            new Vector2(-24f, 288f));
        choicesCanvasGroup = UiFactory.GetOrAddCanvasGroup(choicePanelRoot.gameObject);
        CreatePanelShadow(choicePanelRoot, new Vector2(16f, -16f), new Vector2(24f, 24f), new Color(0f, 0f, 0f, 0.28f));

        choicesBackground = UiFactory.CreateImage(
            "ChoiceOuter",
            choicePanelRoot,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            _theme.SootBlackBase);
        Sprite choicePanelSprite = _theme.ChoicePanelSprite;
        if (choicePanelSprite != null)
        {
            UiFactory.ApplySprite(choicesBackground, choicePanelSprite);
            choicesBackground.color = Color.white;
        }

        RectTransform choiceInner = UiFactory.CreateRect(
            "ChoiceInner",
            choicePanelRoot,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-10f, -10f),
            Vector2.zero);
        UiFactory.CreateImage("ChoiceInnerFill", choiceInner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.1f, 0.12f, 0.15f, 0.74f));
        CreateAccentLine(choiceInner, 14f);
        CreateCornerBrackets(choiceInner, 10f, 14f);

        choicesPromptText = UiFactory.CreateText(
            "ChoicePromptText",
            choiceInner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-32f, 28f),
            new Vector2(0f, -10f),
            _theme.BodyFont,
            13,
            FontStyle.Italic,
            TextAnchor.MiddleLeft,
            _theme.SecondaryText);

        choicesContainer = UiFactory.CreateRect(
            "ChoiceRows",
            choiceInner,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-24f, -44f),
            new Vector2(0f, -10f));
        VerticalLayoutGroup choiceLayout = choicesContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        choiceLayout.spacing = 8f;
        choiceLayout.childControlHeight = false;
        choiceLayout.childControlWidth = true;
        choiceLayout.childForceExpandHeight = false;
        choiceLayout.childForceExpandWidth = true;
        choiceLayout.padding = new RectOffset(0, 0, 26, 0);

        RectTransform portraitPanel = UiFactory.CreateRect(
            "PortraitPanel",
            transform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(PortraitFrameSize, PortraitFrameSize),
            new Vector2(28f, 250f));
        portraitRoot = portraitPanel.gameObject;
        CreatePanelShadow(portraitPanel, new Vector2(14f, -14f), new Vector2(18f, 18f), new Color(0f, 0f, 0f, 0.26f));

        portraitFrameDecoration = UiFactory.CreateImage(
            "PortraitFrame",
            portraitPanel,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            _theme.MutedBrass);
        Sprite portraitFrameSprite = _theme.PortraitFrameSprite;
        if (portraitFrameSprite != null)
        {
            UiFactory.ApplySprite(portraitFrameDecoration, portraitFrameSprite);
            portraitFrameDecoration.color = Color.white;
        }

        portraitMatteFill = UiFactory.CreateImage(
            "PortraitMatte",
            portraitPanel,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-(PortraitBrassBorder * 2f), -(PortraitBrassBorder * 2f)),
            Vector2.zero,
            new Color(0.08f, 0.09f, 0.12f, 0.94f));

        portraitBackdrop = UiFactory.CreateImage(
            "PortraitBackdrop",
            portraitMatteFill.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-(PortraitMatteBorder * 2f), -(PortraitMatteBorder * 2f)),
            Vector2.zero,
            _theme.PanelInner);
        CreateAccentLine(portraitPanel, 0f);
        CreateCornerBrackets(portraitPanel, 10f, 12f);

        RectTransform portraitImageRect = UiFactory.CreateRect(
            "PortraitImage",
            portraitBackdrop.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);
        portraitImage = portraitImageRect.gameObject.AddComponent<RawImage>();
        portraitImage.raycastTarget = false;
        portraitImage.color = Color.white;
        portraitAspectFitter = portraitImageRect.gameObject.AddComponent<AspectRatioFitter>();
        portraitAspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        portraitAspectFitter.aspectRatio = 1f;

        portraitPlaceholderText = UiFactory.CreateText(
            "PortraitPlaceholder",
            portraitBackdrop.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            _theme.DisplayFont,
            32,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            _theme.SecondaryText);
        portraitPlaceholderText.text = "?";

        portraitNamePlate = UiFactory.CreateImage(
            "PortraitNameTag",
            portraitPanel,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0f),
            new Vector2(-40f, 40f),
            new Vector2(0f, 12f),
            _theme.MutedBrass);

        portraitNameText = UiFactory.CreateText(
            "PortraitNameText",
            portraitNamePlate.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-28f, -8f),
            Vector2.zero,
            _theme.DisplayFont,
            18,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            _theme.Charcoal);
    }

    private string FormatSpeakerName(string speakerName)
    {
        if (string.IsNullOrWhiteSpace(speakerName))
        {
            return string.Empty;
        }

        bool hasLatinLetter = false;
        for (int i = 0; i < speakerName.Length; i++)
        {
            char c = speakerName[i];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
            {
                hasLatinLetter = true;
                continue;
            }

            if (char.IsWhiteSpace(c) || char.IsDigit(c) || c == '\'' || c == '-' || c == '_')
            {
                continue;
            }

            return speakerName;
        }

        return hasLatinLetter ? speakerName.ToUpperInvariant() : speakerName;
    }

    private static bool ShouldUseDisplayFont(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        bool hasLatinLetter = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
            {
                hasLatinLetter = true;
                continue;
            }

            if (char.IsWhiteSpace(c) || char.IsDigit(c) || c == '\'' || c == '-' || c == '_')
            {
                continue;
            }

            return false;
        }

        return hasLatinLetter;
    }

    private void SetCanvasGroup(CanvasGroup group, bool visible)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void CreateAccentLine(Transform parent, float inset)
    {
        Image line = UiFactory.CreateImage(
            "AccentLine",
            parent,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-(inset * 2f), 2f),
            new Vector2(0f, -1f),
            new Color(_theme.MutedBrass.r, _theme.MutedBrass.g, _theme.MutedBrass.b, 0.72f));
        line.raycastTarget = false;
    }

    private void CreateLeftAccent(Transform parent, float inset)
    {
        Image line = UiFactory.CreateImage(
            "LeftAccent",
            parent,
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, 0.5f),
            new Vector2(2f, -20f),
            new Vector2(inset, 0f),
            new Color(_theme.MutedBrass.r, _theme.MutedBrass.g, _theme.MutedBrass.b, 0.52f));
        line.raycastTarget = false;
    }

    private void CreatePanelShadow(RectTransform panel, Vector2 offset, Vector2 expand, Color color)
    {
        Image shadow = UiFactory.CreateImage(
            "PanelShadow",
            panel,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            expand,
            offset,
            color);
        shadow.raycastTarget = false;
        shadow.transform.SetAsFirstSibling();
    }

    private void StyleChoiceLabel(TMP_Text label)
    {
        if (label == null)
        {
            return;
        }

        label.font = UiFactory.GetOrCreateDefaultTmpFont(_theme.BodyFont);
        label.fontSize = 16f;
        label.color = _theme.SecondaryText;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        UiFactory.RefreshTextMaterial(label, false);
    }

    private static void ApplyStretchInsets(RectTransform rectTransform, float left, float top, float right, float bottom)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    private void CreateCornerBrackets(Transform parent, float inset, float length)
    {
        CreateCornerBracket(parent, "TopLeft", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), inset, -inset, length, 2f);
        CreateCornerBracket(parent, "TopRight", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), -inset, -inset, length, 2f);
        CreateCornerBracket(parent, "BottomLeft", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), inset, inset, length, 2f);
        CreateCornerBracket(parent, "BottomRight", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), -inset, inset, length, 2f);
    }

    private void CreateCornerBracket(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, float x, float y, float length, float thickness)
    {
        Color color = new Color(_theme.MutedBrass.r, _theme.MutedBrass.g, _theme.MutedBrass.b, 0.55f);
        Image horizontal = UiFactory.CreateImage(
            $"{name}_H",
            parent,
            anchorMin,
            anchorMax,
            pivot,
            new Vector2(length, thickness),
            new Vector2(x, y),
            color);
        horizontal.raycastTarget = false;

        Image vertical = UiFactory.CreateImage(
            $"{name}_V",
            parent,
            anchorMin,
            anchorMax,
            pivot,
            new Vector2(thickness, length),
            new Vector2(x, y),
            color);
        vertical.raycastTarget = false;
    }
}
