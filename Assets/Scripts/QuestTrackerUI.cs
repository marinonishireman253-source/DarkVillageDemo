using UnityEngine;

public class QuestTrackerUI : MonoBehaviour
{
    [SerializeField] private float completedBannerDuration = 2.5f;

    private float _completedBannerUntil;
    private string _completedBannerText = string.Empty;
    private QuestTracker _tracker;

    private void OnEnable()
    {
        QuestTracker.OnInstanceChanged += HandleTrackerInstanceChanged;
        BindTracker(QuestTracker.Instance);
    }

    private void OnDisable()
    {
        QuestTracker.OnInstanceChanged -= HandleTrackerInstanceChanged;
        BindTracker(null);
        HideCanvasView();
    }

    private void Update()
    {
        if (string.IsNullOrWhiteSpace(_completedBannerText))
        {
            return;
        }

        if (Time.unscaledTime <= _completedBannerUntil)
        {
            return;
        }

        _completedBannerText = string.Empty;
        SyncCanvasView();
    }

    private void HandleTrackerInstanceChanged(QuestTracker tracker)
    {
        BindTracker(tracker);
    }

    private void HandleObjectiveChanged(QuestTracker tracker)
    {
        SyncCanvasView();
    }

    private void HandleObjectiveCompleted(QuestTracker tracker)
    {
        if (tracker == null)
        {
            return;
        }

        _completedBannerText = string.IsNullOrWhiteSpace(tracker.LastCompletedObjectiveText)
            ? "当前目标已完成"
            : tracker.LastCompletedObjectiveText;
        _completedBannerUntil = Time.unscaledTime + Mathf.Max(0.5f, completedBannerDuration);
        SyncCanvasView();
    }

    private void BindTracker(QuestTracker tracker)
    {
        if (_tracker == tracker)
        {
            return;
        }

        if (_tracker != null)
        {
            _tracker.OnObjectiveChanged -= HandleObjectiveChanged;
            _tracker.OnObjectiveCompleted -= HandleObjectiveCompleted;
        }

        _tracker = tracker;

        if (_tracker != null)
        {
            _tracker.OnObjectiveChanged += HandleObjectiveChanged;
            _tracker.OnObjectiveCompleted += HandleObjectiveCompleted;
            SyncCanvasView();
            return;
        }

        _completedBannerText = string.Empty;
        _completedBannerUntil = 0f;
        HideCanvasView();
    }

    private void SyncCanvasView()
    {
        if (_tracker == null)
        {
            HideCanvasView();
            return;
        }

        if (!UiBootstrap.TryGetHudView(out HudCanvasView hudView))
        {
            return;
        }

        bool hasObjectiveText = !string.IsNullOrWhiteSpace(_tracker.CurrentObjectiveText);
        hudView.SetQuestPanel(
            hasObjectiveText,
            _tracker.IsCompleted ? "任务更新" : "当前目标",
            _tracker.CurrentObjectiveText);

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
