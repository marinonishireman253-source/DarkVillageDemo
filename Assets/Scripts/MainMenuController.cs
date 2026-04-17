using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    private static readonly Color BackgroundColor = new Color32(0x05, 0x07, 0x0B, 0xFF);
    private static readonly Color ButtonNormalTint = Color.white;
    private static readonly Color ButtonHoverTint = new Color(1f, 0.96f, 0.9f, 0.96f);
    private static readonly Color ButtonPressedTint = new Color(0.9f, 0.84f, 0.73f, 0.92f);
    private static readonly Color ButtonDisabledTint = new Color(0.42f, 0.42f, 0.42f, 0.62f);
    private static readonly Color EmberBrightColor = new Color(0.78f, 0.78f, 0.78f, 0.18f);
    private static readonly Color EmberDimColor = new Color(0.45f, 0.45f, 0.45f, 0.03f);

    private Canvas _canvas;
    private UiTheme _theme;
    private Button _newJourneyButton;
    private Button _continueButton;
    private Button _quitButton;
    private TMP_Text _continueButtonText;
    private bool _isTransitioning;

    private void Awake()
    {
        Time.timeScale = 1f;
        _theme = UiTheme.CreateRuntimeDefault();
        _theme.EnsureRuntimeDefaults();

        EnsureCamera();
        EnsureEventSystem();
        BuildMenu();
        BuildAshParticles();
        RefreshContinueButton();
        SelectDefaultButton();
    }

    private void Update()
    {
        if (_isTransitioning || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            QuitGame();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            StartNewJourney();
        }

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            ContinueJourney();
        }
