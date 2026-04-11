using DarkVillage.UI.Effects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HudCanvasView : MonoBehaviour
{
    private const float InteractionPromptWidth = 780f;
    private const float InteractionPromptHeight = 180f;
    private const float WorldMarkerWidth = 220f;
    private const float WorldMarkerHeight = 80f;

    private UiTheme _theme;
    private UiStateCoordinator _stateCoordinator;

    [Header("Quest Panel (Top-Right anchored)")]
    [SerializeField] private RectTransform _questPanel;
    [SerializeField] private TMP_Text _questTitleText;
    [SerializeField] private TMP_Text _questBodyText;
    private CanvasGroup _questGroup;

    [Header("Quest Completion Banner")]
    [SerializeField] private RectTransform _completionBanner;
    [SerializeField] private TMP_Text _completionBannerText;
    private CanvasGroup _completionBannerGroup;

    [Header("Interaction Prompt (Bottom hovering)")]
    [SerializeField] private RectTransform _interactionPrompt;
    [SerializeField] private TMP_Text _interactionKeyText;
    [SerializeField] private TMP_Text _interactionTitleText;
    [SerializeField] private TMP_Text _interactionHintText;
    private CanvasGroup _interactionGroup;

    [Header("Combat Panel (Top-Left anchored)")]
    [SerializeField] private RectTransform _combatPanel;
    [SerializeField] private Image _combatEmberGlow;
    [SerializeField] private TMP_Text _playerLineText;
    [SerializeField] private TMP_Text _combatHintText;
    [SerializeField] private TMP_Text _encounterLineText;
    [SerializeField] private TMP_Text _enemyLineText;
    private CanvasGroup _combatGroup;
    private EmberBreathingEffect _combatEmberEffect;

    [Header("World Marker")]
    [SerializeField] private RectTransform _worldMarkerRoot;
    [SerializeField] private RectTransform _worldMarker;
    [SerializeField] private TMP_Text _worldMarkerText;
    private CanvasGroup _worldMarkerGroup;

    public void Initialize(UiTheme theme, RectTransform worldMarkerRoot, UiStateCoordinator stateCoordinator)
    {
        if (_theme != null)
        {
            return;
        }

        _theme = theme != null ? theme : UiTheme.CreateRuntimeDefault();
        _theme.EnsureRuntimeDefaults();
        _worldMarkerRoot = worldMarkerRoot;
        _stateCoordinator = stateCoordinator;

        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            UiFactory.Stretch(root);
        }

        BuildQuestPanel();
        BuildCompletionBanner();
        BuildInteractionPrompt();
        BuildCombatPanel();
        BuildWorldMarker();

        HideQuestPanel();
        HideCompletionBanner();
        HideInteractionPrompt();
        HideCombatPanel();
        HideWorldMarker();

        if (_stateCoordinator != null)
        {
            _stateCoordinator.OnModeChanged += HandleModeChanged;
            HandleModeChanged(_stateCoordinator.CurrentMode);
        }
    }

    private void OnDestroy()
    {
        if (_stateCoordinator != null)
        {
            _stateCoordinator.OnModeChanged -= HandleModeChanged;
        }
    }

    public void SetQuestPanel(bool visible, string title, string body)
    {
        if (_questPanel == null)
        {
            return;
        }

        _questPanel.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        _questTitleText.text = string.IsNullOrWhiteSpace(title) ? "当前目标" : title.Trim();
        _questBodyText.text = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();

        float bodyHeight = Mathf.Max(24f, _questBodyText.preferredHeight);
        float panelHeight = Mathf.Clamp(bodyHeight + 40f, 64f, 118f);
        _questPanel.sizeDelta = new Vector2(_questPanel.sizeDelta.x, panelHeight);
    }

    public void HideQuestPanel()
    {
        if (_questPanel != null)
        {
            _questPanel.gameObject.SetActive(false);
        }
    }

    public void UpdateQuest(string title, string objective)
    {
        bool shouldShow = !string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(objective);
        SetQuestPanel(shouldShow, title, objective);
    }

    public void SetCompletionBanner(bool visible, string body)
    {
        if (_completionBanner == null)
        {
            return;
        }

        _completionBanner.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        _completionBannerText.text = string.IsNullOrWhiteSpace(body) ? "当前目标已完成" : body.Trim();

        float bodyHeight = Mathf.Max(24f, _completionBannerText.preferredHeight);
        float panelHeight = Mathf.Clamp(bodyHeight + 28f, 52f, 96f);
        _completionBanner.sizeDelta = new Vector2(_completionBanner.sizeDelta.x, panelHeight);
    }

    public void HideCompletionBanner()
    {
        if (_completionBanner != null)
        {
            _completionBanner.gameObject.SetActive(false);
        }
    }

    public void SetInteractionPrompt(bool visible, string displayName, string promptText, string keyText)
    {
        if (_interactionPrompt == null)
        {
            return;
        }

        _interactionPrompt.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        _interactionKeyText.text = string.IsNullOrWhiteSpace(keyText) ? "E" : keyText.Trim();
        _interactionTitleText.text = string.IsNullOrWhiteSpace(displayName) ? "悬停物件" : displayName.Trim();
        string prompt = string.IsNullOrWhiteSpace(promptText) ? "进行交互" : promptText.Trim();
        _interactionHintText.text = prompt;
    }

    public void HideInteractionPrompt()
    {
        if (_interactionPrompt != null)
        {
            _interactionPrompt.gameObject.SetActive(false);
        }
    }

    public void ShowInteraction(string prompt, string key)
    {
        SetInteractionPrompt(true, "可交互对象", prompt, key);
        if (_interactionHintText != null)
        {
            _interactionHintText.color = _theme.MossGreen;
        }
    }

    public void HideInteraction()
    {
        HideInteractionPrompt();
    }

    public void SetCombatPanel(bool visible, string playerLine, string controlsLine, string encounterLine, string enemyLine, bool showEncounterDetails)
    {
        if (_combatPanel == null)
        {
            return;
        }

        _combatPanel.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        _playerLineText.text = string.IsNullOrWhiteSpace(playerLine) ? string.Empty : playerLine.Trim();
        _combatHintText.text = string.IsNullOrWhiteSpace(controlsLine) ? string.Empty : controlsLine.Trim();
        _encounterLineText.gameObject.SetActive(showEncounterDetails);
        _enemyLineText.gameObject.SetActive(showEncounterDetails);
        _encounterLineText.text = string.IsNullOrWhiteSpace(encounterLine) ? string.Empty : encounterLine.Trim();
        _enemyLineText.text = string.IsNullOrWhiteSpace(enemyLine) ? string.Empty : enemyLine.Trim();

        float targetHeight = showEncounterDetails ? 118f : 74f;
        _combatPanel.sizeDelta = new Vector2(_combatPanel.sizeDelta.x, targetHeight);

        if (_combatEmberEffect != null)
        {
            _combatEmberEffect.SetPanicMode(showEncounterDetails || !string.IsNullOrWhiteSpace(enemyLine));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_combatPanel);
    }

    public void HideCombatPanel()
    {
        if (_combatPanel != null)
        {
            _combatPanel.gameObject.SetActive(false);
        }

        if (_combatEmberEffect != null)
        {
            _combatEmberEffect.SetPanicMode(false);
        }
    }

    public void SetCombatAlertState(bool isInDanger)
    {
        if (isInDanger)
        {
            SetCombatPanel(true, "危险迫近", "保持移动，准备应战", string.Empty, string.Empty, false);
            if (_combatEmberEffect != null)
            {
                _combatEmberEffect.SetPanicMode(true);
            }
        }
        else
        {
            HideCombatPanel();
        }
    }

    public void SetWorldMarker(bool visible, Camera worldCamera, Vector3 worldPosition, string markerText)
    {
        if (_worldMarker == null)
        {
            return;
        }

        if (!visible || worldCamera == null || _worldMarkerRoot == null)
        {
            HideWorldMarker();
            return;
        }

        Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z <= 0f)
        {
            HideWorldMarker();
            return;
        }

        RectTransform rootRect = _worldMarkerRoot;
        Vector3 clampedScreen = new Vector3(
            Mathf.Clamp(screenPosition.x, 16f, Screen.width - 16f),
            Mathf.Clamp(screenPosition.y, 16f, Screen.height - 96f),
            screenPosition.z);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, clampedScreen, null, out Vector2 localPoint))
        {
            HideWorldMarker();
            return;
        }

        _worldMarker.gameObject.SetActive(true);
        _worldMarker.anchoredPosition = localPoint + new Vector2(0f, 18f);
        _worldMarkerText.text = string.IsNullOrWhiteSpace(markerText) ? "目标" : markerText.Trim();
    }

    public void HideWorldMarker()
    {
        if (_worldMarker != null)
        {
            _worldMarker.gameObject.SetActive(false);
        }
    }

    public void SetWorldMarkerPreview(bool visible, string markerText, Vector2 anchoredPosition)
    {
        if (_worldMarker == null)
        {
            return;
        }

        if (!visible)
        {
            HideWorldMarker();
            return;
        }

        _worldMarker.gameObject.SetActive(true);
        _worldMarker.anchoredPosition = anchoredPosition;
        if (_worldMarkerText != null)
        {
            _worldMarkerText.text = string.IsNullOrWhiteSpace(markerText) ? "目标" : markerText.Trim();
        }
    }

    private void BuildQuestPanel()
    {
        _questPanel = UiFactory.CreateRect(
            "QuestPanel",
            transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(336f, 112f),
            new Vector2(-24f, -24f));
        _questGroup = UiFactory.GetOrAddCanvasGroup(_questPanel.gameObject);
        CreatePanelShadow(_questPanel, new Vector2(14f, -14f), new Vector2(18f, 18f), new Color(0f, 0f, 0f, 0.24f));

        UiFactory.CreateImage("QuestOuter", _questPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.05f, 0.06f, 0.08f, 0.82f));
        RectTransform inner = UiFactory.CreateRect("QuestInner", _questPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-6f, -6f), Vector2.zero);
        UiFactory.CreateImage("QuestInnerFill", inner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.12f, 0.14f, 0.17f, 0.62f));
        CreateAccentLine(inner, 10f);
        CreateCornerBrackets(inner, 10f, 12f, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.48f));

        _questTitleText = UiFactory.CreateText(
            "QuestTitle",
            inner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-28f, 22f),
            new Vector2(0f, -10f),
            _theme.DisplayFont,
            13,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.Brass);

        _questBodyText = UiFactory.CreateText(
            "QuestBody",
            inner,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-28f, -40f),
            new Vector2(0f, -10f),
            _theme.BodyFont,
            16,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            _theme.PrimaryText);
    }

    private void BuildCompletionBanner()
    {
        _completionBanner = UiFactory.CreateRect(
            "CompletionBanner",
            transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(440f, 72f),
            new Vector2(0f, -26f));
        _completionBannerGroup = UiFactory.GetOrAddCanvasGroup(_completionBanner.gameObject);
        CreatePanelShadow(_completionBanner, new Vector2(16f, -14f), new Vector2(22f, 18f), new Color(0f, 0f, 0f, 0.26f));

        UiFactory.CreateImage("CompletionOuter", _completionBanner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.12f, 0.17f, 0.12f, 0.88f));
        RectTransform inner = UiFactory.CreateRect("CompletionInner", _completionBanner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-6f, -6f), Vector2.zero);
        UiFactory.CreateImage("CompletionInnerFill", inner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.2f, 0.28f, 0.18f, 0.76f));
        CreateCornerBrackets(inner, 10f, 12f, new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.35f));

        _completionBannerText = UiFactory.CreateText(
            "CompletionText",
            inner,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-32f, -18f),
            Vector2.zero,
            _theme.BodyFont,
            16,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);
    }

    private void BuildInteractionPrompt()
    {
        _interactionPrompt = UiFactory.CreateRect(
            "InteractionPrompt",
            transform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(InteractionPromptWidth, InteractionPromptHeight),
            new Vector2(0f, 140f));
        _interactionGroup = UiFactory.GetOrAddCanvasGroup(_interactionPrompt.gameObject);
        CreatePanelShadow(_interactionPrompt, new Vector2(16f, -16f), new Vector2(24f, 22f), new Color(0f, 0f, 0f, 0.24f));

        Image interactionOuter = UiFactory.CreateImage("InteractionOuter", _interactionPrompt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(_theme.Charcoal.r, _theme.Charcoal.g, _theme.Charcoal.b, 0.9f));
        Sprite interactionPromptSprite = _theme.InteractionPromptSprite;
        if (interactionPromptSprite != null)
        {
            UiFactory.ApplySprite(interactionOuter, interactionPromptSprite);
            interactionOuter.color = Color.white;
        }
        RectTransform inner = UiFactory.CreateRect("InteractionInner", _interactionPrompt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f), Vector2.zero);
        UiFactory.CreateImage("InteractionInnerFill", inner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.12f, 0.14f, 0.17f, 0.7f));
        CreateAccentLine(inner, 22f);
        CreateCornerBrackets(inner, 18f, 16f, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.4f));

        RectTransform keyBadgeFrame = UiFactory.CreateRect(
            "KeyBadge",
            inner,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(86f, 86f),
            new Vector2(44f, -10f));
        Image keyBadgeOuter = UiFactory.CreateImage("KeyBadgeOuter", keyBadgeFrame, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.52f));
        Image keyBadgeInner = UiFactory.CreateImage("KeyBadgeInner", keyBadgeFrame, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-4f, -4f), Vector2.zero, new Color(_theme.Slate.r, _theme.Slate.g, _theme.Slate.b, 0.98f));
        Sprite keycapBadgeSprite = _theme.KeycapBadgeSprite;
        if (keycapBadgeSprite != null)
        {
            UiFactory.ApplySprite(keyBadgeOuter, keycapBadgeSprite);
            keyBadgeOuter.color = Color.white;
            keyBadgeInner.color = new Color(0f, 0f, 0f, 0f);
        }
        else
        {
            CreateTypewriterBadgeTicks(keyBadgeFrame, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.28f));
        }

        _interactionKeyText = UiFactory.CreateText(
            "KeyText",
            keyBadgeFrame,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-10f, -8f),
            Vector2.zero,
            _theme.DisplayFont,
            28,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            _theme.WaxWhiteText);
        _interactionKeyText.characterSpacing = 3f;

        _interactionTitleText = UiFactory.CreateText(
            "InteractionTitle",
            inner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-170f, 36f),
            new Vector2(90f, -18f),
            _theme.DisplayFont,
            24,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            _theme.MutedBrass);
        _interactionTitleText.characterSpacing = 3f;

        _interactionHintText = UiFactory.CreateText(
            "InteractionHint",
            inner,
            new Vector2(0f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-170f, 54f),
            new Vector2(90f, -20f),
            _theme.BodyFont,
            28,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            _theme.MossGreen);
        _interactionHintText.lineSpacing = 4f;
    }

    private void BuildCombatPanel()
    {
        _combatPanel = UiFactory.CreateRect(
            "CombatPanel",
            transform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(336f, 118f),
            new Vector2(18f, -18f));
        _combatGroup = UiFactory.GetOrAddCanvasGroup(_combatPanel.gameObject);
        CreatePanelShadow(_combatPanel, new Vector2(14f, -14f), new Vector2(18f, 18f), new Color(0f, 0f, 0f, 0.28f));

        _combatEmberGlow = UiFactory.CreateImage(
            "CombatEmberGlow",
            _combatPanel,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-18f, -18f),
            Vector2.zero,
            new Color(_theme.EmberRed.r, _theme.EmberRed.g, _theme.EmberRed.b, 0.22f));
        Sprite combatEmberSprite = _theme.CombatEmberGlowSprite;
        if (combatEmberSprite != null)
        {
            UiFactory.ApplySprite(_combatEmberGlow, combatEmberSprite);
            _combatEmberGlow.color = new Color(_theme.EmberRed.r, _theme.EmberRed.g, _theme.EmberRed.b, 0.58f);
        }

        Image combatOuter = UiFactory.CreateImage("CombatOuter", _combatPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.04f, 0.05f, 0.07f, 0.76f));
        Sprite combatPanelSprite = _theme.CombatPanelSprite;
        if (combatPanelSprite != null)
        {
            UiFactory.ApplySprite(combatOuter, combatPanelSprite);
            combatOuter.color = Color.white;
        }
        RectTransform inner = UiFactory.CreateRect("CombatInner", _combatPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-6f, -6f), Vector2.zero);
        UiFactory.CreateImage("CombatInnerFill", inner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.12f, 0.14f, 0.17f, 0.62f));
        CreateAccentLine(inner, 10f);
        CreateCornerBrackets(inner, 10f, 12f, new Color(_theme.Ember.r, _theme.Ember.g, _theme.Ember.b, 0.4f));
        _combatEmberGlow.transform.SetSiblingIndex(1);
        _combatEmberGlow.raycastTarget = false;
        _combatEmberEffect = _combatEmberGlow.gameObject.GetComponent<EmberBreathingEffect>();
        if (_combatEmberEffect == null)
        {
            _combatEmberEffect = _combatEmberGlow.gameObject.AddComponent<EmberBreathingEffect>();
        }
        _combatEmberEffect.Configure(1.35f, 0.06f, 0.22f);
        _combatEmberEffect.SetBaseColor(_combatEmberGlow.color);
        _combatEmberEffect.SetPanicMode(false);

        VerticalLayoutGroup layout = inner.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 10);
        layout.spacing = 4f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        _playerLineText = CreateCombatLine(inner, 18, FontStyle.Bold, _theme.PrimaryText);
        _combatHintText = CreateCombatLine(inner, 14, FontStyle.Normal, _theme.SecondaryText);
        _encounterLineText = CreateCombatLine(inner, 14, FontStyle.Normal, _theme.SecondaryText);
        _enemyLineText = CreateCombatLine(inner, 14, FontStyle.Normal, _theme.SecondaryText);
    }

    private void BuildWorldMarker()
    {
        if (_worldMarkerRoot == null)
        {
            return;
        }

        _worldMarker = UiFactory.CreateRect(
            "WorldMarker",
            _worldMarkerRoot,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(WorldMarkerWidth, WorldMarkerHeight),
            Vector2.zero);
        _worldMarkerGroup = UiFactory.GetOrAddCanvasGroup(_worldMarker.gameObject);

        UiFactory.CreateImage("WorldMarkerShadow", _worldMarker, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(12f, 10f), new Vector2(0f, -2f), new Color(0f, 0f, 0f, 0.18f));
        Image worldMarkerBg = UiFactory.CreateImage("WorldMarkerBg", _worldMarker, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(_theme.Charcoal.r, _theme.Charcoal.g, _theme.Charcoal.b, 0.82f));
        Sprite markerChipSprite = _theme.MarkerChipSprite;
        if (markerChipSprite != null)
        {
            UiFactory.ApplySprite(worldMarkerBg, markerChipSprite);
            worldMarkerBg.color = Color.white;
        }
        UiFactory.CreateImage("WorldMarkerInset", _worldMarker, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-4f, -4f), Vector2.zero, new Color(_theme.Slate.r, _theme.Slate.g, _theme.Slate.b, 0.5f));
        UiFactory.CreateImage("SmokeTop", _worldMarker, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-14f, 10f), new Vector2(0f, -2f), new Color(0f, 0f, 0f, 0.18f));
        UiFactory.CreateImage("SmokeBottom", _worldMarker, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-20f, 8f), new Vector2(0f, 2f), new Color(0f, 0f, 0f, 0.12f));
        CreateMarkerIcon(_worldMarker, new Vector2(26f, 0f));
        _worldMarkerText = UiFactory.CreateText(
            "WorldMarkerText",
            _worldMarker,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-70f, -12f),
            new Vector2(30f, 0f),
            _theme.DisplayFont,
            20,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            _theme.WaxWhiteTextSoft);
        _worldMarkerText.characterSpacing = 1.5f;
    }

    private TMP_Text CreateCombatLine(Transform parent, int fontSize, FontStyle fontStyle, Color color)
    {
        TMP_Text line = UiFactory.CreateText(
            "Line",
            parent,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 24f),
            Vector2.zero,
            _theme.BodyFont,
            fontSize,
            fontStyle,
            TextAnchor.MiddleLeft,
            color);

        LayoutElement layoutElement = line.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 22f;
        return line;
    }

    private void HandleModeChanged(UiStateCoordinator.UiMode mode)
    {
        SetPanelAlpha(_questGroup, mode == UiStateCoordinator.UiMode.Dialogue ? 0.45f : mode == UiStateCoordinator.UiMode.ChapterComplete ? 0f : 1f);
        SetPanelAlpha(_completionBannerGroup, mode == UiStateCoordinator.UiMode.ChapterComplete ? 0f : 1f);
        SetPanelAlpha(_interactionGroup, mode == UiStateCoordinator.UiMode.Dialogue || mode == UiStateCoordinator.UiMode.ChapterComplete ? 0f : 1f);
        SetPanelAlpha(_combatGroup, mode == UiStateCoordinator.UiMode.Dialogue ? 0.4f : mode == UiStateCoordinator.UiMode.ChapterComplete ? 0f : 1f);
        SetPanelAlpha(_worldMarkerGroup, mode == UiStateCoordinator.UiMode.Dialogue || mode == UiStateCoordinator.UiMode.ChapterComplete ? 0.55f : 1f);
    }

    private void SetPanelAlpha(CanvasGroup group, float alpha)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = alpha;
    }

    private void CreateAccentLine(Transform parent, float leftInset)
    {
        Image line = UiFactory.CreateImage(
            "AccentLine",
            parent,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-(leftInset * 2f), 2f),
            new Vector2(0f, -1f),
            new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.75f));
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

    private void CreateCornerBrackets(Transform parent, float inset, float length, Color color)
    {
        CreateCornerBracket(parent, "TopLeft", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), inset, -inset, length, 2f, color);
        CreateCornerBracket(parent, "TopRight", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), -inset, -inset, length, 2f, color);
        CreateCornerBracket(parent, "BottomLeft", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), inset, inset, length, 2f, color);
        CreateCornerBracket(parent, "BottomRight", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), -inset, inset, length, 2f, color);
    }

    private void CreateCornerBracket(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, float x, float y, float length, float thickness, Color color)
    {
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

    private void CreateTypewriterBadgeTicks(Transform parent, Color color)
    {
        Image topTick = UiFactory.CreateImage(
            "BadgeTickTop",
            parent,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(30f, 2f),
            new Vector2(0f, -10f),
            color);
        topTick.raycastTarget = false;

        Image bottomTick = UiFactory.CreateImage(
            "BadgeTickBottom",
            parent,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(30f, 2f),
            new Vector2(0f, 10f),
            color);
        bottomTick.raycastTarget = false;
    }

    private void CreateMarkerIcon(Transform parent, Vector2 anchoredPosition)
    {
        RectTransform iconRoot = UiFactory.CreateRect(
            "MarkerIcon",
            parent,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(28f, 28f),
            anchoredPosition);

        Image diamond = UiFactory.CreateImage(
            "MarkerDiamond",
            iconRoot,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(14f, 14f),
            Vector2.zero,
            new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.78f));
        diamond.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
        diamond.raycastTarget = false;

        Image crossH = UiFactory.CreateImage(
            "MarkerCrossH",
            iconRoot,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(22f, 1.5f),
            Vector2.zero,
            new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.38f));
        crossH.raycastTarget = false;

        Image crossV = UiFactory.CreateImage(
            "MarkerCrossV",
            iconRoot,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(1.5f, 22f),
            Vector2.zero,
            new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.38f));
        crossV.raycastTarget = false;
    }
}
