using UnityEngine;

public sealed class PlayerStatusHud : MonoBehaviour
{
    private PlayerCombat _playerCombat;

    private void Update()
    {
        if (_playerCombat == null)
        {
            PlayerMover player = FindFirstObjectByType<PlayerMover>();
            _playerCombat = player != null ? player.GetComponent<PlayerCombat>() : null;
        }

        SyncStatusPanel();
    }

    private void OnDisable()
    {
        if (UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            hudView.HideStatusPanel();
        }
    }

    private void SyncStatusPanel()
    {
        if (!UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            return;
        }

        if (_playerCombat == null || _playerCombat.Health == null)
        {
            hudView.HideStatusPanel();
            return;
        }

        string title = "伊尔萨恩";
        string body = $"生命 {_playerCombat.Health.CurrentHealth}/{_playerCombat.Health.MaxHealth}";
        hudView.SetStatusPanel(true, title, body);
    }
}
