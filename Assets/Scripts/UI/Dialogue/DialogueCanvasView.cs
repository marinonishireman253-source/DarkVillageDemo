using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueCanvasView : MonoBehaviour
{
    private sealed class ChoiceRow
    {
        public GameObject Root;
        public Image Background;
        public TMP_Text Label;
        public Button Button;
    }

    [Header("Dialogue")]
    [SerializeField] private CanvasGroup dialogueCanvasGroup;
    [SerializeField] private RectTransform dialoguePanelRoot;
    [SerializeField] private Image dialogueBackground;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private Image speakerNamePlate;
    [SerializeField] private TMP_Text hintText;

    [Header("Portrait")]
    [SerializeField] private GameObject portraitRoot;
    [SerializeField] private Image portraitFrameDecoration;
    [SerializeField] private Image portraitMatteFill;
    [SerializeField] private Image portraitBackdrop;
    [SerializeField] private RawImage portraitImage;
    [SerializeField] private AspectRatioFitter portraitAspectFitter;
    [SerializeField] private TMP_Text portraitPlaceholderText;
    [SerializeField] private Image portraitNamePlate;
    [SerializeField] private TMP_Text portraitNameText;

    [Header("Choices")]
    [SerializeField] private CanvasGroup choicesCanvasGroup;
    [SerializeField] private RectTransform choicePanelRoot;
    [SerializeField] private Image choicesBackground;
    [SerializeField] private TMP_Text choicesPromptText;
    [SerializeField] private RectTransform choicesContainer;

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

        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            UiFactory.Stretch(root);
        }

        BuildLayout();
        HideDialogue();
        HideChoices();
        HidePortrait();

        _initialized = true;
    }

    public void ShowDialogue(string speakerName, string body, string continueHint)
    {
        if (!_initialized || dialogueCanvasGroup == null || dialoguePanelRoot == null)
        {
            return;
        }

        SetCanvasGroup(dialogueCanvasGroup, true);
        dialoguePanelRoot.gameObject.SetActive(true);

        bool hasSpeaker = !string.IsNullOrWhiteSpace(speakerName);
        speakerNamePlate.gameObject.SetActive(hasSpeaker);
        speakerNameText.text = hasSpeaker ? speakerName.Trim() : string.Empty;
        dialogueText.text = string.IsNullOrWhiteSpace(body) ? "..." : body.Trim();
        hintText.text = string.IsNullOrWhiteSpace(continueHint) ? string.Empty : continueHint.Trim();

        float bodyHeight = Mathf.Clamp(dialogueText.preferredHeight, 90f, 170f);
        dialoguePanelRoot.sizeDelta = new Vector2(dialoguePanelRoot.sizeDelta.x, bodyHeight + 158f);
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
        if (dialoguePanelRoot != null)
        {
            dialoguePanelRoot.gameObject.SetActive(false);
        }
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
        choicesPromptText.text = "选择你的回应";

        EnsureChoiceRows(choices.Count);

        float panelHeight = Mathf.Clamp(choices.Count * 86f + 96f, 180f, 430f);
        choicePanelRoot.sizeDelta = new Vector2(choicePanelRoot.sizeDelta.x, panelHeight);

        for (int i = 0; i < _choiceRows.Count; i++)
        {
            ChoiceRow row = _choiceRows[i];
            bool shouldShow = i < choices.Count;
            row.Root.SetActive(shouldShow);
            if (!shouldShow)
            {
                continue;
            }

            bool isSelected = i == selectedIndex;
            row.Background.color = isSelected
                ? new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.3f)
                : new Color(1f, 1f, 1f, 0.05f);
            row.Label.color = isSelected ? _theme.PrimaryText : _theme.SecondaryText;
            row.Label.text = (isSelected ? "> " : string.Empty) + choices[i].ChoiceText;

            row.Button.onClick.RemoveAllListeners();
            int capturedIndex = i;
            row.Button.onClick.AddListener(() => DialogueRunner.Instance?.SelectChoice(capturedIndex));
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
        portraitBackdrop.color = backgroundColor.a > 0f ? backgroundColor : _theme.Slate;
        portraitImage.texture = portraitTexture;
        portraitImage.gameObject.SetActive(portraitTexture != null);
        portraitPlaceholderText.gameObject.SetActive(portraitTexture == null);
        portraitNamePlate.gameObject.SetActive(!string.IsNullOrWhiteSpace(displayName));
        portraitNameText.text = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim();

        if (portraitAspectFitter != null)
        {
            portraitAspectFitter.aspectRatio = portraitTexture != null && portraitTexture.height > 0
                ? portraitTexture.width / (float)portraitTexture.height
                : 1f;
        }
    }

    public void HidePortrait()
    {
        if (portraitRoot != null)
        {
            portraitRoot.SetActive(false);
        }
    }

    private void BuildLayout()
    {
        dialoguePanelRoot = UiFactory.CreateRect(
            "DialoguePanel",
            transform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(1440f, 310f),
            new Vector2(0f, 24f));
        dialogueCanvasGroup = UiFactory.GetOrAddCanvasGroup(dialoguePanelRoot.gameObject);

        UiFactory.CreateImage("DialogueShadow", dialoguePanelRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(26f, 26f), new Vector2(18f, -18f), new Color(0f, 0f, 0f, 0.34f));
        dialogueBackground = UiFactory.CreateImage("DialogueOuter", dialoguePanelRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.03f, 0.04f, 0.06f, 0.94f));
        RectTransform dialogueInner = UiFactory.CreateRect("DialogueInner", dialoguePanelRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-14f, -14f), Vector2.zero);
        UiFactory.CreateImage("DialogueFill", dialogueInner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.1f, 0.14f, 0.96f));
        UiFactory.CreateImage("Accent", dialogueInner, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(3f, -26f), Vector2.zero, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.88f));

        BuildPortrait(dialogueInner);

        speakerNamePlate = UiFactory.CreateImage(
            "SpeakerPlate",
            dialogueInner,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(260f, 40f),
            new Vector2(306f, -28f),
            new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.18f));
        speakerNameText = UiFactory.CreateText(
            "SpeakerName",
            speakerNamePlate.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-22f, -10f),
            Vector2.zero,
            _theme.DisplayFont,
            16,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.Brass);
        speakerNameText.characterSpacing = 3f;

        TMP_Text chapterText = UiFactory.CreateText(
            "ChapterTag",
            dialogueInner,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(200f, 24f),
            new Vector2(-24f, -28f),
            _theme.DisplayFont,
            12,
            FontStyle.Normal,
            TextAnchor.MiddleRight,
            new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.56f));
        chapterText.gameObject.SetActive(false);

        dialogueText = UiFactory.CreateText(
            "DialogueText",
            dialogueInner,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-346f, -126f),
            new Vector2(306f, -76f),
            _theme.BodyFont,
            25,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            _theme.PrimaryText);
        dialogueText.lineSpacing = 10f;

        hintText = UiFactory.CreateText(
            "HintText",
            dialogueInner,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(520f, 26f),
            new Vector2(-26f, 22f),
            _theme.BodyFont,
            14,
            FontStyle.Normal,
            TextAnchor.MiddleRight,
            new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.72f));

        choicePanelRoot = UiFactory.CreateRect(
            "ChoicePanel",
            transform,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(480f, 268f),
            new Vector2(-24f, 352f));
        choicesCanvasGroup = UiFactory.GetOrAddCanvasGroup(choicePanelRoot.gameObject);
        UiFactory.CreateImage("ChoiceShadow", choicePanelRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(22f, 22f), new Vector2(14f, -14f), new Color(0f, 0f, 0f, 0.28f));
        choicesBackground = UiFactory.CreateImage("ChoiceOuter", choicePanelRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.04f, 0.05f, 0.07f, 0.92f));
        RectTransform choiceInner = UiFactory.CreateRect("ChoiceInner", choicePanelRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-12f, -12f), Vector2.zero);
        UiFactory.CreateImage("ChoiceFill", choiceInner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.1f, 0.14f, 0.96f));
        UiFactory.CreateImage("ChoiceAccent", choiceInner, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(3f, -24f), Vector2.zero, new Color(_theme.Moss.r, _theme.Moss.g, _theme.Moss.b, 0.9f));

        choicesPromptText = UiFactory.CreateText(
            "ChoicesPrompt",
            choiceInner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-36f, 26f),
            new Vector2(18f, -18f),
            _theme.DisplayFont,
            14,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.Brass);
        choicesPromptText.characterSpacing = 5f;

        choicesContainer = UiFactory.CreateRect(
            "ChoicesContainer",
            choiceInner,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);
        choicesContainer.offsetMin = new Vector2(18f, 18f);
        choicesContainer.offsetMax = new Vector2(-18f, -52f);

        VerticalLayoutGroup layout = choicesContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
    }

    private void BuildPortrait(RectTransform parent)
    {
        RectTransform portraitFrame = UiFactory.CreateRect(
            "PortraitFrame",
            parent,
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, 0.5f),
            new Vector2(258f, -42f),
            new Vector2(20f, 0f));
        portraitRoot = portraitFrame.gameObject;

        portraitFrameDecoration = UiFactory.CreateImage("PortraitOuter", portraitFrame, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.32f));
        portraitMatteFill = UiFactory.CreateImage("PortraitFrameFill", portraitFrame, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f), Vector2.zero, new Color(0.09f, 0.1f, 0.12f, 0.95f));

        RectTransform artworkMask = UiFactory.CreateRect(
            "PortraitArtworkMask",
            portraitFrame,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(214f, 214f),
            new Vector2(0f, 6f));
        Image mask = artworkMask.gameObject.AddComponent<Image>();
        mask.color = Color.white;
        Mask uiMask = artworkMask.gameObject.AddComponent<Mask>();
        uiMask.showMaskGraphic = false;

        portraitBackdrop = UiFactory.CreateImage("PortraitBackdrop", artworkMask, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.14f, 0.16f, 0.2f, 0.94f));
        portraitImage = UiFactory.CreateRect("PortraitImage", artworkMask, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
        portraitImage.color = Color.white;
        portraitAspectFitter = portraitImage.gameObject.AddComponent<AspectRatioFitter>();
        portraitAspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        portraitAspectFitter.aspectRatio = 1f;

        portraitPlaceholderText = UiFactory.CreateText(
            "PortraitPlaceholder",
            artworkMask,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-30f, -30f),
            Vector2.zero,
            _theme.DisplayFont,
            18,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.5f));
        portraitPlaceholderText.text = string.Empty;
        portraitPlaceholderText.alignment = TextAlignmentOptions.Center;

        portraitNamePlate = UiFactory.CreateImage(
            "PortraitNamePlate",
            portraitFrame,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(206f, 34f),
            new Vector2(0f, 18f),
            new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.18f));
        portraitNameText = UiFactory.CreateText(
            "PortraitName",
            portraitNamePlate.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-20f, -8f),
            Vector2.zero,
            _theme.DisplayFont,
            14,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            _theme.PrimaryText);
    }

    private void EnsureChoiceRows(int requiredCount)
    {
        while (_choiceRows.Count < requiredCount)
        {
            _choiceRows.Add(CreateChoiceRow(_choiceRows.Count));
        }
    }

    private ChoiceRow CreateChoiceRow(int index)
    {
        RectTransform rowRoot = UiFactory.CreateRect(
            "ChoiceRow_" + index,
            choicesContainer,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, 76f),
            Vector2.zero);
        LayoutElement layoutElement = rowRoot.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 76f;

        Image background = UiFactory.CreateImage("Background", rowRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.05f));
        Button button = rowRoot.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        TMP_Text label = UiFactory.CreateText(
            "Label",
            rowRoot,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-28f, -18f),
            Vector2.zero,
            _theme.BodyFont,
            19,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            _theme.SecondaryText);
        label.textWrappingMode = TextWrappingModes.Normal;

        return new ChoiceRow
        {
            Root = rowRoot.gameObject,
            Background = background,
            Label = label,
            Button = button
        };
    }

    private static void SetCanvasGroup(CanvasGroup canvasGroup, bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }
}
