using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader : MonoBehaviour
{
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

    public static void Load(string sceneReference, float delay = 0f)
    {
        Instance.LoadScene(delay);
    }

    public static void LoadMain(float delay = 0f)
    {
        Instance.LoadScene(delay);
    }

    public static void ReloadCurrent(float delay = 0f)
    {
        Instance.LoadScene(delay);
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

    private void LoadScene(float delay)
    {
        StopAllCoroutines();
        StartCoroutine(LoadSceneRoutine(delay));
    }

    private IEnumerator LoadSceneRoutine(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        if (GameStateHub.Instance != null)
        {
            GameStateHub.Instance.Save();
        }
        else
        {
            SaveSystem.SaveIfPossible();
        }
        SceneManager.LoadScene(MainScenePath);
    }
}
