using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ModalCanvasView : MonoBehaviour
{
    private UiTheme _theme;

    [Header("Chapter Complete")]
    [SerializeField] private RectTransform _chapterCompleteRoot;
    [SerializeField] private TMP_Text _chapterCompleteTitle;
    [SerializeField] private TMP_Text _chapterCompleteBody;
    [SerializeField] private TMP_Text _chapterCompleteHint;

    [Header("Buttons")]
    [SerializeField] private Button _primaryButton;
    [SerializeField] private Button _secondaryButton;
    [SerializeField] private TMP_Text _primaryLabel;
    [SerializeField] private TMP_Text _secondaryLabel;

    [Header("Binary Choice")]
    [SerializeField] private RectTransform _binaryChoiceRoot;
    [SerializeField] private TMP_Text _binaryChoiceTitle;
    [SerializeField] private TMP_Text _binaryChoiceBody;
    [SerializeField] private TMP_Text _binaryChoiceHint;
    [SerializeField] private Button _leftChoiceButton;
    [SerializeField] private Button _rightChoiceButton;
    [SerializeField] private TMP_Text _leftChoiceLabel;
    [SerializeField] private TMP_Text _rightChoiceLabel;

    private Action _primaryAction;
    private Action _secondaryAction;
    private Action _leftChoiceAction;
    private Action _rightChoiceAction;

    public void Initialize(UiTheme theme)
    {
        if (_theme != null)
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

        BuildChapterCompleteModal();
        BuildBinaryChoiceModal();
        HideChapterComplete();
        HideBinaryChoice();
    }

    public void ShowChapterComplete(string title, string body, string hintText, string primaryLabel, string secondaryLabel, Action onPrimary, Action onSecondary)
    {
        if (_chapterCompleteRoot == null)
        {
            return;
        }

        HideBinaryChoice();

        _primaryAction = onPrimary;
        _secondaryAction = onSecondary;
        _chapterCompleteRoot.gameObject.SetActive(true);

        _chapterCompleteTitle.text = string.IsNullOrWhiteSpace(title) ? "当前段落完成" : title.Trim();
        _chapterCompleteBody.text = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();
        _chapterCompleteHint.text = string.IsNullOrWhiteSpace(hintText) ? string.Empty : hintText.Trim();

        SetButtonContent(_primaryButton, _primaryLabel, string.IsNullOrWhiteSpace(primaryLabel) ? "确认" : primaryLabel.Trim(), HandlePrimaryPressed);

        bool hasSecondary = !string.IsNullOrWhiteSpace(secondaryLabel);
        _secondaryButton.gameObject.SetActive(hasSecondary);
        if (hasSecondary)
        {
            SetButtonContent(_secondaryButton, _secondaryLabel, secondaryLabel.Trim(), HandleSecondaryPressed);
        }
    }

    public void HideChapterComplete()
    {
        if (_chapterCompleteRoot != null)
        {
            _chapterCompleteRoot.gameObject.SetActive(false);
        }

        _primaryAction = null;
        _secondaryAction = null;
    }

    public void ShowBinaryChoice(
        string title,
        string body,
        string hintText,
        string leftLabel,
        string rightLabel,
        Action onLeft,
        Action onRight)
    {
        if (_binaryChoiceRoot == null)
        {
            return;
        }

        HideChapterComplete();

        _leftChoiceAction = onLeft;
        _rightChoiceAction = onRight;
        _binaryChoiceRoot.gameObject.SetActive(true);

        _binaryChoiceTitle.text = string.IsNullOrWhiteSpace(title) ? "做出选择" : title.Trim();
        _binaryChoiceBody.text = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();
        _binaryChoiceHint.text = string.IsNullOrWhiteSpace(hintText) ? string.Empty : hintText.Trim();

        SetButtonContent(_leftChoiceButton, _leftChoiceLabel, string.IsNullOrWhiteSpace(leftLabel) ? "左侧选择" : leftLabel.Trim(), HandleLeftChoicePressed);
        SetButtonContent(_rightChoiceButton, _rightChoiceLabel, string.IsNullOrWhiteSpace(rightLabel) ? "右侧选择" : rightLabel.Trim(), HandleRightChoicePressed);
    }

    public void HideBinaryChoice()
    {
        if (_binaryChoiceRoot != null)
        {
            _binaryChoiceRoot.gameObject.SetActive(false);
        }

        _leftChoiceAction = null;
        _rightChoiceAction = null;
    }

    public void ShowModal(string title, string body, string btn1Text, string btn2Text = "")
    {
        ShowChapterComplete(title, body, string.Empty, btn1Text, btn2Text, null, null);
    }

    public void HideModal()
    {
        HideChapterComplete();
        HideBinaryChoice();
    }

    private void BuildChapterCompleteModal()
    {
        _chapterCompleteRoot = UiFactory.CreateRect(
            "ChapterCompleteModal",
            transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);

        UiFactory.CreateImage("Backdrop", _chapterCompleteRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.01f, 0.01f, 0.02f, 0.76f));

        RectTransform panel = CreateMainPanel(_chapterCompleteRoot, "ChapterPanel", new Vector2(980f, 640f));

        TMP_Text eyebrow = UiFactory.CreateText(
            "Eyebrow",
            panel,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-80f, 24f),
            new Vector2(34f, -28f),
            _theme.DisplayFont,
            13,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            _theme.Brass);
        eyebrow.gameObject.SetActive(false);

        _chapterCompleteTitle = UiFactory.CreateText(
            "Title",
            panel,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-80f, 58f),
            new Vector2(34f, -74f),
            _theme.DisplayFont,
            40,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);

        _chapterCompleteBody = UiFactory.CreateText(
            "Body",
            panel,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-80f, -214f),
            new Vector2(34f, -142f),
            _theme.BodyFont,
            24,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            _theme.SecondaryText);
        _chapterCompleteBody.lineSpacing = 10f;

        _chapterCompleteHint = UiFactory.CreateText(
            "Hint",
            panel,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
            new Vector2(-80f, 26f),
            new Vector2(34f, 122f),
            _theme.BodyFont,
            15,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.72f));

        RectTransform buttonsRow = UiFactory.CreateRect(
            "ButtonsRow",
            panel,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(-68f, 84f),
            new Vector2(0f, 28f));

        _primaryButton = CreateActionButton(buttonsRow, "PrimaryButton", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(364f, 84f), new Vector2(0f, 0f), true, out _primaryLabel);
        _secondaryButton = CreateActionButton(buttonsRow, "SecondaryButton", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(300f, 84f), new Vector2(0f, 0f), false, out _secondaryLabel);
    }

    private void BuildBinaryChoiceModal()
    {
        _binaryChoiceRoot = UiFactory.CreateRect(
            "BinaryChoiceModal",
            transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);

        UiFactory.CreateImage("Backdrop", _binaryChoiceRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.01f, 0.01f, 0.02f, 0.78f));

        RectTransform panel = CreateMainPanel(_binaryChoiceRoot, "BinaryChoicePanel", new Vector2(1260f, 660f));

        TMP_Text eyebrow = UiFactory.CreateText(
            "Eyebrow",
            panel,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-80f, 24f),
            new Vector2(34f, -28f),
            _theme.DisplayFont,
            13,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            _theme.Brass);
        eyebrow.gameObject.SetActive(false);

        _binaryChoiceTitle = UiFactory.CreateText(
            "Title",
            panel,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-80f, 52f),
            new Vector2(34f, -74f),
            _theme.DisplayFont,
            36,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);

        _binaryChoiceBody = UiFactory.CreateText(
            "Body",
            panel,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            new Vector2(-80f, 56f),
            new Vector2(34f, -132f),
            _theme.BodyFont,
            22,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            _theme.SecondaryText);

        _binaryChoiceHint = UiFactory.CreateText(
            "Hint",
            panel,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 0f),
            new Vector2(-80f, 26f),
            new Vector2(34f, 30f),
            _theme.BodyFont,
            15,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.72f));

        _leftChoiceButton = CreateChoiceButton(panel, "LeftChoice", new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(-12f, 304f), new Vector2(0f, 82f), out _leftChoiceLabel);
        _rightChoiceButton = CreateChoiceButton(panel, "RightChoice", new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-12f, 304f), new Vector2(0f, 82f), out _rightChoiceLabel);
    }

    private RectTransform CreateMainPanel(Transform parent, string name, Vector2 size)
    {
        RectTransform panel = UiFactory.CreateRect(
            name,
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            size,
            Vector2.zero);
        UiFactory.CreateImage("Shadow", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(40f, 40f), new Vector2(22f, -22f), new Color(0f, 0f, 0f, 0.4f));
        UiFactory.CreateImage("Outer", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.03f, 0.04f, 0.06f, 0.96f));
        RectTransform inner = UiFactory.CreateRect("Inner", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-16f, -16f), Vector2.zero);
        UiFactory.CreateImage("Fill", inner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.1f, 0.14f, 0.98f));
        UiFactory.CreateImage("LeftAccent", inner, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(4f, -40f), Vector2.zero, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.92f));
        return inner;
    }

    private Button CreateActionButton(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta,
        Vector2 anchoredPosition,
        bool primary,
        out TMP_Text label)
    {
        RectTransform root = UiFactory.CreateRect(name, parent, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
        Image background = UiFactory.CreateImage("Background", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
            primary
                ? new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.22f)
                : new Color(1f, 1f, 1f, 0.05f));
        UiFactory.CreateImage("Outline", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-2f, -2f), Vector2.zero,
            primary
                ? new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.42f)
                : new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.16f));

        label = UiFactory.CreateText(
            "Label",
            root,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-32f, -18f),
            Vector2.zero,
            _theme.BodyFont,
            22,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            _theme.PrimaryText);
        label.textWrappingMode = TextWrappingModes.Normal;

        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        return button;
    }

    private Button CreateChoiceButton(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta,
        Vector2 anchoredPosition,
        out TMP_Text label)
    {
        RectTransform root = UiFactory.CreateRect(name, parent, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
        if (anchorMin.x == 0f)
        {
            root.offsetMin = new Vector2(34f, anchoredPosition.y);
            root.offsetMax = new Vector2(-8f, anchoredPosition.y + sizeDelta.y);
        }
        else
        {
            root.offsetMin = new Vector2(8f, anchoredPosition.y);
            root.offsetMax = new Vector2(-34f, anchoredPosition.y + sizeDelta.y);
        }

        Image background = UiFactory.CreateImage("Background", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.05f));
        UiFactory.CreateImage("Outline", root, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-2f, -2f), Vector2.zero, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.18f));
        UiFactory.CreateImage("TopAccent", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-28f, 2f), new Vector2(0f, -1f), new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.4f));

        label = UiFactory.CreateText(
            "Label",
            root,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-34f, -30f),
            Vector2.zero,
            _theme.BodyFont,
            22,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            _theme.PrimaryText);
        label.textWrappingMode = TextWrappingModes.Normal;

        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        return button;
    }

    private void SetButtonContent(Button button, TMP_Text label, string content, Action callback)
    {
        if (button == null || label == null)
        {
            return;
        }

        label.text = content;
        button.onClick.RemoveAllListeners();
        if (callback != null)
        {
            button.onClick.AddListener(() => callback());
        }
    }

    private void HandlePrimaryPressed()
    {
        _primaryAction?.Invoke();
    }

    private void HandleSecondaryPressed()
    {
        _secondaryAction?.Invoke();
    }

    private void HandleLeftChoicePressed()
    {
        _leftChoiceAction?.Invoke();
    }

    private void HandleRightChoicePressed()
    {
        _rightChoiceAction?.Invoke();
    }
}
