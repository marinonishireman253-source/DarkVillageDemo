using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AshParlorChoiceOverlay : MonoBehaviour
{
    public static AshParlorChoiceOverlay Instance { get; private set; }
    public static bool IsVisible => Instance != null && Instance._isVisible;
    public static event System.Action<bool> OnVisibilityChanged;

    private FloorRunController _controller;
    private PlayerMover _player;
    private bool _isVisible;
    private bool _riskSelected = true;
    private float _previousTimeScale = 1f;
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
            Hide();
            Instance = null;
        }
    }

    private void Update()
    {
        if (!_isVisible)
        {
            return;
        }

        if (Time.frameCount <= _openedFrame)
        {
            return;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Hide();
                return;
            }

            if (Keyboard.current.aKey.wasPressedThisFrame
                || Keyboard.current.leftArrowKey.wasPressedThisFrame
                || Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                SetSelection(true);
            }
            else if (Keyboard.current.dKey.wasPressedThisFrame
                || Keyboard.current.rightArrowKey.wasPressedThisFrame
                || Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                SetSelection(false);
            }
            else if (Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.spaceKey.wasPressedThisFrame
                || Keyboard.current.eKey.wasPressedThisFrame)
            {
                ConfirmSelection();
                return;
            }
        }

        RefreshModal();
    }

    public void Show(FloorRunController controller, PlayerMover player)
    {
        if (_isVisible || controller == null)
        {
            return;
        }

        _controller = controller;
        _player = player;
        _riskSelected = true;
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _isVisible = true;
        _openedFrame = Time.frameCount;
        OnVisibilityChanged?.Invoke(true);
        RefreshModal();
    }

    public void Hide(bool restoreTimeScale = true)
    {
        if (!_isVisible)
        {
            return;
        }

        _isVisible = false;
        _controller = null;
        _player = null;
        _openedFrame = -1;
        OnVisibilityChanged?.Invoke(false);

        if (restoreTimeScale)
        {
            Time.timeScale = _previousTimeScale;
        }

        if (UiBootstrap.TryGetModalView(out ModalCanvasView modalView))
        {
            modalView.HideModal();
        }
    }

    private void SetSelection(bool selectRisk)
    {
        if (_riskSelected == selectRisk)
        {
            return;
        }

        _riskSelected = selectRisk;
        RefreshModal();
    }

    private void ConfirmSelection()
    {
        if (_controller == null)
        {
            Hide();
            return;
        }

        FloorRunController controller = _controller;
        PlayerMover player = _player;
        AshParlorChoiceInteractable.ChoiceKind kind = _riskSelected
            ? AshParlorChoiceInteractable.ChoiceKind.Risky
            : AshParlorChoiceInteractable.ChoiceKind.Safe;

        Hide();
        controller.TryResolveChoice(kind, player);
    }

    private void RefreshModal()
    {
        if (!UiBootstrap.TryGetModalView(out ModalCanvasView modalView))
        {
            return;
        }

        FloorRunController.ChoiceOverlayConfig config = _controller != null
            ? _controller.GetChoiceOverlayConfig()
            : new FloorRunController.ChoiceOverlayConfig(
                "选择房分支",
                "前方的路断成两条。直接在下面两个按钮里选一条路。",
                string.Empty,
                "风险",
                "走向更危险的答案。",
                "保守",
                "走向更安全的沉默。");
        string defaultHint = _riskSelected
            ? "A / ← / 1 选中左侧    D / → / 2 切到右侧    Enter / E 确认    Esc 返回"
            : "D / → / 2 选中右侧    A / ← / 1 切到左侧    Enter / E 确认    Esc 返回";
        string hint = string.IsNullOrWhiteSpace(config.Hint) ? defaultHint : config.Hint;
        string primaryLabel = _riskSelected
            ? $"> [{config.RiskTitle}] {config.RiskDetail}"
            : $"[{config.RiskTitle}] {config.RiskDetail}";
        string secondaryLabel = !_riskSelected
            ? $"> [{config.SafeTitle}] {config.SafeDetail}"
            : $"[{config.SafeTitle}] {config.SafeDetail}";

        modalView.ShowBinaryChoice(
            config.Title,
            config.Body,
            hint,
            primaryLabel,
            secondaryLabel,
            SelectRisk,
            SelectSafe);
    }

    private void SelectRisk()
    {
        _riskSelected = true;
        ConfirmSelection();
    }

    private void SelectSafe()
    {
        _riskSelected = false;
        ConfirmSelection();
    }
}
