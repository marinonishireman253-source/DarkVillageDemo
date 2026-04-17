using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Globalization;

public static class PlayerJumpSmokeCheck
{
    private const string RunningKey = "DarkVillage.PlayerJumpSmokeCheck.Running";
    private const string BatchModeKey = "DarkVillage.PlayerJumpSmokeCheck.BatchMode";
    private const string RunStartKey = "DarkVillage.PlayerJumpSmokeCheck.RunStart";
    private const string PlayStartKey = "DarkVillage.PlayerJumpSmokeCheck.PlayStart";
    private const string PhaseKey = "DarkVillage.PlayerJumpSmokeCheck.Phase";
    private const string BaselineYKey = "DarkVillage.PlayerJumpSmokeCheck.BaselineY";
    private const string PeakYKey = "DarkVillage.PlayerJumpSmokeCheck.PeakY";
    private const string SkipInitialMenuRedirectKey = "DarkVillage.SkipInitialMenuRedirect";
    private const double TimeoutSeconds = 12d;
    private const double ValidationDelaySeconds = 1.5d;
    private const float RequiredJumpRise = 0.45f;
    private const float LandingTolerance = 0.08f;

    private static bool s_CallbacksRegistered;

    private enum Phase
    {
        Boot = 0,
        TriggerJump = 1,
        AwaitApex = 2,
        AwaitLanding = 3
    }

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

    [MenuItem("Tools/DarkVillage/Smoke Test Player Jump")]
    public static void RunFromMenu()
    {
        Start(false);
    }

    public static void RunInBatchMode()
    {
        Start(true);
    }

    [MenuItem("Tools/DarkVillage/Clear Player Jump Smoke Test State")]
    public static void ClearStateFromMenu()
    {
        Cleanup();
        Debug.Log("[PlayerJumpSmokeCheck] Cleared smoke test state.");
    }

