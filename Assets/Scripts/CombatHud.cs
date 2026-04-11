using UnityEngine;

public class CombatHud : MonoBehaviour
{
    private PlayerCombat _playerCombat;

    private void Update()
    {
        if (_playerCombat == null)
        {
            PlayerMover player = FindFirstObjectByType<PlayerMover>();
            _playerCombat = player != null ? player.GetComponent<PlayerCombat>() : null;
        }

        SyncCanvasView();
    }

    private void OnDisable()
    {
        if (UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            hudView.HideCombatPanel();
        }
    }

    private void SyncCanvasView()
    {
        if (!UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            return;
        }

        if (_playerCombat == null || _playerCombat.Health == null)
        {
            hudView.HideCombatPanel();
            return;
        }

        bool showEncounterDetails = CombatEncounterTrigger.ActiveEncounter != null;
        if (!showEncounterDetails)
        {
            hudView.HideCombatPanel();
            return;
        }

        string playerLine = $"伊尔萨恩  HP {_playerCombat.Health.CurrentHealth}/{_playerCombat.Health.MaxHealth}";
        string controlsLine = "攻击键: Space / J / 鼠标左键";
        string encounterLine = showEncounterDetails
            ? $"战斗: {CombatEncounterTrigger.ActiveEncounter.EncounterName}"
            : string.Empty;

        string enemyLine = string.Empty;
        if (showEncounterDetails)
        {
            enemyLine = CombatEncounterTrigger.ActiveEnemy != null && CombatEncounterTrigger.ActiveEnemy.Health != null
                ? $"{CombatEncounterTrigger.ActiveEnemy.EnemyName}  HP {CombatEncounterTrigger.ActiveEnemy.Health.CurrentHealth}/{CombatEncounterTrigger.ActiveEnemy.Health.MaxHealth}"
                : "敌人未锁定";
        }

        hudView.SetCombatPanel(true, playerLine, controlsLine, encounterLine, enemyLine, showEncounterDetails);
    }
}
