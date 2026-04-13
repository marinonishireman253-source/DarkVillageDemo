using UnityEngine;

public class QuestTrackerUI : MonoBehaviour
{
    [SerializeField] private float completedBannerDuration = 2.5f;

    private float _completedBannerUntil;
    private string _completedBannerText = string.Empty;
    private bool _lastCompletedState;

    private void Update()
    {
        QuestTracker tracker = QuestTracker.Instance;
        if (tracker == null)
        {
            _lastCompletedState = false;
            HideCanvasView();
            return;
        }

        if (!_lastCompletedState && tracker.IsCompleted)
        {
            _completedBannerText = string.IsNullOrWhiteSpace(tracker.LastCompletedObjectiveText)
                ? "当前目标已完成"
                : tracker.LastCompletedObjectiveText;
            _completedBannerUntil = Time.unscaledTime + Mathf.Max(0.5f, completedBannerDuration);
        }

        _lastCompletedState = tracker.IsCompleted;

        SyncCanvasView(tracker);
    }

    private void OnDisable()
    {
        HideCanvasView();
    }

    private void SyncCanvasView(QuestTracker tracker)
    {
        if (!UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            return;
        }

        bool hasObjectiveText = !string.IsNullOrWhiteSpace(tracker.CurrentObjectiveText);
        hudView.SetQuestPanel(
            hasObjectiveText,
            tracker.IsCompleted ? "任务更新" : "当前目标",
            tracker.CurrentObjectiveText);

        bool showBanner = !string.IsNullOrWhiteSpace(_completedBannerText) && Time.unscaledTime <= _completedBannerUntil;
        if (showBanner)
        {
            hudView.SetCompletionBanner(true, _completedBannerText);
        }
        else
        {
            hudView.HideCompletionBanner();
        }

        hudView.HideWorldMarker();
    }

    private void HideCanvasView()
    {
        if (!UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            return;
        }

        hudView.HideQuestPanel();
        hudView.HideCompletionBanner();
        hudView.HideWorldMarker();
    }
}
