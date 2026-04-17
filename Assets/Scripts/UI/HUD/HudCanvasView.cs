using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HudCanvasView : MonoBehaviour
{
    private UiTheme _theme;
    private UiStateCoordinator _stateCoordinator;
    private bool _combatDanger;

    [Header("Quest Dock")]
    [SerializeField] private RectTransform _questPanel;
    [SerializeField] private TMP_Text _questTitleText;
    [SerializeField] private TMP_Text _questBodyText;
    private CanvasGroup _questGroup;

    [Header("Completion Toast")]
    [SerializeField] private RectTransform _completionBanner;
    [SerializeField] private TMP_Text _completionBannerText;
    private CanvasGroup _completionBannerGroup;

    [Header("Interaction Prompt")]
    [SerializeField] private RectTransform _interactionPrompt;
    [SerializeField] private TMP_Text _interactionKeyText;
    [SerializeField] private TMP_Text _interactionTitleText;
    [SerializeField] private TMP_Text _interactionHintText;
    private CanvasGroup _interactionGroup;

    [Header("Status Dock")]
    [SerializeField] private RectTransform _statusPanel;
    [SerializeField] private TMP_Text _statusTitleText;
    [SerializeField] private TMP_Text _statusBodyText;
    [SerializeField] private TMP_Text _statusMetaText;
    [SerializeField] private Image _statusHealthFill;
    private CanvasGroup _statusGroup;

    [Header("Environment Dock")]
    [SerializeField] private RectTransform _environmentPanel;
    [SerializeField] private TMP_Text _environmentIconText;
    [SerializeField] private TMP_Text _environmentTitleText;
    [SerializeField] private TMP_Text _environmentBodyText;
    private CanvasGroup _environmentGroup;

    [Header("Combat Dock")]
    [SerializeField] private RectTransform _combatPanel;
    [SerializeField] private TMP_Text _combatEncounterText;
    [SerializeField] private TMP_Text _combatControlsText;
    [SerializeField] private TMP_Text _combatPlayerText;
    [SerializeField] private TMP_Text _combatEnemyText;
    [SerializeField] private Image _combatPlayerFill;
    [SerializeField] private Image _combatEnemyFill;
    [SerializeField] private Image _combatDangerGlow;
    private CanvasGroup _combatGroup;

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

        BuildLayout();
        HideQuestPanel();
        HideCompletionBanner();
        HideInteractionPrompt();
        HideStatusPanel();
        HideEnvironmentPanel();
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
        _questBodyText.text = string.IsNullOrWhiteSpace(body) ? "继续探索村庄深处。" : body.Trim();

        float bodyHeight = Mathf.Clamp(_questBodyText.preferredHeight, 30f, 88f);
        _questPanel.sizeDelta = new Vector2(_questPanel.sizeDelta.x, bodyHeight + 74f);
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

        _completionBannerText.text = string.IsNullOrWhiteSpace(body) ? "任务已推进" : body.Trim();
        float bodyHeight = Mathf.Clamp(_completionBannerText.preferredHeight, 18f, 44f);
        _completionBanner.sizeDelta = new Vector2(_completionBanner.sizeDelta.x, bodyHeight + 24f);
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
        _interactionTitleText.text = string.IsNullOrWhiteSpace(displayName) ? "可交互对象" : displayName.Trim();
        _interactionHintText.text = string.IsNullOrWhiteSpace(promptText) ? "进行交互" : promptText.Trim();
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
    }

    public void HideInteraction()
    {
        HideInteractionPrompt();
    }

    public void SetStatusPanel(bool visible, string title, string body)
    {
        if (_statusPanel == null)
        {
            return;
        }

        _statusPanel.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        _statusTitleText.text = string.IsNullOrWhiteSpace(title) ? "旅者状态" : title.Trim();

        if (TryExtractFraction(body, out float normalized, out string compactValue))
        {
            _statusBodyText.text = "生命";
            _statusMetaText.text = compactValue;
            _statusHealthFill.fillAmount = normalized;
        }
        else
        {
            _statusBodyText.text = string.IsNullOrWhiteSpace(body) ? "生命状态未知" : body.Trim();
            _statusMetaText.text = "监测中";
            _statusHealthFill.fillAmount = 0.55f;
        }
    }

    public void HideStatusPanel()
    {
        if (_statusPanel != null)
        {
            _statusPanel.gameObject.SetActive(false);
        }
    }

    public void SetEnvironmentPanel(bool visible, string icon, string title, string body)
    {
        if (_environmentPanel == null)
        {
            return;
        }

        _environmentPanel.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        _environmentIconText.text = string.IsNullOrWhiteSpace(icon) ? "●" : icon.Trim();
        _environmentTitleText.text = string.IsNullOrWhiteSpace(title) ? "区域状态" : title.Trim();
        _environmentBodyText.text = string.IsNullOrWhiteSpace(body) ? "未进入光区" : body.Trim();
    }

    public void HideEnvironmentPanel()
    {
        if (_environmentPanel != null)
        {
            _environmentPanel.gameObject.SetActive(false);
        }
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

        _combatEncounterText.text = string.IsNullOrWhiteSpace(encounterLine) ? "战斗已触发" : encounterLine.Trim();
        _combatControlsText.gameObject.SetActive(showEncounterDetails);
        _combatControlsText.text = string.IsNullOrWhiteSpace(controlsLine) ? "Space / J / 鼠标左键 攻击" : controlsLine.Trim();
        _combatPlayerText.text = string.IsNullOrWhiteSpace(playerLine) ? "玩家状态未知" : playerLine.Trim();
        _combatEnemyText.text = string.IsNullOrWhiteSpace(enemyLine) ? "敌人状态未知" : enemyLine.Trim();

        _combatPlayerFill.fillAmount = TryExtractFraction(playerLine, out float playerNormalized, out _) ? playerNormalized : 0.65f;
        _combatEnemyFill.fillAmount = TryExtractFraction(enemyLine, out float enemyNormalized, out _) ? enemyNormalized : 0.8f;

        RefreshCombatTint();
    }

    public void HideCombatPanel()
    {
        if (_combatPanel != null)
        {
            _combatPanel.gameObject.SetActive(false);
        }
    }

    public void SetCombatAlertState(bool isInDanger)
    {
        _combatDanger = isInDanger;
        RefreshCombatTint();
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

        Vector3 clampedScreen = new Vector3(
            Mathf.Clamp(screenPosition.x, 26f, Screen.width - 26f),
            Mathf.Clamp(screenPosition.y, 34f, Screen.height - 110f),
            screenPosition.z);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_worldMarkerRoot, clampedScreen, null, out Vector2 localPoint))
        {
            HideWorldMarker();
            return;
        }

        _worldMarker.gameObject.SetActive(true);
        _worldMarker.anchoredPosition = localPoint + new Vector2(0f, 30f);
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
        _worldMarkerText.text = string.IsNullOrWhiteSpace(markerText) ? "目标" : markerText.Trim();
    }

    private void BuildLayout()
    {
        BuildStatusPanel();
        BuildEnvironmentPanel();
        BuildQuestPanel();
        BuildCompletionBanner();
        BuildInteractionPrompt();
        BuildCombatPanel();
        BuildWorldMarker();
    }

    private void BuildStatusPanel()
    {
        RectTransform inner = CreateCard(
            "StatusPanel",
            transform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(266f, 104f),
            new Vector2(22f, -20f),
            new Color(0.03f, 0.04f, 0.06f, 0.92f),
            new Color(0.09f, 0.11f, 0.15f, 0.94f),
            _theme.ChoicePanelSprite,
            out _statusPanel);
        _statusGroup = UiFactory.GetOrAddCanvasGroup(_statusPanel.gameObject);

        TMP_Text eyebrow = CreateEyebrow(inner, "Eyebrow", string.Empty, new Vector2(0f, -18f));
        eyebrow.gameObject.SetActive(false);

        _statusTitleText = UiFactory.CreateText(
            "StatusTitle",
            inner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-34f, 24f),
            new Vector2(18f, -32f),
            _theme.BodyFont,
            19,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);

        _statusBodyText = UiFactory.CreateText(
            "StatusBody",
            inner,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(52f, 16f),
            new Vector2(18f, 30f),
            _theme.BodyFont,
            13,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.SecondaryText);

        RectTransform barRoot = UiFactory.CreateRect(
            "StatusHealthBar",
            inner,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
            new Vector2(-36f, 8f),
            new Vector2(18f, 12f));
        Image barBackground = UiFactory.CreateImage("BarBackground", barRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
        barBackground.raycastTarget = false;
        _statusHealthFill = UiFactory.CreateImage("BarFill", barRoot, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.77f, 0.29f, 0.24f, 0.96f));
        _statusHealthFill.type = Image.Type.Filled;
        _statusHealthFill.fillMethod = Image.FillMethod.Horizontal;
        _statusHealthFill.fillOrigin = 0;

        _statusMetaText = UiFactory.CreateText(
            "StatusMeta",
            inner,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(92f, 18f),
            new Vector2(-18f, 30f),
            _theme.BodyFont,
            12,
            FontStyle.Bold,
            TextAnchor.MiddleRight,
            _theme.Brass);
    }

    private void BuildQuestPanel()
    {
        RectTransform inner = CreateCard(
            "QuestPanel",
            transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(332f, 126f),
            new Vector2(-22f, -20f),
            new Color(0.04f, 0.05f, 0.08f, 0.9f),
            new Color(0.09f, 0.1f, 0.14f, 0.95f),
            _theme.ChoicePanelSprite,
            out _questPanel);
        _questGroup = UiFactory.GetOrAddCanvasGroup(_questPanel.gameObject);

        TMP_Text eyebrow = CreateEyebrow(inner, "Eyebrow", string.Empty, new Vector2(0f, -18f));
        eyebrow.gameObject.SetActive(false);

        _questTitleText = UiFactory.CreateText(
            "QuestTitle",
            inner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-34f, 26f),
            new Vector2(18f, -40f),
            _theme.BodyFont,
            18,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);

        _questBodyText = UiFactory.CreateText(
            "QuestBody",
            inner,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-36f, -68f),
            new Vector2(18f, -66f),
            _theme.BodyFont,
            16,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            _theme.SecondaryText);
        _questBodyText.lineSpacing = 4f;
    }

    private void BuildEnvironmentPanel()
    {
        RectTransform inner = CreateCard(
            "EnvironmentPanel",
            transform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(266f, 82f),
            new Vector2(22f, -132f),
            new Color(0.03f, 0.04f, 0.06f, 0.88f),
            new Color(0.09f, 0.11f, 0.15f, 0.92f),
            _theme.ChoicePanelSprite,
            out _environmentPanel);
        _environmentGroup = UiFactory.GetOrAddCanvasGroup(_environmentPanel.gameObject);

        _environmentIconText = UiFactory.CreateText(
            "EnvironmentIcon",
            inner,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(34f, 34f),
            new Vector2(26f, 0f),
            _theme.BodyFont,
            24,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            _theme.Brass);

        _environmentTitleText = UiFactory.CreateText(
            "EnvironmentTitle",
            inner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-74f, 20f),
            new Vector2(58f, -22f),
            _theme.DisplayFont,
            16,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);

        _environmentBodyText = UiFactory.CreateText(
            "EnvironmentBody",
            inner,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-74f, -36f),
            new Vector2(58f, -28f),
            _theme.BodyFont,
            13,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            _theme.SecondaryText);
    }

    private void BuildCompletionBanner()
    {
        RectTransform inner = CreateCard(
            "CompletionBanner",
            transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(420f, 68f),
            new Vector2(0f, -22f),
            new Color(0.12f, 0.17f, 0.12f, 0.92f),
            new Color(0.18f, 0.24f, 0.16f, 0.95f),
            _theme.ChoicePanelSprite,
            out _completionBanner);
        _completionBannerGroup = UiFactory.GetOrAddCanvasGroup(_completionBanner.gameObject);

        TMP_Text eyebrow = CreateEyebrow(inner, "Eyebrow", string.Empty, new Vector2(0f, -14f));
        eyebrow.gameObject.SetActive(false);

        _completionBannerText = UiFactory.CreateText(
            "CompletionText",
            inner,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-34f, -24f),
            new Vector2(0f, 4f),
            _theme.BodyFont,
            16,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            _theme.PrimaryText);
    }

    private void BuildInteractionPrompt()
    {
        RectTransform inner = CreateCard(
            "InteractionPrompt",
            transform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(610f, 92f),
            new Vector2(0f, 26f),
            new Color(0.02f, 0.03f, 0.05f, 0.92f),
            new Color(0.08f, 0.09f, 0.12f, 0.94f),
            _theme.InteractionPromptSprite,
            out _interactionPrompt);
        _interactionGroup = UiFactory.GetOrAddCanvasGroup(_interactionPrompt.gameObject);

        RectTransform keyPlate = UiFactory.CreateRect(
            "KeyPlate",
            inner,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(64f, 64f),
            new Vector2(18f, 0f));
        Image keyPlateOuter = UiFactory.CreateImage("KeyPlateOuter", keyPlate, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.76f));
        if (_theme.KeycapBadgeSprite != null)
        {
            UiFactory.ApplySprite(keyPlateOuter, _theme.KeycapBadgeSprite);
            keyPlateOuter.color = Color.white;
        }
        UiFactory.CreateImage("KeyPlateInner", keyPlate, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-6f, -6f), Vector2.zero, new Color(0.12f, 0.1f, 0.08f, 0.98f));
        _interactionKeyText = UiFactory.CreateText(
            "KeyText",
            keyPlate,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-10f, -10f),
            Vector2.zero,
            _theme.DisplayFont,
            22,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            _theme.PrimaryText);
        _interactionKeyText.characterSpacing = 3f;

        _interactionTitleText = UiFactory.CreateText(
            "InteractionTitle",
            inner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-112f, 20f),
            new Vector2(92f, -18f),
            _theme.DisplayFont,
            14,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.Brass);
        _interactionTitleText.characterSpacing = 3f;

        _interactionHintText = UiFactory.CreateText(
            "InteractionHint",
            inner,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0.5f),
            new Vector2(-112f, -26f),
            new Vector2(92f, -8f),
            _theme.BodyFont,
            18,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);
    }

    private void BuildCombatPanel()
    {
        RectTransform inner = CreateCard(
            "CombatPanel",
            transform,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(344f, 176f),
            new Vector2(-22f, 24f),
            new Color(0.09f, 0.03f, 0.03f, 0.94f),
            new Color(0.14f, 0.05f, 0.06f, 0.95f),
            _theme.CombatPanelSprite,
            out _combatPanel);
        _combatGroup = UiFactory.GetOrAddCanvasGroup(_combatPanel.gameObject);

        _combatDangerGlow = UiFactory.CreateImage(
            "DangerGlow",
            inner,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(18f, 18f),
            Vector2.zero,
            new Color(0.7f, 0.19f, 0.16f, 0.12f));
        if (_theme.CombatEmberGlowSprite != null)
        {
            UiFactory.ApplySprite(_combatDangerGlow, _theme.CombatEmberGlowSprite, Image.Type.Simple);
            _combatDangerGlow.color = Color.white;
        }
        _combatDangerGlow.transform.SetAsFirstSibling();

        TMP_Text eyebrow = CreateEyebrow(inner, "Eyebrow", string.Empty, new Vector2(0f, -18f));
        eyebrow.gameObject.SetActive(false);

        _combatEncounterText = UiFactory.CreateText(
            "EncounterText",
            inner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-34f, 24f),
            new Vector2(18f, -38f),
            _theme.DisplayFont,
            17,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);

        _combatPlayerText = UiFactory.CreateText(
            "PlayerText",
            inner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-34f, 18f),
            new Vector2(18f, -58f),
            _theme.BodyFont,
            14,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);
        _combatPlayerFill = CreateFilledBar(inner, "PlayerBar", new Vector2(18f, -82f), new Vector2(-34f, 6f), new Color(0.86f, 0.33f, 0.3f, 0.95f));

        _combatEnemyText = UiFactory.CreateText(
            "EnemyText",
            inner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-34f, 18f),
            new Vector2(18f, -100f),
            _theme.BodyFont,
            14,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);
        _combatEnemyFill = CreateFilledBar(inner, "EnemyBar", new Vector2(18f, -124f), new Vector2(-34f, 6f), new Color(0.92f, 0.59f, 0.28f, 0.95f));

        _combatControlsText = UiFactory.CreateText(
            "ControlsText",
            inner,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
            new Vector2(-34f, 22f),
            new Vector2(18f, 8f),
            _theme.BodyFont,
            12,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.78f));
    }

    private void BuildWorldMarker()
    {
        if (_worldMarkerRoot == null)
        {
            return;
        }

        RectTransform inner = CreateCard(
            "WorldMarker",
            _worldMarkerRoot,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(204f, 62f),
            Vector2.zero,
            new Color(0.03f, 0.04f, 0.05f, 0.9f),
            new Color(0.09f, 0.1f, 0.12f, 0.92f),
            _theme.MarkerChipSprite,
            out _worldMarker);
        _worldMarkerGroup = UiFactory.GetOrAddCanvasGroup(_worldMarker.gameObject);

        RectTransform iconRoot = UiFactory.CreateRect(
            "MarkerSigil",
            inner,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(28f, 28f),
            new Vector2(26f, 0f));
        Image diamond = UiFactory.CreateImage("MarkerDiamond", iconRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(16f, 16f), Vector2.zero, _theme.Brass);
        diamond.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);

        _worldMarkerText = UiFactory.CreateText(
            "WorldMarkerText",
            inner,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-60f, -14f),
            new Vector2(20f, 0f),
            _theme.DisplayFont,
            16,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            _theme.PrimaryText);
    }

    private void HandleModeChanged(UiStateCoordinator.UiMode mode)
    {
        SetPanelAlpha(_questGroup, mode == UiStateCoordinator.UiMode.Dialogue ? 0.38f : mode == UiStateCoordinator.UiMode.Inventory ? 0.24f : mode == UiStateCoordinator.UiMode.ChapterComplete ? 0f : 1f);
        SetPanelAlpha(_completionBannerGroup, mode == UiStateCoordinator.UiMode.ChapterComplete ? 0f : 1f);
        SetPanelAlpha(_interactionGroup, mode == UiStateCoordinator.UiMode.Dialogue || mode == UiStateCoordinator.UiMode.Inventory || mode == UiStateCoordinator.UiMode.ChapterComplete ? 0f : 1f);
        SetPanelAlpha(_statusGroup, mode == UiStateCoordinator.UiMode.Dialogue ? 0.38f : mode == UiStateCoordinator.UiMode.Inventory ? 0.5f : mode == UiStateCoordinator.UiMode.ChapterComplete ? 0f : 1f);
        SetPanelAlpha(_combatGroup, mode == UiStateCoordinator.UiMode.Combat ? 1f : mode == UiStateCoordinator.UiMode.Dialogue || mode == UiStateCoordinator.UiMode.Inventory ? 0.2f : mode == UiStateCoordinator.UiMode.ChapterComplete ? 0f : 1f);
        SetPanelAlpha(_worldMarkerGroup, mode == UiStateCoordinator.UiMode.Dialogue ? 0.45f : mode == UiStateCoordinator.UiMode.Inventory ? 0.32f : mode == UiStateCoordinator.UiMode.ChapterComplete ? 0.12f : 1f);
    }

    private void RefreshCombatTint()
    {
        if (_combatDangerGlow == null)
        {
            return;
        }

        _combatDangerGlow.color = _combatDanger
            ? new Color(0.72f, 0.2f, 0.17f, 0.28f)
            : new Color(0.56f, 0.26f, 0.18f, 0.1f);
    }

    private RectTransform CreateCard(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta,
        Vector2 anchoredPosition,
        Color outerColor,
        Color innerColor,
        Sprite chromeSprite,
        out RectTransform root)
    {
        root = UiFactory.CreateRect(name, parent, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
        CreateShadow(root, new Vector2(18f, -18f), new Vector2(24f, 24f), new Color(0f, 0f, 0f, 0.26f));
        root.SetAsLastSibling();

        Image outer = UiFactory.CreateImage("Outer", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, outerColor);
        outer.raycastTarget = false;
        UiComponentCatalog.ApplyChrome(outer, chromeSprite, outerColor);
        Image frame = UiFactory.CreateImage("Frame", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-2f, -2f), Vector2.zero, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.2f));
        frame.raycastTarget = false;
        if (chromeSprite != null)
        {
            frame.color = new Color(1f, 1f, 1f, 0f);
        }

        RectTransform inner = UiFactory.CreateRect("Inner", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-12f, -12f), Vector2.zero);
        Image fill = UiFactory.CreateImage("Fill", inner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            chromeSprite != null ? new Color(innerColor.r, innerColor.g, innerColor.b, innerColor.a * 0.45f) : innerColor);
        fill.raycastTarget = false;
        UiFactory.CreateImage("AccentLine", inner, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, -18f), new Vector2(0f, 0f), new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.84f));
        return inner;
    }

    private TMP_Text CreateEyebrow(Transform parent, string name, string content, Vector2 anchoredPosition)
    {
        TMP_Text text = UiFactory.CreateText(
            name,
            parent,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-40f, 22f),
            new Vector2(22f, anchoredPosition.y),
            _theme.DisplayFont,
            13,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.84f));
        text.text = content;
        return text;
    }

    private Image CreateFilledBar(Transform parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta, Color fillColor)
    {
        RectTransform barRoot = UiFactory.CreateRect(
            name,
            parent,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            sizeDelta,
            anchoredPosition);
        UiFactory.CreateImage("Background", barRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
        Image fill = UiFactory.CreateImage("Fill", barRoot, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, fillColor);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        return fill;
    }

    private void CreateShadow(RectTransform panel, Vector2 offset, Vector2 expand, Color color)
    {
        Image shadow = UiFactory.CreateImage("Shadow", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), expand, offset, color);
        shadow.raycastTarget = false;
        shadow.transform.SetAsFirstSibling();
    }

    private void SetPanelAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
        {
            group.alpha = alpha;
        }
    }

    private static bool TryExtractFraction(string text, out float normalized, out string compactValue)
    {
        normalized = 1f;
        compactValue = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        int slashIndex = text.IndexOf('/');
        if (slashIndex <= 0 || slashIndex >= text.Length - 1)
        {
            return false;
        }

        int leftEnd = slashIndex - 1;
        while (leftEnd >= 0 && !char.IsDigit(text[leftEnd]))
        {
            leftEnd--;
        }

        if (leftEnd < 0)
        {
            return false;
        }

        int leftStart = leftEnd;
        while (leftStart >= 0 && char.IsDigit(text[leftStart]))
        {
            leftStart--;
        }

        int rightStart = slashIndex + 1;
        while (rightStart < text.Length && !char.IsDigit(text[rightStart]))
        {
            rightStart++;
        }

        if (rightStart >= text.Length)
        {
            return false;
        }

        int rightEnd = rightStart;
        while (rightEnd < text.Length && char.IsDigit(text[rightEnd]))
        {
            rightEnd++;
        }

        string leftValue = text.Substring(leftStart + 1, leftEnd - leftStart);
        string rightValue = text.Substring(rightStart, rightEnd - rightStart);
        if (!int.TryParse(leftValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int current))
        {
            return false;
        }

        if (!int.TryParse(rightValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int maximum) || maximum <= 0)
        {
            return false;
        }

        normalized = Mathf.Clamp01(current / (float)maximum);
        compactValue = current.ToString(CultureInfo.InvariantCulture) + " / " + maximum.ToString(CultureInfo.InvariantCulture);
        return true;
    }
}
