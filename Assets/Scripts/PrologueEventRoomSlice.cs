using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PrologueEventRoomSlice : MonoBehaviour
{
    private const string RootName = "__PrologueEventRoomSlice";
    private const string WorldEntryMarkerName = "WorldEntryMarker";

    private static readonly Color FloorColor = new Color(0.13f, 0.13f, 0.15f);
    private static readonly Color WallColor = new Color(0.18f, 0.16f, 0.18f);
    private static readonly Color RitualColor = new Color(0.45f, 0.09f, 0.09f);
    private static readonly Color AshColor = new Color(0.68f, 0.64f, 0.6f);
    private static readonly Color EchoColor = new Color(0.63f, 0.68f, 0.9f);
    private static readonly Color EmberColor = new Color(0.82f, 0.42f, 0.18f);

    private PlayerMover _player;
    private Vector3 _forward;
    private Vector3 _right;
    private Vector3 _roomAnchor;
    private bool _worldMode;

    public static void Ensure(PlayerMover player)
    {
        if (player == null || SceneManager.GetActiveScene().name != SceneLoader.PrologueEventRoomSceneName)
        {
            return;
        }

        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            PrologueEventRoomSlice existingSlice = existingRoot.GetComponent<PrologueEventRoomSlice>();
            existingSlice?.BindRuntimeState(player);
            return;
        }

        GameObject root = new GameObject(RootName);
        PrologueEventRoomSlice slice = root.AddComponent<PrologueEventRoomSlice>();
        slice.Build(player);
    }

    public static void EnsureWorld(PlayerMover player, Vector3 worldOffset, Vector3 worldForward)
    {
        if (player == null)
        {
            return;
        }

        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            return;
        }

        GameObject root = new GameObject(RootName);
        PrologueEventRoomSlice slice = root.AddComponent<PrologueEventRoomSlice>();
        slice.Build(player, true, worldOffset, worldForward);
    }

    private void BindRuntimeState(PlayerMover player)
    {
        _player = player;
        _forward = new Vector3(1f, 0f, 1f).normalized;
        _right = Vector3.Cross(Vector3.up, _forward).normalized;
        _roomAnchor = _forward * 6.5f;
        PositionPlayer();
    }

    private void Build(PlayerMover player, bool worldMode = false, Vector3 worldOffset = default, Vector3 worldForward = default)
    {
        _player = player;
        _worldMode = worldMode;
        _forward = worldMode && worldForward.sqrMagnitude > 0.001f
            ? Vector3.ProjectOnPlane(worldForward, Vector3.up).normalized
            : new Vector3(1f, 0f, 1f).normalized;

        if (_forward.sqrMagnitude <= 0.001f)
        {
            _forward = new Vector3(1f, 0f, 1f).normalized;
        }

        _right = Vector3.Cross(Vector3.up, _forward).normalized;
        _roomAnchor = _forward * 6.5f;

        if (!_worldMode)
        {
            PositionPlayer();
        }
        BuildRoomShell();
        BuildEventFlow();

        if (_worldMode)
        {
            transform.position = worldOffset;
            CreateWorldMarker(WorldEntryMarkerName, _forward * 4.5f + Vector3.up);
        }
        else
        {
            StartCoroutine(ShowEntranceBeat());
        }
    }

    private void PositionPlayer()
    {
        Vector3 spawnPosition = _roomAnchor - _forward * 2f + Vector3.up;
        _player.transform.position = spawnPosition;
        _player.transform.rotation = Quaternion.LookRotation(_forward, Vector3.up);

        CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(_player.transform, true);
        }
    }

    private void BuildRoomShell()
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);

        // The exploration camera stays outside the room; this cutaway layout keeps the space readable.
        CreateBlock("RitualFloor", _roomAnchor, new Vector3(16f, 0.2f, 22f), rotation, FloorColor);
        CreateBlock("NorthWall", _roomAnchor + _forward * 10.2f + Vector3.up * 2.1f, new Vector3(16f, 4.2f, 0.5f), rotation, WallColor);
        CreateBlock("WestWall", _roomAnchor - _right * 7.8f + Vector3.up * 2.1f, new Vector3(0.5f, 4.2f, 22f), rotation, WallColor);
        CreateBlock("EastWall", _roomAnchor + _right * 7.8f + Vector3.up * 2.1f, new Vector3(0.5f, 4.2f, 22f), rotation, WallColor);
        CreateBlock("EntranceLintel", _roomAnchor - _forward * 8.4f + Vector3.up * 2.9f, new Vector3(4.4f, 0.6f, 0.8f), rotation, WallColor);
        CreateBlock("EntrancePierLeft", _roomAnchor - _forward * 8.4f - _right * 2.4f + Vector3.up * 1.2f, new Vector3(0.9f, 2.4f, 0.8f), rotation, WallColor);
        CreateBlock("EntrancePierRight", _roomAnchor - _forward * 8.4f + _right * 2.4f + Vector3.up * 1.2f, new Vector3(0.9f, 2.4f, 0.8f), rotation, WallColor);

        CreateBlock("RitualDais", _roomAnchor + _forward * 1.6f + Vector3.up * 0.35f, new Vector3(3.8f, 0.7f, 3.8f), rotation, new Color(0.21f, 0.18f, 0.18f));
        CreateBlock("RearAltar", _roomAnchor + _forward * 4.7f + Vector3.up * 1.1f, new Vector3(3f, 2.2f, 1.1f), rotation, new Color(0.24f, 0.2f, 0.18f));
        CreateBlock("RearBannerLeft", _roomAnchor + _forward * 7.5f - _right * 4.1f + Vector3.up * 2.2f, new Vector3(0.4f, 3.8f, 1.4f), rotation, RitualColor * 0.8f);
        CreateBlock("RearBannerRight", _roomAnchor + _forward * 7.5f + _right * 4.1f + Vector3.up * 2.2f, new Vector3(0.4f, 3.8f, 1.4f), rotation, RitualColor * 0.8f);
        CreateBlock("RitualMarkHorizontal", _roomAnchor + _forward * 1.8f + Vector3.up * 0.05f, new Vector3(6.4f, 0.03f, 0.45f), rotation, RitualColor);
        CreateBlock("RitualMarkVertical", _roomAnchor + _forward * 1.8f + Vector3.up * 0.05f, new Vector3(0.45f, 0.03f, 6.4f), rotation, RitualColor);
        CreateBlock("DraggedStain", _roomAnchor + _forward * 0.9f + _right * 0.2f + Vector3.up * 0.04f, new Vector3(0.7f, 0.02f, 4.4f), rotation, new Color(0.2f, 0.05f, 0.05f));

        for (int i = 0; i < 4; i++)
        {
            float signX = i < 2 ? -1f : 1f;
            float signZ = i % 2 == 0 ? -1f : 1f;
            CreateBlock(
                $"Pillar_{i + 1}",
                _roomAnchor + _right * signX * 4.8f + _forward * (signZ < 0f ? 1.2f : 6f) + Vector3.up * 1.7f,
                new Vector3(0.7f, 3.4f, 0.7f),
                rotation,
                WallColor);
        }

        CreateBrazier("Brazier_LeftFront", _roomAnchor - _right * 5.2f - _forward * 1.8f);
        CreateBrazier("Brazier_RightFront", _roomAnchor + _right * 5.2f - _forward * 1.8f);
        CreateBrazier("Brazier_LeftRear", _roomAnchor - _right * 5.4f + _forward * 7.8f);
        CreateBrazier("Brazier_RightRear", _roomAnchor + _right * 5.4f + _forward * 7.8f);
        CreateDebrisPile("DebrisPile_Left", _roomAnchor - _right * 4.3f + _forward * 3.2f);
        CreateDebrisPile("DebrisPile_Right", _roomAnchor + _right * 4.1f + _forward * 4.8f);
        CreateCollapsedPews("PewCluster_Left", _roomAnchor - _right * 3.6f - _forward * 2.1f, -1f);
        CreateCollapsedPews("PewCluster_Right", _roomAnchor + _right * 3.6f - _forward * 2.1f, 1f);
        CreateReliquaryShelf("Reliquary_Left", _roomAnchor - _right * 6.1f + _forward * 5.2f, -1f);
        CreateReliquaryShelf("Reliquary_Right", _roomAnchor + _right * 6.1f + _forward * 5.2f, 1f);
        CreateChainAnchor("ChainAnchor_Center", _roomAnchor + _forward * 2.2f);
        PlaceImportedDressing();
        HidePlaceholderVisuals();

        for (int i = 0; i < 3; i++)
        {
            GameObject ember = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ember.name = $"EchoEmber_{i + 1}";
            ember.transform.SetParent(transform, true);
            ember.transform.position = _roomAnchor + _forward * (0.9f + i * 1.05f) + _right * (i - 1f) * 1.1f + Vector3.up * (0.65f + i * 0.2f);
            ember.transform.localScale = Vector3.one * (0.35f + i * 0.12f);
            Tint(ember.GetComponent<Renderer>(), EchoColor);
        }
    }

    private void BuildEventFlow()
    {
        QuestObjectiveTarget altarObjective = CreateRitualAltar();
        QuestObjectiveTarget tokenObjective = CreateAshenToken();
        QuestObjectiveTarget echoObjective = CreateEchoCircle();

        altarObjective.SetNextObjective(tokenObjective);
        tokenObjective.SetNextObjective(echoObjective);

        if (QuestTracker.Instance != null && string.IsNullOrWhiteSpace(QuestTracker.Instance.CurrentObjectiveId))
        {
            altarObjective.RegisterAsCurrentObjective();
        }
    }

    private QuestObjectiveTarget CreateRitualAltar()
    {
        Vector3 position = _roomAnchor + _forward * 1.9f + Vector3.up * 1.15f;
        GameObject altar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        altar.name = "RitualAltar";
        altar.transform.SetParent(transform, true);
        altar.transform.position = position;
        altar.transform.localScale = new Vector3(1.6f, 0.6f, 1f);
        altar.transform.rotation = Quaternion.LookRotation(_right, Vector3.up);
        Tint(altar.GetComponent<Renderer>(), RitualColor);

        TestStoneInteractable interactable = altar.AddComponent<TestStoneInteractable>();
        interactable.ConfigureFallbackDialogue(
            "裂开的祭台",
            "调查",
            "祭台边缘还残留着温度，血迹却已经干得发黑。\n\n地上被拖拽出的痕迹并不是人形，更像一团不断蜷缩又伸展的影子。");

        QuestObjectiveTarget objective = altar.AddComponent<QuestObjectiveTarget>();
        objective.Configure("inspect_ritual_altar", "调查房间中央的裂开祭台", "调查", false, true);
        return objective;
    }

    private QuestObjectiveTarget CreateAshenToken()
    {
        Vector3 position = _roomAnchor - _forward * 0.2f - _right * 1.8f + Vector3.up * 0.35f;
        GameObject token = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        token.name = "AshenToken";
        token.transform.SetParent(transform, true);
        token.transform.position = position;
        token.transform.localScale = Vector3.one * 0.45f;
        Tint(token.GetComponent<Renderer>(), AshColor);

        PickupInteractable interactable = token.AddComponent<PickupInteractable>();
        interactable.Configure("ashen_prayer_token", "灰烬祷片", "拾取", false);

        QuestObjectiveTarget objective = token.AddComponent<QuestObjectiveTarget>();
        objective.Configure("pickup_ashen_token", "拾取散落在地上的灰烬祷片", "证物", false, true);
        return objective;
    }

    private QuestObjectiveTarget CreateEchoCircle()
    {
        GameObject triggerRoot = new GameObject("EchoCircle");
        triggerRoot.transform.SetParent(transform, true);
        triggerRoot.transform.position = _roomAnchor + _forward * 6.5f + Vector3.up * 0.8f;
        triggerRoot.transform.rotation = Quaternion.LookRotation(_forward, Vector3.up);

        BoxCollider colliderComponent = triggerRoot.AddComponent<BoxCollider>();
        colliderComponent.size = new Vector3(2.8f, 1.8f, 2.8f);
        colliderComponent.isTrigger = true;

        TriggerZoneObjective triggerObjective = triggerRoot.AddComponent<TriggerZoneObjective>();
        triggerObjective.Configure("enter_echo_circle", false, true, true);

        CombatEncounterTrigger encounterTrigger = triggerRoot.AddComponent<CombatEncounterTrigger>();
        encounterTrigger.Configure(
            "裂隙中的回响",
            "defeat_ritual_echo",
            "击败从祭台残响中凝出的怪物",
            "战斗",
            "仪式回响",
            3,
            1,
            SceneLoader.Chapter01RedCreekEntranceSceneName);
        encounterTrigger.ConfigureDialogue(
            "残响",
            new[]
            {
                "那道祷告声忽然变得清晰，像是有人在你身边完成了最后一个音节。",
                "祭台上方的黑雾猛地向内收束，硬生生拧出了一具会站立的轮廓。"
            },
            "伊尔萨恩",
            new[]
            {
                "这东西更像从仪式里漏出来的一截残魂，不像真正的生物。",
                "它最后喊出的还是‘赤溪村’。先去那里，看看到底是谁在把人变成这种东西。"
            });
        encounterTrigger.ConfigureRequirement(
            "ashen_prayer_token",
            "伊尔萨恩",
            new[]
            {
                "地上那片灰烬祷片还没拿起来。残响是顺着它爬出来的。"
            });

        QuestObjectiveTarget objective = triggerRoot.AddComponent<QuestObjectiveTarget>();
        objective.Configure("enter_echo_circle", "靠近祭台后的残响源头", "异象", false, false);
        return objective;
    }

    private void CreateBrazier(string name, Vector3 basePosition)
    {
        GameObject bowl = CreateBlock(name, basePosition + Vector3.up * 0.7f, new Vector3(0.9f, 0.22f, 0.9f), Quaternion.identity, new Color(0.2f, 0.18f, 0.18f));
        bowl.transform.SetParent(transform, true);

        GameObject pillar = CreateBlock($"{name}_Post", basePosition + Vector3.up * 0.35f, new Vector3(0.18f, 0.7f, 0.18f), Quaternion.identity, new Color(0.24f, 0.22f, 0.2f));
        pillar.transform.SetParent(transform, true);

        GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flame.name = $"{name}_Flame";
        flame.transform.SetParent(transform, true);
        flame.transform.position = basePosition + Vector3.up * 1.05f;
        flame.transform.localScale = new Vector3(0.42f, 0.56f, 0.42f);
        Tint(flame.GetComponent<Renderer>(), EmberColor);
    }

    private void CreateWorldMarker(string name, Vector3 localPosition)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = localPosition;
    }

    private void CreateDebrisPile(string name, Vector3 center)
    {
        for (int i = 0; i < 3; i++)
        {
            float side = i - 1f;
            GameObject debris = CreateBlock(
                $"{name}_{i + 1}",
                center + _right * side * 0.36f + _forward * side * 0.18f + Vector3.up * (0.12f + i * 0.04f),
                new Vector3(0.55f + i * 0.14f, 0.24f + i * 0.06f, 0.62f),
                Quaternion.LookRotation(_right * (side >= 0f ? 1f : -1f), Vector3.up),
                new Color(0.22f, 0.2f, 0.19f));
            debris.transform.SetParent(transform, true);
        }
    }

    private void CreateCollapsedPews(string name, Vector3 center, float sideSign)
    {
        Quaternion baseRotation = Quaternion.LookRotation(_right * sideSign, Vector3.up);
        CreateBlock($"{name}_BenchA", center + Vector3.up * 0.42f, new Vector3(2.6f, 0.24f, 0.52f), baseRotation, new Color(0.29f, 0.22f, 0.17f));
        CreateBlock($"{name}_BenchB", center + _forward * 1.3f + Vector3.up * 0.36f, new Vector3(2.1f, 0.22f, 0.5f), Quaternion.LookRotation(_right * sideSign + _forward * 0.2f, Vector3.up), new Color(0.25f, 0.19f, 0.16f));
        CreateBlock($"{name}_BenchC", center - _forward * 1.1f + _right * sideSign * 0.5f + Vector3.up * 0.3f, new Vector3(1.7f, 0.2f, 0.46f), Quaternion.LookRotation(_right * sideSign - _forward * 0.35f, Vector3.up), new Color(0.23f, 0.18f, 0.16f));
    }

    private void CreateReliquaryShelf(string name, Vector3 center, float sideSign)
    {
        Quaternion wallRotation = Quaternion.LookRotation(_right * -sideSign, Vector3.up);
        CreateBlock($"{name}_Frame", center + Vector3.up * 1.6f, new Vector3(1.8f, 3f, 0.45f), wallRotation, new Color(0.23f, 0.21f, 0.19f));
        CreateBlock($"{name}_ShelfA", center + Vector3.up * 0.95f, new Vector3(1.55f, 0.12f, 0.72f), wallRotation, new Color(0.31f, 0.24f, 0.18f));
        CreateBlock($"{name}_ShelfB", center + Vector3.up * 1.7f, new Vector3(1.55f, 0.12f, 0.72f), wallRotation, new Color(0.31f, 0.24f, 0.18f));
        CreateBlock($"{name}_Urn", center + Vector3.up * 1.18f + _forward * 0.1f, new Vector3(0.34f, 0.5f, 0.34f), wallRotation, AshColor);
        CreateBlock($"{name}_Icon", center + Vector3.up * 2.08f - _forward * 0.08f, new Vector3(0.42f, 0.62f, 0.08f), wallRotation, RitualColor * 0.9f);
    }

    private void CreateChainAnchor(string name, Vector3 center)
    {
        CreateBlock($"{name}_Ceiling", center + Vector3.up * 3.8f, new Vector3(3.2f, 0.14f, 3.2f), Quaternion.identity, new Color(0.16f, 0.14f, 0.15f));
        CreateBlock($"{name}_ChainA", center - _right * 1.15f + _forward * 0.8f + Vector3.up * 2.3f, new Vector3(0.08f, 2.2f, 0.08f), Quaternion.identity, AshColor);
        CreateBlock($"{name}_ChainB", center + _right * 1.1f + _forward * 0.7f + Vector3.up * 2.15f, new Vector3(0.08f, 1.9f, 0.08f), Quaternion.identity, AshColor);
        CreateBlock($"{name}_ChainC", center + _forward * 1.4f + Vector3.up * 2.45f, new Vector3(0.08f, 2.4f, 0.08f), Quaternion.identity, AshColor);
    }

    private void PlaceImportedDressing()
    {
        Quaternion forwardRotation = Quaternion.LookRotation(_forward, Vector3.up);
        Quaternion sideRotation = Quaternion.LookRotation(_right, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/ModularDungeon/FBX/gate-metal-bars",
            "Imported_EntranceBars",
            transform,
            _roomAnchor - _forward * 8.25f + Vector3.up * 0.04f,
            forwardRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Shelf_Arch",
            "Imported_ReliquaryShelfLeft",
            transform,
            _roomAnchor - _right * 6.05f + _forward * 5.15f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Shelf_Arch",
            "Imported_ReliquaryShelfRight",
            transform,
            _roomAnchor + _right * 6.05f + _forward * 5.15f + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_right, Vector3.up),
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/CandleStick_Stand",
            "Imported_CandleStandLeft",
            transform,
            _roomAnchor - _right * 5.1f - _forward * 1.8f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/CandleStick_Stand",
            "Imported_CandleStandRight",
            transform,
            _roomAnchor + _right * 5.1f - _forward * 1.8f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Chain_Coil",
            "Imported_ChainCoil",
            transform,
            _roomAnchor + _forward * 1.1f - _right * 0.6f + Vector3.up * 0.06f,
            forwardRotation,
            Vector3.one * 0.86f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Floor_UnevenBrick",
            "Imported_RitualFloorA",
            transform,
            _roomAnchor - _forward * 3.8f + Vector3.up * 0.03f,
            forwardRotation,
            new Vector3(2.3f, 1f, 2.1f));
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Floor_UnevenBrick",
            "Imported_RitualFloorB",
            transform,
            _roomAnchor + Vector3.up * 0.03f,
            forwardRotation,
            new Vector3(2.5f, 1f, 2.4f));
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Floor_UnevenBrick",
            "Imported_RitualFloorC",
            transform,
            _roomAnchor + _forward * 4.2f + Vector3.up * 0.03f,
            forwardRotation,
            new Vector3(2.3f, 1f, 2.1f));
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Floor_UnevenBrick",
            "Imported_RitualFloorD",
            transform,
            _roomAnchor + _forward * 8.2f + Vector3.up * 0.03f,
            forwardRotation,
            new Vector3(2.2f, 1f, 1.8f));
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/brick-wall",
            "Imported_NorthWallLeft",
            transform,
            _roomAnchor + _forward * 10.2f - _right * 4.8f + Vector3.up * 0.02f,
            sideRotation,
            new Vector3(1.35f, 1.05f, 1f));
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/brick-wall",
            "Imported_NorthWallRight",
            transform,
            _roomAnchor + _forward * 10.2f + _right * 4.8f + Vector3.up * 0.02f,
            sideRotation,
            new Vector3(1.35f, 1.05f, 1f));
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/brick-wall",
            "Imported_WestWallMid",
            transform,
            _roomAnchor - _right * 7.78f + _forward * 2.8f + Vector3.up * 0.02f,
            forwardRotation,
            new Vector3(1.5f, 1.05f, 1f));
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/brick-wall",
            "Imported_EastWallMid",
            transform,
            _roomAnchor + _right * 7.78f + _forward * 2.8f + Vector3.up * 0.02f,
            forwardRotation,
            new Vector3(1.5f, 1.05f, 1f));
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/stone-wall-column",
            "Imported_EntrancePierLeft",
            transform,
            _roomAnchor - _forward * 8.4f - _right * 2.42f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/stone-wall-column",
            "Imported_EntrancePierRight",
            transform,
            _roomAnchor - _forward * 8.4f + _right * 2.42f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/altar-stone",
            "Imported_RearAltar",
            transform,
            _roomAnchor + _forward * 4.95f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Banner_1_Cloth",
            "Imported_RearBannerLeft",
            transform,
            _roomAnchor + _forward * 7.4f - _right * 4.2f + Vector3.up * 0.2f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 1.18f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Banner_2_Cloth",
            "Imported_RearBannerRight",
            transform,
            _roomAnchor + _forward * 7.4f + _right * 4.2f + Vector3.up * 0.2f,
            Quaternion.LookRotation(-_right, Vector3.up),
            Vector3.one * 1.18f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Chandelier",
            "Imported_RitualChandelier",
            transform,
            _roomAnchor + _forward * 2.1f + Vector3.up * 3.35f,
            forwardRotation,
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/bench-damaged",
            "Imported_PewLeft",
            transform,
            _roomAnchor - _right * 3.5f - _forward * 2f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.95f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/bench-damaged",
            "Imported_PewRight",
            transform,
            _roomAnchor + _right * 3.5f - _forward * 2f + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_right, Vector3.up),
            Vector3.one * 0.95f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/fire-basket",
            "Imported_BrazierRearLeft",
            transform,
            _roomAnchor - _right * 5.4f + _forward * 7.8f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/fire-basket",
            "Imported_BrazierRearRight",
            transform,
            _roomAnchor + _right * 5.4f + _forward * 7.8f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.82f);
    }

    private void HidePlaceholderVisuals()
    {
        HideRenderers(
            "NorthWall",
            "WestWall",
            "EastWall",
            "EntranceLintel",
            "EntrancePierLeft",
            "EntrancePierRight",
            "RearAltar",
            "RearBannerLeft",
            "RearBannerRight",
            "PewCluster_Left_BenchA",
            "PewCluster_Left_BenchB",
            "PewCluster_Left_BenchC",
            "PewCluster_Right_BenchA",
            "PewCluster_Right_BenchB",
            "PewCluster_Right_BenchC",
            "Reliquary_Left_Frame",
            "Reliquary_Left_ShelfA",
            "Reliquary_Left_ShelfB",
            "Reliquary_Left_Urn",
            "Reliquary_Left_Icon",
            "Reliquary_Right_Frame",
            "Reliquary_Right_ShelfA",
            "Reliquary_Right_ShelfB",
            "Reliquary_Right_Urn",
            "Reliquary_Right_Icon",
            "ChainAnchor_Center_Ceiling",
            "ChainAnchor_Center_ChainA",
            "ChainAnchor_Center_ChainB",
            "ChainAnchor_Center_ChainC",
            "Brazier_LeftFront_Post",
            "Brazier_LeftFront_Bowl",
            "Brazier_LeftFront_Flame",
            "Brazier_RightFront_Post",
            "Brazier_RightFront_Bowl",
            "Brazier_RightFront_Flame",
            "Brazier_LeftRear_Post",
            "Brazier_LeftRear_Bowl",
            "Brazier_LeftRear_Flame",
            "Brazier_RightRear_Post",
            "Brazier_RightRear_Bowl",
            "Brazier_RightRear_Flame");
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

    private IEnumerator ShowEntranceBeat()
    {
        yield return null;

        if (SimpleDialogueUI.Instance == null || ChapterState.GetFlag("prologue_event_room_entered"))
        {
            yield break;
        }

        ChapterState.SetFlag("prologue_event_room_entered", true);
        SimpleDialogueUI.Instance.Show(
            "伊尔萨恩",
            "这不是街区尽头的巷子，更像被硬生生剥出来的一间仪式房。",
            "先确认祭台和散落的东西。要是这里真和失踪案有关，线索不会只留在墙上。");
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
