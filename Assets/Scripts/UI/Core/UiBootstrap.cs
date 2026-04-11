using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public sealed class UiBootstrap : MonoBehaviour
{
    public static UiBootstrap Instance { get; private set; }

    [Header("Optional Theme Asset")]
    [SerializeField] private UiTheme themeAsset;

    [Header("Optional Scene / Prefab References")]
    [SerializeField] private RectTransform configuredUiRoot;
    [SerializeField] private UiBackdropView configuredBackdropView;
    [SerializeField] private HudCanvasView configuredHudView;
    [SerializeField] private DialogueCanvasView configuredDialogueView;
    [SerializeField] private ModalCanvasView configuredModalView;
    [SerializeField] private RectTransform configuredWorldMarkerRoot;

    public UiTheme Theme { get; private set; }
    public UiStateCoordinator StateCoordinator { get; private set; }
    public HudCanvasView HudView { get; private set; }
    public DialogueCanvasView DialogueView { get; private set; }
    public ModalCanvasView ModalView { get; private set; }
    public UiBackdropView BackdropView { get; private set; }

    private RectTransform _uiRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Theme = LoadTheme();
        Theme.EnsureRuntimeDefaults();

        StateCoordinator = GetComponent<UiStateCoordinator>();
        if (StateCoordinator == null)
        {
            StateCoordinator = gameObject.AddComponent<UiStateCoordinator>();
        }

        EnsureEventSystem();
        EnsureRuntimeRoot();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static bool TryGetDialogueView(out DialogueCanvasView view)
    {
        view = Instance != null ? Instance.DialogueView : null;
        return view != null;
    }

    public static bool TryGetHudView(out HudCanvasView view)
    {
        view = Instance != null ? Instance.HudView : null;
        return view != null;
    }

    public static bool TryGetModalView(out ModalCanvasView view)
    {
        view = Instance != null ? Instance.ModalView : null;
        return view != null;
    }

    private UiTheme LoadTheme()
    {
        if (themeAsset != null)
        {
            return themeAsset;
        }

        UiTheme resourceTheme = Resources.Load<UiTheme>("UI/DefaultUiTheme");
        return resourceTheme != null ? resourceTheme : UiTheme.CreateRuntimeDefault();
    }

    private void EnsureEventSystem()
    {
        EventSystem existingEventSystem = FindFirstObjectByType<EventSystem>();
        if (existingEventSystem != null)
        {
            if (existingEventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                existingEventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            return;
        }

        GameObject eventSystemObject = new GameObject("__EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }

    private void EnsureRuntimeRoot()
    {
        BindConfiguredReferences();

        if (_uiRoot == null)
        {
            _uiRoot = UiFactory.CreateRect("UIRoot", transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        EnsureBackdropView();
        EnsureHudView();
        EnsureDialogueView();
        EnsureModalView();
        EnsureManagerFacade();
    }

    private void BindConfiguredReferences()
    {
        _uiRoot = configuredUiRoot != null
            ? configuredUiRoot
            : transform.Find("UIRoot") as RectTransform;

        BackdropView = configuredBackdropView != null
            ? configuredBackdropView
            : GetComponentInChildren<UiBackdropView>(true);

        HudView = configuredHudView != null
            ? configuredHudView
            : GetComponentInChildren<HudCanvasView>(true);

        DialogueView = configuredDialogueView != null
            ? configuredDialogueView
            : GetComponentInChildren<DialogueCanvasView>(true);

        ModalView = configuredModalView != null
            ? configuredModalView
            : GetComponentInChildren<ModalCanvasView>(true);

        configuredWorldMarkerRoot = configuredWorldMarkerRoot != null
            ? configuredWorldMarkerRoot
            : FindLayerRoot("Canvas_WorldMarkers", "WorldMarkerLayer");
    }

    private void EnsureBackdropView()
    {
        if (BackdropView == null)
        {
            Canvas backdropCanvas = FindOrCreateLayerCanvas("Canvas_Backdrop", 0);
            RectTransform backdropRoot = FindOrCreateLayerRoot(backdropCanvas.transform, "BackdropLayer");
            BackdropView = backdropRoot.GetComponent<UiBackdropView>();
            if (BackdropView == null)
            {
                BackdropView = backdropRoot.gameObject.AddComponent<UiBackdropView>();
            }
        }

        BackdropView.Initialize(Theme, StateCoordinator);
    }

    private void EnsureHudView()
    {
        Canvas hudCanvas = FindOrCreateLayerCanvas("Canvas_HUD", 100);
        Canvas worldMarkerCanvas = FindOrCreateLayerCanvas("Canvas_WorldMarkers", 200);

        RectTransform hudRoot = FindOrCreateLayerRoot(hudCanvas.transform, "HudLayer");
        RectTransform worldMarkerRoot = configuredWorldMarkerRoot != null
            ? configuredWorldMarkerRoot
            : FindOrCreateLayerRoot(worldMarkerCanvas.transform, "WorldMarkerLayer");

        configuredWorldMarkerRoot = worldMarkerRoot;

        if (HudView == null)
        {
            HudView = hudRoot.GetComponent<HudCanvasView>();
            if (HudView == null)
            {
                HudView = hudRoot.gameObject.AddComponent<HudCanvasView>();
            }
        }

        HudView.Initialize(Theme, worldMarkerRoot, StateCoordinator);
    }

    private void EnsureDialogueView()
    {
        Canvas dialogueCanvas = FindOrCreateLayerCanvas("Canvas_Dialogue", 300);
        RectTransform dialogueRoot = FindOrCreateLayerRoot(dialogueCanvas.transform, "DialogueLayer");
        if (DialogueView == null)
        {
            DialogueView = dialogueRoot.GetComponent<DialogueCanvasView>();
            if (DialogueView == null)
            {
                DialogueView = dialogueRoot.gameObject.AddComponent<DialogueCanvasView>();
            }
        }

        DialogueView.Initialize(Theme);
    }

    private void EnsureModalView()
    {
        FindOrCreateLayerCanvas("Canvas_Overlay", 400);
        Canvas modalCanvas = FindOrCreateLayerCanvas("Canvas_Modal", 500);
        RectTransform modalRoot = FindOrCreateLayerRoot(modalCanvas.transform, "ModalLayer");
        if (ModalView == null)
        {
            ModalView = modalRoot.GetComponent<ModalCanvasView>();
            if (ModalView == null)
            {
                ModalView = modalRoot.gameObject.AddComponent<ModalCanvasView>();
            }
        }

        ModalView.Initialize(Theme);
        FindOrCreateLayerCanvas("Canvas_Debug", 900);
    }

    private void EnsureManagerFacade()
    {
        UIManager manager = FindFirstObjectByType<UIManager>();
        if (manager == null)
        {
            manager = gameObject.GetComponent<UIManager>();
            if (manager == null)
            {
                manager = gameObject.AddComponent<UIManager>();
            }
        }

        manager.BindViews(DialogueView, HudView, ModalView);
        manager.RefreshBindings();
    }

    private Canvas FindOrCreateLayerCanvas(string canvasName, int sortingOrder)
    {
        if (_uiRoot == null)
        {
            return null;
        }

        Transform existing = _uiRoot.Find(canvasName);
        if (existing != null && existing.TryGetComponent(out Canvas existingCanvas))
        {
            existingCanvas.sortingOrder = sortingOrder;
            return existingCanvas;
        }

        return UiFactory.CreateLayerCanvas(_uiRoot, canvasName, sortingOrder);
    }

    private RectTransform FindOrCreateLayerRoot(Transform canvasTransform, string rootName)
    {
        if (canvasTransform == null)
        {
            return null;
        }

        Transform existing = canvasTransform.Find(rootName);
        if (existing is RectTransform existingRect)
        {
            return existingRect;
        }

        return UiFactory.CreateRect(rootName, canvasTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
    }

    private RectTransform FindLayerRoot(string canvasName, string rootName)
    {
        if (_uiRoot == null)
        {
            return null;
        }

        Transform canvas = _uiRoot.Find(canvasName);
        if (canvas == null)
        {
            return null;
        }

        return canvas.Find(rootName) as RectTransform;
    }
}
