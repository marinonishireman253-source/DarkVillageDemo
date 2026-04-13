using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AshParlorChoiceOverlay : MonoBehaviour
{
    public static AshParlorChoiceOverlay Instance { get; private set; }
    public static bool IsVisible => Instance != null && Instance._isVisible;

    private AshParlorRunController _controller;
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

    public void Show(AshParlorRunController controller, PlayerMover player)
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

        AshParlorRunController controller = _controller;
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

        string title = "选择房分支";
        string body =
            "前方的路断成两条。直接在下面两个按钮里选一条路。";
        string hint = _riskSelected
            ? "A / ← / 1 选中风险    D / → / 2 切到保守    Enter / E 确认    Esc 返回"
            : "D / → / 2 选中保守    A / ← / 1 切到风险    Enter / E 确认    Esc 返回";
        string primaryLabel = _riskSelected
            ? "> [风险] 走向低吼\n终局更危险，但可获得线索结晶。"
            : "[风险] 走向低吼\n终局更危险，但可获得线索结晶。";
        string secondaryLabel = !_riskSelected
            ? "> [保守] 走向沉寂\n终局更平稳，但不会获得额外线索。"
            : "[保守] 走向沉寂\n终局更平稳，但不会获得额外线索。";

        modalView.ShowBinaryChoice(
            title,
            body,
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
