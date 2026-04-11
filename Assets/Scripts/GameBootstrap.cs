using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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
        ConfigurePlayerMovement(player);
        EnsureSystems();
        EnsureUi();
        TowerInteriorSlice.Ensure(player);
        QuestTracker.Instance?.ClearObjective();
        EnsureCamera(player);
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
        TowerInteriorSlice.Ensure(player);
        QuestTracker.Instance?.ClearObjective();
        EnsureCamera(player);
    }

    private PlayerMover EnsurePlayer()
    {
        PlayerMover existingPlayer = FindFirstObjectByType<PlayerMover>();
        if (existingPlayer != null)
        {
            EnsurePlayerCombat(existingPlayer.gameObject);
            EnsurePlayerVisual(existingPlayer.gameObject);
            return existingPlayer;
        }

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = DefaultPlayerSpawnPosition;
        PlayerMover mover = player.AddComponent<PlayerMover>();
        EnsurePlayerCombat(player);
        EnsurePlayerVisual(player);
        return mover;
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
        if (player == null)
        {
            return;
        }

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
        if (!cameraRoot.CompareTag("MainCamera"))
        {
            cameraRoot.tag = "MainCamera";
        }

        if (cameraRoot.GetComponent<AudioListener>() == null)
        {
            cameraRoot.AddComponent<AudioListener>();
        }

        if (cameraRoot.GetComponent<UniversalAdditionalCameraData>() == null)
        {
            cameraRoot.AddComponent<UniversalAdditionalCameraData>();
        }

        mainCamera.enabled = true;
        mainCamera.orthographic = false;
        mainCamera.fieldOfView = InteriorCameraProfile.FieldOfView;

        CameraFollow follow = cameraRoot.GetComponent<CameraFollow>();
        if (follow == null)
        {
            follow = cameraRoot.AddComponent<CameraFollow>();
        }

        follow.Configure(InteriorCameraProfile.Offset, false, InteriorCameraProfile.LookOffset, InteriorCameraProfile.EulerAngles);
        follow.ConfigureSprintFeel(Vector3.zero, 5f);
        follow.ConfigureHorizontalBounds(TowerInteriorSlice.CameraTrackXRange);

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
            new GameObject("InteractionPromptUI").AddComponent<InteractionPromptUI>();
        }

        if (FindFirstObjectByType<QuestTrackerUI>() == null)
        {
            new GameObject("QuestTrackerUI").AddComponent<QuestTrackerUI>();
        }

        if (FindFirstObjectByType<ChapterCompleteOverlay>() == null)
        {
            new GameObject("ChapterCompleteOverlay").AddComponent<ChapterCompleteOverlay>();
        }

        if (FindFirstObjectByType<CombatHud>() == null)
        {
            new GameObject("CombatHud").AddComponent<CombatHud>();
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

    private bool IsMainScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        return scene.name == SceneLoader.MainSceneName || scene.path == SceneLoader.MainScenePath;
    }

    private void EnsurePlayerCombat(GameObject playerObject)
    {
        if (playerObject == null)
        {
            return;
        }

        if (playerObject.GetComponent<CombatantHealth>() == null)
        {
            playerObject.AddComponent<CombatantHealth>();
        }

        if (playerObject.GetComponent<PlayerCombat>() == null)
        {
            playerObject.AddComponent<PlayerCombat>();
        }

        CapsuleCollider capsule = playerObject.GetComponent<CapsuleCollider>();
        if (capsule == null)
        {
            capsule = playerObject.AddComponent<CapsuleCollider>();
        }

        capsule.center = new Vector3(0f, 1f, 0f);
        capsule.height = 2f;
        capsule.radius = 0.33f;

        Rigidbody body = playerObject.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = playerObject.AddComponent<Rigidbody>();
        }

        body.useGravity = false;
        body.isKinematic = true;
        body.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void EnsurePlayerVisual(GameObject playerObject)
    {
        if (playerObject == null)
        {
            return;
        }

        PlayerCharacterVisual legacyVisual = playerObject.GetComponent<PlayerCharacterVisual>();
        if (legacyVisual != null)
        {
            Destroy(legacyVisual);
        }

        if (playerObject.GetComponent<PlayerSpriteVisual>() == null)
        {
            playerObject.AddComponent<PlayerSpriteVisual>();
        }
    }
}
