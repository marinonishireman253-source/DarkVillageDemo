using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
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
        EnsureSystems();
        EnsureUi();
        EnsureFallbackInteractables();
        EnsureQuestFlow();
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
}
