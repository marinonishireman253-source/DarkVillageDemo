using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class FloorSummaryPanel : MonoBehaviour
{
    private const string RiskChoiceLabel = "选择了风险之路";
    private const string SafeChoiceLabel = "选择了安全之路";
    private const string RiskNarrative = "你选择直面灰烬。代价刻在身上，但你带走了一片真相的碎片。";
    private const string SafeNarrative = "你绕过了深渊的边缘。安全抵达——却把一些东西永远地留在了身后。";

    public readonly struct SummaryData
    {
        public SummaryData(string title, string collectionLine, string choiceLine, string narrativeLine, string continueLabel)
        {
            Title = title;
            CollectionLine = collectionLine;
            ChoiceLine = choiceLine;
            NarrativeLine = narrativeLine;
            ContinueLabel = continueLabel;
        }

        public string Title { get; }
        public string CollectionLine { get; }
        public string ChoiceLine { get; }
        public string NarrativeLine { get; }
        public string ContinueLabel { get; }
    }

    public static FloorSummaryPanel Instance { get; private set; }
    public static bool IsVisible => Instance != null && Instance._isVisible;
    public static event Action<bool> OnVisibilityChanged;

    private UiTheme _theme;
    private CanvasGroup _canvasGroup;
    private TMP_Text _titleText;
    private TMP_Text _collectionValueText;
    private TMP_Text _choiceValueText;
    private TMP_Text _narrativeText;
    private Button _continueButton;
    private TMP_Text _continueLabel;

    private Action _continueAction;
    private Coroutine _fadeRoutine;
    private bool _isVisible;
    private int _openedFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (_isVisible)
            {
                OnVisibilityChanged?.Invoke(false);
            }

            Instance = null;
        }
    }

    private void Update()
    {
        if (!_isVisible || Time.frameCount <= _openedFrame || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame
            || Keyboard.current.spaceKey.wasPressedThisFrame
            || Keyboard.current.eKey.wasPressedThisFrame
            || Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ConfirmContinue();
        }
    }

    public void Initialize(UiTheme theme)
    {
        _theme = theme != null ? theme : UiTheme.CreateRuntimeDefault();
        _theme.EnsureRuntimeDefaults();

        RectTransform root = transform as RectTransform;
        if (root != null)
        {
            UiFactory.Stretch(root);
        }

        if (_canvasGroup != null)
        {
            return;
        }

        BuildLayout();
        HideImmediate();
    }

    public void Show(SummaryData summary, Action onContinue)
    {
        if (_canvasGroup == null)
        {
            Initialize(_theme);
        }

        _continueAction = onContinue;
        _isVisible = true;
        _openedFrame = Time.frameCount;
        OnVisibilityChanged?.Invoke(true);
        gameObject.SetActive(true);

        _titleText.text = string.IsNullOrWhiteSpace(summary.Title) ? "—— 灰烬客厅 · 通过 ——" : summary.Title.Trim();
        _collectionValueText.text = string.IsNullOrWhiteSpace(summary.CollectionLine) ? "0 / 0" : summary.CollectionLine.Trim();
        _choiceValueText.text = string.IsNullOrWhiteSpace(summary.ChoiceLine) ? "未记录选择" : summary.ChoiceLine.Trim();
        _narrativeText.text = string.IsNullOrWhiteSpace(summary.NarrativeLine) ? string.Empty : summary.NarrativeLine.Trim();
        _continueLabel.text = string.IsNullOrWhiteSpace(summary.ContinueLabel) ? "继续" : summary.ContinueLabel.Trim();

        _continueButton.onClick.RemoveAllListeners();
        _continueButton.onClick.AddListener(ConfirmContinue);

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
        _fadeRoutine = StartCoroutine(FadeInRoutine(0.3f));

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(_continueButton.gameObject);
        }
    }

    public SummaryData BuildCurrentSummary(string title, IEnumerable<string> floorItemIds, string continueLabel)
    {
        InventoryController.FloorCollectionSummary collectionSummary = GameStateHub.Instance != null
            ? GameStateHub.Instance.GetFloorCollectionSummary(floorItemIds)
            : InventoryController.GetCurrentFloorCollectionSummary(floorItemIds);
        ChapterState.ChoiceResult choiceResult = GameStateHub.Instance != null
            ? GameStateHub.Instance.CurrentChoiceResult
            : ChapterState.ChoiceResult.None;
        string choiceLine;
        string narrativeLine;

        switch (choiceResult)
        {
            case ChapterState.ChoiceResult.Risk:
                choiceLine = RiskChoiceLabel;
                narrativeLine = RiskNarrative;
                break;
            case ChapterState.ChoiceResult.Safe:
                choiceLine = SafeChoiceLabel;
                narrativeLine = SafeNarrative;
                break;
            default:
                choiceLine = "尚未留下明确的选择记录";
                narrativeLine = "这一层已经结束，但它留下的重量还没有名字。";
                break;
        }

        return new SummaryData(
            title,
            $"{collectionSummary.CollectedCount} / {collectionSummary.TotalCollectibleCount}",
            choiceLine,
            narrativeLine,
            continueLabel);
    }

    public void Hide()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        HideImmediate();
    }

    private void BuildLayout()
    {
        _canvasGroup = UiFactory.GetOrAddCanvasGroup(gameObject);

        UiFactory.CreateImage(
            "Backdrop",
            transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            new Color(0f, 0f, 0f, 0.85f));

        RectTransform panel = UiFactory.CreateRect(
            "SummaryPanel",
            transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(1040f, 620f),
            Vector2.zero);

        UiFactory.CreateImage("Shadow", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(28f, 28f), new Vector2(14f, -14f), new Color(0f, 0f, 0f, 0.34f));
        UiFactory.CreateImage("Outer", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.04f, 0.05f, 0.07f, 0.96f));
        RectTransform inner = UiFactory.CreateRect("Inner", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-20f, -20f), Vector2.zero);
        UiFactory.CreateImage("Fill", inner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.1f, 0.14f, 0.98f));
        UiFactory.CreateImage("TopAccent", inner, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-112f, 3f), new Vector2(0f, -1f), new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.54f));

        _titleText = UiFactory.CreateText(
            "Title",
            inner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-132f, 58f),
            new Vector2(0f, -72f),
            _theme.DisplayFont,
            40,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            _theme.PrimaryText);
        _titleText.textWrappingMode = TextWrappingModes.NoWrap;

        UiFactory.CreateImage(
            "TitleDivider",
            inner,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(736f, 2f),
            new Vector2(0f, -116f),
            new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.24f));

        RectTransform summaryBlock = UiFactory.CreateRect(
            "SummaryBlock",
            inner,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(760f, 316f),
            new Vector2(0f, -12f));
        UiFactory.CreateImage("SummaryBlockFill", summaryBlock, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.12f, 0.11f, 0.1f, 0.82f));
        UiFactory.CreateImage("SummaryBlockOutline", summaryBlock, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-2f, -2f), Vector2.zero, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.18f));
        UiFactory.CreateImage("SummaryBlockTop", summaryBlock, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-36f, 2f), new Vector2(0f, -1f), new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.28f));

        TMP_Text collectionLabel = UiFactory.CreateText(
            "CollectionLabel",
            summaryBlock,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-96f, 22f),
            new Vector2(0f, -34f),
            _theme.BodyFont,
            16,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            _theme.Brass);
        collectionLabel.text = "收集物数量";

        _collectionValueText = UiFactory.CreateText(
            "CollectionValue",
            summaryBlock,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-96f, 50f),
            new Vector2(0f, -80f),
            _theme.DisplayFont,
            40,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            _theme.PrimaryText);

        TMP_Text choiceLabel = UiFactory.CreateText(
            "ChoiceLabel",
            summaryBlock,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-96f, 22f),
            new Vector2(0f, -128f),
            _theme.BodyFont,
            16,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            _theme.Brass);
        choiceLabel.text = "选择结果";

        _choiceValueText = UiFactory.CreateText(
            "ChoiceValue",
            summaryBlock,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-96f, 38f),
            new Vector2(0f, -170f),
            _theme.BodyFont,
            30,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            _theme.PrimaryText);

        TMP_Text narrativeLabel = UiFactory.CreateText(
            "NarrativeLabel",
            summaryBlock,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-96f, 22f),
            new Vector2(0f, -220f),
            _theme.BodyFont,
            16,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            _theme.Brass);
        narrativeLabel.text = "叙事总结";

        _narrativeText = UiFactory.CreateText(
            "NarrativeValue",
            summaryBlock,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-112f, 66f),
            new Vector2(0f, -250f),
            _theme.BodyFont,
            20,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            _theme.SecondaryText);
        _narrativeText.textWrappingMode = TextWrappingModes.Normal;
        _narrativeText.lineSpacing = -6f;

        RectTransform buttonRoot = UiFactory.CreateRect(
            "ContinueButton",
            inner,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(264f, 72f),
            new Vector2(0f, 52f));

        Image buttonBackground = UiFactory.CreateImage(
            "Background",
            buttonRoot,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.28f));
        UiFactory.CreateImage(
            "Outline",
            buttonRoot,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-2f, -2f),
            Vector2.zero,
            new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.46f));

        _continueLabel = UiFactory.CreateText(
            "Label",
            buttonRoot,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-20f, -16f),
            Vector2.zero,
            _theme.BodyFont,
            20,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            _theme.PrimaryText);

        _continueButton = buttonRoot.gameObject.AddComponent<Button>();
        _continueButton.targetGraphic = buttonBackground;
    }

    private IEnumerator FadeInRoutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
        _fadeRoutine = null;
    }

    private void ConfirmContinue()
    {
        Action action = _continueAction;
        Hide();
        action?.Invoke();
    }

    private void HideImmediate()
    {
        _isVisible = false;
        _openedFrame = -1;
        _continueAction = null;
        OnVisibilityChanged?.Invoke(false);

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        gameObject.SetActive(false);
    }
}
