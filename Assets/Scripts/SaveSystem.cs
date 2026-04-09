using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SaveSystem : MonoBehaviour
{
    private static readonly Vector3 UnifiedWorldForward = new Vector3(1f, 0f, 1f).normalized;
    private static readonly Vector3 UnifiedWorldRight = Vector3.Cross(Vector3.up, UnifiedWorldForward).normalized;

    [Serializable]
    private sealed class SaveData
    {
        public string sceneReference;
        public float[] playerPosition;
        public float[] playerForward;
        public int playerHealth;
        public string currentObjectiveId;
        public string currentObjectiveText;
        public string currentMarkerText;
        public FlagData[] flags;
        public string[] collectedItems;
        public string savedAtUtc;
    }

    [Serializable]
    private sealed class FlagData
    {
        public string id;
        public bool value;
    }

    private static SaveSystem s_Instance;
    private static bool s_Dirty;
    private static SaveData s_PendingLoad;
    private float _nextAutosaveAt;

    private static string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (s_Instance != null)
        {
            return;
        }

        GameObject root = new GameObject("__SaveSystem");
        DontDestroyOnLoad(root);
        s_Instance = root.AddComponent<SaveSystem>();
    }

    public static void MarkDirty()
    {
        s_Dirty = true;
    }

    public static bool HasSaveData()
    {
        return File.Exists(SavePath);
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }

        s_PendingLoad = null;
        s_Dirty = false;
    }

    public static bool TryLoadLatest()
    {
        if (!File.Exists(SavePath))
        {
            return false;
        }

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null || string.IsNullOrWhiteSpace(data.sceneReference))
        {
            return false;
        }

        NormalizeLegacySceneReference(data);
        s_PendingLoad = data;
        SceneLoader.Load(data.sceneReference);
        return true;
    }

    public static void SaveIfPossible()
    {
        s_Instance?.SaveCurrentState();
    }

    private void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveCurrentState();
        }
    }

    private void Update()
    {
        if (!s_Dirty)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!IsExplorationScene(activeScene))
        {
            return;
        }

        if (Time.unscaledTime < _nextAutosaveAt)
        {
            return;
        }

        SaveCurrentState();
    }

    private void OnApplicationQuit()
    {
        SaveCurrentState();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsExplorationScene(scene))
        {
            return;
        }

        if (s_PendingLoad != null && SceneMatches(scene, s_PendingLoad.sceneReference))
        {
            StartCoroutine(ApplyPendingLoadNextFrame());
            return;
        }

        StartCoroutine(SaveAfterSceneSettles());
    }

    private IEnumerator ApplyPendingLoadNextFrame()
    {
        yield return null;
        yield return null;

        SaveData data = s_PendingLoad;
        s_PendingLoad = null;

        if (data == null)
        {
            yield break;
        }

        Dictionary<string, bool> flags = new Dictionary<string, bool>();
        if (data.flags != null)
        {
            foreach (FlagData flag in data.flags)
            {
                if (flag == null || string.IsNullOrWhiteSpace(flag.id))
                {
                    continue;
                }

                flags[flag.id] = flag.value;
            }
        }

        ChapterState.RestoreRuntimeState(flags, data.collectedItems);

        PlayerMover player = FindFirstObjectByType<PlayerMover>();
        if (player != null)
        {
            if (data.playerPosition != null && data.playerPosition.Length == 3)
            {
                player.transform.position = new Vector3(data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]);
            }

            if (data.playerForward != null && data.playerForward.Length == 3)
            {
                Vector3 forward = new Vector3(data.playerForward[0], 0f, data.playerForward[2]);
                if (forward.sqrMagnitude > 0.001f)
                {
                    player.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
                }
            }

            CombatantHealth health = player.GetComponent<CombatantHealth>();
            if (health != null && data.playerHealth > 0)
            {
                health.RestoreTo(data.playerHealth);
            }
        }

        RestoreObjective(data);
        s_Dirty = false;
    }

    private IEnumerator SaveAfterSceneSettles()
    {
        yield return null;
        SaveCurrentState();
    }

    private void RestoreObjective(SaveData data)
    {
        QuestTracker tracker = QuestTracker.Instance;
        if (tracker == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(data.currentObjectiveId))
        {
            tracker.ClearObjective();
            return;
        }

        QuestObjectiveTarget[] objectives = FindObjectsByType<QuestObjectiveTarget>(FindObjectsSortMode.None);
        QuestObjectiveTarget matched = null;

        foreach (QuestObjectiveTarget objective in objectives)
        {
            if (objective != null && objective.ObjectiveId == data.currentObjectiveId)
            {
                matched = objective;
                break;
            }
        }

        Transform target = matched != null ? matched.transform : null;
        string objectiveText = string.IsNullOrWhiteSpace(data.currentObjectiveText)
            ? matched != null ? matched.ObjectiveText : "前往下一个目标"
            : data.currentObjectiveText;
        string markerText = string.IsNullOrWhiteSpace(data.currentMarkerText)
            ? matched != null ? matched.MarkerText : "目标"
            : data.currentMarkerText;
        tracker.SetObjective(data.currentObjectiveId, objectiveText, target, markerText);
    }

    private void SaveCurrentState()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!IsExplorationScene(activeScene))
        {
            return;
        }

        PlayerMover player = FindFirstObjectByType<PlayerMover>();
        if (player == null)
        {
            return;
        }

        SaveData data = BuildSaveData(activeScene, player);
        string json = JsonUtility.ToJson(data, true);
        Directory.CreateDirectory(Path.GetDirectoryName(SavePath) ?? Application.persistentDataPath);
        File.WriteAllText(SavePath, json);
        s_Dirty = false;
        _nextAutosaveAt = Time.unscaledTime + 1f;
    }

    private SaveData BuildSaveData(Scene activeScene, PlayerMover player)
    {
        CombatantHealth health = player.GetComponent<CombatantHealth>();
        QuestTracker tracker = QuestTracker.Instance;
        Dictionary<string, bool> runtimeFlags = ChapterState.GetRuntimeFlagsSnapshot();
        string[] collectedItems = ChapterState.GetCollectedItemsSnapshot();

        List<FlagData> flags = new List<FlagData>(runtimeFlags.Count);
        foreach (KeyValuePair<string, bool> entry in runtimeFlags)
        {
            flags.Add(new FlagData
            {
                id = entry.Key,
                value = entry.Value
            });
        }

        return new SaveData
        {
            sceneReference = string.IsNullOrWhiteSpace(activeScene.path) ? activeScene.name : activeScene.path,
            playerPosition = new[] { player.transform.position.x, player.transform.position.y, player.transform.position.z },
            playerForward = new[] { player.transform.forward.x, player.transform.forward.y, player.transform.forward.z },
            playerHealth = health != null ? health.CurrentHealth : 0,
            currentObjectiveId = tracker != null && !tracker.IsCompleted ? tracker.CurrentObjectiveId : string.Empty,
            currentObjectiveText = tracker != null && !tracker.IsCompleted ? tracker.CurrentObjectiveText : string.Empty,
            currentMarkerText = tracker != null && !tracker.IsCompleted ? tracker.CurrentMarkerText : string.Empty,
            flags = flags.ToArray(),
            collectedItems = collectedItems,
            savedAtUtc = DateTime.UtcNow.ToString("o")
        };
    }

    private bool IsExplorationScene(Scene scene)
    {
        return scene.name != SceneLoader.BootSceneName
            && scene.name != SceneLoader.TitleSceneName
            && scene.name != SceneLoader.VfxTestBenchSceneName;
    }

    private bool SceneMatches(Scene scene, string sceneReference)
    {
        return scene.name == sceneReference || scene.path == sceneReference;
    }

    private static void NormalizeLegacySceneReference(SaveData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.sceneReference))
        {
            return;
        }

        if (!TryGetLegacyWorldOffset(data.sceneReference, out Vector3 worldOffset))
        {
            return;
        }

        data.sceneReference = SceneLoader.MainScenePath;

        if (data.playerPosition != null && data.playerPosition.Length == 3)
        {
            data.playerPosition[0] += worldOffset.x;
            data.playerPosition[1] += worldOffset.y;
            data.playerPosition[2] += worldOffset.z;
        }
    }

    private static bool TryGetLegacyWorldOffset(string sceneReference, out Vector3 worldOffset)
    {
        Vector3 eventRoomOffset = UnifiedWorldForward * 58f + UnifiedWorldRight * 18f;
        Vector3 entranceOffset = UnifiedWorldForward * 92f + UnifiedWorldRight * 3f;
        Vector3 coreOffset = UnifiedWorldForward * 136f + UnifiedWorldRight * 3f;
        Vector3 bossOffset = UnifiedWorldForward * 182f + UnifiedWorldRight * 5f;
        Vector3 endOffset = bossOffset + UnifiedWorldForward * 26f + UnifiedWorldRight * 2f;

        switch (sceneReference)
        {
            case SceneLoader.PrologueEventRoomSceneName:
            case SceneLoader.PrologueEventRoomScenePath:
                worldOffset = eventRoomOffset;
                return true;

            case SceneLoader.Chapter01RedCreekEntranceSceneName:
            case SceneLoader.Chapter01RedCreekEntranceScenePath:
                worldOffset = entranceOffset;
                return true;

            case SceneLoader.Chapter01RedCreekCoreSceneName:
            case SceneLoader.Chapter01RedCreekCoreScenePath:
                worldOffset = coreOffset;
                return true;

            case SceneLoader.Chapter01BossHouseSceneName:
            case SceneLoader.Chapter01BossHouseScenePath:
                worldOffset = bossOffset;
                return true;

            case SceneLoader.Chapter01EndSceneName:
            case SceneLoader.Chapter01EndScenePath:
                worldOffset = endOffset;
                return true;

            default:
                worldOffset = Vector3.zero;
                return false;
        }
    }
}
