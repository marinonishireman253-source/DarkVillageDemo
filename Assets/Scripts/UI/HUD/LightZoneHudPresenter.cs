using UnityEngine;

public sealed class LightZoneHudPresenter : MonoBehaviour
{
    private void OnEnable()
    {
        LightZoneEffect.OnCurrentPlayerZoneChanged += HandleCurrentPlayerZoneChanged;
        PlayerMover.OnLocalInstanceChanged += HandlePlayerChanged;

        PlayerMover player = PlayerMover.LocalInstance;
        if (player != null)
        {
            HandleCurrentPlayerZoneChanged(LightZoneEffect.FindBest(player.transform.position));
        }
        else
        {
            HandleCurrentPlayerZoneChanged(null);
        }
    }

    private void OnDisable()
    {
        LightZoneEffect.OnCurrentPlayerZoneChanged -= HandleCurrentPlayerZoneChanged;
        PlayerMover.OnLocalInstanceChanged -= HandlePlayerChanged;

        if (UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            hudView.HideEnvironmentPanel();
        }
    }

    private void HandlePlayerChanged(PlayerMover player)
    {
        HandleCurrentPlayerZoneChanged(player != null ? LightZoneEffect.FindBest(player.transform.position) : null);
    }

    private void HandleCurrentPlayerZoneChanged(LightZoneEffect zone)
    {
        if (!UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            return;
        }

        if (zone == null)
        {
            hudView.HideEnvironmentPanel();
            return;
        }

        string icon = zone.IsLit ? "光" : "暗";
        string title = zone.IsLit ? "亮区" : "暗区";
        string body = zone.IsLit
            ? $"{zone.ZoneLabel}：交互范围 +50%，敌人逼近与出手恢复常态。"
            : $"{zone.ZoneLabel}：敌人逼近更快，敌袭频率 +30%，冲刺 -20%，交互范围 -30%。";
        hudView.SetEnvironmentPanel(true, icon, title, body);
    }
}
