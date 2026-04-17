using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader : MonoBehaviour
{
    public const string MenuSceneName = "Menu";
    public const string MenuScenePath = "Assets/Scenes/Menu.unity";
    public const string MainSceneName = "Main";
    public const string MainScenePath = "Assets/Scenes/Main.unity";

    private static SceneLoader s_Instance;

    public static SceneLoader Instance
    {
        get
        {
            if (s_Instance != null)
            {
                return s_Instance;
            }

            GameObject root = new GameObject("__SceneLoader");
            s_Instance = root.AddComponent<SceneLoader>();
            DontDestroyOnLoad(root);
            return s_Instance;
        }
    }

    public static void Load(string sceneReference, float delay = 0f, bool saveBeforeLoad = true)
    {
        Instance.LoadScene(sceneReference, delay, saveBeforeLoad);
    }

    public static void LoadMain(float delay = 0f, bool saveBeforeLoad = true)
    {
        Instance.LoadScene(MainScenePath, delay, saveBeforeLoad);
    }

    public static void LoadMenu(float delay = 0f, bool saveBeforeLoad = true)
    {
        Instance.LoadScene(MenuScenePath, delay, saveBeforeLoad);
    }

    public static void ReloadCurrent(float delay = 0f, bool saveBeforeLoad = true)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        string currentSceneReference = !string.IsNullOrWhiteSpace(activeScene.path)
            ? activeScene.path
            : activeScene.name;

        if (string.IsNullOrWhiteSpace(currentSceneReference))
        {
            currentSceneReference = MainScenePath;
        }

        Instance.LoadScene(currentSceneReference, delay, saveBeforeLoad);
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

    private void LoadScene(string sceneReference, float delay, bool saveBeforeLoad)
    {
        StopAllCoroutines();
        StartCoroutine(LoadSceneRoutine(sceneReference, delay, saveBeforeLoad));
    }

    private IEnumerator LoadSceneRoutine(string sceneReference, float delay, bool saveBeforeLoad)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        if (saveBeforeLoad)
        {
            if (GameStateHub.Instance != null)
            {
                GameStateHub.Instance.Save();
            }
            else
            {
                SaveSystem.SaveIfPossible();
            }
        }

        string resolvedSceneReference = string.IsNullOrWhiteSpace(sceneReference)
            ? MainScenePath
            : sceneReference;
        SceneManager.LoadScene(resolvedSceneReference);
    }
}
