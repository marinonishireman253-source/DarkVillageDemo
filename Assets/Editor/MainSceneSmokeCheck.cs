using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainSceneSmokeCheck
{
    private const string RunningKey = "DarkVillage.MainSceneSmokeCheck.Running";
    private const string BatchModeKey = "DarkVillage.MainSceneSmokeCheck.BatchMode";
    private const string RunStartKey = "DarkVillage.MainSceneSmokeCheck.RunStart";
    private const string PlayStartKey = "DarkVillage.MainSceneSmokeCheck.PlayStart";
    private const double TimeoutSeconds = 10d;
    private const double ValidationDelaySeconds = 1.5d;

    private static bool s_CallbacksRegistered;

    [InitializeOnLoadMethod]
    private static void ResumeInBatchModeIfRunning()
    {
        if (!Application.isBatchMode)
        {
            Cleanup();
            return;
        }

        ResumeIfRunning();
    }

    [MenuItem("Tools/DarkVillage/Smoke Test Main Scene")]
    public static void RunFromMenu()
    {
        Start(runInBatchMode: false);
    }

    public static void RunInBatchMode()
    {
        Start(runInBatchMode: true);
    }

    [MenuItem("Tools/DarkVillage/Clear Smoke Test State")]
    public static void ClearStateFromMenu()
    {
        Cleanup();
        Debug.Log("[MainSceneSmokeCheck] Cleared smoke test state.");
    }

    private static void ResumeIfRunning()
    {
        if (!SessionState.GetBool(RunningKey, false))
        {
            return;
        }

        if (IsStaleEditorState())
        {
            Cleanup();
            return;
        }

        RegisterCallbacks();
    }

    private static void Start(bool runInBatchMode)
    {
        if (SessionState.GetBool(RunningKey, false))
        {
            Debug.LogWarning("[MainSceneSmokeCheck] Smoke check is already running.");
            return;
        }

        SessionState.SetBool(RunningKey, true);
        SessionState.SetBool(BatchModeKey, runInBatchMode);
        SessionState.SetString(RunStartKey, EditorApplication.timeSinceStartup.ToString("R"));
        SessionState.EraseString(PlayStartKey);
        RegisterCallbacks();

        try
        {
            EditorSceneManager.OpenScene(SceneLoader.MainScenePath, OpenSceneMode.Single);
        }
        catch (Exception exception)
        {
            Fail($"Could not open Main scene: {exception.Message}");
            return;
        }

        EditorApplication.isPlaying = true;
    }

    private static void RegisterCallbacks()
    {
        if (s_CallbacksRegistered)
        {
            return;
        }

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += Update;
        s_CallbacksRegistered = true;
    }

    private static void UnregisterCallbacks()
    {
        if (!s_CallbacksRegistered)
        {
            return;
        }

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= Update;
        s_CallbacksRegistered = false;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(RunningKey, false))
        {
            return;
        }

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetString(PlayStartKey, EditorApplication.timeSinceStartup.ToString("R"));
        }
    }

    private static void Update()
    {
        if (!SessionState.GetBool(RunningKey, false))
        {
            UnregisterCallbacks();
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        double runStart = ReadTime(RunStartKey);
        if (runStart > 0d && now - runStart > TimeoutSeconds)
        {
            Fail("Timed out waiting for Main scene smoke test to complete.");
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            return;
        }

        double playStart = ReadTime(PlayStartKey);
        if (playStart <= 0d)
        {
            playStart = now;
            SessionState.SetString(PlayStartKey, playStart.ToString("R"));
        }

        if (now - playStart < ValidationDelaySeconds)
        {
            return;
        }

        string failure = ValidateRuntimeState();
        if (failure != null)
        {
            Fail(failure);
            return;
        }

        Succeed();
    }

    private static string ValidateRuntimeState()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != SceneLoader.MainScenePath)
        {
            return $"Unexpected active scene: {activeScene.path}";
        }

        CorePrefabCatalog catalog = CorePrefabCatalog.Load();
        if (catalog == null)
        {
            return "CorePrefabCatalog could not be loaded from Resources.";
        }

        if (catalog.PlayerPrefab == null || catalog.StandardEnemyPrefab == null || catalog.InteractionPromptPrefab == null || catalog.BrazierPrefab == null)
        {
            return "CorePrefabCatalog is missing one or more prefab references.";
        }

        PlayerMover player = UnityEngine.Object.FindFirstObjectByType<PlayerMover>();
        if (player == null)
        {
            return "PlayerMover instance was not found.";
        }

        if (player.GetComponent<PlayerCombat>() == null || player.GetComponent<CombatantHealth>() == null)
        {
            return "Player instance is missing combat or health components.";
        }

        if (UnityEngine.Object.FindFirstObjectByType<InteractionPromptUI>() == null)
        {
            return "InteractionPromptUI instance was not found.";
        }

        if (UnityEngine.Object.FindFirstObjectByType<InventoryController>() == null)
        {
            return "InventoryController instance was not found.";
        }

        if (UnityEngine.Object.FindFirstObjectByType<SimpleEnemyController>() == null)
        {
            return "No SimpleEnemyController instance was found in Main scene.";
        }

        if (UnityEngine.Object.FindFirstObjectByType<AshParlorBrazierInteractable>() == null)
        {
            return "No AshParlorBrazierInteractable instance was found in Main scene.";
        }

        return null;
    }

    private static void Succeed()
    {
        bool runInBatchMode = SessionState.GetBool(BatchModeKey, false);
        Cleanup();
        Debug.Log("[MainSceneSmokeCheck] Main scene smoke test passed.");
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        if (runInBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    private static void Fail(string message)
    {
        bool runInBatchMode = SessionState.GetBool(BatchModeKey, false);
        Cleanup();
        Debug.LogError("[MainSceneSmokeCheck] " + message);
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        if (runInBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void Cleanup()
    {
        SessionState.EraseBool(RunningKey);
        SessionState.EraseBool(BatchModeKey);
        SessionState.EraseString(RunStartKey);
        SessionState.EraseString(PlayStartKey);
        UnregisterCallbacks();
    }

    private static bool IsStaleEditorState()
    {
        if (Application.isBatchMode)
        {
            return false;
        }

        if (SessionState.GetBool(BatchModeKey, false))
        {
            return false;
        }

        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static double ReadTime(string key)
    {
        string value = SessionState.GetString(key, string.Empty);
        return double.TryParse(value, out double result) ? result : 0d;
    }
}
