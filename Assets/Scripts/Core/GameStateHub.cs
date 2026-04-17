using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameStateHub : MonoBehaviour
{
    private const int DefaultFloorIndex = 0;
    private static int s_CurrentFloorIndex = DefaultFloorIndex;
    private static readonly HashSet<int> s_ClearedFloorIndices = new HashSet<int>();

    public readonly struct ObjectiveStateSnapshot
    {
        public ObjectiveStateSnapshot(string objectiveId, string objectiveText, string markerText, bool isCompleted)
        {
            ObjectiveId = string.IsNullOrWhiteSpace(objectiveId) ? string.Empty : objectiveId.Trim();
            ObjectiveText = string.IsNullOrWhiteSpace(objectiveText) ? string.Empty : objectiveText.Trim();
            MarkerText = string.IsNullOrWhiteSpace(markerText) ? "目标" : markerText.Trim();
            IsCompleted = isCompleted;
        }

        public string ObjectiveId { get; }
        public string ObjectiveText { get; }
        public string MarkerText { get; }
        public bool IsCompleted { get; }
    }

    public static GameStateHub Instance { get; private set; }
    public static event Action<GameStateHub> OnInstanceChanged;
    public static event Action<int> OnCurrentFloorIndexChanged;

    public static int CurrentFloorIndexRuntime => Mathf.Max(0, s_CurrentFloorIndex);
    public static IReadOnlyList<int> KnownFloorIndicesRuntime => GetKnownFloorIndices();

    public static void MarkRuntimeStateDirty()
    {
        SaveSystem.MarkDirty();
    }

    public static void SetCurrentFloorIndexRuntime(int floorIndex)
    {
        int normalizedFloorIndex = NormalizeFloorIndex(floorIndex);
        if (s_CurrentFloorIndex == normalizedFloorIndex)
        {
            return;
        }

        s_CurrentFloorIndex = normalizedFloorIndex;
        OnCurrentFloorIndexChanged?.Invoke(s_CurrentFloorIndex);
        SaveSystem.MarkDirty();
    }

    public static void ResetFloorProgressionRuntime()
    {
        bool hadClearedFloors = s_ClearedFloorIndices.Count > 0;
        s_ClearedFloorIndices.Clear();
        if (hadClearedFloors)
        {
            SaveSystem.MarkDirty();
        }
    }

    public static bool HasClearedFloorRuntime(int floorIndex)
    {
        return s_ClearedFloorIndices.Contains(NormalizeFloorIndex(floorIndex));
    }

    public static bool MarkFloorClearedRuntime(int floorIndex)
    {
        int normalizedFloorIndex = NormalizeFloorIndex(floorIndex);
        bool added = s_ClearedFloorIndices.Add(normalizedFloorIndex);
        if (added)
        {
            SaveSystem.MarkDirty();
        }

        return added;
    }

    public static int SelectNextFloorAfterCompletion(int completedFloorIndex)
    {
        MarkFloorClearedRuntime(completedFloorIndex);

        int nextSequentialFloorIndex = GetNextSequentialFloorIndex();
        if (nextSequentialFloorIndex >= 0)
        {
            SetCurrentFloorIndexRuntime(nextSequentialFloorIndex);
            return nextSequentialFloorIndex;
        }

        int[] availableFloorPool = GetAvailableFloorPoolSnapshot();
        int nextFloorIndex = availableFloorPool.Length > 0
            ? availableFloorPool[UnityEngine.Random.Range(0, availableFloorPool.Length)]
            : NormalizeFloorIndex(DefaultFloorIndex);
        SetCurrentFloorIndexRuntime(nextFloorIndex);
        return nextFloorIndex;
    }

    public int CurrentFloorIndex
    {
        get => CurrentFloorIndexRuntime;
        set => SetCurrentFloorIndexRuntime(value);
    }

    public int[] ClearedFloorIndices => GetClearedFloorIndicesSnapshot();

    public string CurrentObjectiveId => _tracker != null ? _tracker.CurrentObjectiveId : string.Empty;
    public string CurrentObjective => _tracker != null ? _tracker.CurrentObjectiveText : string.Empty;
    public string LastCompletedObjective => _tracker != null ? _tracker.LastCompletedObjectiveText : string.Empty;
    public Transform CurrentObjectiveTarget => _tracker != null ? _tracker.CurrentTarget : null;
    public bool IsCurrentObjectiveCompleted => _tracker != null && _tracker.IsCompleted;

    public ChapterState.ChoiceResult CurrentChoiceResult
    {
        get => ChapterState.CurrentChoiceResult;
        set => ChapterState.SetChoiceResult(value);
    }

    public event Action<string> OnFlagChanged;
    public event Action OnCollectedItemsChanged;
    public event Action<ChapterState.ChoiceResult> OnChoiceResultChanged;

    private QuestTracker _tracker;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        OnInstanceChanged?.Invoke(this);
        BindTracker(QuestTracker.Instance);
    }

    private void OnEnable()
    {
        QuestTracker.OnInstanceChanged += HandleTrackerInstanceChanged;
        ChapterState.OnFlagChanged += HandleChapterFlagChanged;
        ChapterState.OnCollectedItemsChanged += HandleCollectedItemsChanged;
        ChapterState.OnChoiceResultChanged += HandleChoiceResultChanged;
        BindTracker(QuestTracker.Instance);
    }

    private void OnDisable()
    {
        QuestTracker.OnInstanceChanged -= HandleTrackerInstanceChanged;
        ChapterState.OnFlagChanged -= HandleChapterFlagChanged;
        ChapterState.OnCollectedItemsChanged -= HandleCollectedItemsChanged;
        ChapterState.OnChoiceResultChanged -= HandleChoiceResultChanged;
        BindTracker(null);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            OnInstanceChanged?.Invoke(null);
        }
    }

    public bool IsObjectiveComplete(string objectiveId)
    {
        return _tracker != null && _tracker.IsObjectiveComplete(objectiveId);
    }

    public void SetObjective(string objectiveId, string objectiveText, Transform target, string markerText = "目标")
    {
        _tracker?.SetObjective(objectiveId, objectiveText, target, markerText);
    }

    public bool CompleteObjective(string objectiveId)
    {
        return _tracker != null && _tracker.CompleteObjective(objectiveId);
    }

    public void ClearObjective()
    {
        _tracker?.ClearObjective();
    }

    public void SetChapterFlag(string key, string value)
    {
        ChapterState.SetFlagValue(key, value);
    }

    public string GetChapterFlag(string key)
    {
        return ChapterState.GetFlagValue(key);
    }

    public bool IsChapterFlagSet(string key)
    {
        string value = GetChapterFlag(key);
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (bool.TryParse(value, out bool parsedBool))
        {
            return parsedBool;
        }

        if (int.TryParse(value, out int parsedInt))
        {
            return parsedInt != 0;
        }

        return true;
    }

    public bool IsCurrentObjective(string objectiveId)
    {
        return !string.IsNullOrWhiteSpace(objectiveId)
            && !string.IsNullOrWhiteSpace(CurrentObjectiveId)
            && string.Equals(CurrentObjectiveId, objectiveId.Trim(), StringComparison.Ordinal);
    }

    public int GetCollectedItemCount()
    {
        return InventoryController.GetCollectedItemCount();
    }

    public InventoryController.FloorCollectionSummary GetFloorCollectionSummary(IEnumerable<string> floorItemIds = null)
    {
        return InventoryController.GetCurrentFloorCollectionSummary(floorItemIds);
    }

    public bool HasClearedFloor(int floorIndex)
    {
        return HasClearedFloorRuntime(floorIndex);
    }

    public bool HasCollectedItem(string itemId)
    {
        return ChapterState.HasItem(itemId);
    }

    public void CollectItem(string itemId)
    {
        ChapterState.CollectItem(itemId);
    }

    public void ResetRuntimeState()
    {
        ChapterState.ResetRuntime();
    }

    public void ResetFloorProgression()
    {
        ResetFloorProgressionRuntime();
    }

    public void Save()
    {
        SaveSystem.Save();
    }

    public void Load()
    {
        SaveSystem.Load();
    }

    internal Dictionary<string, string> GetFlagSnapshot()
    {
        return ChapterState.GetRuntimeFlagsSnapshot();
    }

    internal string[] GetCollectedItemSnapshot()
    {
        return ChapterState.GetCollectedItemsSnapshot();
    }

    internal int[] GetClearedFloorIndicesSnapshot()
    {
        int[] snapshot = new int[s_ClearedFloorIndices.Count];
        s_ClearedFloorIndices.CopyTo(snapshot);
        Array.Sort(snapshot);
        return snapshot;
    }

    internal ObjectiveStateSnapshot GetObjectiveSnapshot()
    {
        if (_tracker == null)
        {
            return new ObjectiveStateSnapshot(string.Empty, string.Empty, "目标", false);
        }

        return new ObjectiveStateSnapshot(_tracker.CurrentObjectiveId, _tracker.CurrentObjectiveText, _tracker.CurrentMarkerText, _tracker.IsCompleted);
    }

    internal void RestoreState(
        Dictionary<string, string> flags,
        IEnumerable<string> collectedItems,
        ChapterState.ChoiceResult choiceResult,
        ObjectiveStateSnapshot objectiveState,
        Transform objectiveTarget)
    {
        ChapterState.RestoreRuntimeState(flags, collectedItems, choiceResult);

        if (_tracker == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(objectiveState.ObjectiveId))
        {
            _tracker.ClearObjective();
            return;
        }

        _tracker.SetObjective(objectiveState.ObjectiveId, objectiveState.ObjectiveText, objectiveTarget, objectiveState.MarkerText);
        if (objectiveState.IsCompleted)
        {
            _tracker.CompleteObjective(objectiveState.ObjectiveId);
        }
    }

    internal void RestoreClearedFloorIndices(IEnumerable<int> floorIndices)
    {
        s_ClearedFloorIndices.Clear();
        if (floorIndices == null)
        {
            return;
        }

        foreach (int floorIndex in floorIndices)
        {
            s_ClearedFloorIndices.Add(NormalizeFloorIndex(floorIndex));
        }
    }

    private void HandleTrackerInstanceChanged(QuestTracker tracker)
    {
        BindTracker(tracker);
    }

    private void BindTracker(QuestTracker tracker)
    {
        if (_tracker == tracker)
        {
            return;
        }

        if (_tracker != null)
        {
            _tracker.OnObjectiveChanged -= HandleQuestStateChanged;
            _tracker.OnObjectiveCompleted -= HandleQuestStateChanged;
        }

        _tracker = tracker;

        if (_tracker != null)
        {
            _tracker.OnObjectiveChanged += HandleQuestStateChanged;
            _tracker.OnObjectiveCompleted += HandleQuestStateChanged;
        }
    }

    private void HandleQuestStateChanged(QuestTracker tracker)
    {
        SaveSystem.MarkDirty();
    }

    private void HandleChapterFlagChanged(string flagId)
    {
        SaveSystem.MarkDirty();
        OnFlagChanged?.Invoke(flagId);
    }

    private void HandleCollectedItemsChanged()
    {
        SaveSystem.MarkDirty();
        OnCollectedItemsChanged?.Invoke();
    }

    private void HandleChoiceResultChanged(ChapterState.ChoiceResult choiceResult)
    {
        SaveSystem.MarkDirty();
        OnChoiceResultChanged?.Invoke(choiceResult);
    }

    private static int[] GetKnownFloorIndices()
    {
        Array rawValues = Enum.GetValues(typeof(FloorVariant));
        List<int> knownFloorIndices = new List<int>(rawValues.Length);
        foreach (object rawValue in rawValues)
        {
            int floorIndex = (int)rawValue;
            if (floorIndex >= 0 && !knownFloorIndices.Contains(floorIndex))
            {
                knownFloorIndices.Add(floorIndex);
            }
        }

        if (knownFloorIndices.Count == 0)
        {
            knownFloorIndices.Add(DefaultFloorIndex);
        }

        knownFloorIndices.Sort();
        return knownFloorIndices.ToArray();
    }

    private static int NormalizeFloorIndex(int floorIndex)
    {
        int[] knownFloorIndices = GetKnownFloorIndices();
        if (knownFloorIndices.Length == 0)
        {
            return DefaultFloorIndex;
        }

        for (int i = 0; i < knownFloorIndices.Length; i++)
        {
            if (knownFloorIndices[i] == floorIndex)
            {
                return floorIndex;
            }
        }

        if (floorIndex <= knownFloorIndices[0])
        {
            return knownFloorIndices[0];
        }

        return knownFloorIndices[knownFloorIndices.Length - 1];
    }

    private static int GetNextSequentialFloorIndex()
    {
        int[] knownFloorIndices = GetKnownFloorIndices();
        for (int i = 0; i < knownFloorIndices.Length; i++)
        {
            int floorIndex = knownFloorIndices[i];
            if (!s_ClearedFloorIndices.Contains(floorIndex))
            {
                return floorIndex;
            }
        }

        return -1;
    }

    private static int[] GetAvailableFloorPoolSnapshot()
    {
        int[] knownFloorIndices = GetKnownFloorIndices();
        List<int> availableFloorIndices = new List<int>(knownFloorIndices.Length);
        for (int i = 0; i < knownFloorIndices.Length; i++)
        {
            int floorIndex = knownFloorIndices[i];
            if (s_ClearedFloorIndices.Contains(floorIndex))
            {
                availableFloorIndices.Add(floorIndex);
            }
        }

        if (availableFloorIndices.Count == 0)
        {
            availableFloorIndices.Add(knownFloorIndices[0]);
        }

        return availableFloorIndices.ToArray();
    }
}
