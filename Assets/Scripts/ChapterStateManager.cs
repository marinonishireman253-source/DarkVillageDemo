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
        public string value;
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
    private static readonly Dictionary<string, string> _runtimeFlags = new Dictionary<string, string>();
    private static readonly HashSet<string> _runtimeCollected = new HashSet<string>();
    private static readonly List<string> _runtimeCollectedOrder = new List<string>();
    private static ChoiceResult _runtimeChoiceResult = ChoiceResult.None;

    public static event System.Action OnCollectedItemsChanged;
    public static event System.Action<ChoiceResult> OnChoiceResultChanged;
    public static event System.Action<string> OnFlagChanged;

    public static ChoiceResult CurrentChoiceResult => _runtimeChoiceResult;

    public static void SetFlag(string flagId, bool value)
    {
        SetFlagValue(flagId, value ? "true" : "false");
    }

    public static void SetFlagValue(string flagId, string value)
    {
        if (string.IsNullOrWhiteSpace(flagId))
        {
            return;
        }

        string normalizedFlagId = flagId.Trim();
        string normalizedValue = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

        if (_runtimeFlags.TryGetValue(normalizedFlagId, out string currentValue) && currentValue == normalizedValue)
        {
            return;
        }

        _runtimeFlags[normalizedFlagId] = normalizedValue;
        OnFlagChanged?.Invoke(normalizedFlagId);
    }

    public static bool GetFlag(string flagId)
    {
        string value = GetFlagValue(flagId);
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

    public static string GetFlagValue(string flagId)
    {
        if (string.IsNullOrWhiteSpace(flagId))
        {
            return string.Empty;
        }

        return _runtimeFlags.TryGetValue(flagId.Trim(), out string value) ? value : string.Empty;
    }

    public static void SetChoiceResult(ChoiceResult result)
    {
        if (_runtimeChoiceResult == result)
        {
            return;
        }

        _runtimeChoiceResult = result;
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
        OnCollectedItemsChanged?.Invoke();
    }

    public static bool HasItem(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId) && _runtimeCollected.Contains(itemId.Trim());
    }

    public static void ResetRuntime()
    {
        string[] changedFlags = new string[_runtimeFlags.Count];
        _runtimeFlags.Keys.CopyTo(changedFlags, 0);

        _runtimeFlags.Clear();
        _runtimeCollected.Clear();
        _runtimeCollectedOrder.Clear();
        _runtimeChoiceResult = ChoiceResult.None;

        for (int i = 0; i < changedFlags.Length; i++)
        {
            OnFlagChanged?.Invoke(changedFlags[i]);
        }

        OnCollectedItemsChanged?.Invoke();
        OnChoiceResultChanged?.Invoke(_runtimeChoiceResult);
    }

    public static Dictionary<string, string> GetRuntimeFlagsSnapshot()
    {
        return new Dictionary<string, string>(_runtimeFlags);
    }

    public static string[] GetCollectedItemsSnapshot()
    {
        return _runtimeCollectedOrder.ToArray();
    }

    public static void RestoreRuntimeState(Dictionary<string, string> flags, IEnumerable<string> collectedItems, ChoiceResult choiceResult = ChoiceResult.None)
    {
        string[] previousFlags = new string[_runtimeFlags.Count];
        _runtimeFlags.Keys.CopyTo(previousFlags, 0);

        _runtimeFlags.Clear();
        _runtimeCollected.Clear();
        _runtimeCollectedOrder.Clear();
        _runtimeChoiceResult = choiceResult;

        if (flags != null)
        {
            foreach (KeyValuePair<string, string> entry in flags)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                _runtimeFlags[entry.Key.Trim()] = string.IsNullOrWhiteSpace(entry.Value) ? string.Empty : entry.Value.Trim();
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

        for (int i = 0; i < previousFlags.Length; i++)
        {
            OnFlagChanged?.Invoke(previousFlags[i]);
        }

        foreach (string flagId in _runtimeFlags.Keys)
        {
            OnFlagChanged?.Invoke(flagId);
        }

        OnCollectedItemsChanged?.Invoke();
        OnChoiceResultChanged?.Invoke(_runtimeChoiceResult);
    }
}
