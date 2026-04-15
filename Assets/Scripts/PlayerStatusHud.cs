using UnityEngine;

public sealed class PlayerStatusHud : MonoBehaviour
{
    private PlayerCombat _playerCombat;
    private CombatantHealth _health;

    private void OnEnable()
    {
        PlayerCombat.OnLocalInstanceChanged += HandlePlayerCombatChanged;
        BindCombat(PlayerCombat.LocalInstance);
    }

    private void OnDisable()
    {
        PlayerCombat.OnLocalInstanceChanged -= HandlePlayerCombatChanged;
        BindCombat(null);

        if (UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            hudView.HideStatusPanel();
        }
    }

    private void HandlePlayerCombatChanged(PlayerCombat playerCombat)
    {
        BindCombat(playerCombat);
    }

    private void HandleHealthChanged(CombatantHealth health)
    {
        SyncStatusPanel();
    }

    private void BindCombat(PlayerCombat playerCombat)
    {
        if (_playerCombat == playerCombat)
        {
            return;
        }

        if (_health != null)
        {
            _health.OnHealthChanged -= HandleHealthChanged;
        }

        _playerCombat = playerCombat;
        _health = _playerCombat != null ? _playerCombat.Health : null;

        if (_health != null)
        {
            _health.OnHealthChanged += HandleHealthChanged;
        }

        SyncStatusPanel();
    }

    private void SyncStatusPanel()
    {
        if (!UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            return;
        }

        if (_playerCombat == null || _health == null)
        {
            hudView.HideStatusPanel();
            return;
        }

        string title = "伊尔萨恩";
        string body = $"生命 {_health.CurrentHealth}/{_health.MaxHealth}";
        hudView.SetStatusPanel(true, title, body);
    }
}
