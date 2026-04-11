using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ModalCanvasView : MonoBehaviour
{
    private sealed class ModalButtonChrome : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        public Image Background { get; set; }
        public Image Border { get; set; }
        public Image Underline { get; set; }
        public TMP_Text Label { get; set; }
        public Color NormalBackground { get; set; }
        public Color HoverBackground { get; set; }
        public Color PressedBackground { get; set; }
        public Color NormalLabel { get; set; }
        public Color HoverLabel { get; set; }
        public Color PressedLabel { get; set; }
        public bool IsSecondary { get; set; }

        private bool _hovered;
        private bool _selected;
        private bool _pressed;

        private void Awake()
        {
            ApplyState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            ApplyState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            ApplyState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            ApplyState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            ApplyState();
        }

        public void OnSelect(BaseEventData eventData)
        {
            _selected = true;
            ApplyState();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
            _pressed = false;
            ApplyState();
        }

        private void ApplyState()
        {
            bool emphasis = _hovered || _selected;
            if (Background != null)
            {
                Background.color = _pressed ? PressedBackground : (emphasis ? HoverBackground : NormalBackground);
            }

            if (Border != null)
            {
                Border.color = IsSecondary ? new Color(Border.color.r, Border.color.g, Border.color.b, 0f) : Border.color;
            }

            if (Label != null)
            {
                Label.color = _pressed ? PressedLabel : (emphasis ? HoverLabel : NormalLabel);
            }

            if (Underline != null)
            {
                Color underline = Underline.color;
                underline.a = _pressed ? 0.92f : (emphasis ? 0.72f : 0f);
                Underline.color = underline;
            }
        }
    }

    private UiTheme _theme;

    [Header("Modal Configuration (Centered)")]
    [SerializeField] private RectTransform _chapterCompleteRoot;
    [SerializeField] private TMP_Text _chapterCompleteTitle;
    [SerializeField] private TMP_Text _chapterCompleteBody;
    [SerializeField] private TMP_Text _chapterCompleteHint;

    [Header("Buttons")]
    [SerializeField] private Button _primaryButton;
    [SerializeField] private Button _secondaryButton;
    [SerializeField] private TMP_Text _primaryLabel;
    [SerializeField] private TMP_Text _secondaryLabel;
    [SerializeField] private Image _primaryButtonBackground;
    [SerializeField] private Image _primaryButtonUnderline;
    [SerializeField] private Image _secondaryButtonUnderline;

    private Action _primaryAction;
    private Action _secondaryAction;

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
        HideChapterComplete();
    }

    public void ShowChapterComplete(string title, string body, string hintText, string primaryLabel, string secondaryLabel, Action onPrimary, Action onSecondary)
    {
        if (_chapterCompleteRoot == null)
        {
            return;
        }

        _primaryAction = onPrimary;
        _secondaryAction = onSecondary;

        _chapterCompleteRoot.gameObject.SetActive(true);
        _chapterCompleteTitle.text = string.IsNullOrWhiteSpace(title) ? "当前段落完成" : title.Trim();
        _chapterCompleteBody.text = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();
        _chapterCompleteHint.text = string.IsNullOrWhiteSpace(hintText) ? string.Empty : hintText.Trim();
        _primaryLabel.text = string.IsNullOrWhiteSpace(primaryLabel) ? "确定" : primaryLabel.Trim();
        bool hasSecondary = !string.IsNullOrWhiteSpace(secondaryLabel);
        _secondaryLabel.text = hasSecondary ? secondaryLabel.Trim() : string.Empty;
        if (_secondaryButton != null)
        {
            _secondaryButton.gameObject.SetActive(hasSecondary);
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

    public void ShowModal(string title, string body, string btn1Text, string btn2Text = "")
    {
        ShowChapterComplete(
            title,
            body,
            string.Empty,
            btn1Text,
            btn2Text,
            null,
            null);
    }

    public void HideModal()
    {
        HideChapterComplete();
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

        UiFactory.CreateImage("Backdrop", _chapterCompleteRoot, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.02f, 0.02f, 0.03f, 0.54f));

        RectTransform panel = UiFactory.CreateRect(
            "Panel",
            _chapterCompleteRoot,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(1200f, 760f),
            Vector2.zero);

        CreatePanelShadow(panel, new Vector2(34f, -34f), new Vector2(48f, 44f), new Color(0f, 0f, 0f, 0.4f));

        Image panelOuter = UiFactory.CreateImage("PanelOuter", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.04f, 0.05f, 0.07f, 0.97f));
        Sprite modalFrameSprite = _theme.ModalFrameSprite;
        if (modalFrameSprite != null)
        {
            UiFactory.ApplySprite(panelOuter, modalFrameSprite);
            panelOuter.color = Color.white;
        }
        RectTransform inner = UiFactory.CreateRect("PanelInner", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-16f, -16f), Vector2.zero);
        UiFactory.CreateImage("PanelInnerFill", inner, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.11f, 0.13f, 0.17f, 0.95f));
        CreateLinenOverlay(inner);
        CreateAccentLine(inner, 44f);
        CreateHeavyCornerTrim(inner);
        CreateBeamMotif(inner);

        _chapterCompleteTitle = UiFactory.CreateText(
            "Title",
            inner,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-160f, 68f),
            new Vector2(0f, -64f),
            _theme.DisplayFont,
            42,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            _theme.Brass);
        _chapterCompleteTitle.characterSpacing = 4f;

        _chapterCompleteBody = UiFactory.CreateText(
            "Body",
            inner,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-180f, -320f),
            new Vector2(0f, -164f),
            _theme.BodyFont,
            26,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            _theme.WaxWhiteTextSoft);
        _chapterCompleteBody.lineSpacing = 10f;

        RectTransform buttonRow = UiFactory.CreateRect(
            "ButtonRow",
            inner,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(520f, 188f),
            new Vector2(0f, 72f));

        VerticalLayoutGroup buttonLayout = buttonRow.gameObject.AddComponent<VerticalLayoutGroup>();
        buttonLayout.spacing = 20f;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = false;
        buttonLayout.childControlHeight = false;
        buttonLayout.childForceExpandWidth = false;
        buttonLayout.childForceExpandHeight = false;

        _primaryButton = CreatePrimaryModalButton(buttonRow, out _primaryLabel, out _primaryButtonBackground, out _primaryButtonUnderline);
        _primaryButton.onClick.AddListener(InvokePrimaryAction);

        _secondaryButton = CreateSecondaryModalButton(buttonRow, out _secondaryLabel, out _secondaryButtonUnderline);
        _secondaryButton.onClick.AddListener(InvokeSecondaryAction);

        _chapterCompleteHint = UiFactory.CreateText(
            "Hint",
            inner,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(-160f, 36f),
            new Vector2(0f, 18f),
            _theme.BodyFont,
            18,
            FontStyle.Normal,
            TextAnchor.MiddleRight,
            _theme.SecondaryText);
    }

    private Button CreatePrimaryModalButton(Transform parent, out TMP_Text label, out Image background, out Image underline)
    {
        RectTransform root = UiFactory.CreateRect(
            "PrimaryButtonRoot",
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(480f, 120f),
            Vector2.zero);
        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 480f;
        layout.preferredHeight = 120f;

        background = UiFactory.CreateImage(
            "Button",
            root,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            new Color(_theme.Charcoal.r, _theme.Charcoal.g, _theme.Charcoal.b, 0.92f));
        Sprite primaryButtonSprite = _theme.PrimaryButtonSprite;
        if (primaryButtonSprite != null)
        {
            UiFactory.ApplySprite(background, primaryButtonSprite);
            background.color = Color.white;
        }
        UiFactory.CreateImage(
            "WoodInlay",
            root,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-8f, -8f),
            Vector2.zero,
            new Color(_theme.Slate.r, _theme.Slate.g, _theme.Slate.b, 0.52f));
        Image border = UiFactory.CreateImage(
            "ButtonBorder",
            root,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.66f));
        UiFactory.CreateImage(
            "ButtonBorderInset",
            root,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-3f, -3f),
            Vector2.zero,
            new Color(_theme.Charcoal.r, _theme.Charcoal.g, _theme.Charcoal.b, 0.9f));

        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        label = UiFactory.CreateText(
            "Label",
            root,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-40f, -22f),
            Vector2.zero,
            _theme.DisplayFont,
            28,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            _theme.WaxWhiteText);
        label.characterSpacing = 2f;

        underline = UiFactory.CreateImage(
            "HoverUnderline",
            root,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(240f, 2f),
            new Vector2(0f, 26f),
            new Color(_theme.EmberRed.r, _theme.EmberRed.g, _theme.EmberRed.b, 0f));

        ModalButtonChrome chrome = root.gameObject.AddComponent<ModalButtonChrome>();
        chrome.Background = background;
        chrome.Border = border;
        chrome.Underline = underline;
        chrome.Label = label;
        chrome.NormalBackground = new Color(_theme.Charcoal.r, _theme.Charcoal.g, _theme.Charcoal.b, 0.92f);
        chrome.HoverBackground = new Color(0.12f, 0.13f, 0.15f, 0.95f);
        chrome.PressedBackground = new Color(0.03f, 0.04f, 0.05f, 0.98f);
        chrome.NormalLabel = _theme.WaxWhiteText;
        chrome.HoverLabel = new Color(_theme.WaxWhiteText.r, _theme.WaxWhiteText.g * 0.95f, _theme.WaxWhiteText.b * 0.92f, 1f);
        chrome.PressedLabel = new Color(_theme.EmberRed.r, _theme.EmberRed.g * 0.96f, _theme.EmberRed.b * 0.92f, 1f);

        return button;
    }

    private Button CreateSecondaryModalButton(Transform parent, out TMP_Text label, out Image underline)
    {
        RectTransform root = UiFactory.CreateRect(
            "SecondaryButtonRoot",
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(320f, 48f),
            Vector2.zero);
        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 320f;
        layout.preferredHeight = 48f;

        Image background = UiFactory.CreateImage(
            "Button",
            root,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            new Color(0f, 0f, 0f, 0f));

        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        label = UiFactory.CreateText(
            "Label",
            root,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-20f, -10f),
            Vector2.zero,
            _theme.DisplayFont,
            22,
            FontStyle.Normal,
            TextAnchor.MiddleCenter,
            new Color(_theme.DimBrass.r, _theme.DimBrass.g, _theme.DimBrass.b, 0.88f));
        label.characterSpacing = 1.5f;

        underline = UiFactory.CreateImage(
            "HoverUnderline",
            root,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(120f, 2f),
            new Vector2(0f, 6f),
            new Color(_theme.EmberRed.r, _theme.EmberRed.g, _theme.EmberRed.b, 0f));

        ModalButtonChrome chrome = root.gameObject.AddComponent<ModalButtonChrome>();
        chrome.Background = background;
        chrome.Underline = underline;
        chrome.Label = label;
        chrome.NormalBackground = new Color(0f, 0f, 0f, 0f);
        chrome.HoverBackground = new Color(0f, 0f, 0f, 0f);
        chrome.PressedBackground = new Color(0f, 0f, 0f, 0f);
        chrome.NormalLabel = new Color(_theme.DimBrass.r, _theme.DimBrass.g, _theme.DimBrass.b, 0.88f);
        chrome.HoverLabel = new Color(_theme.WaxWhiteText.r, _theme.WaxWhiteText.g, _theme.WaxWhiteText.b, 0.94f);
        chrome.PressedLabel = new Color(_theme.EmberRed.r, _theme.EmberRed.g, _theme.EmberRed.b, 0.92f);
        chrome.IsSecondary = true;

        return button;
    }

    private void InvokePrimaryAction()
    {
        _primaryAction?.Invoke();
    }

    private void InvokeSecondaryAction()
    {
        _secondaryAction?.Invoke();
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
            new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.72f));
        line.raycastTarget = false;
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

    private void CreateHeavyCornerTrim(Transform parent)
    {
        Color brass = new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.72f);
        CreateHeavyCorner(parent, "TopLeft", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), 24f, -24f, 96f, 8f, brass);
        CreateHeavyCorner(parent, "TopRight", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), -24f, -24f, 96f, 8f, brass);
        CreateHeavyCorner(parent, "BottomLeft", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), 24f, 24f, 96f, 8f, brass);
        CreateHeavyCorner(parent, "BottomRight", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), -24f, 24f, 96f, 8f, brass);
    }

    private void CreateHeavyCorner(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, float x, float y, float length, float thickness, Color color)
    {
        CreateCornerBracket(parent, $"{name}_Outer", anchorMin, anchorMax, pivot, x, y, length, thickness, color);
        CreateCornerBracket(parent, $"{name}_Inner", anchorMin, anchorMax, pivot, x + Mathf.Sign(x) * -10f, y + Mathf.Sign(y) * -10f, length * 0.58f, 3f, new Color(color.r, color.g, color.b, 0.34f));
    }

    private void CreateBeamMotif(Transform parent)
    {
        Image topBeam = UiFactory.CreateImage(
            "TopBeam",
            parent,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(220f, 2f),
            new Vector2(0f, -108f),
            new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.16f));
        topBeam.raycastTarget = false;

        Image bottomBeam = UiFactory.CreateImage(
            "BottomBeam",
            parent,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(220f, 2f),
            new Vector2(0f, 126f),
            new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.12f));
        bottomBeam.raycastTarget = false;
    }

    private void CreateLinenOverlay(Transform parent)
    {
        RectTransform linenRoot = UiFactory.CreateRect(
            "LinenOverlay",
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(880f, 500f),
            new Vector2(0f, -12f));
        linenRoot.SetAsLastSibling();

        for (int i = 0; i < 8; i++)
        {
            float y = 200f - (i * 58f);
            Image thread = UiFactory.CreateImage(
                $"LinenH_{i}",
                linenRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(760f, 1f),
                new Vector2(0f, y),
                new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.018f));
            thread.raycastTarget = false;
        }

        for (int i = 0; i < 6; i++)
        {
            float x = -260f + (i * 104f);
            Image thread = UiFactory.CreateImage(
                $"LinenV_{i}",
                linenRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(1f, 340f),
                new Vector2(x, 0f),
                new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.014f));
            thread.raycastTarget = false;
        }
    }
}
