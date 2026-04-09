using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Chapter01EndSlice : MonoBehaviour
{
    private const string RootName = "__RedCreekEndSlice";
    private const string WorldEntryMarkerName = "WorldEntryMarker";

    private static readonly Color FloorColor = new Color(0.12f, 0.11f, 0.12f);
    private static readonly Color WallColor = new Color(0.18f, 0.16f, 0.17f);
    private static readonly Color RelicColor = new Color(0.5f, 0.08f, 0.1f);
    private static readonly Color AshColor = new Color(0.63f, 0.6f, 0.56f);
    private static readonly Color EchoColor = new Color(0.44f, 0.5f, 0.62f);

    private PlayerMover _player;
    private Vector3 _forward;
    private Vector3 _right;
    private bool _worldMode;

    public static void Ensure(PlayerMover player)
    {
        if (player == null || SceneManager.GetActiveScene().name != SceneLoader.Chapter01EndSceneName)
        {
            return;
        }

        if (GameObject.Find(RootName) != null)
        {
            return;
        }

        GameObject root = new GameObject(RootName);
        Chapter01EndSlice slice = root.AddComponent<Chapter01EndSlice>();
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
        Chapter01EndSlice slice = root.AddComponent<Chapter01EndSlice>();
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
        BuildCellar();
        BuildFlow();
        if (_worldMode)
        {
            transform.position = worldOffset;
            CreateWorldMarker(WorldEntryMarkerName, -_forward * 7.5f + Vector3.up);
        }
        else
        {
            StartCoroutine(ShowArrivalBeat());
        }
    }

    private void PositionPlayer()
    {
        Vector3 spawnPosition = -_forward * 7.5f + Vector3.up;
        _player.transform.position = spawnPosition;
        _player.transform.rotation = Quaternion.LookRotation(_forward, Vector3.up);

        CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(_player.transform, true);
        }
    }

    private void BuildCellar()
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);

        CreateBlock("CellarFloor", Vector3.zero, new Vector3(14f, 0.2f, 18f), rotation, FloorColor);
        CreateBlock("BackWall", _forward * 8.8f + Vector3.up * 2.1f, new Vector3(14f, 4.2f, 0.5f), rotation, WallColor);
        CreateBlock("WestWall", -_right * 6.8f + Vector3.up * 2.1f, new Vector3(0.5f, 4.2f, 18f), rotation, WallColor);
        CreateBlock("EastWall", _right * 6.8f + Vector3.up * 2.1f, new Vector3(0.5f, 4.2f, 18f), rotation, WallColor);
        CreateBlock("RelicPlinth", _forward * 3.6f + Vector3.up * 0.5f, new Vector3(3.2f, 1f, 3.2f), rotation, new Color(0.22f, 0.18f, 0.18f));
        CreateBlock("BoundHeart", _forward * 3.7f + Vector3.up * 1.5f, new Vector3(1.2f, 1.8f, 1.2f), rotation, RelicColor);
        CreateBlock("RitualChainLeft", _forward * 4.5f - _right * 2.7f + Vector3.up * 1.9f, new Vector3(0.14f, 2.8f, 0.14f), rotation, AshColor);
        CreateBlock("RitualChainRight", _forward * 4.5f + _right * 2.7f + Vector3.up * 1.9f, new Vector3(0.14f, 2.8f, 0.14f), rotation, AshColor);
        CreateBlock("AshTable", -_right * 3.1f + _forward * 1.4f + Vector3.up * 0.9f, new Vector3(2.2f, 1f, 1.2f), Quaternion.LookRotation(_right, Vector3.up), new Color(0.32f, 0.25f, 0.18f));
        CreateBlock("ExitSeal", _forward * 6.8f + Vector3.up * 0.04f, new Vector3(4.2f, 0.05f, 2.8f), rotation, EchoColor);
        CreateArchiveShelf("ArchiveShelf_Left", -_right * 5.6f + _forward * 2.8f, -1f);
        CreateArchiveShelf("ArchiveShelf_Right", _right * 5.6f + _forward * 2.8f, 1f);
        CreateVaultSupports("VaultSupports", _forward * 1.2f);
        CreateRitualChannel("RitualChannel", _forward * 3.9f);
        CreateCandleStand("CandleStand_Left", -_right * 2.2f + _forward * 2.9f);
        CreateCandleStand("CandleStand_Right", _right * 2.2f + _forward * 2.9f);
        CreateSealCabinet("SealCabinet", -_right * 4.6f - _forward * 1.1f);
        CreateCollapsedArchive("CollapsedArchive", _right * 4.8f - _forward * 1.6f);
        CreateExitPortcullis("ExitPortcullis", _forward * 7.2f);
        PlaceImportedDressing();
        HidePlaceholderVisuals();

        for (int i = 0; i < 5; i++)
        {
            GameObject echo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            echo.name = $"FinalEcho_{i + 1}";
            echo.transform.SetParent(transform, true);
            echo.transform.position = _forward * (2.3f + i * 0.7f) + _right * ((i % 2 == 0 ? -1f : 1f) * 1.4f) + Vector3.up * (0.75f + i * 0.1f);
            echo.transform.localScale = Vector3.one * (0.45f + i * 0.08f);
            Tint(echo.GetComponent<Renderer>(), EchoColor);
        }
    }

    private void BuildFlow()
    {
        QuestObjectiveTarget heartObjective = CreateBoundHeartObjective();
        QuestObjectiveTarget notesObjective = CreateAshTableObjective();
        QuestObjectiveTarget finishObjective = CreateChapterFinish();

        heartObjective.SetNextObjective(notesObjective);
        notesObjective.SetNextObjective(finishObjective);

        if (QuestTracker.Instance != null && string.IsNullOrWhiteSpace(QuestTracker.Instance.CurrentObjectiveId))
        {
            heartObjective.RegisterAsCurrentObjective();
        }
    }

    private QuestObjectiveTarget CreateBoundHeartObjective()
    {
        GameObject heart = GameObject.Find("BoundHeart");
        TestStoneInteractable interactable = heart.AddComponent<TestStoneInteractable>();
        interactable.ConfigureFallbackDialogue(
            "被束缚的心核",
            "调查",
            "那不是村民变成的怪物，而是一团被村长强行留在地下的“回应”。\n\n所有失踪的人都像被它借走了声音，整座村子只是靠拖延才没有彻底崩掉。");

        QuestObjectiveTarget objective = heart.AddComponent<QuestObjectiveTarget>();
        objective.Configure("inspect_bound_heart", "调查地下室中央被束缚的心核", "真相", false, true);
        return objective;
    }

    private QuestObjectiveTarget CreateAshTableObjective()
    {
        GameObject table = GameObject.Find("AshTable");
        TestStoneInteractable interactable = table.AddComponent<TestStoneInteractable>();
        interactable.ConfigureFallbackDialogue(
            "灰烬记录台",
            "查看",
            "桌上只剩一页没烧干净的手记：\n“封住它不是救村，只是给王都争时间。”\n\n赤溪村并不是第一处，也不会是最后一处。");

        QuestObjectiveTarget objective = table.AddComponent<QuestObjectiveTarget>();
        objective.Configure("inspect_ash_table", "查看灰烬记录台上的残页", "记录", false, true);
        return objective;
    }

    private QuestObjectiveTarget CreateChapterFinish()
    {
        GameObject triggerRoot = new GameObject("ChapterFinish");
        triggerRoot.transform.SetParent(transform, true);
        triggerRoot.transform.position = _forward * 6.8f + Vector3.up;
        triggerRoot.transform.rotation = Quaternion.LookRotation(_forward, Vector3.up);

        BoxCollider colliderComponent = triggerRoot.AddComponent<BoxCollider>();
        colliderComponent.size = new Vector3(4.4f, 2.2f, 3f);
        colliderComponent.center = new Vector3(0f, 0.2f, 0f);
        colliderComponent.isTrigger = true;

        TriggerZoneObjective triggerObjective = triggerRoot.AddComponent<TriggerZoneObjective>();
        triggerObjective.Configure("complete_chapter01", false, true, true);

        SceneDialogueTrigger dialogueTrigger = triggerRoot.AddComponent<SceneDialogueTrigger>();
        dialogueTrigger.Configure(
            "伊尔萨恩",
            "chapter01_complete",
            "赤溪村的源头已经找到了，但这只是被拖到地下的一段回应。",
            "第一章竖切片到这里结束。下一步该去查王都为什么在默许这种封存。");
        dialogueTrigger.ConfigureRequirement(
            null,
            "complete_chapter01",
            "伊尔萨恩",
            "先把地下室里留下的真相看完，再决定离开。");

        QuestObjectiveTarget objective = triggerRoot.AddComponent<QuestObjectiveTarget>();
        objective.Configure("complete_chapter01", "确认地下室真相并结束第一章", "终章", false, false);
        return objective;
    }

    private IEnumerator ShowArrivalBeat()
    {
        yield return null;

        if (SimpleDialogueUI.Instance == null || ChapterState.GetFlag("chapter01_end_seen"))
        {
            yield break;
        }

        ChapterState.SetFlag("chapter01_end_seen", true);
        SimpleDialogueUI.Instance.Show(
            "伊尔萨恩",
            "这里不像祭坛，更像一个被强行改成封存点的地下室。",
            "先看中央那团心核，再翻旁边没烧干净的记录。");
    }

    private void CreateArchiveShelf(string name, Vector3 center, float sideSign)
    {
        Quaternion rotation = Quaternion.LookRotation(_right * -sideSign, Vector3.up);
        CreateBlock($"{name}_Frame", center + Vector3.up * 1.7f, new Vector3(2.1f, 3.2f, 0.5f), rotation, new Color(0.24f, 0.2f, 0.16f));
        CreateBlock($"{name}_ShelfA", center + Vector3.up * 0.9f, new Vector3(1.8f, 0.12f, 0.82f), rotation, new Color(0.31f, 0.24f, 0.18f));
        CreateBlock($"{name}_ShelfB", center + Vector3.up * 1.65f, new Vector3(1.8f, 0.12f, 0.82f), rotation, new Color(0.31f, 0.24f, 0.18f));
        CreateBlock($"{name}_ShelfC", center + Vector3.up * 2.4f, new Vector3(1.8f, 0.12f, 0.82f), rotation, new Color(0.31f, 0.24f, 0.18f));
        CreateBlock($"{name}_LedgerA", center + _forward * 0.15f + Vector3.up * 1.02f, new Vector3(0.34f, 0.32f, 0.18f), rotation, AshColor);
        CreateBlock($"{name}_LedgerB", center - _forward * 0.1f + Vector3.up * 1.74f, new Vector3(0.42f, 0.28f, 0.18f), rotation, new Color(0.46f, 0.39f, 0.29f));
    }

    private void CreateWorldMarker(string name, Vector3 localPosition)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(transform, false);
        marker.transform.localPosition = localPosition;
    }

    private void CreateVaultSupports(string name, Vector3 center)
    {
        for (int i = -1; i <= 1; i++)
        {
            float side = i * 4.1f;
            CreateBlock($"{name}_Left_{i + 2}", center - _right * 4.8f + _forward * side * 0.18f + Vector3.up * 2f, new Vector3(0.34f, 4f, 0.34f), Quaternion.identity, WallColor);
            CreateBlock($"{name}_Right_{i + 2}", center + _right * 4.8f + _forward * side * 0.18f + Vector3.up * 2f, new Vector3(0.34f, 4f, 0.34f), Quaternion.identity, WallColor);
        }
    }

    private void CreateRitualChannel(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);
        CreateBlock($"{name}_Main", center + Vector3.up * 0.02f, new Vector3(1.1f, 0.03f, 5.8f), rotation, RelicColor * 0.75f);
        CreateBlock($"{name}_Cross", center + _forward * 1.1f + Vector3.up * 0.02f, new Vector3(4.1f, 0.03f, 0.9f), rotation, RelicColor * 0.65f);
    }

    private void PlaceImportedDressing()
    {
        Quaternion forwardRotation = Quaternion.LookRotation(_forward, Vector3.up);
        Quaternion sideRotation = Quaternion.LookRotation(_right, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/brick-wall",
            "Imported_EndBackWallA",
            transform,
            _forward * 8.55f - _right * 3.4f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 1.14f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/brick-wall",
            "Imported_EndBackWallB",
            transform,
            _forward * 8.55f + _right * 3.4f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 1.14f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/brick-wall",
            "Imported_EndWestWall",
            transform,
            -_right * 6.55f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 1.26f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/brick-wall",
            "Imported_EndEastWall",
            transform,
            _right * 6.55f + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_right, Vector3.up),
            Vector3.one * 1.26f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/stone-wall-column",
            "Imported_EndColumnLeft",
            transform,
            -_right * 6.1f + _forward * 3.8f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/stone-wall-column",
            "Imported_EndColumnRight",
            transform,
            _right * 6.1f + _forward * 3.8f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/ModularDungeon/FBX/gate-metal-bars",
            "Imported_ExitGate",
            transform,
            _forward * 7.12f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 1.05f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Shelf_Arch",
            "Imported_ArchiveShelfLeft",
            transform,
            -_right * 5.3f + _forward * 2.85f + Vector3.up * 0.04f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Shelf_Arch",
            "Imported_ArchiveShelfRight",
            transform,
            _right * 5.3f + _forward * 2.85f + Vector3.up * 0.04f,
            Quaternion.LookRotation(-_right, Vector3.up),
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/CandleStick_Stand",
            "Imported_CandleStandLeft",
            transform,
            -_right * 2.2f + _forward * 2.85f + Vector3.up * 0.04f,
            forwardRotation,
            Vector3.one * 0.85f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/CandleStick_Stand",
            "Imported_CandleStandRight",
            transform,
            _right * 2.2f + _forward * 2.85f + Vector3.up * 0.04f,
            forwardRotation,
            Vector3.one * 0.85f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Cabinet",
            "Imported_SealCabinet",
            transform,
            -_right * 4.55f - _forward * 1.1f + Vector3.up * 0.04f,
            sideRotation,
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Bookcase_2",
            "Imported_CollapsedArchive",
            transform,
            _right * 4.85f - _forward * 1.55f + Vector3.up * 0.04f,
            Quaternion.LookRotation(-_right, Vector3.up),
            Vector3.one * 0.72f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Scroll_1",
            "Imported_RecordScroll",
            transform,
            -_right * 3f + _forward * 1.35f + Vector3.up * 1.42f,
            sideRotation,
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/altar-stone",
            "Imported_HeartAltar",
            transform,
            _forward * 3.62f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.94f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/iron-fence-border",
            "Imported_RelicFenceLeft",
            transform,
            -_right * 3.55f + _forward * 3.95f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_right, Vector3.up),
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/iron-fence-border",
            "Imported_RelicFenceRight",
            transform,
            _right * 3.55f + _forward * 3.95f + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_right, Vector3.up),
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/coffin",
            "Imported_RelicCoffinLeft",
            transform,
            -_right * 4.9f + _forward * 5.2f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.78f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/coffin-old",
            "Imported_RelicCoffinRight",
            transform,
            _right * 5f + _forward * 5.05f + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_forward, Vector3.up),
            Vector3.one * 0.78f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/fire-basket",
            "Imported_RelicBasketLeft",
            transform,
            -_right * 2.9f + _forward * 4.6f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.88f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/fire-basket",
            "Imported_RelicBasketRight",
            transform,
            _right * 2.9f + _forward * 4.6f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.88f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Table_Large",
            "Imported_EndAshTable",
            transform,
            -_right * 3.1f + _forward * 1.4f + Vector3.up * 0.02f,
            sideRotation,
            Vector3.one * 0.88f);
    }

    private void HidePlaceholderVisuals()
    {
        HideRenderers(
            "BackWall",
            "WestWall",
            "EastWall",
            "AshTable",
            "ExitPortcullis");
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

    private void CreateCandleStand(string name, Vector3 center)
    {
        CreateBlock($"{name}_Stem", center + Vector3.up * 0.8f, new Vector3(0.12f, 1.6f, 0.12f), Quaternion.identity, new Color(0.42f, 0.36f, 0.28f));
        CreateBlock($"{name}_Dish", center + Vector3.up * 1.62f, new Vector3(0.52f, 0.08f, 0.52f), Quaternion.identity, new Color(0.32f, 0.28f, 0.24f));
        CreateBlock($"{name}_FlameA", center + Vector3.up * 1.9f - _right * 0.12f, new Vector3(0.1f, 0.28f, 0.1f), Quaternion.identity, new Color(0.82f, 0.44f, 0.16f));
        CreateBlock($"{name}_FlameB", center + Vector3.up * 1.96f + _right * 0.08f, new Vector3(0.08f, 0.22f, 0.08f), Quaternion.identity, new Color(0.88f, 0.58f, 0.2f));
    }

    private void CreateSealCabinet(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_right, Vector3.up);
        CreateBlock($"{name}_Body", center + Vector3.up * 1.5f, new Vector3(1.8f, 3f, 0.9f), rotation, new Color(0.27f, 0.21f, 0.16f));
        CreateBlock($"{name}_Door", center + _forward * 0.12f + Vector3.up * 1.5f, new Vector3(1.55f, 2.7f, 0.08f), rotation, new Color(0.21f, 0.15f, 0.12f));
        CreateBlock($"{name}_SealStrip", center + _forward * 0.16f + Vector3.up * 1.9f, new Vector3(0.14f, 1.9f, 0.04f), rotation, RelicColor);
    }

    private void CreateCollapsedArchive(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_right, Vector3.up);
        CreateBlock($"{name}_Shelf", center + Vector3.up * 0.7f, new Vector3(2.6f, 0.24f, 0.82f), rotation * Quaternion.Euler(0f, 0f, -18f), new Color(0.24f, 0.19f, 0.15f));
        CreateBlock($"{name}_BoxA", center - _forward * 0.3f + Vector3.up * 0.26f, new Vector3(0.7f, 0.52f, 0.6f), rotation, new Color(0.35f, 0.27f, 0.2f));
        CreateBlock($"{name}_BoxB", center + _forward * 0.75f + _right * 0.2f + Vector3.up * 0.18f, new Vector3(0.82f, 0.36f, 0.64f), rotation, new Color(0.33f, 0.25f, 0.18f));
        CreateBlock($"{name}_Scroll", center + _forward * 0.18f + _right * 0.45f + Vector3.up * 0.11f, new Vector3(0.56f, 0.08f, 0.18f), rotation, AshColor);
    }

    private void CreateExitPortcullis(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_forward, Vector3.up);
        CreateBlock($"{name}_Frame", center + Vector3.up * 1.8f, new Vector3(4.8f, 3.6f, 0.24f), rotation, new Color(0.22f, 0.2f, 0.2f));
        for (int i = -2; i <= 2; i++)
        {
            CreateBlock(
                $"{name}_Bar_{i + 3}",
                center + _right * (i * 0.72f) + Vector3.up * 1.7f,
                new Vector3(0.1f, 3.3f, 0.12f),
                rotation,
                new Color(0.41f, 0.42f, 0.45f));
        }
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
