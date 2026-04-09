using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Chapter01BossHouseSlice : MonoBehaviour
{
    private const string RootName = "__RedCreekBossHouseSlice";
    private const string WorldEntryMarkerName = "WorldEntryMarker";

    private static readonly Color YardColor = new Color(0.18f, 0.17f, 0.16f);
    private static readonly Color HouseColor = new Color(0.22f, 0.19f, 0.18f);
    private static readonly Color TrimColor = new Color(0.34f, 0.26f, 0.18f);
    private static readonly Color AccentColor = new Color(0.42f, 0.1f, 0.11f);
    private static readonly Color FogColor = new Color(0.48f, 0.5f, 0.54f);

    private PlayerMover _player;
    private Vector3 _forward;
    private Vector3 _right;
    private bool _worldMode;

    public static void Ensure(PlayerMover player)
    {
        if (player == null || SceneManager.GetActiveScene().name != SceneLoader.Chapter01BossHouseSceneName)
        {
            return;
        }

        if (GameObject.Find(RootName) != null)
        {
            return;
        }

        GameObject root = new GameObject(RootName);
        Chapter01BossHouseSlice slice = root.AddComponent<Chapter01BossHouseSlice>();
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
        Chapter01BossHouseSlice slice = root.AddComponent<Chapter01BossHouseSlice>();
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
        BuildForecourt();
        BuildFlow();
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

    private void BuildForecourt()
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);

        CreateBlock("BossHouseYard", Vector3.zero, new Vector3(13f, 0.2f, 24f), rotation, YardColor);
        CreateBlock("WestBoundary", -_right * 7f + Vector3.up * 1.8f, new Vector3(0.5f, 3.6f, 24f), rotation, HouseColor);
        CreateBlock("EastBoundary", _right * 7f + Vector3.up * 1.8f, new Vector3(0.5f, 3.6f, 24f), rotation, HouseColor);
        CreateBlock("RearFacade", _forward * 8.8f + Vector3.up * 3.2f, new Vector3(10f, 6.4f, 3.2f), rotation, HouseColor);
        CreateBlock("RearRoof", _forward * 8.8f + Vector3.up * 6.7f, new Vector3(10.8f, 0.8f, 3.8f), rotation, new Color(0.16f, 0.1f, 0.09f));
        CreateBlock("MainDoor", _forward * 7.1f + Vector3.up * 1.5f, new Vector3(1.7f, 3f, 0.28f), rotation, new Color(0.18f, 0.1f, 0.08f));
        CreateBlock("SealStrip", _forward * 6.96f + Vector3.up * 2.2f, new Vector3(1.2f, 0.16f, 0.04f), rotation, AccentColor);
        CreateBlock("StudyWindowLeft", _forward * 7.3f - _right * 2.9f + Vector3.up * 2.3f, new Vector3(1.3f, 1.7f, 0.18f), rotation, TrimColor);
        CreateBlock("StudyWindowRight", _forward * 7.3f + _right * 2.9f + Vector3.up * 2.3f, new Vector3(1.3f, 1.7f, 0.18f), rotation, TrimColor);
        CreateBlock("SidePassageLeft", -_right * 4.8f + _forward * 2.8f + Vector3.up * 0.18f, new Vector3(2.2f, 0.36f, 7.2f), rotation, new Color(0.22f, 0.2f, 0.18f));
        CreateBlock("SidePassageRight", _right * 4.8f + _forward * 1.9f + Vector3.up * 0.18f, new Vector3(2.2f, 0.36f, 6.4f), rotation, new Color(0.22f, 0.2f, 0.18f));
        CreateBlock("CollapsedCart", -_right * 2.8f - _forward * 1.6f + Vector3.up * 0.48f, new Vector3(2.4f, 0.7f, 1.2f), Quaternion.LookRotation(_right, Vector3.up), TrimColor);
        CreateBlock("LedgerDesk", -_right * 3.1f + _forward * 3.8f + Vector3.up * 0.85f, new Vector3(1.9f, 1.1f, 1f), Quaternion.LookRotation(_right, Vector3.up), TrimColor);
        CreateBlock("CellarStairs", _right * 3.6f + _forward * 4.8f + Vector3.up * 0.12f, new Vector3(2.8f, 0.24f, 3.2f), rotation, new Color(0.2f, 0.19f, 0.18f));
        CreateBlock("CellarVoid", _right * 3.6f + _forward * 5.2f + Vector3.up * 0.04f, new Vector3(1.5f, 0.08f, 1.9f), rotation, new Color(0.08f, 0.05f, 0.05f));
        CreateBrazier("DoorBrazierLeft", _forward * 5.8f - _right * 4.9f);
        CreateBrazier("DoorBrazierRight", _forward * 5.8f + _right * 4.9f);
        CreateSealStakeLine("SealStakeLine_Left", -_right * 1.9f + _forward * 4f, -1f);
        CreateSealStakeLine("SealStakeLine_Right", _right * 1.9f + _forward * 4f, 1f);
        CreateReliefCache("ReliefCache", -_right * 5.1f + _forward * 1.4f);
        CreateNoticeStand("MayorNoticeStand", _right * 5f + _forward * 2.4f);
        CreateCellarWinch("CellarWinch", _right * 4.8f + _forward * 5.8f);
        CreatePrayerScreen("PrayerScreen_Left", -_right * 6.4f + _forward * 6.7f, -1f);
        CreatePrayerScreen("PrayerScreen_Right", _right * 6.4f + _forward * 6.7f, 1f);
        PlaceImportedDressing();
        HidePlaceholderVisuals();

        for (int i = 0; i < 4; i++)
        {
            GameObject fog = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fog.name = $"BossHouseFog_{i + 1}";
            fog.transform.SetParent(transform, true);
            fog.transform.position = _forward * (3f + i * 1.5f) + _right * ((i % 2 == 0 ? -1f : 1f) * 1.6f) + Vector3.up * (0.8f + i * 0.08f);
            fog.transform.localScale = Vector3.one * (0.9f + i * 0.12f);
            Tint(fog.GetComponent<Renderer>(), FogColor);
        }
    }

    private void BuildFlow()
    {
        QuestObjectiveTarget deskObjective = CreateMayorDesk();
        QuestObjectiveTarget sealObjective = CreateMayorSeal();
        QuestObjectiveTarget cellarObjective = CreateCellarDescent();

        deskObjective.SetNextObjective(sealObjective);
        sealObjective.SetNextObjective(cellarObjective);

        if (QuestTracker.Instance != null && string.IsNullOrWhiteSpace(QuestTracker.Instance.CurrentObjectiveId))
        {
            deskObjective.RegisterAsCurrentObjective();
        }
    }

    private QuestObjectiveTarget CreateMayorDesk()
    {
        GameObject desk = GameObject.Find("LedgerDesk");
        TestStoneInteractable interactable = desk.AddComponent<TestStoneInteractable>();
        interactable.ConfigureFallbackDialogue(
            "村长的书桌",
            "查看",
            "桌上摊开的不是村务账本，而是一份封街期间的物资与失踪名单。\n\n最后一页被仓促改过：\n“若门后的雾开始回应名字，就把印戒带去地下，不要再让任何人进来。”");

        QuestObjectiveTarget objective = desk.AddComponent<QuestObjectiveTarget>();
        objective.Configure("inspect_mayor_desk", "查看村长屋前留下的书桌与名单", "调查", false, true);
        return objective;
    }

    private QuestObjectiveTarget CreateMayorSeal()
    {
        GameObject seal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        seal.name = "MayorSeal";
        seal.transform.SetParent(transform, true);
        seal.transform.position = _forward * 3.6f - _right * 2f + Vector3.up * 0.42f;
        seal.transform.localScale = Vector3.one * 0.55f;
        Tint(seal.GetComponent<Renderer>(), new Color(0.68f, 0.55f, 0.22f));

        PickupInteractable interactable = seal.AddComponent<PickupInteractable>();
        interactable.Configure("redcreek_mayor_seal", "村长印戒", "拾取", false);

        QuestObjectiveTarget objective = seal.AddComponent<QuestObjectiveTarget>();
        objective.Configure("pickup_mayor_seal", "拾起掉在地上的村长印戒", "证物", false, true);
        return objective;
    }

    private QuestObjectiveTarget CreateCellarDescent()
    {
        GameObject triggerRoot = new GameObject("CellarDescent");
        triggerRoot.transform.SetParent(transform, true);
        triggerRoot.transform.position = _right * 3.6f + _forward * 5.2f + Vector3.up;
        triggerRoot.transform.rotation = Quaternion.LookRotation(_forward, Vector3.up);

        BoxCollider colliderComponent = triggerRoot.AddComponent<BoxCollider>();
        colliderComponent.size = new Vector3(3.2f, 2.2f, 3.6f);
        colliderComponent.center = new Vector3(0f, 0.2f, 0f);
        colliderComponent.isTrigger = true;

        CreateBlock(
            "CellarSealMonolith",
            triggerRoot.transform.position + Vector3.up * 1.45f + _right * 1.15f,
            new Vector3(0.55f, 2.9f, 0.55f),
            Quaternion.LookRotation(_forward, Vector3.up),
            new Color(0.25f, 0.12f, 0.13f));
        CreateBlock(
            "CellarSealMonolithTwin",
            triggerRoot.transform.position + Vector3.up * 1.45f - _right * 1.15f,
            new Vector3(0.55f, 2.9f, 0.55f),
            Quaternion.LookRotation(_forward, Vector3.up),
            new Color(0.25f, 0.12f, 0.13f));
        CreateBlock(
            "CellarSealCore",
            triggerRoot.transform.position + Vector3.up * 0.75f,
            new Vector3(1.65f, 1.1f, 1.65f),
            Quaternion.LookRotation(_forward, Vector3.up),
            new Color(0.16f, 0.05f, 0.06f));

        CombatEncounterTrigger encounterTrigger = triggerRoot.AddComponent<CombatEncounterTrigger>();
        encounterTrigger.Configure(
            "村长屋地窖异化体",
            "defeat_bound_patron",
            "击败盘踞在地窖裂口前的封存心骸",
            "首领",
            "封存心骸",
            5,
            1,
            SceneLoader.Chapter01EndSceneName);
        encounterTrigger.ConfigureDialogue(
            "伊尔萨恩",
            new[]
            {
                "印戒刚靠近裂口，封蜡就开始发热。地下不是空的，有东西在借着封印喘气。",
                "别让它再爬出来。先把这里的回应彻底按回去。"
            },
            "伊尔萨恩",
            new[]
            {
                "它不是村长本人，更像被地下仪式硬留住的一截心核外壳。",
                "下去。真正该看的东西，还在更深处。"
            });
        encounterTrigger.ConfigureRequirement(
            "redcreek_mayor_seal",
            "伊尔萨恩",
            new[]
            {
                "先把桌上的记录看完，再拿上那枚印戒。地下这道封口显然是给它留的。"
            });
        encounterTrigger.ConfigureObjectiveRequirement("descend_cellar_breach");
        encounterTrigger.ConfigurePlayerRestore(true);

        QuestObjectiveTarget objective = triggerRoot.AddComponent<QuestObjectiveTarget>();
        objective.Configure("descend_cellar_breach", "进入村长屋下方的地下裂口", "终点", false, false);
        return objective;
    }

    private IEnumerator ShowArrivalBeat()
    {
        yield return null;

        if (SimpleDialogueUI.Instance == null || ChapterState.GetFlag("boss_house_seen"))
        {
            yield break;
        }

        ChapterState.SetFlag("boss_house_seen", true);
        SimpleDialogueUI.Instance.Show(
            "伊尔萨恩",
            "村长屋前没有搏斗痕迹，只有一层新的封蜡和还没熄灭的火盆。",
            "先看书桌和地上的印戒，再决定要不要进地下。");
    }

    private void CreateBrazier(string name, Vector3 position)
    {
        CreateBlock($"{name}_Base", position + Vector3.up * 0.35f, new Vector3(0.24f, 0.7f, 0.24f), Quaternion.identity, new Color(0.24f, 0.22f, 0.2f));
        CreateBlock($"{name}_Bowl", position + Vector3.up * 0.72f, new Vector3(0.82f, 0.18f, 0.82f), Quaternion.identity, new Color(0.2f, 0.18f, 0.18f));
        CreateBlock($"{name}_Flame", position + Vector3.up * 1.05f, new Vector3(0.34f, 0.48f, 0.34f), Quaternion.identity, new Color(0.82f, 0.42f, 0.16f));
    }

    private void CreateWorldMarker(string name, Vector3 localPosition)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = localPosition;
    }

    private void PlaceImportedDressing()
    {
        Quaternion forwardRotation = Quaternion.LookRotation(_forward, Vector3.up);
        Quaternion sideRotation = Quaternion.LookRotation(_right, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Door_Round",
            "Imported_BossHouseFacade",
            transform,
            _forward * 7.3f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 1.2f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_Front_Brick8",
            "Imported_BossHouseRoof",
            transform,
            _forward * 8.8f + Vector3.up * 6.55f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 1.2f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Window_Roof_Wide",
            "Imported_BossHouseWindowLeft",
            transform,
            _forward * 7.55f - _right * 2.9f + Vector3.up * 2.15f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 1.02f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Window_Roof_Wide",
            "Imported_BossHouseWindowRight",
            transform,
            _forward * 7.55f + _right * 2.9f + Vector3.up * 2.15f,
            Quaternion.LookRotation(-_right, Vector3.up),
            Vector3.one * 1.02f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/DoorFrame_Round_WoodDark",
            "Imported_MainDoorFrame",
            transform,
            _forward * 7.05f + Vector3.up * 1.42f,
            forwardRotation,
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Door_2_Round",
            "Imported_MainDoor",
            transform,
            _forward * 6.96f + Vector3.up * 1.18f,
            forwardRotation,
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_Wagon",
            "Imported_YardWagon",
            transform,
            -_right * 2.8f - _forward * 1.6f + Vector3.up * 0.15f,
            sideRotation,
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Crate_Wooden",
            "Imported_ReliefCrateA",
            transform,
            -_right * 5.4f + _forward * 1.15f + Vector3.up * 0.16f,
            sideRotation,
            Vector3.one * 0.85f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/FarmCrate_Empty",
            "Imported_ReliefCrateB",
            transform,
            -_right * 4.55f + _forward * 1.75f + Vector3.up * 0.16f,
            forwardRotation,
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Barrel",
            "Imported_DoorBarrelLeft",
            transform,
            _forward * 5.6f - _right * 4.15f + Vector3.up * 0.18f,
            forwardRotation,
            Vector3.one * 0.78f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Barrel",
            "Imported_DoorBarrelRight",
            transform,
            _forward * 5.55f + _right * 4.1f + Vector3.up * 0.18f,
            forwardRotation,
            Vector3.one * 0.78f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Lantern_Wall",
            "Imported_LanternLeft",
            transform,
            _forward * 6.45f - _right * 3.7f + Vector3.up * 2.15f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.8f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Lantern_Wall",
            "Imported_LanternRight",
            transform,
            _forward * 6.45f + _right * 3.7f + Vector3.up * 2.15f,
            Quaternion.LookRotation(-_right, Vector3.up),
            Vector3.one * 0.8f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Scroll_2",
            "Imported_DeskScroll",
            transform,
            -_right * 3.15f + _forward * 3.7f + Vector3.up * 1.42f,
            sideRotation,
            Vector3.one * 0.8f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/iron-fence-border",
            "Imported_SealFenceLeft",
            transform,
            -_right * 6.2f + _forward * 4.8f + Vector3.up * 0.04f,
            forwardRotation,
            Vector3.one * 0.88f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/iron-fence-border",
            "Imported_SealFenceRight",
            transform,
            _right * 6.2f + _forward * 4.8f + Vector3.up * 0.04f,
            forwardRotation,
            Vector3.one * 0.88f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/gravestone-cross",
            "Imported_SealStoneLeft",
            transform,
            -_right * 5.6f + _forward * 6.4f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/gravestone-round",
            "Imported_SealStoneRight",
            transform,
            _right * 5.6f + _forward * 6.3f + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_right, Vector3.up),
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/coffin-old",
            "Imported_SealCoffin",
            transform,
            _right * 1.85f + _forward * 4.9f + Vector3.up * 0.02f,
            sideRotation,
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/fire-basket",
            "Imported_SealBasketLeft",
            transform,
            -_right * 4.75f + _forward * 5.6f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/fire-basket",
            "Imported_SealBasketRight",
            transform,
            _right * 4.75f + _forward * 5.6f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.92f);
    }

    private void HidePlaceholderVisuals()
    {
        HideRenderers(
            "RearFacade",
            "RearRoof",
            "MainDoor",
            "SealStrip",
            "StudyWindowLeft",
            "StudyWindowRight",
            "CollapsedCart");
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

    private void CreateSealStakeLine(string name, Vector3 center, float sideSign)
    {
        for (int i = 0; i < 3; i++)
        {
            float depth = i * 1.25f;
            CreateBlock(
                $"{name}_Stake_{i + 1}",
                center + _forward * depth + Vector3.up * 0.9f,
                new Vector3(0.14f, 1.8f, 0.14f),
                Quaternion.identity,
                new Color(0.28f, 0.2f, 0.15f));
            CreateBlock(
                $"{name}_Seal_{i + 1}",
                center + _forward * depth + Vector3.up * 1.48f + _right * sideSign * 0.12f,
                new Vector3(0.42f, 0.62f, 0.06f),
                Quaternion.LookRotation(_right * -sideSign, Vector3.up),
                AccentColor);
        }
    }

    private void CreateReliefCache(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);
        CreateBlock($"{name}_Pallet", center + Vector3.up * 0.18f, new Vector3(2.4f, 0.36f, 2f), rotation, new Color(0.29f, 0.22f, 0.16f));
        CreateBlock($"{name}_CrateA", center - _right * 0.6f + Vector3.up * 0.64f, new Vector3(0.9f, 0.92f, 0.9f), rotation, new Color(0.35f, 0.25f, 0.17f));
        CreateBlock($"{name}_CrateB", center + _right * 0.7f + _forward * 0.25f + Vector3.up * 0.52f, new Vector3(0.8f, 0.68f, 0.8f), rotation, new Color(0.33f, 0.24f, 0.18f));
        CreateBlock($"{name}_Bundle", center + _forward * 0.7f + Vector3.up * 0.42f, new Vector3(1.1f, 0.54f, 0.7f), rotation, new Color(0.46f, 0.41f, 0.33f));
    }

    private void CreateNoticeStand(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_right, Vector3.up);
        CreateBlock($"{name}_Post", center + Vector3.up * 1.2f, new Vector3(0.2f, 2.4f, 0.2f), Quaternion.identity, new Color(0.28f, 0.21f, 0.15f));
        CreateBlock($"{name}_Board", center + Vector3.up * 1.8f, new Vector3(1.4f, 1.2f, 0.12f), rotation, new Color(0.39f, 0.31f, 0.22f));
        CreateBlock($"{name}_WaxSeal", center + Vector3.up * 1.8f + _forward * 0.08f, new Vector3(0.24f, 0.24f, 0.05f), rotation, AccentColor);
    }

    private void CreateCellarWinch(string name, Vector3 center)
    {
        CreateBlock($"{name}_PostLeft", center - _right * 0.82f + Vector3.up * 1.4f, new Vector3(0.16f, 2.8f, 0.16f), Quaternion.identity, new Color(0.25f, 0.19f, 0.14f));
        CreateBlock($"{name}_PostRight", center + _right * 0.82f + Vector3.up * 1.4f, new Vector3(0.16f, 2.8f, 0.16f), Quaternion.identity, new Color(0.25f, 0.19f, 0.14f));
        CreateBlock($"{name}_Beam", center + Vector3.up * 2.75f, new Vector3(1.9f, 0.16f, 0.16f), Quaternion.LookRotation(_right, Vector3.up), new Color(0.3f, 0.22f, 0.16f));
        CreateBlock($"{name}_Hook", center + Vector3.up * 1.8f, new Vector3(0.08f, 1.8f, 0.08f), Quaternion.identity, new Color(0.49f, 0.44f, 0.39f));
    }

    private void CreatePrayerScreen(string name, Vector3 center, float sideSign)
    {
        Quaternion rotation = Quaternion.LookRotation(_right * -sideSign, Vector3.up);
        CreateBlock($"{name}_Frame", center + Vector3.up * 1.8f, new Vector3(0.32f, 3.6f, 2f), rotation, new Color(0.24f, 0.2f, 0.17f));
        CreateBlock($"{name}_Cloth", center + Vector3.up * 1.8f, new Vector3(0.08f, 3.1f, 1.6f), rotation, new Color(0.34f, 0.11f, 0.1f));
        CreateBlock($"{name}_Tag", center + _forward * 0.45f + Vector3.up * 2.3f, new Vector3(0.06f, 0.55f, 0.36f), rotation, new Color(0.58f, 0.49f, 0.34f));
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
