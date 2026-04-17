using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuSceneSmokeCheck
{
    private static readonly Runner s_Runner = new();

    [InitializeOnLoadMethod]
    private static void ResumeInBatchModeIfRunning()
    {
        s_Runner.ResumeInBatchModeIfRunning();
    }

    [MenuItem("Tools/DarkVillage/Smoke Test Menu Scene")]
    public static void RunFromMenu()
    {
        s_Runner.RunFromMenu();
    }

    public static void RunInBatchMode()
    {
        s_Runner.RunInBatchMode();
    }

    private sealed class Runner : SceneSmokeCheckRunner
    {
        protected override string CheckName => nameof(MenuSceneSmokeCheck);
        protected override string StateKeyPrefix => "DarkVillage.MenuSceneSmokeCheck";
        protected override string SceneLabel => "Menu scene";
        protected override string ScenePath => SceneLoader.MenuScenePath;

        protected override string ValidateRuntimeState()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != SceneLoader.MenuScenePath)
            {
                return $"Unexpected active scene: {activeScene.path}";
            }

            MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>();
            if (controller == null)
            {
                return "MainMenuController instance was not found.";
            }

            if (Camera.main == null)
            {
                return "Main camera was not found.";
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                return "EventSystem instance was not found.";
            }

            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            if (buttons.Length < 3)
            {
                return $"Expected at least 3 buttons, found {buttons.Length}.";
            }

            Text[] labels = Object.FindObjectsByType<Text>(FindObjectsSortMode.None);
            bool foundTitle = false;
            bool foundSubtitle = false;
            for (int i = 0; i < labels.Length; i++)
            {
                Text label = labels[i];
                if (label == null)
                {
                    continue;
                }

                if (label.text == "ERSARN")
                {
                    foundTitle = true;
                }
                else if (label.text == "异常空间")
                {
                    foundSubtitle = true;
                }
            }

            if (!foundTitle || !foundSubtitle)
            {
                return "Menu title or subtitle text was not found.";
            }

            if (Object.FindFirstObjectByType<ParticleSystem>() == null)
            {
                return "Ash particle system was not found.";
            }

            return null;
        }
    }
}
