using UnityEngine;

public sealed class LightZoneHudPresenter : MonoBehaviour
{
    private void OnEnable()
    {
        LightZoneEffect.OnCurrentPlayerZoneChanged += HandleCurrentPlayerZoneChanged;

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

        if (UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            hudView.HideEnvironmentPanel();
        }
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

        string icon = zone.IsLit ? "☼" : "☾";
        string title = zone.IsLit ? "亮区" : "暗区";
        string body = zone.IsLit
            ? $"{zone.ZoneLabel}：交互更轻松。"
            : $"{zone.ZoneLabel}：敌人更凶，行动更受限。";
        hudView.SetEnvironmentPanel(true, icon, title, body);
    }
}
