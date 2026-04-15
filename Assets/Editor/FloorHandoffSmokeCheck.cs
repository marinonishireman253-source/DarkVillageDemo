using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FloorHandoffSmokeCheck
{
    private const string RunningKey = "DarkVillage.FloorHandoffSmokeCheck.Running";
    private const string BatchModeKey = "DarkVillage.FloorHandoffSmokeCheck.BatchMode";
    private const string RunStartKey = "DarkVillage.FloorHandoffSmokeCheck.RunStart";
    private const string PlayStartKey = "DarkVillage.FloorHandoffSmokeCheck.PlayStart";
    private const string PhaseKey = "DarkVillage.FloorHandoffSmokeCheck.Phase";
    private const double TimeoutSeconds = 18d;
    private const double ValidationDelaySeconds = 1.5d;

    private static readonly MethodInfo ConfirmContinueMethod = typeof(FloorSummaryPanel).GetMethod("ConfirmContinue", BindingFlags.Instance | BindingFlags.NonPublic);
    private static bool s_CallbacksRegistered;

    private enum Phase
    {
        Boot = 0,
        PrepareSummary = 1,
        ShowSummary = 2,
        ConfirmSummary = 3,
        AwaitMirror = 4
    }

    [MenuItem("Tools/DarkVillage/Smoke Test Floor Handoff")]
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
            Debug.LogWarning("[FloorHandoffSmokeCheck] Smoke check is already running.");
            return;
        }

        SessionState.SetBool(RunningKey, true);
        SessionState.SetBool(BatchModeKey, runInBatchMode);
        SessionState.SetString(RunStartKey, EditorApplication.timeSinceStartup.ToString("R"));
        SessionState.EraseString(PlayStartKey);
        SessionState.SetInt(PhaseKey, (int)Phase.Boot);
        RegisterCallbacks();

        GameStateHub.SetCurrentFloorIndexRuntime(0);
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
            Fail("Timed out waiting for floor handoff smoke test to complete.");
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

        switch ((Phase)SessionState.GetInt(PhaseKey, (int)Phase.Boot))
        {
            case Phase.Boot:
                AdvanceBootPhase();
                break;
            case Phase.PrepareSummary:
                AdvancePrepareSummaryPhase();
                break;
            case Phase.ShowSummary:
                AdvanceShowSummaryPhase();
                break;
            case Phase.ConfirmSummary:
                AdvanceConfirmSummaryPhase();
                break;
            case Phase.AwaitMirror:
                AdvanceAwaitMirrorPhase();
                break;
        }
    }

    private static void AdvanceBootPhase()
    {
        if (SceneManager.GetActiveScene().path != SceneLoader.MainScenePath)
        {
            Fail("Unexpected active scene during boot phase.");
            return;
        }

        if (UnityEngine.Object.FindFirstObjectByType<AshParlorRunController>() == null)
        {
            return;
        }

        if (GameStateHub.CurrentFloorIndexRuntime != 0)
        {
            Fail($"Expected floor index 0 at boot but got {GameStateHub.CurrentFloorIndexRuntime}.");
            return;
        }

        SessionState.SetInt(PhaseKey, (int)Phase.PrepareSummary);
    }

    private static void AdvancePrepareSummaryPhase()
    {
        AshParlorRunController controller = UnityEngine.Object.FindFirstObjectByType<AshParlorRunController>();
        PlayerMover player = UnityEngine.Object.FindFirstObjectByType<PlayerMover>();
        if (controller == null || player == null)
        {
            return;
        }

        controller.PrepareFloorSummaryTest(AshParlorRunController.FloorSummaryTestPreset.RiskSummary, player);
        SessionState.SetInt(PhaseKey, (int)Phase.ShowSummary);
    }

    private static void AdvanceShowSummaryPhase()
    {
        AshParlorRunController controller = UnityEngine.Object.FindFirstObjectByType<AshParlorRunController>();
        PlayerMover player = UnityEngine.Object.FindFirstObjectByType<PlayerMover>();
        if (controller == null || player == null)
        {
            return;
        }

        controller.TryUseExit(player);
        SessionState.SetInt(PhaseKey, (int)Phase.ConfirmSummary);
    }

    private static void AdvanceConfirmSummaryPhase()
    {
        if (!FloorSummaryPanel.IsVisible || FloorSummaryPanel.Instance == null)
        {
            return;
        }

        if (ConfirmContinueMethod == null)
        {
            Fail("Could not reflect FloorSummaryPanel.ConfirmContinue.");
            return;
        }

        ConfirmContinueMethod.Invoke(FloorSummaryPanel.Instance, null);
        SessionState.SetInt(PhaseKey, (int)Phase.AwaitMirror);
    }

    private static void AdvanceAwaitMirrorPhase()
    {
        if (SceneManager.GetActiveScene().path != SceneLoader.MainScenePath)
        {
            return;
        }

        if (UnityEngine.Object.FindFirstObjectByType<MirrorCorridorRunController>() == null)
        {
            return;
        }

        if (GameStateHub.CurrentFloorIndexRuntime != 1)
        {
            Fail($"Expected floor index 1 after handoff but got {GameStateHub.CurrentFloorIndexRuntime}.");
            return;
        }

        Succeed();
    }

    private static void Succeed()
    {
        bool runInBatchMode = SessionState.GetBool(BatchModeKey, false);
        Cleanup();
        GameStateHub.SetCurrentFloorIndexRuntime(0);
        Debug.Log("[FloorHandoffSmokeCheck] Floor handoff smoke test passed.");
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
        GameStateHub.SetCurrentFloorIndexRuntime(0);
        Debug.LogError("[FloorHandoffSmokeCheck] " + message);
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
        SessionState.EraseInt(PhaseKey);
        UnregisterCallbacks();
    }

    private static double ReadTime(string key)
    {
        string rawValue = SessionState.GetString(key, string.Empty);
        return double.TryParse(rawValue, out double value) ? value : -1d;
    }
}
