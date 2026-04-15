using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class GameBootstrap : MonoBehaviour
{
    private static GameBootstrap s_Instance;

    private readonly struct CameraProfile
    {
        public CameraProfile(Vector3 offset, Vector3 lookOffset, Vector3 eulerAngles, float fieldOfView)
        {
            Offset = offset;
            LookOffset = lookOffset;
            EulerAngles = eulerAngles;
            FieldOfView = fieldOfView;
        }

        public Vector3 Offset { get; }
        public Vector3 LookOffset { get; }
        public Vector3 EulerAngles { get; }
        public float FieldOfView { get; }
    }

    private static readonly Vector3 DefaultPlayerSpawnPosition = new Vector3(0f, 1f, 0f);
    private static readonly CameraProfile InteriorCameraProfile = new CameraProfile(new Vector3(0f, 3.8f, -15.7f), new Vector3(0f, 1.4f, 0f), new Vector3(8f, 0f, 0f), 23f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (s_Instance != null)
        {
            return;
        }

        GameObject root = new GameObject("__RuntimeBootstrap");
        DontDestroyOnLoad(root);
        s_Instance = root.AddComponent<GameBootstrap>();
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        RunSceneBootstrap();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RunSceneBootstrap();
    }

    private void RunSceneBootstrap()
    {
        if (!IsMainScene())
        {
            return;
        }

        PlayerMover player = EnsurePlayer();
        EnsureSystems();
        EnsureBackgroundMusic();
        ConfigurePlayerMovement(player);
        EnsurePlayerSplash(player);
        EnsureInteriorLighting();
        EnsureUi();
        EnsureInventory();
        EnsureGameStateHub();
        TowerInteriorSlice.Ensure(player);
        GameStateHub.Instance?.ClearObjective();
        EnsureCamera(player);
        EnsureWetFloor();
        EnsureRain();
        EnsurePuddleZones();
        StartCoroutine(FinalizeSceneSetupNextFrame(player));
    }

    private IEnumerator FinalizeSceneSetupNextFrame(PlayerMover initialPlayer)
    {
        yield return null;

        if (!IsMainScene())
        {
            yield break;
        }

        PlayerMover player = initialPlayer != null ? initialPlayer : FindFirstObjectByType<PlayerMover>();
        if (player == null)
        {
            yield break;
        }

        ConfigurePlayerMovement(player);
        EnsurePlayerSplash(player);
        EnsureInteriorLighting();
        EnsureBackgroundMusic();
        EnsureUi();
        EnsureInventory();
        EnsureGameStateHub();
        TowerInteriorSlice.Ensure(player);
        GameStateHub.Instance?.ClearObjective();
        EnsureCamera(player);
        EnsureWetFloor();
        EnsureRain();
        EnsurePuddleZones();
    }

    private PlayerMover EnsurePlayer()
    {
        PlayerMover existingPlayer = FindFirstObjectByType<PlayerMover>();
        if (existingPlayer != null)
        {
            ValidatePlayerPrefabInstance(existingPlayer.gameObject);
            return existingPlayer;
        }

        CorePrefabCatalog catalog = CorePrefabCatalog.Load();
        GameObject playerPrefab = catalog != null ? catalog.PlayerPrefab : null;
        if (playerPrefab == null)
        {
            Debug.LogError("[GameBootstrap] Missing Player prefab in CorePrefabCatalog.");
            return null;
        }

        GameObject player = Instantiate(playerPrefab);
        player.name = playerPrefab.name;
        player.transform.position = DefaultPlayerSpawnPosition;
        ValidatePlayerPrefabInstance(player);
        return player.GetComponent<PlayerMover>();
    }

    private void ConfigurePlayerMovement(PlayerMover player)
    {
        if (player == null)
        {
            return;
        }

        player.ConfigureSideScroller(TowerInteriorSlice.WalkDepth, TowerInteriorSlice.PlayableXRange);
    }

    private void EnsureCamera(PlayerMover player)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }

        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
        }

        GameObject cameraRoot = mainCamera.gameObject;
        if (!cameraRoot.activeSelf)
        {
            cameraRoot.SetActive(true);
        }

        if (!cameraRoot.CompareTag("MainCamera"))
        {
            cameraRoot.tag = "MainCamera";
        }

        if (cameraRoot.GetComponent<AudioListener>() == null)
        {
            cameraRoot.AddComponent<AudioListener>();
        }

        AudioListener audioListener = cameraRoot.GetComponent<AudioListener>();
        if (audioListener != null && !audioListener.enabled)
        {
            audioListener.enabled = true;
        }

        if (cameraRoot.GetComponent<UniversalAdditionalCameraData>() == null)
        {
            cameraRoot.AddComponent<UniversalAdditionalCameraData>();
        }

        mainCamera.enabled = true;
        mainCamera.orthographic = false;
        mainCamera.fieldOfView = InteriorCameraProfile.FieldOfView;
        mainCamera.targetDisplay = 0;

        CameraFollow follow = cameraRoot.GetComponent<CameraFollow>();
        if (follow == null)
        {
            follow = cameraRoot.AddComponent<CameraFollow>();
        }

        follow.Configure(InteriorCameraProfile.Offset, false, InteriorCameraProfile.LookOffset, InteriorCameraProfile.EulerAngles);
        follow.ConfigureSprintFeel(Vector3.zero, 5f);
        follow.ConfigureHorizontalBounds(TowerInteriorSlice.CameraTrackXRange);

        if (player == null)
        {
            Vector3 fallbackTargetPosition = DefaultPlayerSpawnPosition;
            mainCamera.transform.position = fallbackTargetPosition + InteriorCameraProfile.Offset;
            mainCamera.transform.rotation = Quaternion.Euler(InteriorCameraProfile.EulerAngles);
            follow.SetTarget(null, false);
            Debug.LogWarning("[GameBootstrap] Camera fallback activated before PlayerMover was available.");
            return;
        }

        RoomCameraZone roomZone = TowerInteriorSlice.FindBestZone(player);
        if (roomZone != null)
        {
            follow.ConfigureRoomZone(roomZone, true);
        }

        follow.SetTarget(player.transform, true);
    }

    private void EnsureSystems()
    {
        if (FindFirstObjectByType<QuestTracker>() == null)
        {
            new GameObject("QuestTracker").AddComponent<QuestTracker>();
        }
    }

    private void EnsureBackgroundMusic()
    {
        BackgroundMusicPlayer backgroundMusicPlayer = FindFirstObjectByType<BackgroundMusicPlayer>();
        if (backgroundMusicPlayer == null)
        {
            backgroundMusicPlayer = new GameObject("BackgroundMusicPlayer").AddComponent<BackgroundMusicPlayer>();
        }

        backgroundMusicPlayer.RefreshForActiveScene();
    }

    private void EnsureUi()
    {
        if (FindFirstObjectByType<UiBootstrap>() == null)
        {
            new GameObject("UiBootstrap").AddComponent<UiBootstrap>();
        }

        if (FindFirstObjectByType<SimpleDialogueUI>() == null)
        {
            new GameObject("SimpleDialogueUI").AddComponent<SimpleDialogueUI>();
        }

        if (FindFirstObjectByType<InteractionPromptUI>() == null)
        {
            CorePrefabCatalog catalog = CorePrefabCatalog.Load();
            GameObject interactionPromptPrefab = catalog != null ? catalog.InteractionPromptPrefab : null;
            if (interactionPromptPrefab != null)
            {
                GameObject promptObject = Instantiate(interactionPromptPrefab);
                promptObject.name = interactionPromptPrefab.name;
            }
            else
            {
                Debug.LogError("[GameBootstrap] Missing InteractionPrompt prefab in CorePrefabCatalog.");
            }
        }

        if (FindFirstObjectByType<QuestTrackerUI>() == null)
        {
            new GameObject("QuestTrackerUI").AddComponent<QuestTrackerUI>();
        }

        if (FindFirstObjectByType<ChapterCompleteOverlay>() == null)
        {
            new GameObject("ChapterCompleteOverlay").AddComponent<ChapterCompleteOverlay>();
        }

        if (FindFirstObjectByType<AshParlorChoiceOverlay>() == null)
        {
            new GameObject("AshParlorChoiceOverlay").AddComponent<AshParlorChoiceOverlay>();
        }

        if (FindFirstObjectByType<PlayerStatusHud>() == null)
        {
            new GameObject("PlayerStatusHud").AddComponent<PlayerStatusHud>();
        }

        if (FindFirstObjectByType<LightZoneHudPresenter>() == null)
        {
            new GameObject("LightZoneHudPresenter").AddComponent<LightZoneHudPresenter>();
        }

        if (FindFirstObjectByType<DialogueRunner>() == null)
        {
            new GameObject("DialogueRunner").AddComponent<DialogueRunner>();
        }

        if (FindFirstObjectByType<DialogueUIBridge>() == null)
        {
            new GameObject("DialogueUIBridge").AddComponent<DialogueUIBridge>();
        }

        if (FindFirstObjectByType<DialogueChoiceUI>() == null)
        {
            new GameObject("DialogueChoiceUI").AddComponent<DialogueChoiceUI>();
        }

        if (FindFirstObjectByType<PortraitController>() == null)
        {
            new GameObject("PortraitController").AddComponent<PortraitController>();
        }

        if (FindFirstObjectByType<UiPreviewController>() == null)
        {
            new GameObject("UiPreviewController").AddComponent<UiPreviewController>();
        }

        if (FindFirstObjectByType<DialogueVoicePlayer>() == null)
        {
            DialogueVoicePlayer voicePlayer = new GameObject("DialogueVoicePlayer").AddComponent<DialogueVoicePlayer>();
            voicePlayer.LoadDefaultClips();
        }
    }

    private void EnsureInventory()
    {
        if (FindFirstObjectByType<InventoryController>() == null)
        {
            new GameObject("InventoryController").AddComponent<InventoryController>();
        }
    }

    private void EnsureGameStateHub()
    {
        if (FindFirstObjectByType<GameStateHub>() == null)
        {
            new GameObject("GameStateHub").AddComponent<GameStateHub>();
        }
    }

    private void EnsureInteriorLighting()
    {
        RenderSettings.sun = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.07f, 0.065f, 1f);
        RenderSettings.ambientIntensity = 0.22f;
        RenderSettings.fog = false;

        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null || light.type != LightType.Directional)
            {
                continue;
            }

            light.enabled = false;
            light.intensity = 0f;
        }
    }

    private void EnsureWetFloor()
    {
        WetFloorSetup.Ensure();
    }

    private void EnsureRain()
    {
        RainSystem.Disable();
    }

    private void EnsurePlayerSplash(PlayerMover player)
    {
        if (player == null)
        {
            return;
        }

        if (player.GetComponent<PlayerSplashEffect>() == null)
        {
            Debug.LogWarning("[GameBootstrap] Player prefab instance is missing PlayerSplashEffect.", player.gameObject);
        }
    }

    private void EnsurePuddleZones()
    {
        PuddleZoneSetup.Ensure();
    }

    private bool IsMainScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        return scene.name == SceneLoader.MainSceneName || scene.path == SceneLoader.MainScenePath;
    }

    private static void ValidatePlayerPrefabInstance(GameObject playerObject)
    {
        if (playerObject == null)
        {
            return;
        }

        WarnIfMissingComponent<PlayerMover>(playerObject, "PlayerMover");
        WarnIfMissingComponent<PlayerCombat>(playerObject, "PlayerCombat");
        WarnIfMissingComponent<CombatantHealth>(playerObject, "CombatantHealth");
        WarnIfMissingComponent<CapsuleCollider>(playerObject, "CapsuleCollider");
        WarnIfMissingComponent<Rigidbody>(playerObject, "Rigidbody");
        WarnIfMissingComponent<PlayerSpriteVisual>(playerObject, "PlayerSpriteVisual");
        WarnIfMissingComponent<PlayerSplashEffect>(playerObject, "PlayerSplashEffect");
    }

    private static void WarnIfMissingComponent<T>(GameObject target, string componentName) where T : Component
    {
        if (target != null && target.GetComponent<T>() == null)
        {
            Debug.LogWarning($"[GameBootstrap] Expected {componentName} on prefab instance '{target.name}'.", target);
        }
    }
}
