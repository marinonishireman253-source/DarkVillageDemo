using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MirrorCorridorSmokeCheck
{
    private const string RunningKey = "DarkVillage.MirrorCorridorSmokeCheck.Running";
    private const string PlayStartKey = "DarkVillage.MirrorCorridorSmokeCheck.PlayStart";
    private const string RunStartKey = "DarkVillage.MirrorCorridorSmokeCheck.RunStart";
    private const double TimeoutSeconds = 12d;
    private const double ValidationDelaySeconds = 1.75d;

    private static bool s_CallbacksRegistered;

    [MenuItem("Tools/DarkVillage/Smoke Test Mirror Corridor")]
    public static void RunFromMenu()
    {
        Start(false);
    }

    public static void RunInBatchMode()
    {
        Start(true);
    }

    private static void Start(bool runInBatchMode)
    {
        if (SessionState.GetBool(RunningKey, false))
        {
            Debug.LogWarning("[MirrorCorridorSmokeCheck] Smoke check is already running.");
            return;
        }

        SessionState.SetBool(RunningKey, true);
        SessionState.SetBool("DarkVillage.MirrorCorridorSmokeCheck.BatchMode", runInBatchMode);
        SessionState.SetString(RunStartKey, EditorApplication.timeSinceStartup.ToString("R"));
        SessionState.EraseString(PlayStartKey);
        RegisterCallbacks();

        GameStateHub.SetCurrentFloorIndexRuntime(1);
        EditorSceneManager.OpenScene(SceneLoader.MainScenePath, OpenSceneMode.Single);
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
            Fail("Timed out waiting for Mirror Corridor smoke test to complete.");
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
        if (SceneManager.GetActiveScene().path != SceneLoader.MainScenePath)
        {
            return "Unexpected active scene for Mirror Corridor smoke test.";
        }

        if (GameStateHub.CurrentFloorIndexRuntime != 1)
        {
            return $"CurrentFloorIndexRuntime should be 1 but was {GameStateHub.CurrentFloorIndexRuntime}.";
        }

        if (UnityEngine.Object.FindFirstObjectByType<MirrorCorridorRunController>() == null)
        {
            return "MirrorCorridorRunController instance was not found.";
        }

        if (UnityEngine.Object.FindFirstObjectByType<AshParlorRunController>() != null)
        {
            return "AshParlorRunController should not exist during Mirror Corridor smoke test.";
        }

        SimpleEnemyController[] enemies = UnityEngine.Object.FindObjectsByType<SimpleEnemyController>(FindObjectsSortMode.None);
        if (enemies.Length < 3)
        {
            return $"Expected at least 3 enemies in Mirror Corridor, found {enemies.Length}.";
        }

        if (UnityEngine.Object.FindObjectsByType<AshParlorBrazierInteractable>(FindObjectsSortMode.None).Length < 2)
        {
            return "Expected two brazier interactables in Mirror Corridor.";
        }

        if (UnityEngine.Object.FindFirstObjectByType<AshParlorChoicePromptInteractable>() == null)
        {
            return "Mirror Corridor choice prompt was not found.";
        }

        if (UnityEngine.Object.FindFirstObjectByType<PickupInteractable>() == null)
        {
            return "Mirror Corridor reward pickup was not found.";
        }

        return null;
    }

    private static void Succeed()
    {
        bool runInBatchMode = SessionState.GetBool("DarkVillage.MirrorCorridorSmokeCheck.BatchMode", false);
        Cleanup();
        GameStateHub.SetCurrentFloorIndexRuntime(0);
        Debug.Log("[MirrorCorridorSmokeCheck] Mirror Corridor smoke test passed.");
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
        bool runInBatchMode = SessionState.GetBool("DarkVillage.MirrorCorridorSmokeCheck.BatchMode", false);
        Cleanup();
        GameStateHub.SetCurrentFloorIndexRuntime(0);
        Debug.LogError("[MirrorCorridorSmokeCheck] " + message);
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
        SessionState.EraseBool("DarkVillage.MirrorCorridorSmokeCheck.BatchMode");
        SessionState.EraseString(RunStartKey);
        SessionState.EraseString(PlayStartKey);
        UnregisterCallbacks();
    }

    private static double ReadTime(string key)
    {
        string rawValue = SessionState.GetString(key, string.Empty);
        return double.TryParse(rawValue, out double value) ? value : -1d;
    }
}