    private static void Start(bool runInBatchMode)
    {
        if (SessionState.GetBool(RunningKey, false))
        {
            Debug.LogWarning("[PlayerJumpSmokeCheck] Smoke check is already running.");
            return;
        }

        SessionState.SetBool(RunningKey, true);
        SessionState.SetBool(BatchModeKey, runInBatchMode);
        SessionState.SetString(RunStartKey, EditorApplication.timeSinceStartup.ToString("R", CultureInfo.InvariantCulture));
        SessionState.EraseString(PlayStartKey);
        SessionState.SetInt(PhaseKey, (int)Phase.Boot);
        SessionState.SetFloat(BaselineYKey, 0f);
        SessionState.SetFloat(PeakYKey, float.NegativeInfinity);

        PlayerPrefs.SetInt(SkipInitialMenuRedirectKey, 1);
        PlayerPrefs.Save();

        RegisterCallbacks();
        GameStateHub.SetCurrentFloorIndexRuntime(0);
        EditorSceneManager.OpenScene(SceneLoader.MainScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void ResumeIfRunning()
    {
        if (!SessionState.GetBool(RunningKey, false))
        {
            return;
        }

        RegisterCallbacks();
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
            SessionState.SetString(PlayStartKey, EditorApplication.timeSinceStartup.ToString("R", CultureInfo.InvariantCulture));
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
            Fail("Timed out waiting for player jump smoke test to complete. " + DescribeCurrentState());
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
            case Phase.TriggerJump:
                AdvanceTriggerJumpPhase();
                break;
            case Phase.AwaitApex:
                AdvanceAwaitApexPhase();
                break;
            case Phase.AwaitLanding:
                AdvanceAwaitLandingPhase();
                break;
        }
    }

    private static void AdvanceBootPhase()
    {
        if (SceneManager.GetActiveScene().path != SceneLoader.MainScenePath)
        {
            Fail("Unexpected active scene during player jump smoke test.");
            return;
        }

        if (DialogueRunner.IsActive)
        {
            DialogueRunner.Instance?.Advance();
            return;
        }

        if (SimpleDialogueUI.IsOpen)
        {
            SimpleDialogueUI.Instance?.Hide();
            return;
        }

        PlayerMover player = Object.FindFirstObjectByType<PlayerMover>();
        PlayerJumpMotor jumpMotor = Object.FindFirstObjectByType<PlayerJumpMotor>();
        if (player == null || jumpMotor == null)
        {
            return;
        }

        if (UiStateCoordinator.Instance != null && UiStateCoordinator.Instance.BlocksPlayerMovement)
        {
            return;
        }

        if (!player.IsGrounded)
        {
            return;
        }

        SessionState.SetFloat(BaselineYKey, player.transform.position.y);
        SessionState.SetFloat(PeakYKey, player.transform.position.y);
        SessionState.SetInt(PhaseKey, (int)Phase.TriggerJump);
    }

    private static void AdvanceTriggerJumpPhase()
    {
        PlayerJumpMotor jumpMotor = Object.FindFirstObjectByType<PlayerJumpMotor>();
        if (jumpMotor == null)
        {
            Fail("PlayerJumpMotor instance was not found.");
            return;
        }

        jumpMotor.QueueJump();
        SessionState.SetInt(PhaseKey, (int)Phase.AwaitApex);
    }

    private static void AdvanceAwaitApexPhase()
    {
        PlayerMover player = Object.FindFirstObjectByType<PlayerMover>();
        if (player == null)
        {
            Fail("PlayerMover instance disappeared during jump smoke test.");
            return;
        }

        float baselineY = SessionState.GetFloat(BaselineYKey, player.transform.position.y);
        float peakY = Mathf.Max(SessionState.GetFloat(PeakYKey, baselineY), player.transform.position.y);
        SessionState.SetFloat(PeakYKey, peakY);

        if (peakY - baselineY < RequiredJumpRise)
        {
            return;
        }

        if (player.VerticalVelocity > 0f)
        {
            return;
        }

        SessionState.SetInt(PhaseKey, (int)Phase.AwaitLanding);
    }

    private static void AdvanceAwaitLandingPhase()
    {
        PlayerMover player = Object.FindFirstObjectByType<PlayerMover>();
        if (player == null)
        {
            Fail("PlayerMover instance disappeared before landing.");
            return;
        }

        float baselineY = SessionState.GetFloat(BaselineYKey, player.transform.position.y);
        float peakY = SessionState.GetFloat(PeakYKey, baselineY);
        if (peakY - baselineY < RequiredJumpRise)
        {
            Fail($"Jump apex was too low: rise={peakY - baselineY:0.###}.");
            return;
        }

        if (!player.IsGrounded)
        {
            return;
        }

        if (Mathf.Abs(player.transform.position.y - baselineY) > LandingTolerance)
        {
            Fail($"Player landed at unexpected height: baseline={baselineY:0.###}, current={player.transform.position.y:0.###}.");
            return;
        }

        Succeed();
    }

    private static void Succeed()
    {
        bool runInBatchMode = SessionState.GetBool(BatchModeKey, false);
        Cleanup();
        Debug.Log("[PlayerJumpSmokeCheck] Player jump smoke test passed.");
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
        Debug.LogError("[PlayerJumpSmokeCheck] " + message);
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
        PlayerPrefs.DeleteKey(SkipInitialMenuRedirectKey);
        SessionState.EraseBool(RunningKey);
        SessionState.EraseBool(BatchModeKey);
        SessionState.EraseString(RunStartKey);
        SessionState.EraseString(PlayStartKey);
        SessionState.EraseInt(PhaseKey);
        SessionState.EraseFloat(BaselineYKey);
        SessionState.EraseFloat(PeakYKey);
        UnregisterCallbacks();
    }

    private static double ReadTime(string key)
    {
        string rawValue = SessionState.GetString(key, string.Empty);
        return double.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double value) ? value : -1d;
    }

    private static string DescribeCurrentState()
    {
        Phase phase = (Phase)SessionState.GetInt(PhaseKey, (int)Phase.Boot);
        PlayerMover player = Object.FindFirstObjectByType<PlayerMover>();
        PlayerJumpMotor jumpMotor = Object.FindFirstObjectByType<PlayerJumpMotor>();
        if (player == null)
        {
            return $"phase={phase}, player=missing, jumpMotor={(jumpMotor == null ? "missing" : "present")}";
        }

        float baselineY = SessionState.GetFloat(BaselineYKey, player.transform.position.y);
        float peakY = SessionState.GetFloat(PeakYKey, player.transform.position.y);
        return $"phase={phase}, grounded={player.IsGrounded}, y={player.transform.position.y:0.###}, baseline={baselineY:0.###}, peak={peakY:0.###}, verticalVelocity={player.VerticalVelocity:0.###}, jumpMotor={(jumpMotor == null ? "missing" : "present")}";
    }
}
