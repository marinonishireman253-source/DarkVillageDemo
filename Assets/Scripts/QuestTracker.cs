using UnityEngine;

public class QuestTracker : MonoBehaviour
{
    public static QuestTracker Instance { get; private set; }
    public static event System.Action<QuestTracker> OnInstanceChanged;

    public string CurrentObjectiveId { get; private set; } = string.Empty;
    public string CurrentObjectiveText { get; private set; } = string.Empty;
    public string CurrentMarkerText { get; private set; } = "目标";
    public string LastCompletedObjectiveText { get; private set; } = string.Empty;
    public Transform CurrentTarget { get; private set; }
    public bool IsCompleted { get; private set; }

    public event System.Action<QuestTracker> OnObjectiveChanged;
    public event System.Action<QuestTracker> OnObjectiveCompleted;

    public bool IsObjectiveComplete(string objectiveId)
    {
        return !string.IsNullOrWhiteSpace(objectiveId)
            && !string.IsNullOrWhiteSpace(CurrentObjectiveId)
            && CurrentObjectiveId == objectiveId
            && IsCompleted;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        OnInstanceChanged?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            OnInstanceChanged?.Invoke(null);
        }
    }

    public void SetObjective(string objectiveId, string objectiveText, Transform target, string markerText = "目标")
    {
        CurrentObjectiveId = string.IsNullOrWhiteSpace(objectiveId) ? string.Empty : objectiveId.Trim();
        CurrentObjectiveText = string.IsNullOrWhiteSpace(objectiveText) ? "前往下一个目标" : objectiveText.Trim();
        CurrentMarkerText = string.IsNullOrWhiteSpace(markerText) ? "目标" : markerText.Trim();
        CurrentTarget = target;
        IsCompleted = false;
        OnObjectiveChanged?.Invoke(this);
    }

    public bool CompleteObjective(string objectiveId)
    {
        if (!string.IsNullOrWhiteSpace(objectiveId) && !string.IsNullOrWhiteSpace(CurrentObjectiveId) && CurrentObjectiveId != objectiveId)
        {
            return false;
        }

        if (IsCompleted)
        {
            return false;
        }

        IsCompleted = true;

        string baseText = string.IsNullOrWhiteSpace(CurrentObjectiveText)
            ? "当前目标已完成"
            : CurrentObjectiveText;

        LastCompletedObjectiveText = baseText;

        if (!baseText.StartsWith("已完成："))
        {
            baseText = $"已完成：{baseText}";
        }

        CurrentObjectiveText = baseText;
        CurrentTarget = null;
        OnObjectiveCompleted?.Invoke(this);
        OnObjectiveChanged?.Invoke(this);
        return true;
    }

    public void ClearObjective()
    {
        CurrentObjectiveId = string.Empty;
        CurrentObjectiveText = string.Empty;
        CurrentMarkerText = "目标";
        CurrentTarget = null;
        IsCompleted = false;
        OnObjectiveChanged?.Invoke(this);
    }
}
