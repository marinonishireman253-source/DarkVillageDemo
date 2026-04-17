using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UiComponentCatalog
{
    private static UiTheme s_FallbackTheme;

    public static Button CreatePrimaryButton(Transform parent, string label)
    {
        UiTheme theme = ResolveTheme();
        RectTransform root = EnsureRectTransform(parent);
        Image background = UiFactory.CreateImage(
            "Background",
            root,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            new Color(theme.Brass.r, theme.Brass.g, theme.Brass.b, 0.22f));
        if (theme.PrimaryButtonSprite != null)
        {
            UiFactory.ApplySprite(background, theme.PrimaryButtonSprite);
            background.color = Color.white;
        }

        Image outline = UiFactory.CreateImage(
            "Outline",
            root,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-2f, -2f),
            Vector2.zero,
            new Color(theme.Brass.r, theme.Brass.g, theme.Brass.b, 0.42f));
        outline.raycastTarget = false;

        Image emberLine = UiFactory.CreateImage(
            "EmberLine",
            root,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(-UiSpacing.XL * 2f, 2f),
            new Vector2(0f, UiSpacing.S),
            new Color(theme.Ember.r, theme.Ember.g, theme.Ember.b, 0.34f));
        emberLine.raycastTarget = false;

        TMP_Text text = CreateBodyText(root, label);
        text.name = "Label";
        text.fontSize = UiFontSize.Button;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Midline;
        text.rectTransform.sizeDelta = new Vector2(-UiSpacing.XL * 2f, -UiSpacing.L);

        Button button = root.GetComponent<Button>();
        if (button == null)
        {
            button = root.gameObject.AddComponent<Button>();
        }

        button.targetGraphic = background;
        return button;
    }

    public static Button CreateSecondaryButton(Transform parent, string label)
    {
        UiTheme theme = ResolveTheme();
        RectTransform root = EnsureRectTransform(parent);
        Image background = UiFactory.CreateImage(
            "Background",
            root,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            new Color(1f, 1f, 1f, 0.05f));
        Image outline = UiFactory.CreateImage(
            "Outline",
            root,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-2f, -2f),
            Vector2.zero,
            new Color(theme.PrimaryText.r, theme.PrimaryText.g, theme.PrimaryText.b, 0.16f));
        outline.raycastTarget = false;

        TMP_Text text = CreateBodyText(root, label);
        text.name = "Label";
        text.fontSize = UiFontSize.Button;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Midline;
        text.rectTransform.sizeDelta = new Vector2(-UiSpacing.XL * 2f, -UiSpacing.L);

        Button button = root.GetComponent<Button>();
        if (button == null)
        {
            button = root.gameObject.AddComponent<Button>();
        }

        button.targetGraphic = background;
        return button;
    }

    public static TMP_Text CreateBodyText(Transform parent, string text)
    {
        UiTheme theme = ResolveTheme();
        TMP_Text bodyText = UiFactory.CreateText(
            "BodyText",
            parent,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-UiSpacing.XL * 2f, -UiSpacing.L),
            Vector2.zero,
            theme.BodyFont,
            UiFontSize.Body,
            FontStyle.Normal,
            TextAnchor.MiddleLeft,
            theme.PrimaryText);
        bodyText.text = text;
        return bodyText;
    }

    public static TMP_Text CreateTitleText(Transform parent, string text)
    {
        UiTheme theme = ResolveTheme();
        TMP_Text titleText = UiFactory.CreateText(
            "TitleText",
            parent,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            new Vector2(-UiSpacing.XL * 2f, -UiSpacing.M),
            Vector2.zero,
            theme.DisplayFont,
            UiFontSize.Title,
            FontStyle.Bold,
            TextAnchor.MiddleLeft,
            theme.PrimaryText);
        titleText.text = text;
        return titleText;
    }

    public static Image CreateModalPanel(Transform parent)
    {
        UiTheme theme = ResolveTheme();
        RectTransform root = EnsureRectTransform(parent);
        Image panelImage = root.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = root.gameObject.AddComponent<Image>();
        }

        UiFactory.ApplySprite(panelImage, theme.ModalFrameSprite);
        panelImage.color = theme.ModalFrameSprite != null ? Color.white : UiColors.PanelOuter;
        panelImage.raycastTarget = false;
        return panelImage;
    }

    public static Image CreateDialogueFrame(Transform parent)
    {
        UiTheme theme = ResolveTheme();
        RectTransform root = EnsureRectTransform(parent);
        Image frameImage = root.GetComponent<Image>();
        if (frameImage == null)
        {
            frameImage = root.gameObject.AddComponent<Image>();
        }

        UiFactory.ApplySprite(frameImage, theme.DialogueFrameSprite);
        frameImage.color = theme.DialogueFrameSprite != null ? Color.white : UiColors.PanelOuter;
        frameImage.raycastTarget = false;
        return frameImage;
    }

    public static void ApplyChrome(Image image, Sprite sprite, Color fallbackColor, Image.Type imageType = Image.Type.Sliced, bool preserveAspect = false)
    {
        if (image == null)
        {
            return;
        }

        image.color = fallbackColor;
        if (sprite == null)
        {
            return;
        }

        UiFactory.ApplySprite(image, sprite, imageType, preserveAspect);
        image.color = Color.white;
    }

    public static RectTransform BuildPanelShell(
        Transform parent,
        Color outerColor,
        Sprite outerSprite,
        Color innerColor,
        Sprite innerSprite,
        Vector2 innerInset)
    {
        Image outer = UiFactory.CreateImage(
            "Outer",
            parent,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            outerColor);
        outer.raycastTarget = false;
        ApplyChrome(outer, outerSprite, outerColor);

        RectTransform inner = UiFactory.CreateRect(
            "Inner",
            parent,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            innerInset,
            Vector2.zero);
        Image fill = UiFactory.CreateImage(
            "Fill",
            inner,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            innerColor);
        fill.raycastTarget = false;
        ApplyChrome(fill, innerSprite, innerColor);
        return inner;
    }

    public static Image CreateAccentLine(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta,
        Vector2 anchoredPosition,
        Color color)
    {
        Image accent = UiFactory.CreateImage(name, parent, anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition, color);
        accent.raycastTarget = false;
        return accent;
    }

    private static RectTransform EnsureRectTransform(Transform parent)
    {
        if (parent is RectTransform rectTransform)
        {
            return rectTransform;
        }

        return UiFactory.CreateRect(
            "UiComponent",
            parent,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero);
    }

    private static UiTheme ResolveTheme()
    {
        UiTheme theme = UiBootstrap.Instance != null ? UiBootstrap.Instance.Theme : null;
        if (theme == null)
        {
            theme = Resources.Load<UiTheme>("UI/DefaultUiTheme");
        }

        if (theme == null)
        {
            if (s_FallbackTheme == null)
            {
                s_FallbackTheme = UiTheme.CreateRuntimeDefault();
            }

            theme = s_FallbackTheme;
        }

        theme.EnsureRuntimeDefaults();
        return theme;
    }
}
