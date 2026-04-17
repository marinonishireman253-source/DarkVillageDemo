using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MainScenePlaytestLauncher
{
    private const string SkipInitialMenuRedirectKey = "DarkVillage.SkipInitialMenuRedirect";

    [InitializeOnLoadMethod]
    private static void RegisterCallbacks()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("Tools/DarkVillage/Play Main Scene _F5")]
    public static void PlayMainScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        PlayerPrefs.SetInt(SkipInitialMenuRedirectKey, 1);
        PlayerPrefs.Save();
        EditorSceneManager.OpenScene(SceneLoader.MainScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

        PlayerPrefs.DeleteKey(SkipInitialMenuRedirectKey);
    }
}
