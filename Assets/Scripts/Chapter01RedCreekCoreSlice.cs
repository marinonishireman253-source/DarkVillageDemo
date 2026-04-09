using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Chapter01RedCreekCoreSlice : MonoBehaviour
{
    private const string RootName = "__RedCreekCoreSlice";
    private const string WorldEntryMarkerName = "WorldEntryMarker";

    private static readonly Color PlazaColor = new Color(0.26f, 0.23f, 0.2f);
    private static readonly Color HouseColor = new Color(0.25f, 0.2f, 0.18f);
    private static readonly Color TableColor = new Color(0.36f, 0.27f, 0.18f);
    private static readonly Color MistColor = new Color(0.55f, 0.57f, 0.6f);
    private static readonly Color ClothColor = new Color(0.49f, 0.18f, 0.15f);

    private PlayerMover _player;
    private Vector3 _forward;
    private Vector3 _right;
    private bool _worldMode;

    public static void Ensure(PlayerMover player)
    {
        if (player == null || SceneManager.GetActiveScene().name != SceneLoader.Chapter01RedCreekCoreSceneName)
        {
            return;
        }

        if (GameObject.Find(RootName) != null)
        {
            return;
        }

        GameObject root = new GameObject(RootName);
        Chapter01RedCreekCoreSlice slice = root.AddComponent<Chapter01RedCreekCoreSlice>();
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
        Chapter01RedCreekCoreSlice slice = root.AddComponent<Chapter01RedCreekCoreSlice>();
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
        BuildSquare();
        BuildObjectives();
        if (_worldMode)
        {
            transform.position = worldOffset;
            CreateWorldMarker(WorldEntryMarkerName, -_forward * 9f + Vector3.up);
        }
        else
        {
            StartCoroutine(ShowArrivalBeat());
        }
    }

    private void PositionPlayer()
    {
        Vector3 spawnPosition = -_forward * 9f + Vector3.up;
        _player.transform.position = spawnPosition;
        _player.transform.rotation = Quaternion.LookRotation(_forward, Vector3.up);

        CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(_player.transform, true);
        }
    }

    private void BuildSquare()
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);

        CreateBlock("VillageSquare", Vector3.zero, new Vector3(11f, 0.2f, 20f), rotation, PlazaColor);
        CreateBlock("WestHouse", -_right * 7.6f + _forward * 0.5f + Vector3.up * 2f, new Vector3(4.6f, 4f, 5.2f), rotation, HouseColor);
        CreateBlock("EastHouse", _right * 7.8f + _forward * 2f + Vector3.up * 2f, new Vector3(4.8f, 4f, 5.6f), rotation, HouseColor);
        CreateBlock("NorthHouse", _forward * 8.2f + Vector3.up * 2.1f, new Vector3(6.2f, 4.2f, 4.8f), rotation, HouseColor);
        CreateBlock("DinnerTable", _forward * 2.5f + Vector3.up * 0.9f, new Vector3(3.6f, 1.2f, 2.1f), rotation, TableColor);
        CreateWell("VillageWell", _forward * 1.1f - _right * 3.4f);
        CreateMarketStall("StallWest", -_right * 4.8f - _forward * 0.4f);
        CreateMarketStall("StallEast", _right * 4.5f + _forward * 0.7f);
        CreateClothesline("ClotheslineNorth", _forward * 4.8f);
        CreateBenchCluster("BenchCluster", _forward * 3.2f + _right * 2.5f);
        CreateMayorFacade("MayorFacade", _forward * 11.4f);
        CreateStoneLane("WestLane", -_right * 2.5f + _forward * 1.8f, 8.2f);
        CreateStoneLane("EastLane", _right * 2.7f + _forward * 1.8f, 7.8f);
        CreateSilentMealCluster("MealCluster_West", -_right * 2.9f + _forward * 3.8f);
        CreateSilentMealCluster("MealCluster_East", _right * 3.2f + _forward * 4.1f);
        CreatePrayerPoleCluster("PrayerPoles", -_right * 5.4f + _forward * 6.2f);
        CreateMayorForecourt("MayorForecourt", _forward * 9.4f);
        PlaceImportedDressing();
        HidePlaceholderVisuals();

        for (int i = 0; i < 5; i++)
        {
            GameObject mist = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mist.name = $"CoreMist_{i + 1}";
            mist.transform.SetParent(transform, true);
            mist.transform.position = _forward * (1.5f + i * 1.8f) + _right * ((i % 2 == 0 ? -1f : 1f) * 2.2f) + Vector3.up * (0.85f + i * 0.08f);
            mist.transform.localScale = Vector3.one * (1.1f + i * 0.14f);
            Tint(mist.GetComponent<Renderer>(), MistColor);
        }
    }

    private void BuildObjectives()
    {
        QuestObjectiveTarget tableObjective = CreateDinnerTable();
        QuestObjectiveTarget residentObjective = CreateShakenResident();
        QuestObjectiveTarget finaleObjective = CreateFinalProbe();

        tableObjective.SetNextObjective(residentObjective);
        residentObjective.SetNextObjective(finaleObjective);

        if (QuestTracker.Instance != null && string.IsNullOrWhiteSpace(QuestTracker.Instance.CurrentObjectiveId))
        {
            tableObjective.RegisterAsCurrentObjective();
        }
    }

    private QuestObjectiveTarget CreateDinnerTable()
    {
        GameObject table = GameObject.Find("DinnerTable");
        TestStoneInteractable interactable = table.AddComponent<TestStoneInteractable>();
        interactable.ConfigureFallbackDialogue(
            "翻倒的饭桌",
            "调查",
            "桌上的碗还温着，椅子却全都朝外翻倒。\n\n有人是一起站起来离开的，而且走得很急。");

        QuestObjectiveTarget objective = table.AddComponent<QuestObjectiveTarget>();
        objective.Configure("inspect_dinner_table", "调查村中心那张翻倒的饭桌", "线索", false, true);
        return objective;
    }

    private QuestObjectiveTarget CreateShakenResident()
    {
        GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = "ShakenResident";
        npc.transform.SetParent(transform, true);
        npc.transform.position = _forward * 5.5f - _right * 2.1f + Vector3.up;
        npc.transform.rotation = Quaternion.LookRotation(-_forward, Vector3.up);
        Tint(npc.GetComponent<Renderer>(), new Color(0.63f, 0.58f, 0.52f));

        TestNpcInteractable interactable = npc.AddComponent<TestNpcInteractable>();
        interactable.ConfigurePresentation("失神村民", "交谈");
        interactable.ConfigureFallbackDialogue(
            "你不该在饭点之后还听见他们说笑的，那不是真人发出来的声音。",
            "屋里每张桌子都像故意摆成了一家人还在吃饭的样子，可没人敢坐下。",
            "村长的屋子在更里面。要查真相，就往那边走。");

        QuestObjectiveTarget objective = npc.AddComponent<QuestObjectiveTarget>();
        objective.Configure("talk_shaken_resident", "与村中心的失神村民交谈", "证词", false, true);
        return objective;
    }

    private QuestObjectiveTarget CreateFinalProbe()
    {
        GameObject triggerRoot = new GameObject("BossHouseApproach");
        triggerRoot.transform.SetParent(transform, true);
        triggerRoot.transform.position = _forward * 11.5f + Vector3.up;
        triggerRoot.transform.rotation = Quaternion.LookRotation(_forward, Vector3.up);

        BoxCollider colliderComponent = triggerRoot.AddComponent<BoxCollider>();
        colliderComponent.size = new Vector3(4.2f, 2.4f, 3.6f);
        colliderComponent.center = new Vector3(0f, 0.3f, 0f);
        colliderComponent.isTrigger = true;

        TriggerZoneObjective triggerObjective = triggerRoot.AddComponent<TriggerZoneObjective>();
        triggerObjective.Configure("reach_boss_house_path", false, true, true);

        SceneDialogueTrigger dialogueTrigger = triggerRoot.AddComponent<SceneDialogueTrigger>();
        dialogueTrigger.Configure(
            "伊尔萨恩",
            "chapter01_core_complete",
            "村长屋那边的门还留着新的封蜡，雾就是从那后面继续往里涌的。",
            "村中心的线索已经够了。去村长屋，把这条线查到底。");
        dialogueTrigger.ConfigureRequirement(
            null,
            "reach_boss_house_path",
            "伊尔萨恩",
            "先把村中心的饭桌和证词都看完，再去村长屋。");
        dialogueTrigger.ConfigureSceneTransition(SceneLoader.Chapter01BossHouseSceneName, 0.45f);

        QuestObjectiveTarget objective = triggerRoot.AddComponent<QuestObjectiveTarget>();
        objective.Configure("reach_boss_house_path", "前往村长屋方向", "推进", false, false);
        return objective;
    }

    private IEnumerator ShowArrivalBeat()
    {
        yield return null;

        if (SimpleDialogueUI.Instance == null || ChapterState.GetFlag("redcreek_core_seen"))
        {
            yield break;
        }

        ChapterState.SetFlag("redcreek_core_seen", true);
        SimpleDialogueUI.Instance.Show(
            "伊尔萨恩",
            "这里比村口更不对劲。饭桌还留着热气，像所有人都在同一刻被什么东西叫走了。",
            "先看饭桌，再问还肯开口的人。");
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

    private void CreateWell(string name, Vector3 position)
    {
        CreateBlock($"{name}_Base", position + Vector3.up * 0.7f, new Vector3(2.1f, 1.4f, 2.1f), Quaternion.identity, new Color(0.34f, 0.32f, 0.3f));
        CreateBlock($"{name}_BeamLeft", position - _right * 0.82f + Vector3.up * 2f, new Vector3(0.2f, 2f, 0.2f), Quaternion.identity, new Color(0.3f, 0.22f, 0.15f));
        CreateBlock($"{name}_BeamRight", position + _right * 0.82f + Vector3.up * 2f, new Vector3(0.2f, 2f, 0.2f), Quaternion.identity, new Color(0.3f, 0.22f, 0.15f));
        CreateBlock($"{name}_Top", position + Vector3.up * 3f, new Vector3(1.9f, 0.16f, 0.16f), Quaternion.LookRotation(_right, Vector3.up), new Color(0.3f, 0.22f, 0.15f));
    }

    private void CreateMarketStall(string name, Vector3 position)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);
        CreateBlock($"{name}_Counter", position + Vector3.up * 0.7f, new Vector3(2.8f, 1.4f, 1.6f), rotation, new Color(0.33f, 0.24f, 0.18f));
        CreateBlock($"{name}_Canopy", position + Vector3.up * 2.2f, new Vector3(3.2f, 0.16f, 1.9f), rotation, ClothColor);
        CreateBlock($"{name}_PostLeft", position - _right * 1.25f + Vector3.up * 1.3f, new Vector3(0.16f, 2.6f, 0.16f), rotation, new Color(0.27f, 0.19f, 0.14f));
        CreateBlock($"{name}_PostRight", position + _right * 1.25f + Vector3.up * 1.3f, new Vector3(0.16f, 2.6f, 0.16f), rotation, new Color(0.27f, 0.19f, 0.14f));
    }

    private void CreateClothesline(string name, Vector3 position)
    {
        CreateBlock($"{name}_PoleLeft", position - _right * 4.2f + Vector3.up * 2.4f, new Vector3(0.14f, 4.8f, 0.14f), Quaternion.identity, new Color(0.24f, 0.18f, 0.14f));
        CreateBlock($"{name}_PoleRight", position + _right * 4.2f + Vector3.up * 2.4f, new Vector3(0.14f, 4.8f, 0.14f), Quaternion.identity, new Color(0.24f, 0.18f, 0.14f));
        CreateBlock($"{name}_Line", position + Vector3.up * 4.2f, new Vector3(8.3f, 0.05f, 0.05f), Quaternion.LookRotation(_right, Vector3.up), new Color(0.2f, 0.17f, 0.16f));
        CreateBlock($"{name}_ClothA", position - _right * 1.6f + Vector3.up * 3.5f, new Vector3(1.1f, 1.4f, 0.08f), Quaternion.LookRotation(_right, Vector3.up), ClothColor);
        CreateBlock($"{name}_ClothB", position + _right * 0.1f + Vector3.up * 3.55f, new Vector3(0.9f, 1.2f, 0.08f), Quaternion.LookRotation(_right, Vector3.up), new Color(0.54f, 0.48f, 0.35f));
        CreateBlock($"{name}_ClothC", position + _right * 1.8f + Vector3.up * 3.45f, new Vector3(1f, 1.3f, 0.08f), Quaternion.LookRotation(_right, Vector3.up), new Color(0.28f, 0.32f, 0.38f));
    }

    private void CreateBenchCluster(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_right, Vector3.up);
        CreateBlock($"{name}_BenchA", center + Vector3.up * 0.42f, new Vector3(2.2f, 0.24f, 0.48f), rotation, new Color(0.36f, 0.26f, 0.18f));
        CreateBlock($"{name}_BenchB", center + _forward * 1.1f + Vector3.up * 0.42f, new Vector3(2.1f, 0.24f, 0.48f), rotation, new Color(0.36f, 0.26f, 0.18f));
        CreateBlock($"{name}_Crate", center - _right * 1.1f + Vector3.up * 0.46f, new Vector3(0.9f, 0.92f, 0.9f), Quaternion.identity, new Color(0.31f, 0.23f, 0.17f));
    }

    private void CreateMayorFacade(string name, Vector3 position)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);
        CreateBlock($"{name}_Base", position + Vector3.up * 2.8f, new Vector3(8.8f, 5.6f, 2.8f), rotation, new Color(0.24f, 0.2f, 0.18f));
        CreateBlock($"{name}_Roof", position + Vector3.up * 5.9f, new Vector3(9.4f, 0.6f, 3.2f), rotation, new Color(0.18f, 0.11f, 0.1f));
        CreateBlock($"{name}_Door", position - _forward * 1.1f + Vector3.up * 1.25f, new Vector3(1.6f, 2.5f, 0.26f), rotation, new Color(0.21f, 0.12f, 0.09f));
    }

    private void CreateWorldMarker(string name, Vector3 localPosition)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = localPosition;
    }

    private void CreateStoneLane(string name, Vector3 center, float length)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);
        CreateBlock($"{name}_Base", center + Vector3.up * 0.03f, new Vector3(1.6f, 0.06f, length), rotation, new Color(0.34f, 0.31f, 0.28f));
        for (int i = -2; i <= 2; i++)
        {
            CreateBlock(
                $"{name}_Stone_{i + 3}",
                center + _forward * (i * 1.6f) + Vector3.up * 0.06f,
                new Vector3(1.2f, 0.08f, 0.9f),
                rotation,
                new Color(0.39f, 0.36f, 0.33f));
        }
    }

    private void CreateSilentMealCluster(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_right, Vector3.up);
        CreateBlock($"{name}_Table", center + Vector3.up * 0.48f, new Vector3(1.8f, 0.18f, 1.2f), rotation, new Color(0.3f, 0.22f, 0.17f));
        CreateBlock($"{name}_BenchLeft", center - _forward * 0.85f + Vector3.up * 0.34f, new Vector3(1.6f, 0.14f, 0.32f), rotation, new Color(0.34f, 0.24f, 0.18f));
        CreateBlock($"{name}_BenchRight", center + _forward * 0.85f + Vector3.up * 0.24f, new Vector3(1.3f, 0.14f, 0.32f), Quaternion.LookRotation(_right + _forward * 0.35f, Vector3.up), new Color(0.29f, 0.21f, 0.16f));
        CreateBlock($"{name}_BowlA", center - _right * 0.35f + Vector3.up * 0.62f, new Vector3(0.2f, 0.08f, 0.2f), Quaternion.identity, new Color(0.58f, 0.54f, 0.48f));
        CreateBlock($"{name}_BowlB", center + _right * 0.32f + Vector3.up * 0.62f, new Vector3(0.2f, 0.08f, 0.2f), Quaternion.identity, new Color(0.58f, 0.54f, 0.48f));
    }

    private void CreatePrayerPoleCluster(string name, Vector3 center)
    {
        for (int i = 0; i < 3; i++)
        {
            float offset = (i - 1f) * 1.1f;
            CreateBlock(
                $"{name}_Pole_{i + 1}",
                center + _right * offset + Vector3.up * 1.95f,
                new Vector3(0.16f, 3.9f, 0.16f),
                Quaternion.identity,
                new Color(0.25f, 0.19f, 0.14f));
            CreateBlock(
                $"{name}_Cloth_{i + 1}",
                center + _right * offset + Vector3.up * 2.7f,
                new Vector3(0.6f, 0.95f, 0.06f),
                Quaternion.LookRotation(_right, Vector3.up),
                ClothColor * 0.9f);
        }
    }

    private void CreateMayorForecourt(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);
        CreateBlock($"{name}_Steps", center + Vector3.up * 0.1f, new Vector3(4.8f, 0.2f, 1.8f), rotation, new Color(0.35f, 0.33f, 0.3f));
        CreateBlock($"{name}_BarrierLeft", center - _right * 2.4f + _forward * 0.6f + Vector3.up * 0.55f, new Vector3(0.28f, 1.1f, 2.1f), rotation, new Color(0.4f, 0.28f, 0.18f));
        CreateBlock($"{name}_BarrierRight", center + _right * 2.4f + _forward * 0.6f + Vector3.up * 0.55f, new Vector3(0.28f, 1.1f, 2.1f), rotation, new Color(0.4f, 0.28f, 0.18f));
        CreateBlock($"{name}_Seal", center + _forward * 0.95f + Vector3.up * 1.9f, new Vector3(2.3f, 0.14f, 0.08f), rotation, ClothColor);
    }

    private void PlaceImportedDressing()
    {
        Quaternion forwardRotation = Quaternion.LookRotation(_forward, Vector3.up);
        Quaternion sideRotation = Quaternion.LookRotation(_right, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Bench",
            "Imported_BenchClusterA",
            transform,
            _forward * 3.1f + _right * 2.4f + Vector3.up * 0.08f,
            sideRotation,
            Vector3.one * 0.95f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Bench",
            "Imported_BenchClusterB",
            transform,
            _forward * 4.2f + _right * 2.4f + Vector3.up * 0.08f,
            sideRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Barrel_Apples",
            "Imported_StallWestBarrel",
            transform,
            -_right * 4.55f - _forward * 0.15f + Vector3.up * 0.16f,
            forwardRotation,
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Bag",
            "Imported_StallEastBag",
            transform,
            _right * 4.4f + _forward * 0.55f + Vector3.up * 0.12f,
            forwardRotation,
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/FarmCrate_Apple",
            "Imported_MealCrate",
            transform,
            _right * 3.1f + _forward * 4f + Vector3.up * 0.16f,
            forwardRotation,
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Lantern_Wall",
            "Imported_MayorLanternLeft",
            transform,
            _forward * 10.6f - _right * 1.45f + Vector3.up * 2.3f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.76f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Lantern_Wall",
            "Imported_MayorLanternRight",
            transform,
            _forward * 10.6f + _right * 1.45f + Vector3.up * 2.3f,
            Quaternion.LookRotation(-_right, Vector3.up),
            Vector3.one * 0.76f);

        PlaceWestHouseFacade("Imported_CoreWestHouse", -_right * 7.6f + _forward * 0.5f, 1f);
        PlaceEastHouseFacade("Imported_CoreEastHouse", _right * 7.8f + _forward * 2f, 1f, true);
        PlaceNorthHouseFacade("Imported_CoreNorthHouse", _forward * 8.2f, 1.04f);
        PlaceMayorFacadeImportedSet("Imported_CoreMayorFacade", _forward * 11.4f);
        PlaceStallImportedSet("Imported_CoreStallWest", -_right * 4.8f - _forward * 0.4f, false);
        PlaceStallImportedSet("Imported_CoreStallEast", _right * 4.5f + _forward * 0.7f, true);
        PlaceWellImportedSet("Imported_CoreWell", _forward * 1.1f - _right * 3.4f);
        PlaceDinnerTableImportedSet("Imported_CoreDinnerTable", _forward * 2.5f);
        PlaceMealImportedSet("Imported_CoreMealWest", -_right * 2.9f + _forward * 3.8f);
        PlaceMealImportedSet("Imported_CoreMealEast", _right * 3.2f + _forward * 4.1f);
        PlacePrayerPolesImportedSet("Imported_CorePrayer", -_right * 5.4f + _forward * 6.2f);
    }

    private void PlaceWestHouseFacade(string prefix, Vector3 center, float scale)
    {
        Quaternion facadeRotation = Quaternion.LookRotation(_right, Vector3.up);
        Quaternion roofRotation = Quaternion.LookRotation(_forward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Door_Round",
            $"{prefix}_Wall",
            transform,
            center + _right * 2.72f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/DoorFrame_Round_WoodDark",
            $"{prefix}_Frame",
            transform,
            center + _right * 2.72f + Vector3.up * 1.28f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Door_2_Round",
            $"{prefix}_Door",
            transform,
            center + _right * 2.66f + Vector3.up * 1.04f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_RoundTiles_6x8",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * 4.55f,
            roofRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Lantern_Wall",
            $"{prefix}_Lantern",
            transform,
            center + _right * 3.25f - _forward * 0.95f + Vector3.up * 2.1f,
            facadeRotation,
            Vector3.one * 0.78f);
    }

    private void PlaceEastHouseFacade(string prefix, Vector3 center, float scale, bool balcony)
    {
        Quaternion facadeRotation = Quaternion.LookRotation(-_right, Vector3.up);
        Quaternion roofRotation = Quaternion.LookRotation(_forward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Door_Flat",
            $"{prefix}_Wall",
            transform,
            center - _right * 2.92f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/DoorFrame_Flat_WoodDark",
            $"{prefix}_Frame",
            transform,
            center - _right * 2.88f + Vector3.up * 1.28f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Door_4_Flat",
            $"{prefix}_Door",
            transform,
            center - _right * 2.82f + Vector3.up * 1.04f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_RoundTiles_8x8",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * 4.62f,
            roofRotation,
            Vector3.one * scale);

        if (balcony)
        {
            RuntimeModelSpawner.Spawn(
                "Imported/Quaternius/MedievalVillage/FBX/Balcony_Simple_Straight",
                $"{prefix}_Balcony",
                transform,
                center - _right * 3.24f + Vector3.up * 2.45f,
                facadeRotation,
                Vector3.one * scale);
        }
    }

    private void PlaceNorthHouseFacade(string prefix, Vector3 center, float scale)
    {
        Quaternion facadeRotation = Quaternion.LookRotation(-_forward, Vector3.up);
        Quaternion roofRotation = Quaternion.LookRotation(_right, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Door_Flat",
            $"{prefix}_Wall",
            transform,
            center - _forward * 2.45f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/DoorFrame_Flat_Brick",
            $"{prefix}_Frame",
            transform,
            center - _forward * 2.42f + Vector3.up * 1.3f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Door_4_Flat",
            $"{prefix}_Door",
            transform,
            center - _forward * 2.36f + Vector3.up * 1.04f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_Front_Brick8",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * 4.72f,
            roofRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Banner_2_Cloth",
            $"{prefix}_Banner",
            transform,
            center - _forward * 3.1f + Vector3.up * 3.2f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.82f);
    }

    private void PlaceMayorFacadeImportedSet(string prefix, Vector3 center)
    {
        Quaternion facadeRotation = Quaternion.LookRotation(-_forward, Vector3.up);
        Quaternion roofRotation = Quaternion.LookRotation(_right, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Door_Round",
            $"{prefix}_Wall",
            transform,
            center - _forward * 1.42f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * 1.22f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/DoorFrame_Round_Brick",
            $"{prefix}_Frame",
            transform,
            center - _forward * 1.38f + Vector3.up * 1.45f,
            facadeRotation,
            Vector3.one * 1.16f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Door_1_Round",
            $"{prefix}_Door",
            transform,
            center - _forward * 1.3f + Vector3.up * 1.12f,
            facadeRotation,
            Vector3.one * 1.12f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_Front_Brick8",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * 6.15f,
            roofRotation,
            Vector3.one * 1.14f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/iron-fence-border-column",
            $"{prefix}_FenceLeft",
            transform,
            center - _right * 2.95f + _forward * 0.8f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.88f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/iron-fence-border-column",
            $"{prefix}_FenceRight",
            transform,
            center + _right * 2.95f + _forward * 0.8f + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_right, Vector3.up),
            Vector3.one * 0.88f);
    }

    private void PlaceStallImportedSet(string prefix, Vector3 center, bool withCart)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            withCart ? "Imported/Quaternius/FantasyProps/FBX/Stall_Cart_Empty" : "Imported/Quaternius/FantasyProps/FBX/Stall_Empty",
            $"{prefix}_Stall",
            transform,
            center + Vector3.up * 0.02f,
            rotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            withCart ? "Imported/Quaternius/FantasyProps/FBX/FarmCrate_Carrot" : "Imported/Quaternius/FantasyProps/FBX/FarmCrate_Apple",
            $"{prefix}_Goods",
            transform,
            center + _forward * 0.85f + Vector3.up * 0.08f,
            rotation,
            Vector3.one * 0.82f);
    }

    private void PlaceWellImportedSet(string prefix, Vector3 center)
    {
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/grave-border",
            $"{prefix}_Border",
            transform,
            center + Vector3.up * 0.02f,
            Quaternion.identity,
            Vector3.one * 0.94f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Bucket_Wooden_1",
            $"{prefix}_Bucket",
            transform,
            center + _right * 0.75f + _forward * 0.35f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_forward, Vector3.up),
            Vector3.one * 0.86f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Rope_1",
            $"{prefix}_Rope",
            transform,
            center + Vector3.up * 2.55f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.9f);
    }

    private void PlaceDinnerTableImportedSet(string prefix, Vector3 center)
    {
        Quaternion sideRotation = Quaternion.LookRotation(_right, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Table_Large",
            $"{prefix}_Table",
            transform,
            center + Vector3.up * 0.02f,
            sideRotation,
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Stool",
            $"{prefix}_StoolLeft",
            transform,
            center - _forward * 0.95f + Vector3.up * 0.02f,
            sideRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Stool",
            $"{prefix}_StoolRight",
            transform,
            center + _forward * 0.95f + Vector3.up * 0.02f,
            sideRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Table_Plate",
            $"{prefix}_Plate",
            transform,
            center + Vector3.up * 1.02f,
            sideRotation,
            Vector3.one);
    }

    private void PlaceMealImportedSet(string prefix, Vector3 center)
    {
        Quaternion sideRotation = Quaternion.LookRotation(_right, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Table_Large",
            $"{prefix}_Table",
            transform,
            center + Vector3.up * 0.02f,
            sideRotation,
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Stool",
            $"{prefix}_StoolA",
            transform,
            center - _forward * 0.86f + Vector3.up * 0.02f,
            sideRotation,
            Vector3.one * 0.84f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Stool",
            $"{prefix}_StoolB",
            transform,
            center + _forward * 0.86f + Vector3.up * 0.02f,
            sideRotation,
            Vector3.one * 0.84f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Table_Plate",
            $"{prefix}_Plate",
            transform,
            center + Vector3.up * 0.94f,
            sideRotation,
            Vector3.one * 0.9f);
    }

    private void PlacePrayerPolesImportedSet(string prefix, Vector3 center)
    {
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Banner_2_Cloth",
            $"{prefix}_BannerA",
            transform,
            center - _right * 1.1f + Vector3.up * 2.65f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.72f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Banner_2_Cloth",
            $"{prefix}_BannerB",
            transform,
            center + Vector3.up * 2.75f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.72f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Banner_2_Cloth",
            $"{prefix}_BannerC",
            transform,
            center + _right * 1.1f + Vector3.up * 2.6f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.72f);
    }

    private void HidePlaceholderVisuals()
    {
        HideRenderers(
            "WestHouse",
            "EastHouse",
            "NorthHouse",
            "DinnerTable",
            "StallWest_Counter",
            "StallWest_Canopy",
            "StallWest_PostLeft",
            "StallWest_PostRight",
            "StallEast_Counter",
            "StallEast_Canopy",
            "StallEast_PostLeft",
            "StallEast_PostRight",
            "BenchCluster_BenchA",
            "BenchCluster_BenchB",
            "BenchCluster_Crate",
            "MayorFacade_Base",
            "MayorFacade_Roof",
            "MayorFacade_Door");
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
