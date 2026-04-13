using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    private PlayerMover _player;

    private void Update()
    {
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerMover>();
        }

        SyncCanvasView();
    }

    private void OnDisable()
    {
        if (UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            hudView.HideInteractionPrompt();
        }
    }

    private void SyncCanvasView()
    {
        if (!UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            return;
        }

        bool shouldShow = !SimpleDialogueUI.IsOpen
            && !InventoryController.IsOpen
            && !AshParlorChoiceOverlay.IsVisible
            && !FloorSummaryPanel.IsVisible
            && _player != null
            && _player.HasInteractableTarget
            && _player.CurrentInteractable != null;

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
    }
}