#endif
    }

    private void EnsureCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }

        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = BackgroundColor;
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 5f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 40f;
        mainCamera.transform.position = new Vector3(0f, 0f, -10f);
        mainCamera.transform.rotation = Quaternion.identity;
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }
        else if (eventSystem.GetComponent<BaseInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private void BuildMenu()
    {
        if (_canvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("MenuCanvas");
        canvasObject.transform.SetParent(transform, false);

        _canvas = canvasObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        Image backgroundImage = CreateFullScreenImage("Background", canvasObject.transform, new Color(0.03f, 0.04f, 0.06f, 1f));
        backgroundImage.raycastTarget = false;
        CreateFullScreenImage("TopFog", canvasObject.transform, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.03f)).rectTransform.offsetMax = new Vector2(0f, -320f);
        CreateFullScreenImage("BottomHaze", canvasObject.transform, new Color(_theme.Ember.r, _theme.Ember.g, _theme.Ember.b, 0.05f)).rectTransform.offsetMin = new Vector2(0f, 720f);

        GameObject frameObject = CreateUiObject("MenuFrame", canvasObject.transform);
        RectTransform frameRect = frameObject.AddComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(840f, 700f);
        frameRect.anchoredPosition = new Vector2(0f, 8f);

        Image frameShadow = CreateInsetImage("FrameShadow", frameRect, new Vector2(28f, 28f), new Vector2(18f, -18f), new Color(0f, 0f, 0f, 0.34f));
        frameShadow.raycastTarget = false;

        Image frameOuter = CreateInsetImage("FrameOuter", frameRect, Vector2.zero, Vector2.zero, new Color(0.03f, 0.04f, 0.06f, 0.95f));
        ApplySprite(frameOuter, _theme.ModalFrameSprite, new Color(0.03f, 0.04f, 0.06f, 0.95f));

        RectTransform frameInner = CreateRect("FrameInner", frameRect, new Vector2(-16f, -16f), Vector2.zero);
        Image frameFill = CreateInsetImage("FrameFill", frameInner, Vector2.zero, Vector2.zero, new Color(0.08f, 0.1f, 0.14f, 0.72f));
        frameFill.raycastTarget = false;

        Image leftAccent = CreateInsetImage("LeftAccent", frameInner, new Vector2(4f, -40f), Vector2.zero, new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.88f));
        leftAccent.rectTransform.anchorMax = new Vector2(0f, 1f);
        leftAccent.rectTransform.sizeDelta = new Vector2(4f, -40f);
        leftAccent.rectTransform.anchoredPosition = Vector2.zero;

        GameObject contentObject = CreateUiObject("Content", frameInner);
        RectTransform contentRect = contentObject.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(660f, 0f);
        contentRect.anchoredPosition = new Vector2(0f, 8f);

        VerticalLayoutGroup contentLayout = contentObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.MiddleCenter;
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = false;
        contentLayout.spacing = 14f;
        contentLayout.padding = new RectOffset(0, 0, 34, 24);

        ContentSizeFitter contentFitter = contentObject.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateTitlePlaque(contentObject.transform);
        CreateTextBlock(contentObject.transform, "Eyebrow", "错位门厅", 16, FontStyles.Bold, _theme.Brass, new Vector2(0f, 24f), 120f, false);
        CreateTextBlock(contentObject.transform, "Title", "ERSARN", 84, FontStyles.Bold, _theme.PrimaryText, new Vector2(0f, 100f), 0f, true);
        CreateTextBlock(contentObject.transform, "Subtitle", "异常空间", 30, FontStyles.Bold, new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.8f), new Vector2(0f, 42f), 80f, false);
        CreateTextBlock(contentObject.transform, "Tagline", "在错位的房间里辨认规则，在沉默到来前做出选择。", 22, FontStyles.Normal, _theme.SecondaryText, new Vector2(0f, 56f), 0f, false);
        CreateSpacer(contentObject.transform, 18f);

        CreateContextCard(contentObject.transform);
        CreateSpacer(contentObject.transform, 16f);

        GameObject buttonColumn = CreateUiObject("Buttons", contentObject.transform);
        RectTransform buttonColumnRect = buttonColumn.AddComponent<RectTransform>();
        buttonColumnRect.sizeDelta = new Vector2(436f, 0f);

        VerticalLayoutGroup buttonLayout = buttonColumn.AddComponent<VerticalLayoutGroup>();
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlHeight = false;
        buttonLayout.childControlWidth = true;
        buttonLayout.childForceExpandHeight = false;
        buttonLayout.childForceExpandWidth = false;
        buttonLayout.spacing = 18f;

        _newJourneyButton = CreateButton(buttonColumn.transform, "NewJourneyButton", "新旅程", StartNewJourney);
        _continueButton = CreateButton(buttonColumn.transform, "ContinueButton", "继续旅程", ContinueJourney);
        _continueButtonText = _continueButton.GetComponentInChildren<TMP_Text>();
        _quitButton = CreateButton(buttonColumn.transform, "QuitButton", "离开", QuitGame, emphasize: false);

        CreateSpacer(contentObject.transform, 20f);
        CreateTextBlock(contentObject.transform, "Hint", BuildHintText(), 16, FontStyles.Normal, new Color(_theme.PrimaryText.r, _theme.PrimaryText.g, _theme.PrimaryText.b, 0.72f), new Vector2(0f, 52f), 0f, false);
    }

    private void CreateContextCard(Transform parent)
    {
        GameObject cardObject = CreateUiObject("ContextCard", parent);
        RectTransform cardRect = cardObject.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(620f, 118f);

        LayoutElement layoutElement = cardObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 620f;
        layoutElement.preferredHeight = 118f;

        Image outer = cardObject.AddComponent<Image>();
        ApplySprite(outer, _theme.ChoicePanelSprite, new Color(0.05f, 0.06f, 0.09f, 0.92f));

        RectTransform inner = CreateRect("Inner", cardRect, new Vector2(-12f, -12f), Vector2.zero);
        Image fill = CreateInsetImage("Fill", inner, Vector2.zero, Vector2.zero, new Color(0.08f, 0.1f, 0.14f, 0.6f));
        fill.raycastTarget = false;

        CreateTextBlock(inner, "Title", "房间已经记住你的失败与停顿。", 18, FontStyles.Bold, _theme.PrimaryText, new Vector2(0f, 24f), 0f, false, new Vector2(22f, -20f), new Vector2(-22f, -18f), TextAlignmentOptions.TopLeft);
        CreateTextBlock(inner, "Body", "保留探索的灰烬感，同时把菜单、提示、对话和模态统一到同一套铜边和烛灰材料里。", 16, FontStyles.Normal, _theme.SecondaryText, new Vector2(0f, 42f), 0f, false, new Vector2(22f, -48f), new Vector2(-22f, -20f), TextAlignmentOptions.TopLeft);
    }

    private void CreateTitlePlaque(Transform parent)
    {
        GameObject plaqueObject = CreateUiObject("TitlePlaque", parent);
        RectTransform plaqueRect = plaqueObject.AddComponent<RectTransform>();
        plaqueRect.sizeDelta = new Vector2(580f, 148f);

        LayoutElement layoutElement = plaqueObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 580f;
        layoutElement.preferredHeight = 148f;

        Image plaque = plaqueObject.AddComponent<Image>();
        ApplySprite(plaque, _theme.ChoicePanelSprite, new Color(0.05f, 0.06f, 0.09f, 0.92f));
        plaque.raycastTarget = false;

        RectTransform inner = CreateRect("TitlePlaqueInner", plaqueRect, new Vector2(-18f, -18f), Vector2.zero);
        Image fill = CreateInsetImage("TitlePlaqueFill", inner, Vector2.zero, Vector2.zero, new Color(0.08f, 0.1f, 0.14f, 0.72f));
        fill.raycastTarget = false;

        Image topAccent = CreateInsetImage("TitlePlaqueTopAccent", inner, new Vector2(-72f, 2f), new Vector2(0f, -1f), new Color(_theme.Brass.r, _theme.Brass.g, _theme.Brass.b, 0.44f));
        topAccent.rectTransform.anchorMin = new Vector2(0f, 1f);
        topAccent.rectTransform.anchorMax = new Vector2(1f, 1f);
        topAccent.rectTransform.pivot = new Vector2(0.5f, 1f);
        topAccent.rectTransform.sizeDelta = new Vector2(-72f, 2f);
        topAccent.raycastTarget = false;

        CreateSpacer(parent, 8f);
    }

    private void BuildAshParticles()
    {
        GameObject particleObject = new GameObject("AshParticles");
        particleObject.transform.SetParent(transform, false);
        particleObject.transform.position = new Vector3(0f, 6f, 0f);

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 10f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = new ParticleSystem.MinMaxGradient(EmberDimColor, EmberBrightColor);
        main.gravityModifier = 0.08f;
        main.maxParticles = 180;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 16f;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(18f, 1.2f, 1f);

        var velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.35f, -0.8f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.06f;
        noise.frequency = 0.18f;
        noise.scrollSpeed = 0.1f;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 1f);

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.62f, 0.62f, 0.62f), 0f),
                new GradientColorKey(new Color(0.88f, 0.88f, 0.88f), 0.45f),
                new GradientColorKey(new Color(0.44f, 0.44f, 0.44f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.22f, 0.12f),
                new GradientAlphaKey(0.12f, 0.78f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        particles.Clear();
        particles.Play();
    }

    private void StartNewJourney()
    {
        if (_isTransitioning)
        {
            return;
        }

        _isTransitioning = true;
        SetButtonsInteractable(false);
        SaveSystem.DeleteSave();
        ChapterState.ResetRuntime();
        DialogueEventSystem.ClearFlags();
        GameStateHub.ResetFloorProgressionRuntime();
        GameStateHub.SetCurrentFloorIndexRuntime(0);
        SceneLoader.LoadMain(saveBeforeLoad: false);
    }

    private void ContinueJourney()
    {
        if (_isTransitioning || !SaveSystem.HasSaveData())
        {
            RefreshContinueButton();
            return;
        }

        _isTransitioning = true;
        SetButtonsInteractable(false);
        DialogueEventSystem.ClearFlags();

        if (!SaveSystem.Load())
        {
            _isTransitioning = false;
            SetButtonsInteractable(true);
            RefreshContinueButton();
            SelectDefaultButton();
            return;
        }
    }

    private void QuitGame()
    {
        if (_isTransitioning)
        {
            return;
        }

        _isTransitioning = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RefreshContinueButton()
    {
        bool hasSaveData = SaveSystem.HasSaveData();
        if (_continueButton != null)
        {
            _continueButton.interactable = hasSaveData;
        }

        if (_continueButtonText != null)
        {
            _continueButtonText.text = hasSaveData ? "继续旅程" : "继续旅程（无存档）";
            _continueButtonText.color = hasSaveData ? _theme.PrimaryText : new Color(_theme.SecondaryText.r, _theme.SecondaryText.g, _theme.SecondaryText.b, 0.62f);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (_newJourneyButton != null)
        {
            _newJourneyButton.interactable = interactable;
        }

        if (_continueButton != null)
        {
            _continueButton.interactable = interactable && SaveSystem.HasSaveData();
        }

        if (_quitButton != null)
        {
            _quitButton.interactable = interactable;
        }
    }

    private void SelectDefaultButton()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        Button defaultButton = SaveSystem.HasSaveData() ? _continueButton : _newJourneyButton;
        if (defaultButton == null)
        {
            return;
        }

        eventSystem.SetSelectedGameObject(defaultButton.gameObject);
    }

    private Button CreateButton(Transform parent, string objectName, string label, UnityAction action, bool emphasize = true)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(436f, 78f);

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 436f;
        layoutElement.preferredHeight = 78f;

        Image image = buttonObject.AddComponent<Image>();
        ApplySprite(image, _theme.PrimaryButtonSprite, emphasize ? new Color(0.14f, 0.11f, 0.08f, 0.96f) : new Color(0.08f, 0.09f, 0.1f, 0.92f));

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonNormalTint;
        colors.highlightedColor = ButtonHoverTint;
        colors.selectedColor = ButtonHoverTint;
        colors.pressedColor = ButtonPressedTint;
        colors.disabledColor = ButtonDisabledTint;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        TMP_Text text = CreateTextBlock(
            buttonObject.transform,
            "Label",
            label,
            26,
            FontStyles.Bold,
            emphasize ? _theme.PrimaryText : new Color(_theme.SecondaryText.r, _theme.SecondaryText.g, _theme.SecondaryText.b, 0.92f),
            new Vector2(0f, 30f),
            220f,
            false);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(26f, 12f);
        text.rectTransform.offsetMax = new Vector2(-26f, -12f);
        text.alignment = TextAlignmentOptions.Center;

        return button;
    }

    private TMP_Text CreateTextBlock(
        Transform parent,
        string objectName,
        string value,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        Vector2 preferredSize,
        float characterSpacing,
        bool useDisplayFont)
    {
        return CreateTextBlock(parent, objectName, value, fontSize, fontStyle, color, preferredSize, characterSpacing, useDisplayFont, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
    }

    private TMP_Text CreateTextBlock(
        Transform parent,
        string objectName,
        string value,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        Vector2 preferredSize,
        float characterSpacing,
        bool useDisplayFont,
        Vector2 offsetMin,
        Vector2 offsetMax,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.sizeDelta = preferredSize;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        LayoutElement layoutElement = textObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = preferredSize.x > 0f ? preferredSize.x : 540f;
        layoutElement.preferredHeight = preferredSize.y > 0f ? preferredSize.y : fontSize + 16f;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = useDisplayFont ? _theme.DisplayFont : _theme.BodyFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.characterSpacing = characterSpacing;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void CreateSpacer(Transform parent, float height)
    {
        GameObject spacerObject = CreateUiObject("Spacer", parent);
        RectTransform rect = spacerObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1f, height);

        LayoutElement layoutElement = spacerObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = height;
        layoutElement.minHeight = height;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName);
        gameObject.transform.SetParent(parent, false);
        gameObject.layer = LayerMask.NameToLayer("UI");
        return gameObject;
    }

    private static string BuildHintText()
    {
        string hint = "Esc 也可以直接离开。";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        hint += "\nF5 快速开始，F9 快速继续。";
#endif
        return hint;
    }

    private Image CreateFullScreenImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = CreateUiObject(name, parent);
        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        GameObject rectObject = CreateUiObject(name, parent);
        RectTransform rect = rectObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;
        return rect;
    }

    private static Image CreateInsetImage(string name, RectTransform parent, Vector2 sizeDelta, Vector2 anchoredPosition, Color color)
    {
        RectTransform rect = CreateRect(name, parent, sizeDelta, anchoredPosition);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static void ApplySprite(Image image, Sprite sprite, Color fallbackColor)
    {
        if (image == null)
        {
            return;
        }

        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            return;
        }

        image.color = fallbackColor;
    }
}
