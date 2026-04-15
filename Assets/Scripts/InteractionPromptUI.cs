using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private Canvas promptCanvas;
    [SerializeField] private CanvasGroup promptCanvasGroup;
    [SerializeField] private Image promptBackground;
    [SerializeField] private TMP_Text displayNameText;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text keyText;

    private PlayerMover _player;
    private UiStateCoordinator _stateCoordinator;

    private void Awake()
    {
        HideLocalPromptImmediate();
    }

    private void OnEnable()
    {
        PlayerMover.OnLocalInstanceChanged += HandlePlayerChanged;
        UiStateCoordinator.OnInstanceChanged += HandleStateCoordinatorChanged;

        BindPlayer(PlayerMover.LocalInstance);
        BindStateCoordinator(UiStateCoordinator.Instance);
        SyncCanvasView();
    }

    private void OnDisable()
    {
        PlayerMover.OnLocalInstanceChanged -= HandlePlayerChanged;
        UiStateCoordinator.OnInstanceChanged -= HandleStateCoordinatorChanged;
        BindPlayer(null);
        BindStateCoordinator(null);

        if (UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            hudView.HideInteractionPrompt();
        }

        HideLocalPromptImmediate();
    }

    private void HandlePlayerChanged(PlayerMover player)
    {
        BindPlayer(player);
    }

    private void HandleInteractableTargetChanged(PlayerMover player, IInteractable interactable)
    {
        SyncCanvasView();
    }

    private void HandleStateCoordinatorChanged(UiStateCoordinator stateCoordinator)
    {
        BindStateCoordinator(stateCoordinator);
    }

    private void HandleModeChanged(UiStateCoordinator.UiMode mode)
    {
        SyncCanvasView();
    }

    private void BindPlayer(PlayerMover player)
    {
        if (_player == player)
        {
            return;
        }

        if (_player != null)
        {
            _player.OnInteractableTargetChanged -= HandleInteractableTargetChanged;
        }

        _player = player;

        if (_player != null)
        {
            _player.OnInteractableTargetChanged += HandleInteractableTargetChanged;
        }

        SyncCanvasView();
    }

    private void BindStateCoordinator(UiStateCoordinator stateCoordinator)
    {
        if (_stateCoordinator == stateCoordinator)
        {
            return;
        }

        if (_stateCoordinator != null)
        {
            _stateCoordinator.OnModeChanged -= HandleModeChanged;
        }

        _stateCoordinator = stateCoordinator;

        if (_stateCoordinator != null)
        {
            _stateCoordinator.OnModeChanged += HandleModeChanged;
        }

        SyncCanvasView();
    }

    private void SyncCanvasView()
    {
        bool shouldShow = ShouldShowPrompt();

        if (UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            HideLocalPromptImmediate();

            if (!shouldShow)
            {
                hudView.HideInteractionPrompt();
                return;
            }

            hudView.SetInteractionPrompt(
                true,
                _player.CurrentInteractable.DisplayName,
                _player.CurrentInteractable.PromptText,
                "E");
            return;
        }

        if (!shouldShow)
        {
            HideLocalPromptImmediate();
            return;
        }

        SetLocalPrompt(
            _player.CurrentInteractable.DisplayName,
            _player.CurrentInteractable.PromptText,
            "E");
    }

    public void ConfigureLocalPrompt(Canvas canvas, CanvasGroup canvasGroup, Image background, TMP_Text displayNameLabel, TMP_Text promptLabel, TMP_Text keyLabel)
    {
        promptCanvas = canvas;
        promptCanvasGroup = canvasGroup;
        promptBackground = background;
        displayNameText = displayNameLabel;
        promptText = promptLabel;
        keyText = keyLabel;
        HideLocalPromptImmediate();
    }

    private bool ShouldShowPrompt()
    {
        return UiStateCoordinator.AllowsInteractionPromptForMode(_stateCoordinator != null ? _stateCoordinator.CurrentMode : UiStateCoordinator.UiMode.Exploration)
            && _player != null
            && _player.HasInteractableTarget
            && _player.CurrentInteractable != null;
    }

    private void SetLocalPrompt(string displayName, string actionPrompt, string key)
    {
        if (displayNameText == null || promptText == null || keyText == null)
        {
            return;
        }

        displayNameText.text = string.IsNullOrWhiteSpace(displayName) ? "可交互对象" : displayName.Trim();
        promptText.text = string.IsNullOrWhiteSpace(actionPrompt) ? "交互" : actionPrompt.Trim();
        keyText.text = string.IsNullOrWhiteSpace(key) ? "E" : key.Trim();

        if (promptCanvas != null)
        {
            promptCanvas.enabled = true;
        }

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 1f;
            promptCanvasGroup.blocksRaycasts = false;
            promptCanvasGroup.interactable = false;
        }

        if (promptBackground != null)
        {
            promptBackground.enabled = true;
        }
    }

    private void HideLocalPromptImmediate()
    {
        if (promptCanvas != null)
        {
            promptCanvas.enabled = false;
        }

        if (promptCanvasGroup != null)
        {
            promptCanvasGroup.alpha = 0f;
            promptCanvasGroup.blocksRaycasts = false;
            promptCanvasGroup.interactable = false;
        }

        if (promptBackground != null)
        {
            promptBackground.enabled = false;
        }
    }
}
