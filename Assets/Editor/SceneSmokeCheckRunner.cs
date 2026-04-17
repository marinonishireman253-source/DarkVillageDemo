using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal abstract class SceneSmokeCheckRunner
{
    private const double TimeoutSeconds = 10d;
    private const double ValidationDelaySeconds = 1.5d;

    private bool _callbacksRegistered;

    protected abstract string CheckName { get; }
    protected abstract string StateKeyPrefix { get; }
    protected abstract string SceneLabel { get; }
    protected abstract string ScenePath { get; }

    private string RunningKey => StateKeyPrefix + ".Running";
    private string BatchModeKey => StateKeyPrefix + ".BatchMode";
    private string RunStartKey => StateKeyPrefix + ".RunStart";
    private string PlayStartKey => StateKeyPrefix + ".PlayStart";

    public void ResumeInBatchModeIfRunning()
    {
        if (!Application.isBatchMode)
        {
            Cleanup();
            return;
        }

        ResumeIfRunning();
    }

    public void RunFromMenu()
    {
        Start(runInBatchMode: false);
    }

    public void RunInBatchMode()
    {
        Start(runInBatchMode: true);
    }

    public void ClearStateFromMenu()
    {
        Cleanup();
        Debug.Log($"[{CheckName}] Cleared smoke test state.");
    }

    protected virtual void BeforeStart()
    {
    }

    protected virtual void AfterCleanup()
    {
    }

    protected abstract string ValidateRuntimeState();

    private void ResumeIfRunning()
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

    private void Start(bool runInBatchMode)
    {
        if (SessionState.GetBool(RunningKey, false))
        {
            Debug.LogWarning($"[{CheckName}] Smoke check is already running.");
            return;
        }

        SessionState.SetBool(RunningKey, true);
        SessionState.SetBool(BatchModeKey, runInBatchMode);
        SessionState.SetString(RunStartKey, EditorApplication.timeSinceStartup.ToString("R"));
        SessionState.EraseString(PlayStartKey);
        BeforeStart();
        RegisterCallbacks();

        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        catch (Exception exception)
        {
            Fail($"Could not open {SceneLabel}: {exception.Message}");
            return;
        }

        EditorApplication.isPlaying = true;
    }

    private void RegisterCallbacks()
    {
        if (_callbacksRegistered)
        {
            return;
        }

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += Update;
        _callbacksRegistered = true;
    }

    private void UnregisterCallbacks()
    {
        if (!_callbacksRegistered)
        {
            return;
        }

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= Update;
        _callbacksRegistered = false;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
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

    private void Update()
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
            Fail($"Timed out waiting for {SceneLabel} smoke test to complete.");
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

    private void Succeed()
    {
        bool runInBatchMode = SessionState.GetBool(BatchModeKey, false);
        Cleanup();
        Debug.Log($"[{CheckName}] {SceneLabel} smoke test passed.");
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        if (runInBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    private void Fail(string message)
    {
        bool runInBatchMode = SessionState.GetBool(BatchModeKey, false);
        Cleanup();
        Debug.LogError($"[{CheckName}] {message}");
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        if (runInBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }

    private void Cleanup()
    {
        SessionState.EraseBool(RunningKey);
        SessionState.EraseBool(BatchModeKey);
        SessionState.EraseString(RunStartKey);
        SessionState.EraseString(PlayStartKey);
        AfterCleanup();
        UnregisterCallbacks();
    }

    private bool IsStaleEditorState()
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
