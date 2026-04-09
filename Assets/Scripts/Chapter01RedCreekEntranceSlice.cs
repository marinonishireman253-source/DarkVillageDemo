using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Chapter01RedCreekEntranceSlice : MonoBehaviour
{
    private const string RootName = "__RedCreekEntranceSlice";
    private const string WorldEntryMarkerName = "WorldEntryMarker";

    private static readonly Color DirtColor = new Color(0.24f, 0.21f, 0.18f);
    private static readonly Color GrassColor = new Color(0.2f, 0.26f, 0.18f);
    private static readonly Color HouseColor = new Color(0.23f, 0.18f, 0.16f);
    private static readonly Color FenceColor = new Color(0.38f, 0.28f, 0.18f);
    private static readonly Color MistColor = new Color(0.48f, 0.5f, 0.54f);
    private static readonly Color FieldColor = new Color(0.31f, 0.25f, 0.17f);

    private PlayerMover _player;
    private Vector3 _forward;
    private Vector3 _right;
    private bool _worldMode;

    public static void Ensure(PlayerMover player)
    {
        if (player == null || SceneManager.GetActiveScene().name != SceneLoader.Chapter01RedCreekEntranceSceneName)
        {
            return;
        }

        if (GameObject.Find(RootName) != null)
        {
            return;
        }

        GameObject root = new GameObject(RootName);
        Chapter01RedCreekEntranceSlice slice = root.AddComponent<Chapter01RedCreekEntranceSlice>();
        slice.Build(player);
    }

    public static void EnsureWorld(PlayerMover player, Vector3 worldOffset, Vector3 worldForward)
    {
        if (player == null)
        {
            return;
        }

        if (GameObject.Find(RootName) != null)
        {
            return;
        }

        GameObject root = new GameObject(RootName);
        Chapter01RedCreekEntranceSlice slice = root.AddComponent<Chapter01RedCreekEntranceSlice>();
        slice.Build(player, true, worldOffset, worldForward);
    }

    private void Build(PlayerMover player, bool worldMode = false, Vector3 worldOffset = default, Vector3 worldForward = default)
    {
        _player = player;
        _worldMode = worldMode;
        _forward = worldMode && worldForward.sqrMagnitude > 0.001f
            ? Vector3.ProjectOnPlane(worldForward, Vector3.up)
            : Camera.main != null
                ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up)
                : new Vector3(1f, 0f, 1f);

        if (_forward.sqrMagnitude <= 0.001f)
        {
            _forward = new Vector3(1f, 0f, 1f);
        }

        _forward.Normalize();
        _right = Vector3.Cross(Vector3.up, _forward).normalized;

        if (!_worldMode)
        {
            PositionPlayer();
        }
        BuildVillageEdge();
        BuildFlow();
        if (_worldMode)
        {
            transform.position = worldOffset;
            CreateWorldMarker(WorldEntryMarkerName, -_forward * 8f + Vector3.up);
        }
        else
        {
            StartCoroutine(ShowArrivalBeat());
        }
    }

    private void PositionPlayer()
    {
        Vector3 spawnPosition = -_forward * 8f + Vector3.up;
        _player.transform.position = spawnPosition;
        _player.transform.rotation = Quaternion.LookRotation(_forward, Vector3.up);

        CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(_player.transform, true);
        }
    }

    private void BuildVillageEdge()
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);

        CreateBlock("Road", Vector3.zero, new Vector3(5.2f, 0.2f, 30f), rotation, DirtColor);
        CreateBlock("GrassLeft", -_right * 6.2f + Vector3.up * 0.02f, new Vector3(7f, 0.08f, 30f), rotation, GrassColor);
        CreateBlock("GrassRight", _right * 6.2f + Vector3.up * 0.02f, new Vector3(7f, 0.08f, 30f), rotation, GrassColor);
        CreateBlock("FieldPatchLeft", -_right * 10.2f + _forward * 1.6f + Vector3.up * 0.03f, new Vector3(4.6f, 0.06f, 13f), rotation, FieldColor);
        CreateBlock("FieldPatchRight", _right * 10f + _forward * 5.4f + Vector3.up * 0.03f, new Vector3(4.2f, 0.06f, 10.6f), rotation, FieldColor);

        for (int i = -2; i <= 2; i++)
        {
            float offset = i * 5f;
            CreateFenceSegment($"FenceLeft_{i + 3}", -_right * 3.4f + _forward * offset);
            CreateFenceSegment($"FenceRight_{i + 3}", _right * 3.4f + _forward * offset);
        }

        CreateHouse("NorthHouse", _forward * 7f - _right * 7.8f);
        CreateHouse("InnHouse", _forward * 10.5f + _right * 8.1f);
        CreateHouse("BarnHouse", _forward * 2.8f + _right * 8.2f);
        CreateWindmillSilhouette("Windmill", -_right * 11.3f + _forward * 10.8f);
        CreateBrokenCart("BrokenCart", -_right * 2.6f + _forward * 2.4f);
        CreateShrineApproach("ShrineApproach", _forward * 4.4f - _right * 2.5f);
        CreateGateArch("VillageGateArch", _forward * 12.8f);
        CreateWatchPost("WatchPost_Left", -_right * 5.7f - _forward * 4.1f);
        CreateWatchPost("WatchPost_Right", _right * 5.7f - _forward * 4.1f);
        CreateAbandonedGarden("HerbPatch_Left", -_right * 9.2f - _forward * 0.8f);
        CreateAbandonedGarden("DryGarden_Right", _right * 9f + _forward * 2.4f);
        CreateDitchEdge("RoadsideDitch_Left", -_right * 5.25f);
        CreateDitchEdge("RoadsideDitch_Right", _right * 5.25f);
        PlaceImportedDressing();
        HidePlaceholderVisuals();

        for (int i = 0; i < 4; i++)
        {
            GameObject mist = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mist.name = $"MistBlob_{i + 1}";
            mist.transform.SetParent(transform, true);
            mist.transform.position = _forward * (6f + i * 2.6f) + _right * ((i % 2 == 0 ? -1f : 1f) * 1.8f) + Vector3.up * (0.8f + i * 0.12f);
            mist.transform.localScale = Vector3.one * (1.15f + i * 0.22f);
            Tint(mist.GetComponent<Renderer>(), MistColor);
        }
    }

    private void BuildFlow()
    {
        QuestObjectiveTarget villagerObjective = CreateSuspiciousVillager();
        QuestObjectiveTarget shrineObjective = CreateWaysideShrine();
        QuestObjectiveTarget routeObjective = CreateVillageGate();

        villagerObjective.SetNextObjective(shrineObjective);
        shrineObjective.SetNextObjective(routeObjective);

        if (QuestTracker.Instance != null && string.IsNullOrWhiteSpace(QuestTracker.Instance.CurrentObjectiveId))
        {
            villagerObjective.RegisterAsCurrentObjective();
        }
    }

    private QuestObjectiveTarget CreateSuspiciousVillager()
    {
        GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = "SuspiciousVillager";
        npc.transform.SetParent(transform, true);
        npc.transform.position = -_forward * 2.5f + _right * 1.8f + Vector3.up;
        npc.transform.rotation = Quaternion.LookRotation(-_forward, Vector3.up);
        Tint(npc.GetComponent<Renderer>(), new Color(0.58f, 0.52f, 0.48f));

        TestNpcInteractable interactable = npc.AddComponent<TestNpcInteractable>();
        interactable.ConfigurePresentation("村口老妇", "交谈");
        interactable.ConfigureFallbackDialogue(
            "你不是村里的人。昨夜之前，这条路上还听得见磨坊和饭锅的声音。",
            "现在每家都说自己什么也没听见，可雾一落下来，窗后全是醒着的人。",
            "你如果执意进去，先去路边的祠龛看看。有人把祷词刻反了。");

        QuestObjectiveTarget objective = npc.AddComponent<QuestObjectiveTarget>();
        objective.Configure("talk_old_woman", "与村口老妇交谈", "引导", false, true);
        return objective;
    }

    private QuestObjectiveTarget CreateWaysideShrine()
    {
        GameObject shrine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shrine.name = "WaysideShrine";
        shrine.transform.SetParent(transform, true);
        shrine.transform.position = _forward * 4.8f - _right * 2.1f + Vector3.up * 0.9f;
        shrine.transform.localScale = new Vector3(1.3f, 1.8f, 0.9f);
        shrine.transform.rotation = Quaternion.LookRotation(_right, Vector3.up);
        Tint(shrine.GetComponent<Renderer>(), new Color(0.47f, 0.46f, 0.42f));

        TestStoneInteractable interactable = shrine.AddComponent<TestStoneInteractable>();
        interactable.ConfigureFallbackDialogue(
            "路边祠龛",
            "查看",
            "祠龛里的旧木牌被人重新翻了面。\n\n背后刻着一行很轻的字：\n“若他们都说平安，就别信第一句。”");

        QuestObjectiveTarget objective = shrine.AddComponent<QuestObjectiveTarget>();
        objective.Configure("inspect_shrine", "查看路边祠龛上的异样祷词", "线索", false, true);
        return objective;
    }

    private QuestObjectiveTarget CreateVillageGate()
    {
        GameObject triggerRoot = new GameObject("VillageCoreGate");
        triggerRoot.transform.SetParent(transform, true);
        triggerRoot.transform.position = _forward * 14f + Vector3.up;
        triggerRoot.transform.rotation = Quaternion.LookRotation(_forward, Vector3.up);

        BoxCollider colliderComponent = triggerRoot.AddComponent<BoxCollider>();
        colliderComponent.size = new Vector3(4f, 2.2f, 3.4f);
        colliderComponent.center = new Vector3(0f, 0.3f, 0f);
        colliderComponent.isTrigger = true;

        TriggerZoneObjective triggerObjective = triggerRoot.AddComponent<TriggerZoneObjective>();
        triggerObjective.Configure("reach_redcreek_core_gate", false, true, true);

        SceneDialogueTrigger dialogueTrigger = triggerRoot.AddComponent<SceneDialogueTrigger>();
        dialogueTrigger.Configure(
            "伊尔萨恩",
            "chapter01_gate_reached",
            "村里的灯都还亮着，可没有一扇窗真的朝向街道。",
            "先进去看看村中心。饭桌和屋门都不像正常收拾过的样子。");
        dialogueTrigger.ConfigureSceneTransition(SceneLoader.Chapter01RedCreekCoreSceneName, 0.45f);

        QuestObjectiveTarget objective = triggerRoot.AddComponent<QuestObjectiveTarget>();
        objective.Configure("reach_redcreek_core_gate", "前往赤溪村更深处", "入口", false, false);
        return objective;
    }

    private IEnumerator ShowArrivalBeat()
    {
        yield return null;

        if (SimpleDialogueUI.Instance == null || ChapterState.GetFlag("redcreek_arrival_seen"))
        {
            yield break;
        }

        ChapterState.SetFlag("redcreek_arrival_seen", true);
        SimpleDialogueUI.Instance.Show(
            "伊尔萨恩",
            "那道残响没把我送回王都，而是直接甩到了赤溪村外。",
            "先和村口的人对话，再确认祠龛上的异常。这里的安静太刻意了。");
    }

    private void CreateHouse(string name, Vector3 position)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);
        CreateBlock(name, position + Vector3.up * 1.9f, new Vector3(4f, 3.8f, 4.8f), rotation, HouseColor);
        CreateBlock($"{name}_Roof", position + Vector3.up * 4.05f, new Vector3(4.6f, 0.5f, 5.2f), rotation, new Color(0.18f, 0.11f, 0.1f));
        CreateBlock($"{name}_Porch", position - _forward * 2.2f + Vector3.up * 0.35f, new Vector3(1.8f, 0.7f, 0.9f), rotation, new Color(0.33f, 0.25f, 0.18f));
    }

    private void CreateWindmillSilhouette(string name, Vector3 position)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);
        CreateBlock($"{name}_Tower", position + Vector3.up * 4.2f, new Vector3(1.4f, 8.4f, 1.4f), rotation, new Color(0.28f, 0.23f, 0.18f));
        CreateBlock($"{name}_BladeVertical", position + Vector3.up * 6.1f, new Vector3(0.2f, 5f, 0.2f), rotation, new Color(0.4f, 0.31f, 0.2f));
        CreateBlock($"{name}_BladeHorizontal", position + Vector3.up * 6.1f, new Vector3(3.8f, 0.2f, 0.2f), rotation, new Color(0.4f, 0.31f, 0.2f));
    }

    private void CreateBrokenCart(string name, Vector3 position)
    {
        Quaternion rotation = Quaternion.LookRotation(_right, Vector3.up);
        CreateBlock($"{name}_Bed", position + Vector3.up * 0.45f, new Vector3(1.9f, 0.34f, 1.3f), rotation, new Color(0.34f, 0.24f, 0.16f));
        CreateBlock($"{name}_Handle", position - _right * 1.1f + Vector3.up * 0.52f, new Vector3(1.8f, 0.12f, 0.12f), rotation, new Color(0.4f, 0.3f, 0.2f));
        CreateWheel($"{name}_WheelLeft", position + _forward * 0.72f - _right * 0.75f + Vector3.up * 0.36f);
        CreateWheel($"{name}_WheelRight", position - _forward * 0.72f - _right * 0.75f + Vector3.up * 0.36f);
    }

    private void CreateShrineApproach(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);
        CreateBlock($"{name}_StepA", center - _forward * 1.1f + Vector3.up * 0.08f, new Vector3(1.8f, 0.16f, 1.4f), rotation, new Color(0.36f, 0.33f, 0.3f));
        CreateBlock($"{name}_StepB", center - _forward * 0.45f + Vector3.up * 0.14f, new Vector3(1.4f, 0.12f, 1f), rotation, new Color(0.4f, 0.37f, 0.34f));
    }

    private void CreateGateArch(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);
        CreateBlock($"{name}_PierLeft", center - _right * 2.2f + Vector3.up * 2.1f, new Vector3(0.7f, 4.2f, 0.9f), rotation, new Color(0.33f, 0.31f, 0.28f));
        CreateBlock($"{name}_PierRight", center + _right * 2.2f + Vector3.up * 2.1f, new Vector3(0.7f, 4.2f, 0.9f), rotation, new Color(0.33f, 0.31f, 0.28f));
        CreateBlock($"{name}_Lintel", center + Vector3.up * 3.95f, new Vector3(5.2f, 0.45f, 1f), rotation, new Color(0.36f, 0.33f, 0.3f));
        CreateBlock($"{name}_Banner", center + Vector3.up * 2.8f, new Vector3(2.8f, 0.9f, 0.08f), rotation, new Color(0.4f, 0.15f, 0.13f));
    }

    private void CreateWatchPost(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);
        CreateBlock($"{name}_Base", center + Vector3.up * 0.18f, new Vector3(2.2f, 0.36f, 2.2f), rotation, new Color(0.28f, 0.24f, 0.2f));
        CreateBlock($"{name}_Tower", center + Vector3.up * 1.5f, new Vector3(1.6f, 2.6f, 1.6f), rotation, HouseColor);
        CreateBlock($"{name}_Roof", center + Vector3.up * 3f, new Vector3(2f, 0.35f, 2f), rotation, new Color(0.18f, 0.11f, 0.1f));
        CreateBlock($"{name}_Signal", center + Vector3.up * 3.75f, new Vector3(0.4f, 0.7f, 0.4f), rotation, new Color(0.56f, 0.23f, 0.15f));
    }

    private void CreateWorldMarker(string name, Vector3 localPosition)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = localPosition;
    }

    private void CreateAbandonedGarden(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);
        CreateBlock($"{name}_Patch", center + Vector3.up * 0.02f, new Vector3(3.8f, 0.05f, 5f), rotation, new Color(0.25f, 0.23f, 0.15f));
        for (int i = 0; i < 4; i++)
        {
            float side = i < 2 ? -1f : 1f;
            float depth = i % 2 == 0 ? -1.2f : 1.1f;
            CreateBlock(
                $"{name}_Stake_{i + 1}",
                center + _right * side * 1.1f + _forward * depth + Vector3.up * 0.32f,
                new Vector3(0.12f, 0.64f, 0.12f),
                rotation,
                new Color(0.33f, 0.25f, 0.17f));
        }
        CreateBlock($"{name}_Tarp", center + _forward * 0.4f + Vector3.up * 0.08f, new Vector3(2.6f, 0.04f, 1.4f), rotation, new Color(0.39f, 0.18f, 0.14f));
    }

    private void CreateDitchEdge(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);
        CreateBlock($"{name}_Trench", center + Vector3.up * 0.01f, new Vector3(0.9f, 0.02f, 28f), rotation, new Color(0.18f, 0.16f, 0.13f));
        CreateBlock($"{name}_Bank", center + Vector3.up * 0.08f, new Vector3(0.55f, 0.1f, 28f), rotation, new Color(0.29f, 0.24f, 0.18f));
    }

    private void PlaceImportedDressing()
    {
        Quaternion forwardRotation = Quaternion.LookRotation(_forward, Vector3.up);
        Quaternion sideRotation = Quaternion.LookRotation(_right, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_Wagon",
            "Imported_BrokenCart",
            transform,
            -_right * 2.6f + _forward * 2.4f + Vector3.up * 0.12f,
            sideRotation,
            Vector3.one * 0.78f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/FarmCrate_Empty",
            "Imported_WatchCrateLeft",
            transform,
            -_right * 5.2f - _forward * 4.1f + Vector3.up * 0.16f,
            forwardRotation,
            Vector3.one * 0.88f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Barrel",
            "Imported_WatchBarrelRight",
            transform,
            _right * 5.15f - _forward * 4.25f + Vector3.up * 0.18f,
            forwardRotation,
            Vector3.one * 0.78f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_WoodenFence_Single",
            "Imported_GateFenceLeft",
            transform,
            _forward * 12.4f - _right * 3.8f + Vector3.up * 0.08f,
            forwardRotation,
            Vector3.one * 0.85f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_WoodenFence_Single",
            "Imported_GateFenceRight",
            transform,
            _forward * 12.4f + _right * 3.8f + Vector3.up * 0.08f,
            forwardRotation,
            Vector3.one * 0.85f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Lantern_Wall",
            "Imported_GateLanternLeft",
            transform,
            _forward * 12.8f - _right * 2.15f + Vector3.up * 2.4f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.72f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Lantern_Wall",
            "Imported_GateLanternRight",
            transform,
            _forward * 12.8f + _right * 2.15f + Vector3.up * 2.4f,
            Quaternion.LookRotation(-_right, Vector3.up),
            Vector3.one * 0.72f);

        PlaceWestHouseFacade("Imported_EntranceNorthHouse", _forward * 7f - _right * 7.8f, 0.98f);
        PlaceEastHouseFacade("Imported_EntranceInnHouse", _forward * 10.5f + _right * 8.1f, 1.02f, true);
        PlaceEastHouseFacade("Imported_EntranceBarnHouse", _forward * 2.8f + _right * 8.2f, 0.9f, false);
        PlaceGateArchImportedSet("Imported_EntranceGateArch", _forward * 12.8f);
        PlaceWatchPostImportedSet("Imported_EntranceWatchLeft", -_right * 5.7f - _forward * 4.1f, false);
        PlaceWatchPostImportedSet("Imported_EntranceWatchRight", _right * 5.7f - _forward * 4.1f, true);
        PlaceShrineImportedSet("Imported_EntranceShrine", _forward * 4.8f - _right * 2.1f);
        PlaceGardenImportedSet("Imported_EntranceHerbPatch", -_right * 9.2f - _forward * 0.8f, false);
        PlaceGardenImportedSet("Imported_EntranceDryGarden", _right * 9f + _forward * 2.4f, true);
    }

    private void PlaceWestHouseFacade(string prefix, Vector3 center, float scale)
    {
        Quaternion facadeRotation = Quaternion.LookRotation(_right, Vector3.up);
        Quaternion roofRotation = Quaternion.LookRotation(_forward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Door_Round",
            $"{prefix}_Wall",
            transform,
            center + _right * 2.55f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/DoorFrame_Round_WoodDark",
            $"{prefix}_DoorFrame",
            transform,
            center + _right * 2.55f + Vector3.up * 1.26f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Door_2_Round",
            $"{prefix}_Door",
            transform,
            center + _right * 2.48f + Vector3.up * 1.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_RoundTiles_6x8",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * 4.4f,
            roofRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_Chimney",
            $"{prefix}_Chimney",
            transform,
            center - _forward * 0.8f + Vector3.up * 5.35f,
            roofRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Lantern_Wall",
            $"{prefix}_Lantern",
            transform,
            center + _right * 3.15f + _forward * 0.95f + Vector3.up * 2.05f,
            facadeRotation,
            Vector3.one * 0.76f);
    }

    private void PlaceEastHouseFacade(string prefix, Vector3 center, float scale, bool addBalcony)
    {
        Quaternion facadeRotation = Quaternion.LookRotation(-_right, Vector3.up);
        Quaternion roofRotation = Quaternion.LookRotation(_forward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Door_Flat",
            $"{prefix}_Wall",
            transform,
            center - _right * 2.65f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/DoorFrame_Flat_WoodDark",
            $"{prefix}_DoorFrame",
            transform,
            center - _right * 2.62f + Vector3.up * 1.26f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Door_4_Flat",
            $"{prefix}_Door",
            transform,
            center - _right * 2.56f + Vector3.up * 1.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_RoundTiles_8x8",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * 4.5f,
            roofRotation,
            Vector3.one * scale);

        if (addBalcony)
        {
            RuntimeModelSpawner.Spawn(
                "Imported/Quaternius/MedievalVillage/FBX/Balcony_Simple_Straight",
                $"{prefix}_Balcony",
                transform,
                center - _right * 3.05f + Vector3.up * 2.4f,
                facadeRotation,
                Vector3.one * scale);
        }
        else
        {
            RuntimeModelSpawner.Spawn(
                "Imported/Quaternius/MedievalVillage/FBX/Overhang_Plaster_Long",
                $"{prefix}_Canopy",
                transform,
                center - _right * 3.02f + Vector3.up * 1.85f,
                facadeRotation,
                Vector3.one * scale);
            RuntimeModelSpawner.Spawn(
                "Imported/Quaternius/FantasyProps/FBX/FarmCrate_Empty",
                $"{prefix}_Crate",
                transform,
                center - _right * 3.45f + _forward * 1.1f + Vector3.up * 0.08f,
                facadeRotation,
                Vector3.one * 0.84f);
        }
    }

    private void PlaceGateArchImportedSet(string prefix, Vector3 center)
    {
        Quaternion forwardRotation = Quaternion.LookRotation(_forward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_Support",
            $"{prefix}_SupportLeft",
            transform,
            center - _right * 2.25f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 1.12f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_Support",
            $"{prefix}_SupportRight",
            transform,
            center + _right * 2.25f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 1.12f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_FrontSupports",
            $"{prefix}_Lintel",
            transform,
            center + Vector3.up * 3.85f,
            forwardRotation,
            Vector3.one * 0.94f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Banner_1_Cloth",
            $"{prefix}_Banner",
            transform,
            center + Vector3.up * 2.7f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.96f);
    }

    private void PlaceWatchPostImportedSet(string prefix, Vector3 center, bool facingBack)
    {
        Quaternion roofRotation = Quaternion.LookRotation(facingBack ? -_forward : _forward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_Tower_RoundTiles",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * 2.95f,
            roofRotation,
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Banner_2_Cloth",
            $"{prefix}_Banner",
            transform,
            center + Vector3.up * 3.2f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.68f);
    }

    private void PlaceShrineImportedSet(string prefix, Vector3 center)
    {
        Quaternion forwardRotation = Quaternion.LookRotation(_forward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/altar-wood",
            $"{prefix}_Altar",
            transform,
            center + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.84f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/candle-multiple",
            $"{prefix}_Candles",
            transform,
            center + _forward * 0.65f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/stone-wall-column",
            $"{prefix}_Column",
            transform,
            center - _right * 0.9f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.78f);
    }

    private void PlaceGardenImportedSet(string prefix, Vector3 center, bool dry)
    {
        Quaternion forwardRotation = Quaternion.LookRotation(_forward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_WoodenFence_Extension1",
            $"{prefix}_Fence",
            transform,
            center + Vector3.up * 0.02f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            dry ? "Imported/Kenney/GraveyardKit/urn-round" : "Imported/Quaternius/FantasyProps/FBX/Bucket_Wooden_1",
            $"{prefix}_Container",
            transform,
            center + _forward * 1.1f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            dry ? "Imported/Kenney/GraveyardKit/debris-wood" : "Imported/Quaternius/FantasyProps/FBX/Pot_1",
            $"{prefix}_Detail",
            transform,
            center - _forward * 0.8f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.9f);
    }

    private void HidePlaceholderVisuals()
    {
        HideRenderers(
            "NorthHouse",
            "NorthHouse_Roof",
            "NorthHouse_Porch",
            "InnHouse",
            "InnHouse_Roof",
            "InnHouse_Porch",
            "BarnHouse",
            "BarnHouse_Roof",
            "BarnHouse_Porch",
            "VillageGateArch_PierLeft",
            "VillageGateArch_PierRight",
            "VillageGateArch_Lintel",
            "VillageGateArch_Banner",
            "BrokenCart_Bed",
            "BrokenCart_Handle");
    }

    private void HideRenderers(params string[] objectNames)
    {
        foreach (string objectName in objectNames)
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                continue;
            }

            Transform target = transform.Find(objectName);
            if (target == null)
            {
                GameObject fallback = GameObject.Find(objectName);
                target = fallback != null ? fallback.transform : null;
            }

            if (target == null)
            {
                continue;
            }

            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }
        }
    }

    private void CreateWheel(string name, Vector3 position)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        wheel.name = name;
        wheel.transform.SetParent(transform, true);
        wheel.transform.position = position;
        wheel.transform.localScale = new Vector3(0.62f, 0.62f, 0.2f);
        wheel.transform.rotation = Quaternion.LookRotation(_right, Vector3.up);
        Tint(wheel.GetComponent<Renderer>(), new Color(0.18f, 0.14f, 0.12f));
    }

    private void CreateFenceSegment(string name, Vector3 position)
    {
        CreateBlock(name, position + Vector3.up * 0.5f, new Vector3(0.18f, 1f, 2.1f), Quaternion.LookRotation(_forward, Vector3.up), FenceColor);
    }

    private GameObject CreateBlock(string name, Vector3 position, Vector3 scale, Quaternion rotation, Color color)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(transform, true);
        block.transform.position = position;
        block.transform.rotation = rotation;
        block.transform.localScale = scale;
        Tint(block.GetComponent<Renderer>(), color);
        return block;
    }

    private void Tint(Renderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        renderer.SetPropertyBlock(propertyBlock);
    }
}
