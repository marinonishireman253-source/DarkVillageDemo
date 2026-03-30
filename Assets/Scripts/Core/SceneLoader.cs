using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader : MonoBehaviour
{
    public const string BootSceneName = "Boot";
    public const string TitleSceneName = "Title";
    public const string MainSceneName = "Main";

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

    public static void LoadBoot(float delay = 0f)
    {
        Instance.LoadScene(BootSceneName, delay);
    }

    public static void LoadTitle(float delay = 0f)
    {
        Instance.LoadScene(TitleSceneName, delay);
    }

    public static void LoadMain(float delay = 0f)
    {
        Instance.LoadScene(MainSceneName, delay);
    }

    public static void ReloadCurrent(float delay = 0f)
    {
        Instance.LoadScene(SceneManager.GetActiveScene().name, delay);
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

    private void LoadScene(string sceneName, float delay)
    {
        StopAllCoroutines();
        StartCoroutine(LoadSceneRoutine(sceneName, delay));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        SceneManager.LoadScene(sceneName);
    }
}
