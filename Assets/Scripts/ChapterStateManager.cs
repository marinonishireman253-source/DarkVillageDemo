using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChapterState", menuName = "Ersarn/Chapter State")]
public class ChapterState : ScriptableObject
{
    public enum ChoiceResult
    {
        None,
        Risk,
        Safe
    }

    [System.Serializable]
    public class QuestEntry
    {
        public string objectiveId;
        public bool completed;
    }

    [System.Serializable]
    public class FlagEntry
    {
        public string flagId;
        public bool value;
    }

    [Header("章节信息")]
    [SerializeField] private string chapterId;
    [SerializeField] private string chapterName;

    [Header("任务状态")]
    [SerializeField] private List<QuestEntry> questStates = new List<QuestEntry>();

    [Header("标记状态")]
    [SerializeField] private List<FlagEntry> flags = new List<FlagEntry>();

    [Header("已收集物品")]
    [SerializeField] private List<string> collectedItems = new List<string>();

    public string ChapterId => chapterId;
    public string ChapterName => chapterName;
    public IReadOnlyList<QuestEntry> QuestStates => questStates;
    public IReadOnlyList<FlagEntry> Flags => flags;
    public IReadOnlyList<string> CollectedItems => collectedItems;

    // 运行时状态（不序列化）
    private static readonly Dictionary<string, bool> _runtimeFlags = new Dictionary<string, bool>();
    private static readonly HashSet<string> _runtimeCollected = new HashSet<string>();
    private static readonly List<string> _runtimeCollectedOrder = new List<string>();
    private static ChoiceResult _runtimeChoiceResult = ChoiceResult.None;

    public static event System.Action OnCollectedItemsChanged;
    public static event System.Action<ChoiceResult> OnChoiceResultChanged;

    public static ChoiceResult CurrentChoiceResult => _runtimeChoiceResult;

    public static void SetFlag(string flagId, bool value)
    {
        if (string.IsNullOrWhiteSpace(flagId))
        {
            return;
        }

        _runtimeFlags[flagId] = value;
        SaveSystem.MarkDirty();
    }

    public static bool GetFlag(string flagId)
    {
        return _runtimeFlags.TryGetValue(flagId, out bool value) && value;
    }

    public static void SetChoiceResult(ChoiceResult result)
    {
        if (_runtimeChoiceResult == result)
        {
            return;
        }

        _runtimeChoiceResult = result;
        SaveSystem.MarkDirty();
        OnChoiceResultChanged?.Invoke(_runtimeChoiceResult);
    }

    public static void CollectItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return;
        }

        string normalizedItemId = itemId.Trim();
        if (!_runtimeCollected.Add(normalizedItemId))
        {
            return;
        }

        _runtimeCollectedOrder.Add(normalizedItemId);
        SaveSystem.MarkDirty();
        OnCollectedItemsChanged?.Invoke();
    }

    public static bool HasItem(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId) && _runtimeCollected.Contains(itemId.Trim());
    }

    public static void ResetRuntime()
    {
        _runtimeFlags.Clear();
        _runtimeCollected.Clear();
        _runtimeCollectedOrder.Clear();
        _runtimeChoiceResult = ChoiceResult.None;
        SaveSystem.MarkDirty();
        OnCollectedItemsChanged?.Invoke();
        OnChoiceResultChanged?.Invoke(_runtimeChoiceResult);
    }

    public static Dictionary<string, bool> GetRuntimeFlagsSnapshot()
    {
        return new Dictionary<string, bool>(_runtimeFlags);
    }

    public static string[] GetCollectedItemsSnapshot()
    {
        return _runtimeCollectedOrder.ToArray();
    }

    public static void RestoreRuntimeState(Dictionary<string, bool> flags, IEnumerable<string> collectedItems, ChoiceResult choiceResult = ChoiceResult.None)
    {
        _runtimeFlags.Clear();
        _runtimeCollected.Clear();
        _runtimeCollectedOrder.Clear();
        _runtimeChoiceResult = choiceResult;

        if (flags != null)
        {
            foreach (KeyValuePair<string, bool> entry in flags)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                _runtimeFlags[entry.Key] = entry.Value;
            }
        }

        if (collectedItems != null)
        {
            foreach (string itemId in collectedItems)
            {
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    continue;
                }

                string normalizedItemId = itemId.Trim();
                if (_runtimeCollected.Add(normalizedItemId))
                {
                    _runtimeCollectedOrder.Add(normalizedItemId);
                }
            }
        }

        OnCollectedItemsChanged?.Invoke();
        OnChoiceResultChanged?.Invoke(_runtimeChoiceResult);
    }
}
