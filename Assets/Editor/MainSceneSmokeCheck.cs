using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainSceneSmokeCheck
{
    private static readonly Runner s_Runner = new();

    [InitializeOnLoadMethod]
    private static void ResumeInBatchModeIfRunning()
    {
        s_Runner.ResumeInBatchModeIfRunning();
    }

    [MenuItem("Tools/DarkVillage/Smoke Test Main Scene")]
    public static void RunFromMenu()
    {
        s_Runner.RunFromMenu();
    }

    public static void RunInBatchMode()
    {
        s_Runner.RunInBatchMode();
    }

    [MenuItem("Tools/DarkVillage/Clear Smoke Test State")]
    public static void ClearStateFromMenu()
    {
        s_Runner.ClearStateFromMenu();
    }

    private sealed class Runner : SceneSmokeCheckRunner
    {
        private const string SkipInitialMenuRedirectKey = "DarkVillage.SkipInitialMenuRedirect";

        protected override string CheckName => nameof(MainSceneSmokeCheck);
        protected override string StateKeyPrefix => "DarkVillage.MainSceneSmokeCheck";
        protected override string SceneLabel => "Main scene";
        protected override string ScenePath => SceneLoader.MainScenePath;

        protected override void BeforeStart()
        {
            PlayerPrefs.SetInt(SkipInitialMenuRedirectKey, 1);
            PlayerPrefs.Save();
        }

        protected override void AfterCleanup()
        {
            PlayerPrefs.DeleteKey(SkipInitialMenuRedirectKey);
        }

        protected override string ValidateRuntimeState()
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

            PlayerMover player = Object.FindFirstObjectByType<PlayerMover>();
            if (player == null)
            {
                return "PlayerMover instance was not found.";
            }

            if (player.GetComponent<PlayerCombat>() == null || player.GetComponent<CombatantHealth>() == null)
            {
                return "Player instance is missing combat or health components.";
            }

            if (Object.FindFirstObjectByType<InteractionPromptUI>() == null)
            {
                return "InteractionPromptUI instance was not found.";
            }

            if (Object.FindFirstObjectByType<InventoryController>() == null)
            {
                return "InventoryController instance was not found.";
            }

            if (Object.FindFirstObjectByType<SimpleEnemyController>() == null)
            {
                return "No SimpleEnemyController instance was found in Main scene.";
            }

            if (Object.FindFirstObjectByType<AshParlorBrazierInteractable>() == null)
            {
                return "No AshParlorBrazierInteractable instance was found in Main scene.";
            }

            return null;
        }
    }
}
