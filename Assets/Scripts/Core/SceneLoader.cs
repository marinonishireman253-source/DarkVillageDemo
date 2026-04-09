using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader : MonoBehaviour
{
    public const string BootSceneName = "Boot";
    public const string TitleSceneName = "Title";
    public const string MainSceneName = "Main";
    public const string VfxTestBenchSceneName = "SampleScene";
    public const string PrologueStreetSceneName = MainSceneName;
    public const string PrologueEventRoomSceneName = "Prologue_EventRoom";
    public const string Chapter01RedCreekEntranceSceneName = "Chapter01_RedCreek_Entrance";
    public const string Chapter01RedCreekCoreSceneName = "Chapter01_RedCreek_Core";
    public const string Chapter01BossHouseSceneName = "Chapter01_BossHouse";
    public const string Chapter01EndSceneName = "Chapter01_End";
    public const string BootScenePath = "Assets/Scenes/Boot/Boot.unity";
    public const string TitleScenePath = "Assets/Scenes/Title/Title.unity";
    public const string MainScenePath = "Assets/Scenes/Main.unity";
    public const string VfxTestBenchScenePath = "Assets/Scenes/SampleScene.unity";
    public const string PrologueEventRoomScenePath = "Assets/Scenes/Prologue/Prologue_EventRoom.unity";
    public const string Chapter01RedCreekEntranceScenePath = "Assets/Scenes/Chapter01/Chapter01_RedCreek_Entrance.unity";
    public const string Chapter01RedCreekCoreScenePath = "Assets/Scenes/Chapter01/Chapter01_RedCreek_Core.unity";
    public const string Chapter01BossHouseScenePath = "Assets/Scenes/Chapter01/Chapter01_BossHouse.unity";
    public const string Chapter01EndScenePath = "Assets/Scenes/Chapter01/Chapter01_End.unity";

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

    public static void Load(string sceneName, float delay = 0f)
    {
        Instance.LoadScene(sceneName, delay);
    }

    public static void LoadTitle(float delay = 0f)
    {
        Instance.LoadScene(TitleSceneName, delay);
    }

    public static void LoadMain(float delay = 0f)
    {
        Instance.LoadScene(MainSceneName, delay);
    }

    public static void LoadVfxTestBench(float delay = 0f)
    {
        Instance.LoadScene(VfxTestBenchSceneName, delay);
    }

    public static void LoadPrologueStreet(float delay = 0f)
    {
        Instance.LoadScene(PrologueStreetSceneName, delay);
    }

    public static void LoadPrologueEventRoom(float delay = 0f)
    {
        Instance.LoadScene(PrologueEventRoomSceneName, delay);
    }

    public static void LoadChapter01RedCreekEntrance(float delay = 0f)
    {
        Instance.LoadScene(Chapter01RedCreekEntranceSceneName, delay);
    }

    public static void LoadChapter01RedCreekCore(float delay = 0f)
    {
        Instance.LoadScene(Chapter01RedCreekCoreSceneName, delay);
    }

    public static void LoadChapter01BossHouse(float delay = 0f)
    {
        Instance.LoadScene(Chapter01BossHouseSceneName, delay);
    }

    public static void LoadChapter01End(float delay = 0f)
    {
        Instance.LoadScene(Chapter01EndSceneName, delay);
    }

    public static void ReloadCurrent(float delay = 0f)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        string reference = string.IsNullOrWhiteSpace(activeScene.path) ? activeScene.name : activeScene.path;
        Instance.LoadScene(reference, delay);
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

    private void LoadScene(string sceneReference, float delay)
    {
        StopAllCoroutines();
        StartCoroutine(LoadSceneRoutine(sceneReference, delay));
    }

    private IEnumerator LoadSceneRoutine(string sceneReference, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        SaveSystem.SaveIfPossible();
        SceneManager.LoadScene(ResolveScenePath(sceneReference));
    }

    private string ResolveScenePath(string sceneReference)
    {
        if (string.IsNullOrWhiteSpace(sceneReference))
        {
            return MainScenePath;
        }

        switch (sceneReference)
        {
            case BootSceneName:
            case BootScenePath:
                return BootScenePath;

            case TitleSceneName:
            case TitleScenePath:
                return TitleScenePath;

            case MainSceneName:
            case MainScenePath:
                return MainScenePath;

            case VfxTestBenchSceneName:
            case VfxTestBenchScenePath:
                return VfxTestBenchScenePath;

            case PrologueEventRoomSceneName:
            case PrologueEventRoomScenePath:
                return PrologueEventRoomScenePath;

            case Chapter01RedCreekEntranceSceneName:
            case Chapter01RedCreekEntranceScenePath:
                return Chapter01RedCreekEntranceScenePath;

            case Chapter01RedCreekCoreSceneName:
            case Chapter01RedCreekCoreScenePath:
                return Chapter01RedCreekCoreScenePath;

            case Chapter01BossHouseSceneName:
            case Chapter01BossHouseScenePath:
                return Chapter01BossHouseScenePath;

            case Chapter01EndSceneName:
            case Chapter01EndScenePath:
                return Chapter01EndScenePath;

            default:
                return sceneReference;
        }
    }
}
