using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class GameBootstrap : MonoBehaviour
{
    private static readonly Vector3 DefaultPlayerSpawnPosition = new Vector3(0f, 1f, 0f);
    private static readonly Vector3 DefaultCameraOffset = new Vector3(-7f, 8f, -7f);
    private static readonly Vector3 DefaultCameraLookOffset = new Vector3(0f, 0.5f, 0f);
    private static readonly Vector3 DefaultCameraEulerAngles = new Vector3(42f, 45f, 0f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        GameObject existing = GameObject.Find("__RuntimeBootstrap");
        if (existing != null)
        {
            return;
        }

        GameObject root = new GameObject("__RuntimeBootstrap");
        root.AddComponent<GameBootstrap>();
    }

    private void Start()
    {
        if (!IsExplorationScene())
        {
            return;
        }

        EnsureCoreWorld();
        EnsureSystems();
        EnsureUi();
        EnsureFallbackInteractables();
        EnsureQuestFlow();
    }

    private void EnsureCoreWorld()
    {
        EnsureGround();
        PlayerMover player = EnsurePlayer();
        EnsureCamera(player);
    }

    private void EnsureGround()
    {
        if (HasWalkableGround())
        {
            return;
        }

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
    }

    private PlayerMover EnsurePlayer()
    {
        PlayerMover existingPlayer = FindFirstObjectByType<PlayerMover>();
        if (existingPlayer != null)
        {
            return existingPlayer;
        }

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = DefaultPlayerSpawnPosition;
        return player.AddComponent<PlayerMover>();
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

        mainCamera.orthographic = false;
        mainCamera.fieldOfView = 50f;
        mainCamera.transform.position = player.transform.position + DefaultCameraOffset;

        CameraFollow follow = cameraRoot.GetComponent<CameraFollow>();
        if (follow == null)
        {
            follow = cameraRoot.AddComponent<CameraFollow>();
        }

        follow.Configure(DefaultCameraOffset, false, DefaultCameraLookOffset, DefaultCameraEulerAngles);

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

        // M2: 对话系统
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

        // 诊断工具
        if (FindFirstObjectByType<RuntimeDiagnostic>() == null)
        {
            new GameObject("RuntimeDiagnostic").AddComponent<RuntimeDiagnostic>();
        }
    }

    private void EnsureFallbackInteractables()
    {
        if (HasInteractableInScene())
        {
            return;
        }

        PlayerMover player = FindFirstObjectByType<PlayerMover>();
        Vector3 basePosition = player != null ? player.transform.position : Vector3.zero;
        Vector3 forward = player != null ? player.transform.forward : Vector3.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = "FallbackTestNpc";
        npc.transform.position = basePosition + forward * 4.5f + right * 1.25f + new Vector3(0f, 0.9f, 0f);
        npc.transform.rotation = Quaternion.LookRotation(-forward, Vector3.up);
        npc.AddComponent<TestNpcInteractable>();

        GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stone.name = "FallbackQuestStone";
        stone.transform.position = basePosition + forward * 8f - right * 1.4f + new Vector3(0f, 0.75f, 0f);
        stone.transform.localScale = new Vector3(1.2f, 1.5f, 0.8f);
        stone.AddComponent<TestStoneInteractable>();

        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pickup.name = "FallbackPickup";
        pickup.transform.position = basePosition + forward * 11f + right * 0.8f + new Vector3(0f, 0.7f, 0f);
        pickup.transform.localScale = Vector3.one * 0.7f;
        pickup.AddComponent<PickupInteractable>();

        GameObject doorRoot = new GameObject("FallbackDoor");
        doorRoot.transform.position = basePosition + forward * 14f;
        GameObject doorVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorVisual.name = "DoorVisual";
        doorVisual.transform.SetParent(doorRoot.transform, false);
        doorVisual.transform.localPosition = new Vector3(0f, 1f, 0f);
        doorVisual.transform.localScale = new Vector3(1.2f, 2f, 0.2f);
        DoorInteractable doorInteractable = doorRoot.AddComponent<DoorInteractable>();
        doorInteractable.SetDoorVisual(doorVisual.transform);

        GameObject triggerZone = new GameObject("FallbackGateTrigger");
        triggerZone.transform.position = basePosition + forward * 17f + new Vector3(0f, 1f, 0f);
        BoxCollider triggerCollider = triggerZone.AddComponent<BoxCollider>();
        triggerCollider.size = new Vector3(2.5f, 2f, 2.5f);
        TriggerZoneObjective triggerObjective = triggerZone.AddComponent<TriggerZoneObjective>();
        triggerObjective.Configure("reach_gate", false, true, true);

        Debug.Log($"[GameBootstrap] Spawned fallback exploration chain near {basePosition}");
    }

    private void EnsureQuestFlow()
    {
        TestNpcInteractable npc = FindFirstObjectByType<TestNpcInteractable>();
        TestStoneInteractable stone = FindFirstObjectByType<TestStoneInteractable>();
        PickupInteractable pickup = FindFirstObjectByType<PickupInteractable>();
        DoorInteractable door = FindFirstObjectByType<DoorInteractable>();
        TriggerZoneObjective triggerZone = FindFirstObjectByType<TriggerZoneObjective>();

        if (npc == null)
        {
            return;
        }

        QuestObjectiveTarget npcObjective = GetOrCreateObjectiveWithDefaults(npc.gameObject, "talk_to_watchman", "与守夜人交谈", "主目标", false, true);

        QuestObjectiveTarget stoneObjective = null;
        if (stone != null)
        {
            stoneObjective = GetOrCreateObjectiveWithDefaults(stone.gameObject, "inspect_stone", "查看石碑上的文字", "线索", false, true);
        }

        QuestObjectiveTarget pickupObjective = null;
        if (pickup != null)
        {
            pickupObjective = GetOrCreateObjectiveWithDefaults(pickup.gameObject, "pickup_token", "拾取旧徽记", "物品", false, true);
        }

        QuestObjectiveTarget doorObjective = null;
        if (door != null)
        {
            doorObjective = GetOrCreateObjectiveWithDefaults(door.gameObject, "open_gate", "开启前方木门", "路径", false, true);
        }

        QuestObjectiveTarget triggerObjective = null;
        if (triggerZone != null)
        {
            triggerObjective = GetOrCreateObjectiveWithDefaults(triggerZone.gameObject, "reach_gate", "穿过门后进入前方区域", "终点", false, false);
        }

        LinkObjectivesIfMissing(npcObjective, stoneObjective);
        LinkObjectivesIfMissing(stoneObjective, pickupObjective);
        LinkObjectivesIfMissing(pickupObjective, doorObjective);
        LinkObjectivesIfMissing(doorObjective, triggerObjective);

        QuestTracker tracker = QuestTracker.Instance;
        if (tracker != null
            && string.IsNullOrWhiteSpace(tracker.CurrentObjectiveId)
            && npcObjective != null
            && !HasAutoRegisterObjectiveInScene())
        {
            npcObjective.RegisterAsCurrentObjective();
        }
    }

    private QuestObjectiveTarget GetOrCreateObjectiveWithDefaults(GameObject target, string id, string text, string marker, bool autoRegister, bool completeOnUse)
    {
        QuestObjectiveTarget objectiveTarget = target.GetComponent<QuestObjectiveTarget>();
        if (objectiveTarget != null)
        {
            return objectiveTarget;
        }

        objectiveTarget = target.AddComponent<QuestObjectiveTarget>();
        ConfigureObjective(objectiveTarget, id, text, marker, autoRegister, completeOnUse);
        return objectiveTarget;
    }

    private void LinkObjectivesIfMissing(QuestObjectiveTarget current, QuestObjectiveTarget next)
    {
        if (current == null || current.NextObjective != null)
        {
            return;
        }

        current.SetNextObjective(next);
    }

    private void ConfigureObjective(QuestObjectiveTarget target, string id, string text, string marker, bool autoRegister, bool completeOnUse)
    {
        if (target == null)
        {
            return;
        }

        target.Configure(id, text, marker, autoRegister, completeOnUse);
    }

    private bool HasInteractableInScene()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IInteractable)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasWalkableGround()
    {
        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);

        foreach (Collider collider in colliders)
        {
            if (collider == null || !collider.enabled || collider.isTrigger)
            {
                continue;
            }

            if (collider.GetComponent<PlayerMover>() != null)
            {
                continue;
            }

            Bounds bounds = collider.bounds;
            if (bounds.size.x >= 8f && bounds.size.z >= 8f && bounds.center.y <= 1f)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasAutoRegisterObjectiveInScene()
    {
        QuestObjectiveTarget[] objectives = FindObjectsByType<QuestObjectiveTarget>(FindObjectsSortMode.None);

        foreach (QuestObjectiveTarget objective in objectives)
        {
            if (objective != null && objective.AutoRegisterOnStart)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsExplorationScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName != SceneLoader.BootSceneName && sceneName != SceneLoader.TitleSceneName;
    }
}
