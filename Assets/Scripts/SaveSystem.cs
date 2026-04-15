using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SaveSystem : MonoBehaviour
{

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
        public string choiceResult;
        public FlagData[] flags;
        public string[] collectedItems;
        public int currentFloorIndex;
        public string savedAtUtc;
    }

    [Serializable]
    private sealed class FlagData
    {
        public string id;
        public string value;
        public bool legacyBoolValue;
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
        GameStateHub.SetCurrentFloorIndexRuntime(data.currentFloorIndex);
        SceneLoader.Load(data.sceneReference);
        return true;
    }

    public static void SaveIfPossible()
    {
        s_Instance?.SaveCurrentState();
    }

    public static void Save()
    {
        SaveIfPossible();
    }

    public static bool Load()
    {
        return TryLoadLatest();
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

        while (GameStateHub.Instance == null)
        {
            yield return null;
        }

        Dictionary<string, string> flags = new Dictionary<string, string>();
        if (data.flags != null)
        {
            foreach (FlagData flag in data.flags)
            {
                if (flag == null || string.IsNullOrWhiteSpace(flag.id))
                {
                    continue;
                }

                flags[flag.id] = !string.IsNullOrWhiteSpace(flag.value)
                    ? flag.value
                    : flag.legacyBoolValue ? "true" : "false";
            }
        }

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

        GameStateHub.Instance.RestoreState(
            flags,
            data.collectedItems,
            ParseChoiceResult(data.choiceResult),
            new GameStateHub.ObjectiveStateSnapshot(
                data.currentObjectiveId,
                data.currentObjectiveText,
                data.currentMarkerText,
                false),
            ResolveObjectiveTarget(data.currentObjectiveId));
        GameStateHub.SetCurrentFloorIndexRuntime(data.currentFloorIndex);
        s_Dirty = false;
    }

    private IEnumerator SaveAfterSceneSettles()
    {
        yield return null;
        SaveCurrentState();
    }

    private static Transform ResolveObjectiveTarget(string objectiveId)
    {
        if (string.IsNullOrWhiteSpace(objectiveId))
        {
            return null;
        }

        QuestObjectiveTarget[] objectives = FindObjectsByType<QuestObjectiveTarget>(FindObjectsSortMode.None);
        foreach (QuestObjectiveTarget objective in objectives)
        {
            if (objective != null && objective.ObjectiveId == objectiveId)
            {
                return objective.transform;
            }
        }

        return null;
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
        GameStateHub gameStateHub = GameStateHub.Instance;
        Dictionary<string, string> runtimeFlags = gameStateHub != null
            ? gameStateHub.GetFlagSnapshot()
            : new Dictionary<string, string>();
        string[] collectedItems = gameStateHub != null
            ? gameStateHub.GetCollectedItemSnapshot()
            : Array.Empty<string>();
        GameStateHub.ObjectiveStateSnapshot objectiveState = gameStateHub != null
            ? gameStateHub.GetObjectiveSnapshot()
            : new GameStateHub.ObjectiveStateSnapshot(string.Empty, string.Empty, "目标", false);

        List<FlagData> flags = new List<FlagData>(runtimeFlags.Count);
        foreach (KeyValuePair<string, string> entry in runtimeFlags)
        {
            flags.Add(new FlagData
            {
                id = entry.Key,
                value = entry.Value,
                legacyBoolValue = string.Equals(entry.Value, "true", StringComparison.OrdinalIgnoreCase)
            });
        }

        return new SaveData
        {
            sceneReference = string.IsNullOrWhiteSpace(activeScene.path) ? activeScene.name : activeScene.path,
            playerPosition = new[] { player.transform.position.x, player.transform.position.y, player.transform.position.z },
            playerForward = new[] { player.transform.forward.x, player.transform.forward.y, player.transform.forward.z },
            playerHealth = health != null ? health.CurrentHealth : 0,
            currentObjectiveId = !objectiveState.IsCompleted ? objectiveState.ObjectiveId : string.Empty,
            currentObjectiveText = !objectiveState.IsCompleted ? objectiveState.ObjectiveText : string.Empty,
            currentMarkerText = !objectiveState.IsCompleted ? objectiveState.MarkerText : string.Empty,
            choiceResult = (gameStateHub != null ? gameStateHub.CurrentChoiceResult : ChapterState.ChoiceResult.None).ToString(),
            flags = flags.ToArray(),
            collectedItems = collectedItems,
            currentFloorIndex = gameStateHub != null ? gameStateHub.CurrentFloorIndex : GameStateHub.CurrentFloorIndexRuntime,
            savedAtUtc = DateTime.UtcNow.ToString("o")
        };
    }

    private static ChapterState.ChoiceResult ParseChoiceResult(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return ChapterState.ChoiceResult.None;
        }

        return Enum.TryParse(rawValue, true, out ChapterState.ChoiceResult parsed)
            ? parsed
            : ChapterState.ChoiceResult.None;
    }

    private bool IsExplorationScene(Scene scene)
    {
        return scene.name == SceneLoader.MainSceneName || scene.path == SceneLoader.MainScenePath;
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

        data.sceneReference = SceneLoader.MainScenePath;
    }
}
