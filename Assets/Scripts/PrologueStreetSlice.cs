using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PrologueStreetSlice : MonoBehaviour
{
    private const string RootName = "__PrologueStreetSlice";
    private const int LayoutVersion = 23;
    private static readonly bool UseImported3DStreetSet = false;
    private const float StreetLength = 44f;
    private const float StreetHalfWidth = 3.9f;
    private const float WalkwayOffset = 6.5f;
    private const float BuildingRowOffset = 15.2f;
    private const float SegmentSpacing = 6.2f;
    private const float GateDistance = 23f;
    private const float FogZoneDistance = 31f;
    private const float PlayerSpawnDistance = 13f;

    private static readonly Color StreetColor = new Color(0.16f, 0.17f, 0.19f);
    private static readonly Color SidewalkColor = new Color(0.22f, 0.2f, 0.18f);
    private static readonly Color BuildingColor = new Color(0.18f, 0.16f, 0.15f);
    private static readonly Color LanternColor = new Color(0.92f, 0.68f, 0.34f);
    private static readonly Color FogColor = new Color(0.32f, 0.08f, 0.08f);
    private static readonly Color FogCoreColor = new Color(0.08f, 0.02f, 0.03f);
    private static readonly Color CorruptionColor = new Color(0.24f, 0.04f, 0.05f);
    private static readonly Color MarkerStoneColor = new Color(0.38f, 0.32f, 0.3f);
    private static readonly Color FogBeaconColor = new Color(0.74f, 0.12f, 0.1f);
    private static readonly Color BarrierColor = new Color(0.29f, 0.08f, 0.07f);

    private PlayerMover _player;
    private Vector3 _streetForward;
    private Vector3 _streetRight;
    private Vector3 _streetCenter;
    private float _groundY;
    [SerializeField] private int _layoutVersion;

    public static void Ensure(PlayerMover player)
    {
        if (player == null || SceneManager.GetActiveScene().name != SceneLoader.MainSceneName)
        {
            return;
        }

        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            PrologueStreetSlice existingSlice = existingRoot.GetComponent<PrologueStreetSlice>();
            if (existingSlice != null && existingSlice.IsLayoutCurrent())
            {
                existingSlice.BindRuntimeState(player);
                return;
            }

            if (Application.isPlaying)
            {
                existingRoot.name = $"{RootName}_Legacy";
                Destroy(existingRoot);
            }
            else
            {
                DestroyImmediate(existingRoot);
            }
        }

        GameObject root = new GameObject(RootName);
        PrologueStreetSlice slice = root.AddComponent<PrologueStreetSlice>();
        slice.Build(player);
    }

    private void BindRuntimeState(PlayerMover player)
    {
        _player = player;
        _streetForward = GetStreetForward();
        _streetRight = Vector3.Cross(Vector3.up, _streetForward).normalized;
        _groundY = PrepareSceneGround();
        _streetCenter = new Vector3(0f, _groundY, 0f);
        PositionPlayer();

        if (QuestTracker.Instance != null && string.IsNullOrWhiteSpace(QuestTracker.Instance.CurrentObjectiveId))
        {
            QuestObjectiveTarget[] objectives = GetComponentsInChildren<QuestObjectiveTarget>(true);
            foreach (QuestObjectiveTarget objective in objectives)
            {
                if (objective != null && objective.ObjectiveId == "talk_watchman_aren")
                {
                    objective.RegisterAsCurrentObjective();
                    break;
                }
            }
        }
    }

    private void Build(PlayerMover player)
    {
        _player = player;
        _streetForward = GetStreetForward();
        _streetRight = Vector3.Cross(Vector3.up, _streetForward).normalized;
        _groundY = PrepareSceneGround();
        _streetCenter = new Vector3(0f, _groundY, 0f);
        _layoutVersion = LayoutVersion;

        PositionPlayer();
        BuildStreetShell();
        BuildLandmarks();
        BuildObjectiveChain();
        if (Application.isPlaying)
        {
            StartCoroutine(ShowOpeningBeat());
        }
    }

    private Vector3 GetStreetForward()
    {
        Vector3 forward = Camera.main != null
            ? Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up)
            : Vector3.right;

        if (forward.sqrMagnitude <= 0.001f)
        {
            forward = Vector3.right;
        }

        forward = forward.normalized;
        if (Vector3.Dot(forward, Vector3.right) < 0f)
        {
            forward = -forward;
        }

        return forward;
    }

    private bool IsLayoutCurrent()
    {
        return _layoutVersion == LayoutVersion
            && transform.childCount > 0
            && transform.Find("Boulevard") != null
            && transform.Find("NorthGate") != null;
    }

    private float PrepareSceneGround()
    {
        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        Collider bestGround = null;
        float bestArea = 0f;

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
            if (bounds.center.y > 1.5f || bounds.size.x < 4f || bounds.size.z < 4f)
            {
                continue;
            }

            float area = bounds.size.x * bounds.size.z;
            if (area <= bestArea)
            {
                continue;
            }

            bestArea = area;
            bestGround = collider;
        }

        if (bestGround == null)
        {
            return 0f;
        }

        Transform groundTransform = bestGround.transform;
        if (bestGround.name == "Ground" && groundTransform.localScale.x < 5.5f && groundTransform.localScale.z < 5.5f)
        {
            groundTransform.position = new Vector3(0f, groundTransform.position.y, 0f);
            groundTransform.localScale = new Vector3(6f, groundTransform.localScale.y, 6f);
        }

        Renderer groundRenderer = bestGround.GetComponent<Renderer>();
        if (groundRenderer != null)
        {
            Tint(groundRenderer, new Color(0.12f, 0.11f, 0.12f));
        }

        return bestGround.bounds.max.y;
    }

    private void PositionPlayer()
    {
        Vector3 spawnPosition = _streetCenter - _streetForward * PlayerSpawnDistance + Vector3.up;
        _player.transform.position = spawnPosition;
        _player.transform.rotation = Quaternion.LookRotation(_streetForward, Vector3.up);

        CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(_player.transform, true);
        }
    }

    private void BuildStreetShell()
    {
        Quaternion streetRotation = Quaternion.LookRotation(_streetForward, Vector3.up);

        GameObject boulevard = CreateBlock("Boulevard", _streetCenter, new Vector3(StreetHalfWidth * 2f, 0.22f, StreetLength), streetRotation, StreetColor);
        boulevard.transform.SetParent(transform, true);

        GameObject leftCurb = CreateBlock("LeftCurb", _streetCenter - _streetRight * (StreetHalfWidth + 0.32f) + Vector3.up * 0.08f, new Vector3(0.45f, 0.18f, StreetLength), streetRotation, new Color(0.28f, 0.25f, 0.24f));
        leftCurb.transform.SetParent(transform, true);

        GameObject rightCurb = CreateBlock("RightCurb", _streetCenter + _streetRight * (StreetHalfWidth + 0.32f) + Vector3.up * 0.08f, new Vector3(0.45f, 0.18f, StreetLength), streetRotation, new Color(0.28f, 0.25f, 0.24f));
        rightCurb.transform.SetParent(transform, true);

        GameObject leftWalkway = CreateBlock("LeftWalkway", _streetCenter - _streetRight * WalkwayOffset + Vector3.up * 0.06f, new Vector3(4f, 0.12f, StreetLength), streetRotation, SidewalkColor);
        leftWalkway.transform.SetParent(transform, true);

        GameObject rightWalkway = CreateBlock("RightWalkway", _streetCenter + _streetRight * WalkwayOffset + Vector3.up * 0.06f, new Vector3(4f, 0.12f, StreetLength), streetRotation, SidewalkColor);
        rightWalkway.transform.SetParent(transform, true);

        GameObject westServiceLane = CreateBlock("WestServiceLane", _streetCenter - _streetRight * (WalkwayOffset + 4.4f) + Vector3.up * 0.03f, new Vector3(4.8f, 0.08f, StreetLength), streetRotation, new Color(0.18f, 0.16f, 0.16f));
        westServiceLane.transform.SetParent(transform, true);

        GameObject eastServiceLane = CreateBlock("EastServiceLane", _streetCenter + _streetRight * (WalkwayOffset + 4.4f) + Vector3.up * 0.03f, new Vector3(4.8f, 0.08f, StreetLength), streetRotation, new Color(0.18f, 0.16f, 0.16f));
        eastServiceLane.transform.SetParent(transform, true);

        CreateDrainageChannel("WestDrain", _streetCenter - _streetRight * 2.9f, streetRotation);
        CreateDrainageChannel("EastDrain", _streetCenter + _streetRight * 2.9f, streetRotation);
        CreateBacklotWall("WestBacklotWall", _streetCenter - _streetRight * (BuildingRowOffset + 5.6f), streetRotation);
        CreateBacklotWall("EastBacklotWall", _streetCenter + _streetRight * (BuildingRowOffset + 5.6f), streetRotation);

        for (int i = -3; i <= 3; i++)
        {
            float segment = i * SegmentSpacing;
            CreateBuildingCluster($"WestBlock_{i + 4}", _streetCenter - _streetRight * BuildingRowOffset + _streetForward * segment, streetRotation);
            CreateBuildingCluster($"EastBlock_{i + 4}", _streetCenter + _streetRight * BuildingRowOffset + _streetForward * segment, streetRotation);
        }

        CreateCrossStreet("CrossStreet_Mid", _streetCenter + _streetForward * 5.8f, streetRotation);
        CreateCrossStreet("CrossStreet_Gate", _streetCenter + _streetForward * 17.8f, streetRotation);
        CreateArchway(_streetCenter + _streetForward * (GateDistance + 1.6f), streetRotation);
        CreateStreetBarricade("StreetBarricade_Left", _streetCenter - _streetRight * 4.6f + _streetForward * 18.8f, streetRotation);
        CreateStreetBarricade("StreetBarricade_Right", _streetCenter + _streetRight * 4.6f + _streetForward * 20.2f, streetRotation);
        CreateFogMass(_streetCenter + _streetForward * FogZoneDistance, streetRotation);
    }

    private void BuildLandmarks()
    {
        CreateStartThreshold(_streetCenter - _streetForward * 12.4f);
        CreateLantern("Lantern_StartLeft", _streetCenter - _streetRight * 4.8f - _streetForward * 9.2f);
        CreateLantern("Lantern_StartRight", _streetCenter + _streetRight * 4.8f - _streetForward * 8.4f);
        CreateLantern("Lantern_MidLeft", _streetCenter - _streetRight * 5.2f + _streetForward * 2.8f);
        CreateLantern("Lantern_MidRight", _streetCenter + _streetRight * 5f + _streetForward * 8.6f);
        CreateLantern("Lantern_GateLeft", _streetCenter - _streetRight * 5.1f + _streetForward * 19.2f);
        CreateLantern("Lantern_GateRight", _streetCenter + _streetRight * 5.1f + _streetForward * 22.8f);
        CreateCanopy("WatchReliefCanopy", _streetCenter - _streetRight * 9.4f - _streetForward * 2.6f, streetFacing: _streetForward);
        CreateAbandonedCart("AbandonedCart", _streetCenter + _streetRight * 2.8f + _streetForward * 11.2f);
        CreateWatchCheckpoint(_streetCenter - _streetForward * 7.2f);
        CreateNoticeSquare(_streetCenter + _streetForward * 7.2f);
        CreateCrestIncident(_streetCenter + _streetForward * 14.8f);
        CreateGateForecourt(_streetCenter + _streetForward * GateDistance);
        CreateMainlineTrail();
        CreateDistrictBands();
        CreateStreetLandmarkHeights();
        CreateOverheadCrossings();
        CreateSpecificBuildLots();
        CreateEdictLine();
        CreateDangerTransition(_streetCenter + _streetForward * 26.8f);
        CreateDistrictStoryAnchors();
        CreateObjectiveReadabilityPass();
        CreatePauseSpaces();
        CreateArrivalCourt(_streetCenter - _streetForward * 9.8f);
        CreateGatePressureLane(_streetCenter + _streetForward * 20.8f);
        CreateSupplyCheckpointRuin(_streetCenter + _streetForward * 8.8f);
        CreateQuarantineCorridor(_streetCenter + _streetForward * 24.8f);
        CreateFogApproachFunnels(_streetCenter + _streetForward * 27.8f);
        CreateFuneraryEscortCart(_streetCenter + _streetRight * 8.8f + _streetForward * 18.6f);
        CreateWardingLine(_streetCenter + _streetForward * 24.6f);
        CreateSideAlleyPocket("WestAlleyPocket", _streetCenter - _streetRight * 11.8f + _streetForward * 4.8f, -1f, true);
        CreateSideAlleyPocket("EastAlleyPocket", _streetCenter + _streetRight * 11.8f + _streetForward * 17.2f, 1f, false);
        CreateForegroundFrame();
        CreateSkylineSilhouettes();
        if (UseImported3DStreetSet)
        {
            PlaceImportedDressing();
        }
        else
        {
            PlacePixelFacadeDressing();
        }

        HidePlaceholderVisuals();
    }

    private void BuildObjectiveChain()
    {
        QuestObjectiveTarget npcObjective = CreateWatchman();
        QuestObjectiveTarget steleObjective = CreateWarningStele();
        QuestObjectiveTarget crestObjective = CreateFallenCrest();
        QuestObjectiveTarget gateObjective = CreateNorthGate();
        QuestObjectiveTarget anomalyObjective = CreateAnomalyZone();

        npcObjective.SetNextObjective(steleObjective);
        steleObjective.SetNextObjective(crestObjective);
        crestObjective.SetNextObjective(gateObjective);
        gateObjective.SetNextObjective(anomalyObjective);

        if (QuestTracker.Instance != null && string.IsNullOrWhiteSpace(QuestTracker.Instance.CurrentObjectiveId))
        {
            npcObjective.RegisterAsCurrentObjective();
        }
    }

    private QuestObjectiveTarget CreateWatchman()
    {
        Vector3 position = _streetCenter - _streetForward * 6.2f + _streetRight * 2.3f + Vector3.up;
        GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = "WatchmanAren";
        npc.transform.SetParent(transform, true);
        npc.transform.position = position;
        npc.transform.rotation = Quaternion.LookRotation(-_streetForward, Vector3.up);
        Tint(npc.GetComponent<Renderer>(), new Color(0.55f, 0.58f, 0.64f));

        TestNpcInteractable interactable = npc.AddComponent<TestNpcInteractable>();
        interactable.ConfigurePresentation("守夜人阿伦", "交谈");
        interactable.ConfigureFallbackDialogue(
            "伊尔萨恩，你终于来了。东街今夜封锁，但黑雾不是从城门外进来的。",
            "去看一眼告示碑，再把路边那枚家徽捡起来。丢徽记的人，半刻钟前刚被拖进雾里。",
            "如果你还要往前，就先把木门打开。我会在这里替你盯住后路。");

        QuestObjectiveTarget objective = npc.AddComponent<QuestObjectiveTarget>();
        objective.Configure("talk_watchman_aren", "与守夜人阿伦交谈", "引导", false, true);
        return objective;
    }

    private QuestObjectiveTarget CreateWarningStele()
    {
        Vector3 position = _streetCenter + _streetForward * 7.2f - _streetRight * 2.4f + new Vector3(0f, 1f, 0f);
        GameObject stele = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stele.name = "WarningStele";
        stele.transform.SetParent(transform, true);
        stele.transform.position = position;
        stele.transform.localScale = new Vector3(0.8f, 1.8f, 0.45f);
        stele.transform.rotation = Quaternion.LookRotation(_streetRight, Vector3.up);
        Tint(stele.GetComponent<Renderer>(), new Color(0.46f, 0.46f, 0.5f));

        TestStoneInteractable interactable = stele.AddComponent<TestStoneInteractable>();
        interactable.ConfigureFallbackDialogue(
            "封街告示碑",
            "查看",
            "告示被人用匕首划开了一角：\n\n“东街第三段出现失踪与异声。若发现家徽、血迹或陌生祷词，立即回报守夜队。”");

        QuestObjectiveTarget objective = stele.AddComponent<QuestObjectiveTarget>();
        objective.Configure("inspect_warning_stele", "查看封街告示碑", "线索", false, true);
        return objective;
    }

    private QuestObjectiveTarget CreateFallenCrest()
    {
        Vector3 position = _streetCenter + _streetForward * 14.8f + _streetRight * 1.6f + new Vector3(0f, 0.72f, 0f);
        GameObject crest = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crest.name = "FallenCrest";
        crest.transform.SetParent(transform, true);
        crest.transform.position = position;
        crest.transform.localScale = Vector3.one * 0.82f;
        Tint(crest.GetComponent<Renderer>(), new Color(0.78f, 0.66f, 0.42f));

        PickupInteractable interactable = crest.AddComponent<PickupInteractable>();
        interactable.Configure("fallen_house_crest", "失落家徽", "拾起家徽", false);

        QuestObjectiveTarget objective = crest.AddComponent<QuestObjectiveTarget>();
        objective.Configure("pickup_fallen_crest", "拾取失落家徽", "证物", false, true);
        return objective;
    }

    private QuestObjectiveTarget CreateNorthGate()
    {
        Vector3 gateCenter = _streetCenter + _streetForward * GateDistance;

        GameObject doorRoot = new GameObject("NorthGate");
        doorRoot.transform.SetParent(transform, true);
        doorRoot.transform.position = gateCenter;
        doorRoot.transform.rotation = Quaternion.LookRotation(_streetForward, Vector3.up);

        GameObject leftFrame = CreateBlock("GateFrameLeft", gateCenter - _streetRight * 1.16f + Vector3.up * 1.35f, new Vector3(0.28f, 2.7f, 0.36f), doorRoot.transform.rotation, new Color(0.21f, 0.15f, 0.12f));
        GameObject rightFrame = CreateBlock("GateFrameRight", gateCenter + _streetRight * 1.16f + Vector3.up * 1.35f, new Vector3(0.28f, 2.7f, 0.36f), doorRoot.transform.rotation, new Color(0.21f, 0.15f, 0.12f));
        GameObject topFrame = CreateBlock("GateFrameTop", gateCenter + Vector3.up * 2.66f, new Vector3(2.6f, 0.2f, 0.36f), doorRoot.transform.rotation, new Color(0.21f, 0.15f, 0.12f));
        leftFrame.transform.SetParent(doorRoot.transform, true);
        rightFrame.transform.SetParent(doorRoot.transform, true);
        topFrame.transform.SetParent(doorRoot.transform, true);

        GameObject hinge = new GameObject("GateHinge");
        hinge.transform.SetParent(doorRoot.transform, false);
        hinge.transform.localPosition = new Vector3(-0.95f, 1.24f, 0f);
        hinge.transform.localRotation = Quaternion.identity;

        GameObject doorVisual = CreateBlock("GateLeaf", hinge.transform.position, new Vector3(1.9f, 2.48f, 0.2f), doorRoot.transform.rotation, new Color(0.32f, 0.19f, 0.12f));
        doorVisual.transform.SetParent(hinge.transform, false);
        doorVisual.transform.localPosition = new Vector3(0.95f, 0f, 0f);
        doorVisual.transform.localRotation = Quaternion.identity;
        if (doorVisual.TryGetComponent(out Renderer doorRenderer))
        {
            doorRenderer.enabled = false;
        }
        if (leftFrame.TryGetComponent(out Renderer leftFrameRenderer))
        {
            leftFrameRenderer.enabled = false;
        }
        if (rightFrame.TryGetComponent(out Renderer rightFrameRenderer))
        {
            rightFrameRenderer.enabled = false;
        }
        if (topFrame.TryGetComponent(out Renderer topFrameRenderer))
        {
            topFrameRenderer.enabled = false;
        }

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Door_8_Round",
            "Imported_GateLeaf",
            hinge.transform,
            hinge.transform.position + doorRoot.transform.right * 0.92f + Vector3.up * 0.02f,
            doorRoot.transform.rotation,
            Vector3.one * 1.08f);

        DoorInteractable interactable = doorRoot.AddComponent<DoorInteractable>();
        interactable.ConfigurePresentation("北段木门", "推门");
        interactable.SetDoorVisual(hinge.transform);
        interactable.ConfigureMotion(new Vector3(0f, 96f, 0f), true);

        BoxCollider colliderComponent = doorRoot.AddComponent<BoxCollider>();
        colliderComponent.center = new Vector3(0f, 1f, 0f);
        colliderComponent.size = new Vector3(3.1f, 2.9f, 2.4f);

        QuestObjectiveTarget objective = doorRoot.AddComponent<QuestObjectiveTarget>();
        objective.Configure("open_north_gate", "打开通往雾区的木门", "通路", false, true);
        return objective;
    }

    private QuestObjectiveTarget CreateAnomalyZone()
    {
        Vector3 position = _streetCenter + _streetForward * (FogZoneDistance - 0.7f) + Vector3.up;
        GameObject triggerRoot = new GameObject("AnomalyThreshold");
        triggerRoot.transform.SetParent(transform, true);
        triggerRoot.transform.position = position;
        triggerRoot.transform.rotation = Quaternion.LookRotation(_streetForward, Vector3.up);

        BoxCollider colliderComponent = triggerRoot.AddComponent<BoxCollider>();
        colliderComponent.size = new Vector3(8.4f, 3f, 6.8f);
        colliderComponent.center = new Vector3(0f, 0.6f, 0f);
        colliderComponent.isTrigger = true;

        TriggerZoneObjective objectiveTrigger = triggerRoot.AddComponent<TriggerZoneObjective>();
        objectiveTrigger.Configure("enter_anomaly_threshold", false, true, true);

        SceneDialogueTrigger dialogueTrigger = triggerRoot.AddComponent<SceneDialogueTrigger>();
        dialogueTrigger.Configure(
            "异常黑雾",
            "prologue_first_anomaly_seen",
            "黑雾并没有贴着地面移动，而是在半空里缓慢收缩，像一颗正在跳动的心脏。",
            "巷口深处那道扭曲的人影只停了一瞬，随即退进更深的黑里。");
        dialogueTrigger.ConfigureSceneTransition(SceneLoader.PrologueEventRoomSceneName, 0.45f);

        QuestObjectiveTarget objective = triggerRoot.AddComponent<QuestObjectiveTarget>();
        objective.Configure("enter_anomaly_threshold", "靠近前方异常黑雾", "异象", false, false);
        return objective;
    }

    private IEnumerator ShowOpeningBeat()
    {
        yield return null;

        if (SimpleDialogueUI.Instance == null || ChapterState.GetFlag("prologue_opening_seen"))
        {
            yield break;
        }

        ChapterState.SetFlag("prologue_opening_seen", true);
        SimpleDialogueUI.Instance.Show(
            "伊尔萨恩",
            "王都东街被提前封了。可地上的车辙还没干，说明人是刚被带走的。",
            "先和守夜人确认情况，再决定要不要继续往雾里走。");
    }

    private void CreateBuildingCluster(string name, Vector3 center, Quaternion rotation)
    {
        float sideSign = Vector3.Dot(center - _streetCenter, _streetRight) >= 0f ? 1f : -1f;
        float forwardAmount = Vector3.Dot(center - _streetCenter, _streetForward);
        float normalized = Mathf.InverseLerp(-16f, 16f, forwardAmount);
        float width = Mathf.Lerp(4.6f, 6.8f, normalized);
        float height = Mathf.Lerp(4.4f, 6.4f, 1f - Mathf.Abs(normalized - 0.5f) * 1.3f);
        float depth = Mathf.Lerp(6.6f, 9.4f, Mathf.Abs(normalized - 0.5f) * 1.1f);
        bool isShopfront = forwardAmount < 4f;
        bool isResidence = forwardAmount >= 4f && forwardAmount < 12f;

        Color shellColor = isShopfront
            ? new Color(0.21f, 0.17f, 0.15f)
            : isResidence
                ? new Color(0.18f, 0.16f, 0.16f)
                : new Color(0.15f, 0.13f, 0.14f);
        Color roofColor = isShopfront
            ? new Color(0.16f, 0.11f, 0.1f)
            : isResidence
                ? new Color(0.12f, 0.1f, 0.11f)
                : new Color(0.1f, 0.08f, 0.09f);

        GameObject shell = CreateBlock(name, center + Vector3.up * (height * 0.5f), new Vector3(width, height, depth), rotation, shellColor);
        shell.transform.SetParent(transform, true);

        GameObject roofCap = CreateBlock($"{name}_RoofCap", center + Vector3.up * (height + 0.25f), new Vector3(width + 0.45f, 0.35f, depth + 0.38f), rotation, roofColor);
        roofCap.transform.SetParent(transform, true);

        GameObject stoop = CreateBlock($"{name}_Stoop", center + _streetRight * sideSign * 2.9f + Vector3.up * 0.22f, new Vector3(1.3f, 0.44f, 1.8f), rotation, new Color(0.26f, 0.23f, 0.2f));
        stoop.transform.SetParent(transform, true);

        CreateFacadeDetails(name, center, sideSign, width, height, depth);
        CreateRoofscape(name, center, rotation, width, height, depth, forwardAmount, sideSign);

        Vector3 frontCenter = center + _streetRight * sideSign * (depth * 0.5f + 0.5f);
        Quaternion facadeRotation = Quaternion.LookRotation(-_streetRight * sideSign, Vector3.up);

        if (isShopfront)
        {
            CreateShopfrontSet(name, frontCenter, sideSign, facadeRotation);
        }
        else if (isResidence)
        {
            CreateResidenceSet(name, frontCenter, sideSign, facadeRotation);
        }
        else
        {
            CreateQuarantineFrontageSet(name, frontCenter, sideSign, facadeRotation);
        }
    }

    private void CreateArchway(Vector3 center, Quaternion rotation)
    {
        GameObject leftPillar = CreateBlock("GatePillarLeft", center - _streetRight * 2.6f + Vector3.up * 2f, new Vector3(0.82f, 4f, 1f), rotation, BuildingColor);
        GameObject rightPillar = CreateBlock("GatePillarRight", center + _streetRight * 2.6f + Vector3.up * 2f, new Vector3(0.82f, 4f, 1f), rotation, BuildingColor);
        GameObject lintel = CreateBlock("GateLintel", center + Vector3.up * 4.05f, new Vector3(6.1f, 0.6f, 1f), rotation, BuildingColor);
        leftPillar.transform.SetParent(transform, true);
        rightPillar.transform.SetParent(transform, true);
        lintel.transform.SetParent(transform, true);
    }

    private void CreateLantern(string name, Vector3 position)
    {
        GameObject post = CreateBlock($"{name}_Post", position + Vector3.up * 1.2f, new Vector3(0.18f, 2.4f, 0.18f), Quaternion.identity, new Color(0.28f, 0.22f, 0.16f));
        GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lamp.name = $"{name}_Lamp";
        lamp.transform.SetParent(transform, true);
        lamp.transform.position = position + Vector3.up * 2.45f;
        lamp.transform.localScale = Vector3.one * 0.34f;
        Tint(lamp.GetComponent<Renderer>(), LanternColor);
        post.transform.SetParent(transform, true);
    }

    private void CreateFogMass(Vector3 center, Quaternion rotation)
    {
        GameObject frontSpill = CreateBlock("FogFrontSpill", center - _streetForward * 2.6f + Vector3.up * 0.02f, new Vector3(11.6f, 0.05f, 4.2f), rotation, FogColor);
        frontSpill.transform.SetParent(transform, true);

        GameObject stain = CreateBlock("FogStain", center + Vector3.up * 0.03f, new Vector3(13.2f, 0.08f, 11.4f), rotation, CorruptionColor);
        stain.transform.SetParent(transform, true);

        GameObject leftMarker = CreateBlock("FogMarkerLeft", center - _streetRight * 4.9f + Vector3.up * 1.8f, new Vector3(0.82f, 3.6f, 0.82f), rotation, MarkerStoneColor);
        GameObject rightMarker = CreateBlock("FogMarkerRight", center + _streetRight * 4.9f + Vector3.up * 1.8f, new Vector3(0.82f, 3.6f, 0.82f), rotation, MarkerStoneColor);
        leftMarker.transform.SetParent(transform, true);
        rightMarker.transform.SetParent(transform, true);
        CreateFogBeacon("FogBeaconLeft", center - _streetRight * 4.9f - _streetForward * 0.8f);
        CreateFogBeacon("FogBeaconRight", center + _streetRight * 4.9f - _streetForward * 0.8f);

        GameObject warningBar = CreateBlock("FogWarningBar", center - _streetForward * 2.75f + Vector3.up * 1.15f, new Vector3(10.8f, 0.3f, 0.24f), rotation, BarrierColor);
        warningBar.transform.SetParent(transform, true);

        GameObject lowVeil = CreateBlock("FogLowVeil", center - _streetForward * 0.7f + Vector3.up * 1.25f, new Vector3(10.4f, 2f, 1.6f), rotation, FogColor);
        lowVeil.transform.SetParent(transform, true);

        for (int row = 0; row < 5; row++)
        {
            float depth = row * 1.35f;

            for (int lane = -3; lane <= 3; lane++)
            {
                Vector3 fogPosition = center + _streetForward * depth + _streetRight * lane * 1.4f + Vector3.up * (1.15f + row * 0.32f);
                float scale = 1.45f + row * 0.26f + Mathf.Abs(lane) * 0.09f;
                CreateFogBlob($"FogBlob_{row + 1}_{lane + 4}", fogPosition, scale, row >= 2 ? FogCoreColor : FogColor);
            }
        }

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "FogHeart";
        core.transform.SetParent(transform, true);
        core.transform.position = center + _streetForward * 1.8f + Vector3.up * 3.4f;
        core.transform.localScale = new Vector3(3.4f, 2.8f, 3.4f);
        Tint(core.GetComponent<Renderer>(), FogCoreColor);

        GameObject topVeil = CreateBlock("FogVeil", center + _streetForward * 0.8f + Vector3.up * 3.1f, new Vector3(10.4f, 3.2f, 0.82f), rotation, FogCoreColor);
        topVeil.transform.SetParent(transform, true);

        GameObject backVeil = CreateBlock("FogBackVeil", center + _streetForward * 2.7f + Vector3.up * 2.9f, new Vector3(8.4f, 2.7f, 0.68f), rotation, FogCoreColor);
        backVeil.transform.SetParent(transform, true);
    }

    private void CreateCrossStreet(string name, Vector3 center, Quaternion rotation)
    {
        GameObject cross = CreateBlock(name, center + Vector3.up * 0.01f, new Vector3(BuildingRowOffset * 2f - 4f, 0.04f, 3.2f), rotation * Quaternion.Euler(0f, 90f, 0f), new Color(0.18f, 0.17f, 0.18f));
        cross.transform.SetParent(transform, true);
    }

    private void CreateBacklotWall(string name, Vector3 center, Quaternion rotation)
    {
        CreateBlock($"{name}_Base", center + Vector3.up * 1.4f, new Vector3(0.44f, 2.8f, StreetLength + 4f), rotation, new Color(0.14f, 0.13f, 0.14f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Cap", center + Vector3.up * 2.92f, new Vector3(0.68f, 0.22f, StreetLength + 4.2f), rotation, new Color(0.19f, 0.17f, 0.18f)).transform.SetParent(transform, true);
    }

    private void CreateDrainageChannel(string name, Vector3 center, Quaternion rotation)
    {
        CreateBlock($"{name}_Cut", center + Vector3.up * 0.01f, new Vector3(0.38f, 0.04f, StreetLength), rotation, new Color(0.1f, 0.1f, 0.11f)).transform.SetParent(transform, true);
        for (int i = -4; i <= 4; i++)
        {
            Vector3 grateCenter = center + _streetForward * (i * 4.6f) + Vector3.up * 0.04f;
            CreateBlock($"{name}_Grate_{i + 5}", grateCenter, new Vector3(0.44f, 0.03f, 0.62f), rotation, new Color(0.24f, 0.24f, 0.26f)).transform.SetParent(transform, true);
        }
    }

    private void CreateStartThreshold(Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("StartThresholdLintel", center + Vector3.up * 3.8f, new Vector3(10.8f, 0.24f, 0.4f), facing, new Color(0.22f, 0.18f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock("StartThresholdPostLeft", center - _streetRight * 5.1f + Vector3.up * 2f, new Vector3(0.36f, 4f, 0.36f), facing, new Color(0.24f, 0.19f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock("StartThresholdPostRight", center + _streetRight * 5.1f + Vector3.up * 2f, new Vector3(0.36f, 4f, 0.36f), facing, new Color(0.24f, 0.19f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock("StartThresholdBanner", center + Vector3.up * 3.05f, new Vector3(0.12f, 1.7f, 4.4f), facing * Quaternion.Euler(0f, 90f, 0f), new Color(0.28f, 0.1f, 0.08f)).transform.SetParent(transform, true);
    }

    private void CreateWatchCheckpoint(Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("WatchTentDeck", center - _streetRight * 9.2f + Vector3.up * 0.16f, new Vector3(3.2f, 0.32f, 4.2f), facing, new Color(0.26f, 0.22f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock("WatchTentRoof", center - _streetRight * 9.2f + Vector3.up * 2.1f, new Vector3(3.8f, 0.26f, 4.7f), facing, new Color(0.2f, 0.19f, 0.22f)).transform.SetParent(transform, true);
        CreateBlock("WatchBarrierA", center - _streetRight * 2.6f - _streetForward * 0.85f + Vector3.up * 0.55f, new Vector3(1.8f, 1.1f, 0.46f), facing, new Color(0.35f, 0.24f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock("WatchBarrierB", center + _streetRight * 2.2f - _streetForward * 1.3f + Vector3.up * 0.55f, new Vector3(1.8f, 1.1f, 0.46f), facing, new Color(0.35f, 0.24f, 0.15f)).transform.SetParent(transform, true);
        CreateBarrelStack("WatchBarrels", center - _streetRight * 5.3f - _streetForward * 1.5f);
        CreateBrazier("WatchBrazier", center + _streetRight * 5.4f - _streetForward * 0.8f, 1f);
        CreateBlock("WatchMapTable", center - _streetRight * 6.8f + _streetForward * 1.2f + Vector3.up * 0.62f, new Vector3(1.6f, 0.18f, 1.1f), facing, new Color(0.35f, 0.25f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock("WatchSpearRack", center + _streetRight * 7.1f + _streetForward * 1.3f + Vector3.up * 1.05f, new Vector3(0.2f, 2.1f, 1.8f), facing, new Color(0.28f, 0.21f, 0.15f)).transform.SetParent(transform, true);
    }

    private void CreateNoticeSquare(Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("NoticePlaza", center + Vector3.up * 0.05f, new Vector3(10.4f, 0.08f, 5.8f), facing, new Color(0.2f, 0.19f, 0.19f)).transform.SetParent(transform, true);
        CreateBlock("NoticeBenchLeft", center - _streetRight * 4.1f + _streetForward * 1.2f + Vector3.up * 0.42f, new Vector3(2.4f, 0.24f, 0.55f), Quaternion.LookRotation(_streetRight, Vector3.up), new Color(0.33f, 0.24f, 0.17f)).transform.SetParent(transform, true);
        CreateBlock("NoticeBenchRight", center + _streetRight * 4.1f + _streetForward * 0.8f + Vector3.up * 0.42f, new Vector3(2.4f, 0.24f, 0.55f), Quaternion.LookRotation(-_streetRight, Vector3.up), new Color(0.33f, 0.24f, 0.17f)).transform.SetParent(transform, true);
        CreatePaperCluster("NoticePapers", center - _streetRight * 0.9f - _streetForward * 0.9f);
    }

    private void CreateCrestIncident(Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("IncidentStain", center + _streetRight * 0.85f + Vector3.up * 0.03f, new Vector3(2.2f, 0.04f, 6.6f), facing, new Color(0.18f, 0.04f, 0.05f)).transform.SetParent(transform, true);
        CreateBlock("IncidentBundleA", center - _streetRight * 2.8f + _streetForward * 0.7f + Vector3.up * 0.38f, new Vector3(1.2f, 0.76f, 1.1f), facing, new Color(0.31f, 0.28f, 0.22f)).transform.SetParent(transform, true);
        CreateBlock("IncidentBundleB", center - _streetRight * 3.6f - _streetForward * 0.4f + Vector3.up * 0.32f, new Vector3(0.9f, 0.64f, 0.9f), facing, new Color(0.34f, 0.27f, 0.19f)).transform.SetParent(transform, true);
        CreateBlock("IncidentLampPost", center + _streetRight * 5.4f + Vector3.up * 1.55f, new Vector3(0.2f, 3.1f, 0.2f), Quaternion.identity, new Color(0.27f, 0.21f, 0.16f)).transform.SetParent(transform, true);
        CreateBrokenCrate("IncidentCrate", center + _streetRight * 3.9f + _streetForward * 1.2f);
        CreateBlock("IncidentDragMark", center + _streetRight * 0.9f + _streetForward * 3.6f + Vector3.up * 0.03f, new Vector3(0.9f, 0.03f, 5.8f), facing, new Color(0.14f, 0.03f, 0.04f)).transform.SetParent(transform, true);
    }

    private void CreateGateForecourt(Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("GatePlaza", center + _streetForward * 1.4f + Vector3.up * 0.05f, new Vector3(11.6f, 0.08f, 7.6f), facing, new Color(0.19f, 0.18f, 0.19f)).transform.SetParent(transform, true);
        CreateStreetBarricade("ForecourtBarricadeLeft", center - _streetRight * 5.6f + _streetForward * 2.5f, facing);
        CreateStreetBarricade("ForecourtBarricadeRight", center + _streetRight * 5.6f + _streetForward * 2f, facing);
        CreateBrazier("GateBrazierLeft", center - _streetRight * 6.2f + _streetForward * 0.8f, 1.15f);
        CreateBrazier("GateBrazierRight", center + _streetRight * 6.2f + _streetForward * 1.2f, 1.15f);
        CreateBlock("GateWarningBannerLeft", center - _streetRight * 3.4f + _streetForward * 0.6f + Vector3.up * 3.1f, new Vector3(0.2f, 2.6f, 1.2f), facing, new Color(0.36f, 0.09f, 0.08f)).transform.SetParent(transform, true);
        CreateBlock("GateWarningBannerRight", center + _streetRight * 3.4f + _streetForward * 0.95f + Vector3.up * 3.1f, new Vector3(0.2f, 2.6f, 1.2f), facing, new Color(0.36f, 0.09f, 0.08f)).transform.SetParent(transform, true);
    }

    private void CreateStreetBarricade(string name, Vector3 center, Quaternion rotation)
    {
        GameObject baseBlock = CreateBlock($"{name}_Base", center + Vector3.up * 0.54f, new Vector3(2.8f, 1.08f, 0.7f), rotation, new Color(0.33f, 0.23f, 0.15f));
        baseBlock.transform.SetParent(transform, true);
        GameObject brace = CreateBlock($"{name}_Brace", center + Vector3.up * 1.1f, new Vector3(0.28f, 1.9f, 0.18f), rotation * Quaternion.Euler(0f, 0f, 28f), new Color(0.29f, 0.2f, 0.13f));
        brace.transform.SetParent(transform, true);
    }

    private void CreateCanopy(string name, Vector3 center, Vector3 streetFacing)
    {
        Quaternion rotation = Quaternion.LookRotation(streetFacing, Vector3.up);
        GameObject deck = CreateBlock($"{name}_Deck", center + Vector3.up * 0.3f, new Vector3(2.8f, 0.6f, 2.2f), rotation, new Color(0.28f, 0.23f, 0.18f));
        deck.transform.SetParent(transform, true);
        GameObject roof = CreateBlock($"{name}_Roof", center + Vector3.up * 2.1f, new Vector3(3.2f, 0.22f, 2.5f), rotation, new Color(0.41f, 0.14f, 0.13f));
        roof.transform.SetParent(transform, true);
        GameObject postLeft = CreateBlock($"{name}_PostLeft", center - _streetRight * 1.2f + Vector3.up * 1.1f, new Vector3(0.14f, 2.2f, 0.14f), rotation, new Color(0.25f, 0.18f, 0.13f));
        postLeft.transform.SetParent(transform, true);
        GameObject postRight = CreateBlock($"{name}_PostRight", center + _streetRight * 1.2f + Vector3.up * 1.1f, new Vector3(0.14f, 2.2f, 0.14f), rotation, new Color(0.25f, 0.18f, 0.13f));
        postRight.transform.SetParent(transform, true);
    }

    private void CreateAbandonedCart(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_streetRight, Vector3.up);
        GameObject bed = CreateBlock($"{name}_Bed", center + Vector3.up * 0.42f, new Vector3(2.4f, 0.34f, 1.5f), rotation, new Color(0.33f, 0.24f, 0.16f));
        bed.transform.SetParent(transform, true);
        GameObject handle = CreateBlock($"{name}_Handle", center - _streetRight * 1.5f + Vector3.up * 0.55f, new Vector3(2.6f, 0.14f, 0.14f), rotation, new Color(0.4f, 0.3f, 0.2f));
        handle.transform.SetParent(transform, true);
    }

    private void CreateMainlineTrail()
    {
        CreateTrailStrip("TrailStart", _streetCenter - _streetForward * 2.2f, 0.7f, 5.4f, new Color(0.54f, 0.45f, 0.21f));
        CreateTrailStrip("TrailMidA", _streetCenter + _streetForward * 8.4f, 0.6f, 6.2f, new Color(0.48f, 0.4f, 0.18f));
        CreateTrailStrip("TrailMidB", _streetCenter + _streetForward * 15.6f, 0.52f, 7.6f, new Color(0.22f, 0.06f, 0.06f));
        CreateTrailStrip("TrailGate", _streetCenter + _streetForward * 23.8f, 0.56f, 7.4f, new Color(0.3f, 0.08f, 0.08f));
    }

    private void CreateTrailStrip(string name, Vector3 center, float width, float depth, Color color)
    {
        Quaternion rotation = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock(name, center + Vector3.up * 0.035f, new Vector3(width, 0.02f, depth), rotation, color).transform.SetParent(transform, true);
    }

    private void CreateDangerTransition(Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("DangerPaintA", center - _streetForward * 2.2f + Vector3.up * 0.03f, new Vector3(9.8f, 0.02f, 0.65f), facing * Quaternion.Euler(0f, 0f, 24f), new Color(0.42f, 0.1f, 0.09f)).transform.SetParent(transform, true);
        CreateBlock("DangerPaintB", center - _streetForward * 0.6f + Vector3.up * 0.03f, new Vector3(9.8f, 0.02f, 0.65f), facing * Quaternion.Euler(0f, 0f, -24f), new Color(0.42f, 0.1f, 0.09f)).transform.SetParent(transform, true);
        CreateStreetBarricade("DangerBarricadeLeft", center - _streetRight * 5.6f, facing);
        CreateStreetBarricade("DangerBarricadeRight", center + _streetRight * 5.6f + _streetForward * 0.8f, facing);
        CreateBrazier("DangerBrazierLeft", center - _streetRight * 6.8f - _streetForward * 0.6f, 1.22f);
        CreateBrazier("DangerBrazierRight", center + _streetRight * 6.8f - _streetForward * 0.2f, 1.22f);
    }

    private void CreateDistrictBands()
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("DistrictBand_Start", _streetCenter - _streetForward * 8.8f + Vector3.up * 0.02f, new Vector3(13.2f, 0.02f, 8.6f), facing, new Color(0.19f, 0.18f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock("DistrictBand_Notice", _streetCenter + _streetForward * 6.8f + Vector3.up * 0.02f, new Vector3(13.6f, 0.02f, 8.2f), facing, new Color(0.21f, 0.19f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock("DistrictBand_Incident", _streetCenter + _streetForward * 14.6f + Vector3.up * 0.02f, new Vector3(13.2f, 0.02f, 7.2f), facing, new Color(0.18f, 0.16f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock("DistrictBand_Gate", _streetCenter + _streetForward * 22.6f + Vector3.up * 0.02f, new Vector3(12.8f, 0.02f, 7.4f), facing, new Color(0.17f, 0.15f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock("DistrictBand_Fog", _streetCenter + _streetForward * 29.2f + Vector3.up * 0.02f, new Vector3(12.2f, 0.02f, 6.8f), facing, new Color(0.17f, 0.1f, 0.1f)).transform.SetParent(transform, true);
    }

    private void CreateStreetLandmarkHeights()
    {
        CreateWatchTower("WestWatchTower", _streetCenter - _streetRight * 18.6f - _streetForward * 6.4f, 8.8f);
        CreateWatchTower("EastWatchTower", _streetCenter + _streetRight * 18.2f + _streetForward * 18.4f, 9.8f);
        CreateBellHouse("BellHouse", _streetCenter - _streetRight * 17.4f + _streetForward * 12.8f);
    }

    private void CreateSpecificBuildLots()
    {
        CreateGuardOfficeLot("GuardOfficeLot", _streetCenter - _streetRight * 14.4f - _streetForward * 8.2f, -1f);
        CreateShutteredInnLot("ShutteredInnLot", _streetCenter + _streetRight * 14.8f - _streetForward * 3.8f, 1f);
        CreateApothecaryLot("ApothecaryLot", _streetCenter - _streetRight * 14.5f + _streetForward * 8.6f, -1f);
        CreateCollapsedResidenceLot("CollapsedResidenceLot", _streetCenter + _streetRight * 14.8f + _streetForward * 15.8f, 1f);
        CreateSealedChapelLot("SealedChapelLot", _streetCenter - _streetRight * 14.6f + _streetForward * 23.4f, -1f);
    }

    private void CreateOverheadCrossings()
    {
        CreateOverheadCrossing("MarketCrossing", _streetCenter + _streetForward * 6.8f, 11.6f, 4.8f, new Color(0.25f, 0.1f, 0.09f));
        CreateOverheadCrossing("GateCrossing", _streetCenter + _streetForward * 21.8f, 10.4f, 4.2f, new Color(0.22f, 0.08f, 0.08f));
    }

    private void CreateEdictLine()
    {
        CreateRoyalEdictBoard("RoyalEdictA", _streetCenter - _streetRight * 6.4f - _streetForward * 3.6f);
        CreateRoyalEdictBoard("RoyalEdictB", _streetCenter + _streetRight * 6.1f + _streetForward * 11.8f);
        CreateRoyalEdictBoard("RoyalEdictC", _streetCenter - _streetRight * 6.2f + _streetForward * 20.2f);
    }

    private void CreateObjectiveReadabilityPass()
    {
        Quaternion streetFacing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("SteleGuideLeft", _streetCenter - _streetRight * 4.6f + _streetForward * 6.2f + Vector3.up * 0.8f, new Vector3(0.18f, 1.6f, 0.18f), Quaternion.identity, new Color(0.46f, 0.35f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock("SteleGuideRight", _streetCenter - _streetRight * 0.8f + _streetForward * 8.1f + Vector3.up * 0.8f, new Vector3(0.18f, 1.6f, 0.18f), Quaternion.identity, new Color(0.46f, 0.35f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock("CrestFocusPatch", _streetCenter + _streetRight * 1.5f + _streetForward * 14.8f + Vector3.up * 0.03f, new Vector3(2.2f, 0.02f, 2.2f), streetFacing, new Color(0.36f, 0.14f, 0.12f)).transform.SetParent(transform, true);
        CreateBlock("GateFocusRunner", _streetCenter + _streetForward * 21.4f + Vector3.up * 0.03f, new Vector3(1.1f, 0.02f, 4.8f), streetFacing, new Color(0.32f, 0.1f, 0.09f)).transform.SetParent(transform, true);
    }

    private void CreateGatePressureLane(Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("GatePressureCrateLeft", center - _streetRight * 3.4f + Vector3.up * 0.42f, new Vector3(1.2f, 0.84f, 1f), facing, new Color(0.34f, 0.24f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock("GatePressureCrateRight", center + _streetRight * 3.2f + _streetForward * 0.8f + Vector3.up * 0.46f, new Vector3(1.3f, 0.92f, 1.1f), facing, new Color(0.34f, 0.24f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock("GatePressurePoleLeft", center - _streetRight * 2.2f + Vector3.up * 1.65f, new Vector3(0.12f, 3.3f, 0.12f), Quaternion.identity, new Color(0.24f, 0.18f, 0.14f)).transform.SetParent(transform, true);
        CreateBlock("GatePressurePoleRight", center + _streetRight * 2.2f + _streetForward * 1.2f + Vector3.up * 1.65f, new Vector3(0.12f, 3.3f, 0.12f), Quaternion.identity, new Color(0.24f, 0.18f, 0.14f)).transform.SetParent(transform, true);
        CreateBlock("GatePressureCloth", center + _streetForward * 0.6f + Vector3.up * 2.55f, new Vector3(0.08f, 1.2f, 4.6f), facing * Quaternion.Euler(0f, 90f, 0f), new Color(0.31f, 0.11f, 0.1f)).transform.SetParent(transform, true);
    }

    private void CreateArrivalCourt(Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("ArrivalCourtDeck", center + Vector3.up * 0.03f, new Vector3(9.6f, 0.03f, 5.6f), facing, new Color(0.18f, 0.17f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock("ArrivalCourtBenchLeft", center - _streetRight * 4.1f + Vector3.up * 0.4f, new Vector3(2.2f, 0.22f, 0.52f), Quaternion.LookRotation(_streetForward, Vector3.up), new Color(0.31f, 0.24f, 0.17f)).transform.SetParent(transform, true);
        CreateBlock("ArrivalCourtBenchRight", center + _streetRight * 4f + _streetForward * 0.6f + Vector3.up * 0.4f, new Vector3(2.2f, 0.22f, 0.52f), Quaternion.LookRotation(_streetForward, Vector3.up), new Color(0.31f, 0.24f, 0.17f)).transform.SetParent(transform, true);
        CreateBlock("ArrivalCourtNoticeRack", center + _streetRight * 4.8f - _streetForward * 1.1f + Vector3.up * 1.4f, new Vector3(0.18f, 2.8f, 1.4f), facing, new Color(0.26f, 0.2f, 0.16f)).transform.SetParent(transform, true);
    }

    private void CreateSupplyCheckpointRuin(Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("SupplyCheckpointDeck", center - _streetRight * 6.8f + Vector3.up * 0.04f, new Vector3(4.4f, 0.04f, 6.8f), facing, new Color(0.18f, 0.17f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock("SupplyCheckpointRackA", center - _streetRight * 7.2f + _streetForward * 1.2f + Vector3.up * 1f, new Vector3(2.1f, 2f, 1.8f), facing, new Color(0.28f, 0.21f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock("SupplyCheckpointRackB", center - _streetRight * 6.4f - _streetForward * 1.5f + Vector3.up * 0.8f, new Vector3(1.8f, 1.6f, 1.6f), facing * Quaternion.Euler(0f, -12f, 0f), new Color(0.27f, 0.2f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock("SupplyCheckpointTarp", center - _streetRight * 6.9f + Vector3.up * 2.3f, new Vector3(0.14f, 1.8f, 4.6f), facing * Quaternion.Euler(0f, 90f, 0f), new Color(0.26f, 0.1f, 0.09f)).transform.SetParent(transform, true);
        CreateBrokenCrate("SupplyBrokenCrateA", center - _streetRight * 5.6f + _streetForward * 2.1f);
        CreateBrokenCrate("SupplyBrokenCrateB", center - _streetRight * 5f - _streetForward * 0.8f);
        CreateBlock("SupplyRegistryBoard", center - _streetRight * 4.9f + _streetForward * 0.8f + Vector3.up * 1.8f, new Vector3(0.16f, 1.4f, 1.1f), Quaternion.LookRotation(_streetRight, Vector3.up), new Color(0.42f, 0.27f, 0.15f)).transform.SetParent(transform, true);
    }

    private void CreateFuneraryEscortCart(Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("FuneraryCartBed", center + Vector3.up * 0.42f, new Vector3(2.8f, 0.34f, 1.6f), facing, new Color(0.25f, 0.19f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock("FuneraryCartSheet", center + Vector3.up * 0.8f, new Vector3(2.3f, 0.42f, 1.2f), facing, new Color(0.56f, 0.56f, 0.54f)).transform.SetParent(transform, true);
        CreateBlock("FuneraryCartPole", center - _streetForward * 1.8f + Vector3.up * 0.54f, new Vector3(3.2f, 0.14f, 0.14f), facing, new Color(0.33f, 0.24f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock("FuneraryCartCandle", center + _streetRight * 1f + Vector3.up * 1.28f, new Vector3(0.18f, 0.52f, 0.18f), Quaternion.identity, new Color(0.84f, 0.62f, 0.22f)).transform.SetParent(transform, true);
    }

    private void CreateQuarantineCorridor(Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("QuarantineWallLeft", center - _streetRight * 5.8f + Vector3.up * 1.3f, new Vector3(0.42f, 2.6f, 8.4f), facing, new Color(0.2f, 0.17f, 0.17f)).transform.SetParent(transform, true);
        CreateBlock("QuarantineWallRight", center + _streetRight * 5.8f + Vector3.up * 1.3f, new Vector3(0.42f, 2.6f, 8.4f), facing, new Color(0.2f, 0.17f, 0.17f)).transform.SetParent(transform, true);
        CreateBlock("QuarantineRailLeft", center - _streetRight * 5.2f + Vector3.up * 2.2f, new Vector3(0.12f, 0.24f, 8.1f), facing, new Color(0.38f, 0.14f, 0.12f)).transform.SetParent(transform, true);
        CreateBlock("QuarantineRailRight", center + _streetRight * 5.2f + Vector3.up * 2.2f, new Vector3(0.12f, 0.24f, 8.1f), facing, new Color(0.38f, 0.14f, 0.12f)).transform.SetParent(transform, true);
        CreateBlock("QuarantineChainA", center + _streetForward * 1.2f + Vector3.up * 1.5f, new Vector3(0.08f, 0.18f, 10.1f), facing * Quaternion.Euler(0f, 90f, 0f), new Color(0.31f, 0.1f, 0.09f)).transform.SetParent(transform, true);
        CreateBlock("QuarantineChainB", center - _streetForward * 1.4f + Vector3.up * 1.6f, new Vector3(0.08f, 0.18f, 10.1f), facing * Quaternion.Euler(0f, 90f, 0f), new Color(0.31f, 0.1f, 0.09f)).transform.SetParent(transform, true);
    }

    private void CreateFogApproachFunnels(Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("FogFunnelLeftA", center - _streetRight * 5.6f - _streetForward * 0.8f + Vector3.up * 0.5f, new Vector3(1.8f, 1f, 2.6f), facing * Quaternion.Euler(0f, 16f, 0f), new Color(0.23f, 0.18f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock("FogFunnelRightA", center + _streetRight * 5.6f - _streetForward * 0.2f + Vector3.up * 0.5f, new Vector3(1.8f, 1f, 2.6f), facing * Quaternion.Euler(0f, -16f, 0f), new Color(0.23f, 0.18f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock("FogFunnelLeftB", center - _streetRight * 4.6f + _streetForward * 1.2f + Vector3.up * 0.68f, new Vector3(1.6f, 1.36f, 2.2f), facing * Quaternion.Euler(0f, 10f, 0f), new Color(0.21f, 0.16f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock("FogFunnelRightB", center + _streetRight * 4.6f + _streetForward * 1.5f + Vector3.up * 0.68f, new Vector3(1.6f, 1.36f, 2.2f), facing * Quaternion.Euler(0f, -10f, 0f), new Color(0.21f, 0.16f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock("FogFunnelChain", center + _streetForward * 1.8f + Vector3.up * 2.3f, new Vector3(0.1f, 0.28f, 8.2f), facing * Quaternion.Euler(0f, 90f, 0f), new Color(0.33f, 0.12f, 0.1f)).transform.SetParent(transform, true);
    }

    private void CreateWardingLine(Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        for (int i = -2; i <= 2; i++)
        {
            Vector3 postCenter = center + _streetRight * (i * 1.55f) + Vector3.up * 1.1f;
            CreateBlock($"WardingPost_{i + 3}", postCenter, new Vector3(0.14f, 2.2f, 0.14f), Quaternion.identity, new Color(0.29f, 0.22f, 0.16f)).transform.SetParent(transform, true);
            CreateBlock($"WardingCloth_{i + 3}", postCenter + Vector3.up * 0.62f, new Vector3(0.08f, 0.82f, 0.56f), facing, new Color(0.41f, 0.12f, 0.1f)).transform.SetParent(transform, true);
        }
        CreateBlock("WardingSealStrip", center + _streetForward * 0.4f + Vector3.up * 0.03f, new Vector3(6.2f, 0.02f, 3.8f), facing, new Color(0.34f, 0.08f, 0.09f)).transform.SetParent(transform, true);
    }

    private void CreatePauseSpaces()
    {
        CreateNoticePocket("NoticePocket", _streetCenter + _streetForward * 7.2f);
        CreateIncidentPocket("IncidentPocket", _streetCenter + _streetForward * 14.8f);
        CreateGatePocket("GatePocket", _streetCenter + _streetForward * 22.8f);
    }

    private void CreateNoticePocket(string name, Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock($"{name}_InsetLeft", center - _streetRight * 5.7f + Vector3.up * 0.04f, new Vector3(2.2f, 0.04f, 3.6f), facing, new Color(0.2f, 0.18f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_InsetRight", center + _streetRight * 5.7f + Vector3.up * 0.04f, new Vector3(2.2f, 0.04f, 3.2f), facing, new Color(0.2f, 0.18f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_PostLeft", center - _streetRight * 6.3f + _streetForward * 1.1f + Vector3.up * 1.2f, new Vector3(0.18f, 2.4f, 0.18f), Quaternion.identity, new Color(0.24f, 0.19f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_PostRight", center + _streetRight * 6.1f - _streetForward * 0.8f + Vector3.up * 1.2f, new Vector3(0.18f, 2.4f, 0.18f), Quaternion.identity, new Color(0.24f, 0.19f, 0.15f)).transform.SetParent(transform, true);
    }

    private void CreateIncidentPocket(string name, Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock($"{name}_OpenPatch", center + _streetRight * 2.6f + Vector3.up * 0.03f, new Vector3(3.2f, 0.03f, 4.8f), facing, new Color(0.18f, 0.16f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_EdgeLeft", center - _streetRight * 4.8f + Vector3.up * 0.26f, new Vector3(1.3f, 0.52f, 4.4f), facing, new Color(0.24f, 0.2f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_EdgeRight", center + _streetRight * 4.9f + Vector3.up * 0.26f, new Vector3(1.5f, 0.52f, 4.8f), facing, new Color(0.24f, 0.2f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_CluePole", center + _streetRight * 4.4f + _streetForward * 1.3f + Vector3.up * 1.4f, new Vector3(0.16f, 2.8f, 0.16f), Quaternion.identity, new Color(0.25f, 0.18f, 0.14f)).transform.SetParent(transform, true);
    }

    private void CreateGatePocket(string name, Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock($"{name}_Apron", center + Vector3.up * 0.03f, new Vector3(7.8f, 0.03f, 5.4f), facing, new Color(0.17f, 0.16f, 0.17f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_EdgeLeft", center - _streetRight * 4.4f + Vector3.up * 0.22f, new Vector3(0.8f, 0.44f, 4.2f), facing, new Color(0.26f, 0.21f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_EdgeRight", center + _streetRight * 4.4f + Vector3.up * 0.22f, new Vector3(0.8f, 0.44f, 4.2f), facing, new Color(0.26f, 0.21f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_BannerBar", center + Vector3.up * 3.1f, new Vector3(8.2f, 0.16f, 0.16f), facing, new Color(0.23f, 0.18f, 0.14f)).transform.SetParent(transform, true);
    }

    private void CreateDistrictStoryAnchors()
    {
        CreateTavernAnchor("TavernAnchor", _streetCenter - _streetRight * 12.4f - _streetForward * 5.4f, -1f);
        CreateApothecaryAnchor("ApothecaryAnchor", _streetCenter + _streetRight * 12.6f + _streetForward * 9.6f, 1f);
        CreateMemorialShrine("MemorialShrine", _streetCenter - _streetRight * 7.8f + _streetForward * 21.4f);
        CreateSupplyPile("SupplyPile", _streetCenter + _streetRight * 7.4f + _streetForward * 4.1f);
    }

    private void CreateSideAlleyPocket(string name, Vector3 center, float sideSign, bool collapsed)
    {
        Quaternion alleyRotation = Quaternion.LookRotation(_streetRight * sideSign, Vector3.up);
        CreateBlock($"{name}_Lane", center + Vector3.up * 0.02f, new Vector3(4.8f, 0.04f, 6.8f), alleyRotation, new Color(0.17f, 0.16f, 0.17f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_WallLeft", center - _streetForward * 2.4f + Vector3.up * 1.4f, new Vector3(0.3f, 2.8f, 6.6f), alleyRotation, new Color(0.16f, 0.14f, 0.14f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_WallRight", center + _streetForward * 2.4f + Vector3.up * 1.4f, new Vector3(0.3f, 2.8f, 6.6f), alleyRotation, new Color(0.16f, 0.14f, 0.14f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Lintel", center + _streetRight * sideSign * 2.3f + Vector3.up * 2.9f, new Vector3(0.4f, 0.22f, 5.4f), alleyRotation, new Color(0.21f, 0.18f, 0.17f)).transform.SetParent(transform, true);

        if (collapsed)
        {
            CreateBlock($"{name}_CollapseA", center + _streetRight * sideSign * 1.2f + Vector3.up * 0.6f, new Vector3(2.4f, 1.2f, 1.4f), alleyRotation, new Color(0.25f, 0.2f, 0.18f)).transform.SetParent(transform, true);
            CreateBlock($"{name}_CollapseB", center + _streetRight * sideSign * 2f + _streetForward * 1.1f + Vector3.up * 1f, new Vector3(1.6f, 2f, 1.2f), alleyRotation * Quaternion.Euler(0f, 0f, 18f), new Color(0.23f, 0.19f, 0.17f)).transform.SetParent(transform, true);
            CreateStreetBarricade($"{name}_Barricade", center - _streetRight * sideSign * 0.2f, alleyRotation);
        }
        else
        {
            CreateBarrelStack($"{name}_Barrels", center + _streetForward * 1.2f);
            CreateBlock($"{name}_ClotheslinePostA", center + _streetRight * sideSign * 1.3f - _streetForward * 1.8f + Vector3.up * 1.7f, new Vector3(0.12f, 3.4f, 0.12f), Quaternion.identity, new Color(0.26f, 0.2f, 0.15f)).transform.SetParent(transform, true);
            CreateBlock($"{name}_ClotheslinePostB", center + _streetRight * sideSign * 1.3f + _streetForward * 1.8f + Vector3.up * 1.7f, new Vector3(0.12f, 3.4f, 0.12f), Quaternion.identity, new Color(0.26f, 0.2f, 0.15f)).transform.SetParent(transform, true);
            CreateBlock($"{name}_Cloth", center + _streetRight * sideSign * 1.3f + Vector3.up * 2.5f, new Vector3(0.08f, 1.1f, 2.6f), alleyRotation, new Color(0.33f, 0.16f, 0.15f)).transform.SetParent(transform, true);
        }
    }

    private void CreateFacadeDetails(string name, Vector3 center, float sideSign, float width, float height, float depth)
    {
        Vector3 frontCenter = center + _streetRight * sideSign * (depth * 0.5f + 0.12f);
        Vector3 inward = -_streetRight * sideSign;
        Quaternion facadeRotation = Quaternion.LookRotation(inward, Vector3.up);

        CreateBlock($"{name}_DoorFrame", frontCenter + Vector3.up * 1.22f, new Vector3(1.46f, 2.52f, 0.08f), facadeRotation, new Color(0.38f, 0.3f, 0.22f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Door", frontCenter + Vector3.up * 1.15f, new Vector3(1.08f, 2.3f, 0.16f), facadeRotation, new Color(0.2f, 0.13f, 0.1f)).transform.SetParent(transform, true);

        float windowSpacing = Mathf.Min(2.1f, width * 0.28f);
        CreateFacadeWindow($"{name}_WindowLeft", frontCenter - _streetForward * windowSpacing + Vector3.up * (height * 0.62f), facadeRotation);
        CreateFacadeWindow($"{name}_WindowRight", frontCenter + _streetForward * windowSpacing + Vector3.up * (height * 0.62f), facadeRotation);

        if (height > 5.3f)
        {
            CreateFacadeWindow($"{name}_UpperLeft", frontCenter - _streetForward * (windowSpacing * 0.75f) + Vector3.up * (height * 0.82f), facadeRotation);
            CreateFacadeWindow($"{name}_UpperRight", frontCenter + _streetForward * (windowSpacing * 0.75f) + Vector3.up * (height * 0.82f), facadeRotation);
        }

        if (Mathf.Abs(Vector3.Dot(center - _streetCenter, _streetForward)) > 9f)
        {
            CreateBoardedDoor($"{name}_Boarding", frontCenter + _streetForward * 0.45f, facadeRotation);
        }
    }

    private void CreateRoofscape(string name, Vector3 center, Quaternion rotation, float width, float height, float depth, float forwardAmount, float sideSign)
    {
        if (forwardAmount < 4f)
        {
            CreateBlock($"{name}_ShopSignPost", center + _streetRight * sideSign * (depth * 0.5f + 0.4f) + Vector3.up * 2.4f + _streetForward * 0.45f, new Vector3(0.18f, 2.1f, 0.18f), rotation, new Color(0.24f, 0.18f, 0.14f)).transform.SetParent(transform, true);
            CreateBlock($"{name}_ShopAwning", center + _streetRight * sideSign * (depth * 0.5f + 0.1f) + Vector3.up * 1.7f, new Vector3(0.28f, 0.22f, Mathf.Min(2.8f, width - 1f)), rotation, new Color(0.34f, 0.12f, 0.11f)).transform.SetParent(transform, true);
            CreateBlock($"{name}_ShopDormer", center + Vector3.up * (height + 0.95f), new Vector3(Mathf.Min(2.2f, width - 1.4f), 0.9f, 1.2f), rotation, new Color(0.2f, 0.16f, 0.14f)).transform.SetParent(transform, true);
        }
        else if (forwardAmount < 12f)
        {
            CreateBlock($"{name}_HomeChimney", center - _streetForward * (width * 0.18f) + Vector3.up * (height + 1.2f), new Vector3(0.7f, 1.9f, 0.7f), rotation, new Color(0.24f, 0.2f, 0.2f)).transform.SetParent(transform, true);
            CreateBlock($"{name}_HomeRearLeanTo", center - _streetRight * sideSign * (depth * 0.35f) + Vector3.up * 1.2f, new Vector3(1.8f, 2.4f, 2.2f), rotation, new Color(0.16f, 0.14f, 0.14f)).transform.SetParent(transform, true);
            CreateBlock($"{name}_HomeRoofPatch", center + _streetForward * (width * 0.12f) + Vector3.up * (height + 0.5f), new Vector3(1.4f, 0.2f, 1.6f), rotation * Quaternion.Euler(0f, 0f, 8f), new Color(0.18f, 0.14f, 0.12f)).transform.SetParent(transform, true);
        }
        else
        {
            CreateBlock($"{name}_SealLathA", center + Vector3.up * (height + 0.55f), new Vector3(width * 0.92f, 0.16f, 0.14f), rotation * Quaternion.Euler(0f, 0f, 7f), new Color(0.34f, 0.24f, 0.17f)).transform.SetParent(transform, true);
            CreateBlock($"{name}_SealLathB", center + _streetForward * 0.8f + Vector3.up * (height + 0.95f), new Vector3(width * 0.76f, 0.16f, 0.14f), rotation * Quaternion.Euler(0f, 0f, -9f), new Color(0.34f, 0.24f, 0.17f)).transform.SetParent(transform, true);
            CreateBlock($"{name}_QuarantineVent", center - _streetForward * (width * 0.16f) + Vector3.up * (height + 0.9f), new Vector3(0.9f, 1.2f, 0.9f), rotation, new Color(0.16f, 0.13f, 0.14f)).transform.SetParent(transform, true);
            CreateBlock($"{name}_RearBrace", center - _streetRight * sideSign * (depth * 0.32f) + Vector3.up * 1.3f, new Vector3(0.28f, 2.6f, 2.2f), rotation * Quaternion.Euler(0f, 0f, 14f * sideSign), new Color(0.29f, 0.22f, 0.16f)).transform.SetParent(transform, true);
        }
    }

    private void CreateFacadeWindow(string name, Vector3 center, Quaternion rotation)
    {
        CreateBlock(name, center, new Vector3(1.02f, 1.18f, 0.14f), rotation, new Color(0.58f, 0.62f, 0.68f)).transform.SetParent(transform, true);
    }

    private void CreateBoardedDoor(string name, Vector3 center, Quaternion rotation)
    {
        CreateBlock($"{name}_A", center + Vector3.up * 1.2f, new Vector3(1.38f, 0.16f, 0.08f), rotation * Quaternion.Euler(0f, 0f, 21f), new Color(0.39f, 0.28f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_B", center + Vector3.up * 0.8f, new Vector3(1.24f, 0.16f, 0.08f), rotation * Quaternion.Euler(0f, 0f, -18f), new Color(0.39f, 0.28f, 0.18f)).transform.SetParent(transform, true);
    }

    private void CreateShopfrontSet(string name, Vector3 frontCenter, float sideSign, Quaternion facadeRotation)
    {
        CreateBlock($"{name}_SupplyCrateA", frontCenter - _streetForward * 1.1f + Vector3.up * 0.34f, new Vector3(0.9f, 0.68f, 0.8f), facadeRotation, new Color(0.34f, 0.24f, 0.17f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_SupplyCrateB", frontCenter + _streetForward * 1.25f + Vector3.up * 0.38f, new Vector3(1f, 0.76f, 0.86f), facadeRotation, new Color(0.34f, 0.24f, 0.17f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_SignArm", frontCenter + _streetRight * sideSign * 0.18f + Vector3.up * 2.8f, new Vector3(0.95f, 0.12f, 0.12f), Quaternion.LookRotation(_streetRight * sideSign, Vector3.up), new Color(0.3f, 0.22f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_HangingSign", frontCenter + _streetRight * sideSign * 0.78f + Vector3.up * 2.2f, new Vector3(0.12f, 1.1f, 0.72f), facadeRotation, new Color(0.34f, 0.14f, 0.12f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_ShutterPanel", frontCenter + Vector3.up * 1.55f, new Vector3(0.12f, 1.7f, 2.6f), facadeRotation, new Color(0.28f, 0.2f, 0.14f)).transform.SetParent(transform, true);
    }

    private void CreateResidenceSet(string name, Vector3 frontCenter, float sideSign, Quaternion facadeRotation)
    {
        CreateBlock($"{name}_PlanterLeft", frontCenter - _streetForward * 0.95f + Vector3.up * 0.28f, new Vector3(0.82f, 0.56f, 0.62f), facadeRotation, new Color(0.3f, 0.26f, 0.2f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_PlanterRight", frontCenter + _streetForward * 0.95f + Vector3.up * 0.28f, new Vector3(0.82f, 0.56f, 0.62f), facadeRotation, new Color(0.3f, 0.26f, 0.2f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_ShrubLeft", frontCenter - _streetForward * 0.95f + Vector3.up * 0.72f, new Vector3(0.62f, 0.44f, 0.44f), facadeRotation, new Color(0.24f, 0.3f, 0.2f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_ShrubRight", frontCenter + _streetForward * 0.95f + Vector3.up * 0.72f, new Vector3(0.62f, 0.44f, 0.44f), facadeRotation, new Color(0.24f, 0.3f, 0.2f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_DoorLamp", frontCenter + _streetRight * sideSign * 0.1f + Vector3.up * 2.2f, new Vector3(0.14f, 0.5f, 0.14f), Quaternion.identity, new Color(0.76f, 0.56f, 0.22f)).transform.SetParent(transform, true);
    }

    private void CreateQuarantineFrontageSet(string name, Vector3 frontCenter, float sideSign, Quaternion facadeRotation)
    {
        CreateBlock($"{name}_SealBoard", frontCenter + Vector3.up * 1.35f, new Vector3(1.42f, 0.18f, 0.1f), facadeRotation, new Color(0.42f, 0.12f, 0.1f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_CrossBarA", frontCenter + Vector3.up * 1.35f, new Vector3(1.52f, 0.16f, 0.08f), facadeRotation * Quaternion.Euler(0f, 0f, 28f), new Color(0.37f, 0.27f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_CrossBarB", frontCenter + Vector3.up * 0.95f, new Vector3(1.44f, 0.16f, 0.08f), facadeRotation * Quaternion.Euler(0f, 0f, -25f), new Color(0.37f, 0.27f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_SandbagLeft", frontCenter - _streetForward * 0.9f + Vector3.up * 0.24f, new Vector3(0.9f, 0.48f, 0.56f), facadeRotation, new Color(0.42f, 0.35f, 0.24f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_SandbagRight", frontCenter + _streetForward * 0.9f + Vector3.up * 0.24f, new Vector3(0.9f, 0.48f, 0.56f), facadeRotation, new Color(0.42f, 0.35f, 0.24f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_WarningTag", frontCenter + _streetRight * sideSign * 0.12f + Vector3.up * 2.1f, new Vector3(0.08f, 0.62f, 0.68f), facadeRotation, new Color(0.56f, 0.16f, 0.14f)).transform.SetParent(transform, true);
    }

    private void CreateTavernAnchor(string name, Vector3 center, float sideSign)
    {
        Quaternion facadeRotation = Quaternion.LookRotation(-_streetRight * sideSign, Vector3.up);
        CreateBlock($"{name}_InnBoard", center + Vector3.up * 2.8f, new Vector3(0.18f, 1.6f, 1.3f), facadeRotation, new Color(0.4f, 0.15f, 0.13f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_ShutterA", center - _streetForward * 1.2f + Vector3.up * 1.6f, new Vector3(0.16f, 1.8f, 1.1f), facadeRotation, new Color(0.28f, 0.2f, 0.14f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_ShutterB", center + _streetForward * 1.2f + Vector3.up * 1.6f, new Vector3(0.16f, 1.8f, 1.1f), facadeRotation, new Color(0.28f, 0.2f, 0.14f)).transform.SetParent(transform, true);
        CreateBarrelStack($"{name}_Barrels", center + _streetForward * 2.2f + _streetRight * sideSign * 0.8f);
    }

    private void CreateApothecaryAnchor(string name, Vector3 center, float sideSign)
    {
        Quaternion facadeRotation = Quaternion.LookRotation(-_streetRight * sideSign, Vector3.up);
        CreateBlock($"{name}_Cabinet", center + Vector3.up * 1.4f, new Vector3(0.8f, 2.8f, 1.8f), facadeRotation, new Color(0.28f, 0.21f, 0.17f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_MortarSign", center + Vector3.up * 2.6f + _streetForward * 1.3f, new Vector3(0.16f, 1.2f, 0.9f), facadeRotation, new Color(0.24f, 0.4f, 0.22f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_HerbRack", center - _streetForward * 1.1f + Vector3.up * 0.78f, new Vector3(1.5f, 1.2f, 0.7f), facadeRotation, new Color(0.3f, 0.25f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Satchel", center + _streetForward * 1.4f + Vector3.up * 0.34f, new Vector3(0.64f, 0.46f, 0.5f), facadeRotation, new Color(0.41f, 0.29f, 0.18f)).transform.SetParent(transform, true);
    }

    private void CreateMemorialShrine(string name, Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock($"{name}_Base", center + Vector3.up * 0.28f, new Vector3(1.9f, 0.56f, 1.4f), facing, new Color(0.28f, 0.26f, 0.25f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Stele", center + Vector3.up * 1.55f, new Vector3(0.7f, 2.2f, 0.34f), facing, new Color(0.4f, 0.39f, 0.41f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Offerings", center + _streetForward * 0.5f + Vector3.up * 0.48f, new Vector3(1.1f, 0.22f, 0.5f), facing, new Color(0.42f, 0.26f, 0.14f)).transform.SetParent(transform, true);
        CreateBrazier($"{name}_CandleLeft", center - _streetRight * 1.1f, 0.55f);
        CreateBrazier($"{name}_CandleRight", center + _streetRight * 1.1f, 0.55f);
    }

    private void CreateSupplyPile(string name, Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock($"{name}_CrateA", center + Vector3.up * 0.42f, new Vector3(1.2f, 0.84f, 1f), facing, new Color(0.34f, 0.24f, 0.17f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_CrateB", center + _streetForward * 1.2f + Vector3.up * 0.36f, new Vector3(0.92f, 0.72f, 0.84f), facing, new Color(0.34f, 0.24f, 0.17f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_SackA", center - _streetRight * 0.9f + _streetForward * 0.4f + Vector3.up * 0.34f, new Vector3(0.9f, 0.68f, 0.74f), facing, new Color(0.4f, 0.32f, 0.22f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_SackB", center + _streetRight * 1f - _streetForward * 0.5f + Vector3.up * 0.3f, new Vector3(0.82f, 0.6f, 0.64f), facing, new Color(0.4f, 0.32f, 0.22f)).transform.SetParent(transform, true);
    }

    private void CreateForegroundFrame()
    {
        Quaternion rotation = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock("ForegroundLeftPost", _streetCenter - _streetRight * 16.6f - _streetForward * 13.8f + Vector3.up * 3.2f, new Vector3(0.42f, 6.4f, 0.42f), rotation, new Color(0.21f, 0.17f, 0.14f)).transform.SetParent(transform, true);
        CreateBlock("ForegroundRightPost", _streetCenter + _streetRight * 16.4f - _streetForward * 12.6f + Vector3.up * 3.2f, new Vector3(0.42f, 6.4f, 0.42f), rotation, new Color(0.21f, 0.17f, 0.14f)).transform.SetParent(transform, true);
        CreateBlock("ForegroundBannerLeft", _streetCenter - _streetRight * 15.9f - _streetForward * 11.9f + Vector3.up * 4.1f, new Vector3(0.16f, 2.8f, 1.8f), rotation, new Color(0.26f, 0.09f, 0.08f)).transform.SetParent(transform, true);
        CreateBlock("ForegroundBannerRight", _streetCenter + _streetRight * 15.7f - _streetForward * 10.8f + Vector3.up * 4.1f, new Vector3(0.16f, 2.8f, 1.8f), rotation, new Color(0.26f, 0.09f, 0.08f)).transform.SetParent(transform, true);
        CreateBlock("ForegroundMidLeft", _streetCenter - _streetRight * 17.2f + _streetForward * 6.5f + Vector3.up * 2.9f, new Vector3(0.28f, 5.8f, 1.9f), rotation, new Color(0.17f, 0.14f, 0.13f)).transform.SetParent(transform, true);
        CreateBlock("ForegroundMidRight", _streetCenter + _streetRight * 17f + _streetForward * 15.8f + Vector3.up * 2.9f, new Vector3(0.28f, 5.8f, 2.2f), rotation, new Color(0.17f, 0.14f, 0.13f)).transform.SetParent(transform, true);
        CreateBlock("ForegroundMidClothLeft", _streetCenter - _streetRight * 16.7f + _streetForward * 6.2f + Vector3.up * 3.6f, new Vector3(0.12f, 1.9f, 2.6f), rotation, new Color(0.22f, 0.09f, 0.08f)).transform.SetParent(transform, true);
        CreateBlock("ForegroundMidClothRight", _streetCenter + _streetRight * 16.5f + _streetForward * 15.5f + Vector3.up * 3.6f, new Vector3(0.12f, 2.1f, 2.8f), rotation, new Color(0.22f, 0.09f, 0.08f)).transform.SetParent(transform, true);
    }

    private void CreateSkylineSilhouettes()
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        for (int i = -2; i <= 2; i++)
        {
            float zOffset = i * 8.4f;
            CreateBlock($"SkylineWest_{i + 3}", _streetCenter - _streetRight * 20.4f + _streetForward * zOffset + Vector3.up * 4.6f, new Vector3(3.2f, 9.2f + i * 0.4f, 4.4f), facing, new Color(0.12f, 0.1f, 0.11f)).transform.SetParent(transform, true);
            CreateBlock($"SkylineEast_{i + 3}", _streetCenter + _streetRight * 20.1f + _streetForward * (zOffset + 2f) + Vector3.up * 4.2f, new Vector3(3.6f, 8.4f + i * 0.35f, 4.2f), facing, new Color(0.12f, 0.1f, 0.11f)).transform.SetParent(transform, true);
        }
    }

    private void CreateWatchTower(string name, Vector3 center, float height)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock($"{name}_Base", center + Vector3.up * 1.6f, new Vector3(2.8f, 3.2f, 2.8f), facing, new Color(0.17f, 0.15f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Top", center + Vector3.up * (height - 1.2f), new Vector3(2.2f, 2.4f, 2.2f), facing, new Color(0.15f, 0.13f, 0.14f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Roof", center + Vector3.up * height, new Vector3(3.1f, 0.34f, 3.1f), facing, new Color(0.11f, 0.1f, 0.11f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Banner", center + Vector3.up * (height - 0.2f), new Vector3(0.14f, 2.4f, 1.1f), facing, new Color(0.27f, 0.09f, 0.08f)).transform.SetParent(transform, true);
    }

    private void CreateBellHouse(string name, Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock($"{name}_Base", center + Vector3.up * 2.2f, new Vector3(3.2f, 4.4f, 3.4f), facing, new Color(0.16f, 0.14f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Roof", center + Vector3.up * 4.9f, new Vector3(4f, 0.4f, 4f), facing, new Color(0.12f, 0.1f, 0.1f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Bell", center + Vector3.up * 3.2f, new Vector3(0.7f, 1.1f, 0.7f), Quaternion.identity, new Color(0.46f, 0.34f, 0.18f)).transform.SetParent(transform, true);
    }

    private void CreateOverheadCrossing(string name, Vector3 center, float width, float height, Color clothColor)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock($"{name}_Beam", center + Vector3.up * height, new Vector3(width, 0.18f, 0.18f), facing, new Color(0.22f, 0.18f, 0.14f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_PostLeft", center - _streetRight * (width * 0.5f - 0.3f) + Vector3.up * (height - 1.2f), new Vector3(0.24f, 2.4f, 0.24f), facing, new Color(0.22f, 0.18f, 0.14f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_PostRight", center + _streetRight * (width * 0.5f - 0.3f) + Vector3.up * (height - 1.2f), new Vector3(0.24f, 2.4f, 0.24f), facing, new Color(0.22f, 0.18f, 0.14f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Cloth", center + Vector3.up * (height - 0.8f), new Vector3(0.1f, 1.6f, width - 1.2f), facing * Quaternion.Euler(0f, 90f, 0f), clothColor).transform.SetParent(transform, true);
    }

    private void CreateRoyalEdictBoard(string name, Vector3 center)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock($"{name}_Post", center + Vector3.up * 1.8f, new Vector3(0.22f, 3.6f, 0.22f), Quaternion.identity, new Color(0.24f, 0.18f, 0.14f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Board", center + Vector3.up * 2.3f, new Vector3(0.16f, 1.6f, 1.5f), Quaternion.LookRotation(_streetRight, Vector3.up), new Color(0.42f, 0.28f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Ribbon", center + Vector3.up * 2.8f, new Vector3(0.08f, 0.52f, 1.2f), facing * Quaternion.Euler(0f, 90f, 0f), new Color(0.36f, 0.1f, 0.09f)).transform.SetParent(transform, true);
    }

    private void CreateGuardOfficeLot(string name, Vector3 center, float sideSign)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock($"{name}_Base", center + Vector3.up * 2.2f, new Vector3(6.8f, 4.4f, 5.2f), facing, new Color(0.18f, 0.17f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Roof", center + Vector3.up * 4.7f, new Vector3(7.4f, 0.32f, 5.8f), facing, new Color(0.12f, 0.11f, 0.11f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Porch", center + _streetRight * sideSign * 2.9f + Vector3.up * 0.46f, new Vector3(2.1f, 0.92f, 2.8f), facing, new Color(0.28f, 0.23f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Door", center + _streetRight * sideSign * 2.76f + Vector3.up * 1.28f, new Vector3(0.16f, 2.3f, 1.28f), Quaternion.LookRotation(-_streetRight * sideSign, Vector3.up), new Color(0.23f, 0.17f, 0.13f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_RecordBoard", center + _streetRight * sideSign * 3.05f + _streetForward * 1.4f + Vector3.up * 1.9f, new Vector3(0.12f, 1.5f, 1.6f), Quaternion.LookRotation(-_streetRight * sideSign, Vector3.up), new Color(0.42f, 0.28f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_SignalBar", center + Vector3.up * 6.1f, new Vector3(4.2f, 0.18f, 0.18f), facing, new Color(0.24f, 0.19f, 0.15f)).transform.SetParent(transform, true);
    }

    private void CreateShutteredInnLot(string name, Vector3 center, float sideSign)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        Quaternion frontage = Quaternion.LookRotation(-_streetRight * sideSign, Vector3.up);
        CreateBlock($"{name}_Base", center + Vector3.up * 2.6f, new Vector3(7.6f, 5.2f, 6.6f), facing, new Color(0.22f, 0.18f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Roof", center + Vector3.up * 5.5f, new Vector3(8.2f, 0.36f, 7.2f), facing, new Color(0.16f, 0.1f, 0.1f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_FrontCanopy", center + _streetRight * sideSign * 3.5f + Vector3.up * 2.2f, new Vector3(0.24f, 0.28f, 4.4f), facing, new Color(0.38f, 0.12f, 0.1f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_SignBlade", center + _streetRight * sideSign * 4f + Vector3.up * 2.8f + _streetForward * 0.8f, new Vector3(0.14f, 1.6f, 1.1f), frontage, new Color(0.36f, 0.15f, 0.12f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_RearKitchen", center - _streetRight * sideSign * 2.6f + _streetForward * 1.6f + Vector3.up * 1.5f, new Vector3(2.4f, 3f, 2.2f), facing, new Color(0.18f, 0.15f, 0.14f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_PorchRail", center + _streetRight * sideSign * 3.1f + Vector3.up * 0.75f, new Vector3(0.12f, 0.8f, 4f), frontage, new Color(0.3f, 0.23f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_BoardedWindowA", center + _streetRight * sideSign * 3.18f - _streetForward * 1.2f + Vector3.up * 1.85f, new Vector3(0.12f, 1.5f, 1.2f), frontage, new Color(0.29f, 0.21f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_BoardedWindowB", center + _streetRight * sideSign * 3.18f + _streetForward * 1.2f + Vector3.up * 1.85f, new Vector3(0.12f, 1.5f, 1.2f), frontage, new Color(0.29f, 0.21f, 0.15f)).transform.SetParent(transform, true);
    }

    private void CreateApothecaryLot(string name, Vector3 center, float sideSign)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        Quaternion frontage = Quaternion.LookRotation(-_streetRight * sideSign, Vector3.up);
        CreateBlock($"{name}_Base", center + Vector3.up * 2.4f, new Vector3(6.2f, 4.8f, 5.6f), facing, new Color(0.19f, 0.17f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Roof", center + Vector3.up * 5f, new Vector3(6.8f, 0.34f, 6.2f), facing, new Color(0.13f, 0.11f, 0.11f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_SideLab", center - _streetRight * sideSign * 2.1f - _streetForward * 1.4f + Vector3.up * 1.6f, new Vector3(2f, 3.2f, 2.2f), facing, new Color(0.16f, 0.15f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_GlassBay", center + _streetRight * sideSign * 3.02f + Vector3.up * 1.8f, new Vector3(0.14f, 2.2f, 2.8f), frontage, new Color(0.5f, 0.58f, 0.6f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_HerbFrame", center + _streetRight * sideSign * 3.2f + _streetForward * 1.1f + Vector3.up * 2.3f, new Vector3(0.12f, 1.2f, 1.4f), frontage, new Color(0.24f, 0.42f, 0.22f)).transform.SetParent(transform, true);
    }

    private void CreateCollapsedResidenceLot(string name, Vector3 center, float sideSign)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock($"{name}_Base", center + Vector3.up * 2.2f, new Vector3(6.6f, 4.4f, 5.8f), facing, new Color(0.17f, 0.15f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_RemainingRoof", center - _streetForward * 0.8f + Vector3.up * 4.6f, new Vector3(5f, 0.3f, 4.2f), facing * Quaternion.Euler(0f, 0f, -8f), new Color(0.12f, 0.1f, 0.1f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_CollapseMassA", center + _streetRight * sideSign * 1.9f + _streetForward * 1.2f + Vector3.up * 1.1f, new Vector3(2.4f, 2.2f, 2.2f), facing * Quaternion.Euler(0f, 0f, 16f), new Color(0.23f, 0.18f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_CollapseMassB", center + _streetRight * sideSign * 3f + _streetForward * 1.9f + Vector3.up * 0.8f, new Vector3(2.2f, 1.6f, 1.8f), facing * Quaternion.Euler(0f, 0f, -18f), new Color(0.21f, 0.17f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_ExposedFrame", center + _streetRight * sideSign * 3.1f + Vector3.up * 2.4f, new Vector3(0.18f, 3.2f, 2.4f), Quaternion.LookRotation(-_streetRight * sideSign, Vector3.up), new Color(0.31f, 0.23f, 0.17f)).transform.SetParent(transform, true);
    }

    private void CreateSealedChapelLot(string name, Vector3 center, float sideSign)
    {
        Quaternion facing = Quaternion.LookRotation(_streetForward, Vector3.up);
        Quaternion frontage = Quaternion.LookRotation(-_streetRight * sideSign, Vector3.up);
        CreateBlock($"{name}_Nave", center + Vector3.up * 3f, new Vector3(7.2f, 6f, 6.2f), facing, new Color(0.16f, 0.15f, 0.16f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Roof", center + Vector3.up * 6.3f, new Vector3(7.8f, 0.36f, 6.8f), facing, new Color(0.11f, 0.09f, 0.1f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_FrontArch", center + _streetRight * sideSign * 3.55f + Vector3.up * 2.2f, new Vector3(0.22f, 3.8f, 2.4f), frontage, new Color(0.22f, 0.19f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_SealPanel", center + _streetRight * sideSign * 3.7f + Vector3.up * 1.9f, new Vector3(0.12f, 2.9f, 1.7f), frontage, new Color(0.42f, 0.12f, 0.1f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_SpireStub", center + Vector3.up * 8f, new Vector3(1.3f, 3.2f, 1.3f), facing, new Color(0.13f, 0.12f, 0.13f)).transform.SetParent(transform, true);
    }

    private void CreateBarrelStack(string name, Vector3 center)
    {
        CreateBlock($"{name}_A", center + Vector3.up * 0.42f, new Vector3(0.72f, 0.84f, 0.72f), Quaternion.identity, new Color(0.33f, 0.23f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_B", center + _streetRight * 0.82f + Vector3.up * 0.42f, new Vector3(0.72f, 0.84f, 0.72f), Quaternion.identity, new Color(0.33f, 0.23f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Top", center + _streetRight * 0.42f + Vector3.up * 1.16f, new Vector3(0.66f, 0.72f, 0.66f), Quaternion.identity, new Color(0.29f, 0.21f, 0.14f)).transform.SetParent(transform, true);
    }

    private void CreateBrazier(string name, Vector3 center, float flameScale)
    {
        CreateBlock($"{name}_Post", center + Vector3.up * 0.64f, new Vector3(0.2f, 1.28f, 0.2f), Quaternion.identity, new Color(0.24f, 0.2f, 0.18f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Bowl", center + Vector3.up * 1.26f, new Vector3(0.86f, 0.18f, 0.86f), Quaternion.identity, new Color(0.2f, 0.18f, 0.18f)).transform.SetParent(transform, true);
        GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flame.name = $"{name}_Flame";
        flame.transform.SetParent(transform, true);
        flame.transform.position = center + Vector3.up * 1.68f;
        flame.transform.localScale = new Vector3(0.38f, 0.52f, 0.38f) * flameScale;
        Tint(flame.GetComponent<Renderer>(), new Color(0.84f, 0.42f, 0.18f));
    }

    private void CreatePaperCluster(string name, Vector3 center)
    {
        CreateBlock($"{name}_A", center + Vector3.up * 0.05f, new Vector3(0.58f, 0.02f, 0.44f), Quaternion.LookRotation(_streetRight, Vector3.up), new Color(0.7f, 0.66f, 0.58f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_B", center + _streetRight * 0.34f + _streetForward * 0.18f + Vector3.up * 0.05f, new Vector3(0.46f, 0.02f, 0.32f), Quaternion.LookRotation(-_streetForward, Vector3.up), new Color(0.68f, 0.63f, 0.55f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_C", center - _streetRight * 0.28f - _streetForward * 0.16f + Vector3.up * 0.05f, new Vector3(0.4f, 0.02f, 0.28f), Quaternion.LookRotation(_streetForward, Vector3.up), new Color(0.74f, 0.69f, 0.6f)).transform.SetParent(transform, true);
    }

    private void CreateBrokenCrate(string name, Vector3 center)
    {
        Quaternion rotation = Quaternion.LookRotation(_streetForward, Vector3.up);
        CreateBlock($"{name}_Base", center + Vector3.up * 0.28f, new Vector3(1.2f, 0.56f, 1f), rotation, new Color(0.31f, 0.22f, 0.15f)).transform.SetParent(transform, true);
        CreateBlock($"{name}_Lid", center + _streetRight * 0.42f + Vector3.up * 0.66f, new Vector3(1f, 0.12f, 0.24f), rotation * Quaternion.Euler(0f, 0f, 24f), new Color(0.36f, 0.25f, 0.17f)).transform.SetParent(transform, true);
    }

    private void CreateFogBlob(string name, Vector3 position, float scale, Color color)
    {
        GameObject fogBlob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fogBlob.name = name;
        fogBlob.transform.SetParent(transform, true);
        fogBlob.transform.position = position;
        fogBlob.transform.localScale = Vector3.one * scale;
        Tint(fogBlob.GetComponent<Renderer>(), color);
    }

    private void CreateFogBeacon(string name, Vector3 position)
    {
        GameObject post = CreateBlock($"{name}_Post", position + Vector3.up * 1.2f, new Vector3(0.24f, 2.4f, 0.24f), Quaternion.identity, MarkerStoneColor);
        post.transform.SetParent(transform, true);

        GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lamp.name = $"{name}_Lamp";
        lamp.transform.SetParent(transform, true);
        lamp.transform.position = position + Vector3.up * 2.55f;
        lamp.transform.localScale = Vector3.one * 0.52f;
        Tint(lamp.GetComponent<Renderer>(), FogBeaconColor);
    }

    private void PlaceImportedDressing()
    {
        Quaternion forwardRotation = Quaternion.LookRotation(_streetForward, Vector3.up);

        PlaceContinuousStreetSurfaceImportedDressing(forwardRotation);
        PlaceDistrictSurfaceImportedDressing(forwardRotation);
        PlaceDistrictMassingImportedDressing();
        PlaceThresholdImportedDressing(forwardRotation);
        PlaceEntranceImportedDressing(forwardRotation);
        PlaceCheckpointImportedDressing(forwardRotation);
        PlaceNoticeImportedDressing(forwardRotation);
        PlaceGateImportedDressing(forwardRotation);
        PlaceFogImportedDressing(forwardRotation);
        PlaceHeroLotImportedDressing();
        PlaceRowClusterImportedDressing();
        PlacePerimeterWallImportedDressing();
        PlaceAlleyImportedDressing();
        PlaceStoryAnchorImportedDressing(forwardRotation);
        PlaceCivicStructureImportedDressing();
        PlaceForegroundImportedDressing();
    }

    private void PlacePixelFacadeDressing()
    {
        PlaceHeroLotPixelDressing();
        PlaceRowClusterPixelDressing();
    }

    private void PlaceHeroLotPixelDressing()
    {
        Vector3 guardOfficeCenter = _streetCenter - _streetRight * 14.4f - _streetForward * 8.2f;
        Vector3 tavernCenter = _streetCenter - _streetRight * 12.4f - _streetForward * 5.4f;
        Vector3 shutteredInnCenter = _streetCenter + _streetRight * 14.8f - _streetForward * 3.8f;
        Vector3 apothecaryCenter = _streetCenter - _streetRight * 14.5f + _streetForward * 8.6f;
        Vector3 collapsedResidenceCenter = _streetCenter + _streetRight * 14.8f + _streetForward * 15.8f;
        Vector3 sealedChapelCenter = _streetCenter - _streetRight * 14.6f + _streetForward * 23.4f;

        PlacePixelHouse(
            "Pixel_GuardOfficeHouse",
            guardOfficeCenter,
            true,
            5.4f,
            6.9f,
            5.2f,
            PixelFacadeFactory.FacadeStyle.GuardOffice);
        PlacePixelHouse(
            "Pixel_TavernHouse",
            tavernCenter,
            true,
            5.8f,
            7.4f,
            5.6f,
            PixelFacadeFactory.FacadeStyle.Tavern);
        PlacePixelHouse(
            "Pixel_ShutteredInnHouse",
            shutteredInnCenter,
            false,
            6.4f,
            7.8f,
            5.8f,
            PixelFacadeFactory.FacadeStyle.ShutteredInn);
        PlacePixelHouse(
            "Pixel_ApothecaryHouse",
            apothecaryCenter,
            true,
            5.6f,
            6.4f,
            5.4f,
            PixelFacadeFactory.FacadeStyle.Apothecary);
        PlacePixelHouse(
            "Pixel_CollapsedResidenceHouse",
            collapsedResidenceCenter,
            false,
            6.2f,
            6.8f,
            5.4f,
            PixelFacadeFactory.FacadeStyle.CollapsedResidence);
        PlacePixelHouse(
            "Pixel_SealedChapelHouse",
            sealedChapelCenter,
            true,
            6.6f,
            7.2f,
            7f,
            PixelFacadeFactory.FacadeStyle.Chapel);
    }

    private void PlaceRowClusterPixelDressing()
    {
        Vector3 westEarly = _streetCenter - _streetRight * BuildingRowOffset - _streetForward * 18.6f;
        Vector3 westMid = _streetCenter - _streetRight * BuildingRowOffset + _streetForward * 0.2f;
        Vector3 westLate = _streetCenter - _streetRight * BuildingRowOffset + _streetForward * 18.6f;
        Vector3 eastEarly = _streetCenter + _streetRight * BuildingRowOffset - _streetForward * 12.4f;
        Vector3 eastMid = _streetCenter + _streetRight * BuildingRowOffset + _streetForward * 6.2f;
        Vector3 eastLate = _streetCenter + _streetRight * BuildingRowOffset + _streetForward * 18.6f;

        PlacePixelHouse(
            "Pixel_WestRowEarlyHouse",
            westEarly,
            true,
            8.2f,
            5.8f,
            5f,
            PixelFacadeFactory.FacadeStyle.RowShop);
        PlacePixelHouse(
            "Pixel_WestRowMidHouse",
            westMid,
            true,
            8f,
            5.8f,
            4.9f,
            PixelFacadeFactory.FacadeStyle.RowResidence);
        PlacePixelHouse(
            "Pixel_WestRowLateHouse",
            westLate,
            true,
            8f,
            5.8f,
            5f,
            PixelFacadeFactory.FacadeStyle.RowQuarantine);
        PlacePixelHouse(
            "Pixel_EastRowEarlyHouse",
            eastEarly,
            false,
            8.2f,
            5.8f,
            5f,
            PixelFacadeFactory.FacadeStyle.RowShop);
        PlacePixelHouse(
            "Pixel_EastRowMidHouse",
            eastMid,
            false,
            8f,
            5.8f,
            4.9f,
            PixelFacadeFactory.FacadeStyle.RowResidence);
        PlacePixelHouse(
            "Pixel_EastRowLateHouse",
            eastLate,
            false,
            8f,
            5.8f,
            5f,
            PixelFacadeFactory.FacadeStyle.RowQuarantine);
    }

    private void PlaceContinuousStreetSurfaceImportedDressing(Quaternion forwardRotation)
    {
        float[] segmentCenters = { -18f, -12f, -6f, 0f, 6f, 12f, 18f, 24f, 30f };

        for (int i = 0; i < segmentCenters.Length; i++)
        {
            float z = segmentCenters[i];
            Vector3 streetCenter = _streetCenter + _streetForward * z + Vector3.up * 0.045f;
            Vector3 leftWalkway = _streetCenter - _streetRight * WalkwayOffset + _streetForward * z + Vector3.up * 0.05f;
            Vector3 rightWalkway = _streetCenter + _streetRight * WalkwayOffset + _streetForward * z + Vector3.up * 0.05f;
            Vector3 westServiceLane = _streetCenter - _streetRight * (WalkwayOffset + 4.4f) + _streetForward * z + Vector3.up * 0.04f;
            Vector3 eastServiceLane = _streetCenter + _streetRight * (WalkwayOffset + 4.4f) + _streetForward * z + Vector3.up * 0.04f;

            string streetAsset = z >= 20f
                ? "Imported/Kenney/GraveyardKit/road"
                : z <= -8f
                    ? "Imported/Quaternius/MedievalVillage/FBX/Floor_WoodDark"
                    : "Imported/Quaternius/MedievalVillage/FBX/Floor_UnevenBrick";
            string walkwayAsset = z >= 20f
                ? "Imported/Quaternius/MedievalVillage/FBX/Floor_UnevenBrick"
                : "Imported/Quaternius/MedievalVillage/FBX/Floor_RedBrick";
            string serviceAsset = z >= 16f
                ? "Imported/Quaternius/MedievalVillage/FBX/Floor_UnevenBrick"
                : "Imported/Quaternius/MedievalVillage/FBX/Floor_WoodDark";

            RuntimeModelSpawner.Spawn(
                streetAsset,
                $"Imported_BoulevardBand_{i + 1}",
                transform,
                streetCenter,
                forwardRotation,
                new Vector3(4.05f, 1f, 3.2f));
            RuntimeModelSpawner.Spawn(
                walkwayAsset,
                $"Imported_LeftWalkwayBand_{i + 1}",
                transform,
                leftWalkway,
                forwardRotation,
                new Vector3(2.25f, 1f, 3.2f));
            RuntimeModelSpawner.Spawn(
                walkwayAsset,
                $"Imported_RightWalkwayBand_{i + 1}",
                transform,
                rightWalkway,
                forwardRotation,
                new Vector3(2.25f, 1f, 3.2f));
            RuntimeModelSpawner.Spawn(
                serviceAsset,
                $"Imported_WestServiceBand_{i + 1}",
                transform,
                westServiceLane,
                forwardRotation,
                new Vector3(2.55f, 1f, 3.2f));
            RuntimeModelSpawner.Spawn(
                serviceAsset,
                $"Imported_EastServiceBand_{i + 1}",
                transform,
                eastServiceLane,
                forwardRotation,
                new Vector3(2.55f, 1f, 3.2f));
        }

        Quaternion crossRotation = forwardRotation * Quaternion.Euler(0f, 90f, 0f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/road",
            "Imported_CrossStreet_Mid",
            transform,
            _streetCenter + _streetForward * 5.8f + Vector3.up * 0.04f,
            crossRotation,
            new Vector3(7.2f, 1f, 1.7f));
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/road",
            "Imported_CrossStreet_Gate",
            transform,
            _streetCenter + _streetForward * 17.8f + Vector3.up * 0.04f,
            crossRotation,
            new Vector3(7.2f, 1f, 1.7f));
    }

    private void PlaceDistrictSurfaceImportedDressing(Quaternion forwardRotation)
    {
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Floor_WoodDark",
            "Imported_StartCourtSurface",
            transform,
            _streetCenter - _streetForward * 11.1f + Vector3.up * 0.045f,
            forwardRotation,
            new Vector3(3.6f, 1f, 2.8f));
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Floor_Brick",
            "Imported_CheckpointSurface",
            transform,
            _streetCenter - _streetForward * 6.8f + Vector3.up * 0.04f,
            forwardRotation,
            new Vector3(3.8f, 1f, 3.1f));
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Floor_RedBrick",
            "Imported_NoticePlazaSurface",
            transform,
            _streetCenter + _streetForward * 7.8f + Vector3.up * 0.04f,
            forwardRotation,
            new Vector3(4.6f, 1f, 3.4f));
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Floor_UnevenBrick",
            "Imported_IncidentSurface",
            transform,
            _streetCenter + _streetForward * 15.2f + Vector3.up * 0.04f,
            forwardRotation,
            new Vector3(4.1f, 1f, 3.2f));
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Floor_UnevenBrick",
            "Imported_GateForecourtSurface",
            transform,
            _streetCenter + _streetForward * 22.6f + Vector3.up * 0.045f,
            forwardRotation,
            new Vector3(4.8f, 1f, 3.8f));
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Floor_RedBrick",
            "Imported_QuarantineSurface",
            transform,
            _streetCenter + _streetForward * 27.4f + Vector3.up * 0.045f,
            forwardRotation,
            new Vector3(4.2f, 1f, 3.1f));
    }

    private void PlaceDistrictMassingImportedDressing()
    {
        PlaceEntryBlockSet("Imported_EntryWestBlock", _streetCenter - _streetRight * 10.8f - _streetForward * 10.1f, true, 0.98f);
        PlaceEntryBlockSet("Imported_EntryEastBlock", _streetCenter + _streetRight * 10.8f - _streetForward * 10.1f, false, 0.98f);
        PlacePlazaBlockSet("Imported_PlazaWestBlock", _streetCenter - _streetRight * 10.9f + _streetForward * 6.9f, true, 1f);
        PlacePlazaBlockSet("Imported_PlazaEastBlock", _streetCenter + _streetRight * 10.9f + _streetForward * 8.4f, false, 1f);
        PlaceQuarantineBlockSet("Imported_GateWestBlock", _streetCenter - _streetRight * 10.8f + _streetForward * 22.4f, true, 0.98f);
        PlaceQuarantineBlockSet("Imported_GateEastBlock", _streetCenter + _streetRight * 10.8f + _streetForward * 22.9f, false, 0.98f);
        PlaceReliefStationImportedSet("Imported_ReliefStation", _streetCenter - _streetRight * 9.2f - _streetForward * 2.7f);
    }

    private void PlaceThresholdImportedDressing(Quaternion forwardRotation)
    {
        Vector3 startCenter = _streetCenter - _streetForward * 12.4f;
        Vector3 gateCenter = _streetCenter + _streetForward * (GateDistance + 1.6f);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_Support",
            "Imported_StartSupportLeft",
            transform,
            startCenter - _streetRight * 5.15f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_streetForward, Vector3.up),
            Vector3.one * 1.12f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_Support",
            "Imported_StartSupportRight",
            transform,
            startCenter + _streetRight * 5.15f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_streetForward, Vector3.up),
            Vector3.one * 1.12f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_FrontSupports",
            "Imported_StartLintel",
            transform,
            startCenter + Vector3.up * 3.55f,
            forwardRotation,
            Vector3.one * 1.06f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Banner_2_Cloth",
            "Imported_StartBanner",
            transform,
            startCenter + Vector3.up * 2.85f,
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one * 1.05f);

        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/pillar-square",
            "Imported_GatePillarLeft",
            transform,
            gateCenter - _streetRight * 2.75f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 1.08f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/pillar-square",
            "Imported_GatePillarRight",
            transform,
            gateCenter + _streetRight * 2.75f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 1.08f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_FrontSupports",
            "Imported_GateLintel",
            transform,
            gateCenter + Vector3.up * 3.95f,
            forwardRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Banner_1_Cloth",
            "Imported_GateBanner",
            transform,
            gateCenter + Vector3.up * 3.18f,
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one);
    }

    private void PlaceEntryBlockSet(string prefix, Vector3 center, bool westSide, float scale)
    {
        Vector3 facadeOffset = _streetRight * (westSide ? 3.15f : -3.15f);
        Vector3 sideOffset = _streetForward * 2.6f;
        Quaternion facadeRotation = Quaternion.LookRotation(westSide ? _streetRight : -_streetRight, Vector3.up);
        Quaternion oppositeRotation = facadeRotation * Quaternion.Euler(0f, 180f, 0f);
        Quaternion roofRotation = Quaternion.LookRotation(_streetForward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Arch",
            $"{prefix}_Arch",
            transform,
            center + facadeOffset + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Window_Wide_Round",
            $"{prefix}_WindowFront",
            transform,
            center + facadeOffset + sideOffset + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Straight",
            $"{prefix}_RearWall",
            transform,
            center + facadeOffset - sideOffset + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Corner_ExteriorWide_Wood",
            $"{prefix}_FrontCorner",
            transform,
            center + _streetRight * (westSide ? 2.3f : -2.3f) + _streetForward * 4f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Corner_ExteriorWide_Wood",
            $"{prefix}_RearCorner",
            transform,
            center + _streetRight * (westSide ? 2.3f : -2.3f) - _streetForward * 4f + Vector3.up * 0.02f,
            oppositeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_RoundTiles_8x14",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * 5.45f,
            roofRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Overhang_Plaster_Long",
            $"{prefix}_Overhang",
            transform,
            center + facadeOffset * 1.06f + Vector3.up * 2.25f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Stairs_Exterior_Straight",
            $"{prefix}_Steps",
            transform,
            center + facadeOffset * 1.18f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * 0.84f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_ExteriorBorder_Straight2",
            $"{prefix}_Border",
            transform,
            center + facadeOffset * 1.35f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
    }

    private void PlacePlazaBlockSet(string prefix, Vector3 center, bool westSide, float scale)
    {
        Vector3 facadeOffset = _streetRight * (westSide ? 3.2f : -3.2f);
        Quaternion facadeRotation = Quaternion.LookRotation(westSide ? _streetRight : -_streetRight, Vector3.up);
        Quaternion oppositeRotation = facadeRotation * Quaternion.Euler(0f, 180f, 0f);
        Quaternion roofRotation = Quaternion.LookRotation(_streetForward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Door_RoundInset",
            $"{prefix}_DoorWall",
            transform,
            center + facadeOffset + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Window_Wide_Flat2",
            $"{prefix}_FrontWindow",
            transform,
            center + facadeOffset + _streetForward * 2.6f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Window_Wide_Round",
            $"{prefix}_RearWindow",
            transform,
            center + facadeOffset - _streetForward * 2.6f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Corner_Exterior_Wood",
            $"{prefix}_CornerFront",
            transform,
            center + _streetRight * (westSide ? 2.45f : -2.45f) + _streetForward * 4.15f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Corner_Exterior_Wood",
            $"{prefix}_CornerRear",
            transform,
            center + _streetRight * (westSide ? 2.45f : -2.45f) - _streetForward * 4.15f + Vector3.up * 0.02f,
            oppositeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_RoundTiles_8x12",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * 5.25f,
            roofRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Balcony_Simple_Straight",
            $"{prefix}_Balcony",
            transform,
            center + facadeOffset * 1.1f + Vector3.up * 2.8f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_ExteriorBorder_Straight1",
            $"{prefix}_BaseBorder",
            transform,
            center + facadeOffset * 1.34f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
    }

    private void PlaceQuarantineBlockSet(string prefix, Vector3 center, bool westSide, float scale)
    {
        Vector3 facadeOffset = _streetRight * (westSide ? 3.1f : -3.1f);
        Quaternion facadeRotation = Quaternion.LookRotation(westSide ? _streetRight : -_streetRight, Vector3.up);
        Quaternion oppositeRotation = facadeRotation * Quaternion.Euler(0f, 180f, 0f);
        Quaternion roofRotation = Quaternion.LookRotation(_streetForward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Door_Round",
            $"{prefix}_DoorWall",
            transform,
            center + facadeOffset + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Window_Wide_Round",
            $"{prefix}_FrontWall",
            transform,
            center + facadeOffset + _streetForward * 2.4f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Straight",
            $"{prefix}_RearWall",
            transform,
            center + facadeOffset - _streetForward * 2.5f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Corner_ExteriorWide_Brick",
            $"{prefix}_FrontCorner",
            transform,
            center + _streetRight * (westSide ? 2.3f : -2.3f) + _streetForward * 4.05f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Corner_ExteriorWide_Brick",
            $"{prefix}_RearCorner",
            transform,
            center + _streetRight * (westSide ? 2.3f : -2.3f) - _streetForward * 4.05f + Vector3.up * 0.02f,
            oppositeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_Front_Brick8",
            $"{prefix}_RoofFront",
            transform,
            center + Vector3.up * 5.05f,
            roofRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Overhang_UnevenBrick_Long",
            $"{prefix}_Overhang",
            transform,
            center + facadeOffset * 1.05f + Vector3.up * 2.15f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_ExteriorBorder_Straight2",
            $"{prefix}_Border",
            transform,
            center + facadeOffset * 1.28f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/iron-fence-border-column",
            $"{prefix}_SealColumn",
            transform,
            center + facadeOffset * 1.42f + _streetForward * 1.55f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * 0.92f);
    }

    private void PlaceReliefStationImportedSet(string prefix, Vector3 center)
    {
        Quaternion forwardRotation = Quaternion.LookRotation(_streetForward, Vector3.up);
        Quaternion sideRotation = Quaternion.LookRotation(_streetRight, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Stairs_Exterior_Platform",
            $"{prefix}_Deck",
            transform,
            center + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.86f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_FrontSupports",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * 2.4f,
            forwardRotation,
            Vector3.one * 0.88f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Stall_Empty",
            $"{prefix}_Stall",
            transform,
            center + Vector3.up * 0.02f,
            sideRotation,
            Vector3.one * 0.92f);
    }

    private void PlaceEntranceImportedDressing(Quaternion forwardRotation)
    {
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/lightpost-single",
            "Imported_EntrancePostLeft",
            transform,
            _streetCenter - _streetRight * 5.25f - _streetForward * 9.2f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.98f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/lightpost-single",
            "Imported_EntrancePostRight",
            transform,
            _streetCenter + _streetRight * 5.25f - _streetForward * 8.4f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.98f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_WoodenFence_Extension1",
            "Imported_EntranceFenceLeft",
            transform,
            _streetCenter - _streetRight * 7.2f - _streetForward * 10.1f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_WoodenFence_Extension1",
            "Imported_EntranceFenceRight",
            transform,
            _streetCenter + _streetRight * 7.2f - _streetForward * 9.7f + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_streetRight, Vector3.up),
            Vector3.one);
    }

    private void PlaceCheckpointImportedDressing(Quaternion forwardRotation)
    {
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_Wagon",
            "Imported_CheckpointWagon",
            transform,
            _streetCenter - _streetRight * 7.8f - _streetForward * 5.6f + Vector3.up * 0.12f,
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Crate_Wooden",
            "Imported_CheckpointCrateA",
            transform,
            _streetCenter - _streetRight * 8.7f - _streetForward * 3.3f + Vector3.up * 0.12f,
            forwardRotation,
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/FarmCrate_Empty",
            "Imported_CheckpointCrateB",
            transform,
            _streetCenter - _streetRight * 7.4f - _streetForward * 2.4f + Vector3.up * 0.12f,
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one * 0.95f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Lantern_Wall",
            "Imported_CheckpointLanternLeft",
            transform,
            _streetCenter - _streetRight * 5.3f - _streetForward * 8.8f + Vector3.up * 2.1f,
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one * 0.85f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Lantern_Wall",
            "Imported_CheckpointLanternRight",
            transform,
            _streetCenter + _streetRight * 5.2f - _streetForward * 8.2f + Vector3.up * 2.1f,
            Quaternion.LookRotation(-_streetRight, Vector3.up),
            Vector3.one * 0.85f);
    }

    private void PlaceNoticeImportedDressing(Quaternion forwardRotation)
    {
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Bench",
            "Imported_NoticeBenchLeft",
            transform,
            _streetCenter - _streetRight * 4.3f + _streetForward * 8.4f + Vector3.up * 0.04f,
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Bench",
            "Imported_NoticeBenchRight",
            transform,
            _streetCenter + _streetRight * 4.1f + _streetForward * 8.2f + Vector3.up * 0.04f,
            Quaternion.LookRotation(-_streetRight, Vector3.up),
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Barrel",
            "Imported_IncidentBarrel",
            transform,
            _streetCenter + _streetRight * 3.8f + _streetForward * 14.2f + Vector3.up * 0.12f,
            forwardRotation,
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Bag",
            "Imported_IncidentBag",
            transform,
            _streetCenter - _streetRight * 2.9f + _streetForward * 15.4f + Vector3.up * 0.08f,
            forwardRotation,
            Vector3.one * 0.88f);
    }

    private void PlaceGateImportedDressing(Quaternion forwardRotation)
    {
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_WoodenFence_Single",
            "Imported_GateFenceLeft",
            transform,
            _streetCenter - _streetRight * 6.8f + _streetForward * 21.5f + Vector3.up * 0.05f,
            forwardRotation,
            Vector3.one * 1.05f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_WoodenFence_Single",
            "Imported_GateFenceRight",
            transform,
            _streetCenter + _streetRight * 6.8f + _streetForward * 22.2f + Vector3.up * 0.05f,
            forwardRotation,
            Vector3.one * 1.05f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Lantern_Wall",
            "Imported_GateLanternLeft",
            transform,
            _streetCenter - _streetRight * 4.8f + _streetForward * 22.9f + Vector3.up * 2.2f,
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Lantern_Wall",
            "Imported_GateLanternRight",
            transform,
            _streetCenter + _streetRight * 4.8f + _streetForward * 23f + Vector3.up * 2.2f,
            Quaternion.LookRotation(-_streetRight, Vector3.up),
            Vector3.one * 0.9f);
    }

    private void PlaceFogImportedDressing(Quaternion forwardRotation)
    {
        Vector3 quarantineCenter = _streetCenter + _streetForward * 24.8f;
        Vector3 funnelCenter = _streetCenter + _streetForward * 27.8f;

        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/iron-fence-border",
            "Imported_FogFenceLeft",
            transform,
            _streetCenter - _streetRight * 7.4f + _streetForward * 28.4f + Vector3.up * 0.04f,
            forwardRotation,
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/iron-fence-border",
            "Imported_FogFenceRight",
            transform,
            _streetCenter + _streetRight * 7.4f + _streetForward * 28.8f + Vector3.up * 0.04f,
            forwardRotation,
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/gravestone-cross-large",
            "Imported_FogMarkerStoneLeft",
            transform,
            _streetCenter - _streetRight * 6.1f + _streetForward * 30.2f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/gravestone-round",
            "Imported_FogMarkerStoneRight",
            transform,
            _streetCenter + _streetRight * 6.3f + _streetForward * 30.8f + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_streetRight, Vector3.up),
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/fire-basket",
            "Imported_FogBasketLeft",
            transform,
            _streetCenter - _streetRight * 4.8f + _streetForward * 28.6f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.95f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/fire-basket",
            "Imported_FogBasketRight",
            transform,
            _streetCenter + _streetRight * 4.8f + _streetForward * 28.9f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.95f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/altar-stone",
            "Imported_FogRitualStone",
            transform,
            _streetCenter + _streetForward * 30.9f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.95f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/pine-crooked",
            "Imported_FogTreeLeft",
            transform,
            _streetCenter - _streetRight * 11.8f + _streetForward * 32.2f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 1.15f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/pine-crooked",
            "Imported_FogTreeRight",
            transform,
            _streetCenter + _streetRight * 11.4f + _streetForward * 31.8f + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_streetForward, Vector3.up),
            Vector3.one * 1.08f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/rocks-tall",
            "Imported_FogRocksLeft",
            transform,
            _streetCenter - _streetRight * 9.7f + _streetForward * 33.1f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/coffin-old",
            "Imported_FogCoffin",
            transform,
            _streetCenter + _streetRight * 7.6f + _streetForward * 29.7f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one * 0.88f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/iron-fence-border",
            "Imported_QuarantineFenceLeft",
            transform,
            quarantineCenter - _streetRight * 5.45f + Vector3.up * 0.04f,
            forwardRotation,
            Vector3.one * 0.98f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/iron-fence-border",
            "Imported_QuarantineFenceRight",
            transform,
            quarantineCenter + _streetRight * 5.45f + Vector3.up * 0.04f,
            forwardRotation,
            Vector3.one * 0.98f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/iron-fence-border-column",
            "Imported_QuarantineColumnLeft",
            transform,
            quarantineCenter - _streetRight * 5.95f - _streetForward * 2.2f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/iron-fence-border-column",
            "Imported_QuarantineColumnRight",
            transform,
            quarantineCenter + _streetRight * 5.95f + _streetForward * 2f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/crypt-small",
            "Imported_FunnelCryptLeft",
            transform,
            funnelCenter - _streetRight * 6.4f + _streetForward * 0.6f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/crypt-small",
            "Imported_FunnelCryptRight",
            transform,
            funnelCenter + _streetRight * 6.5f + _streetForward * 1f + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_streetRight, Vector3.up),
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/fence-gate",
            "Imported_FunnelFenceLeft",
            transform,
            funnelCenter - _streetRight * 4.9f - _streetForward * 0.4f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_streetForward, Vector3.up),
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/fence-gate",
            "Imported_FunnelFenceRight",
            transform,
            funnelCenter + _streetRight * 4.9f - _streetForward * 0.1f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_streetForward, Vector3.up),
            Vector3.one * 0.9f);
    }

    private void PlaceHeroLotImportedDressing()
    {
        Vector3 guardOfficeCenter = _streetCenter - _streetRight * 14.4f - _streetForward * 8.2f;
        Vector3 tavernCenter = _streetCenter - _streetRight * 12.4f - _streetForward * 5.4f;
        Vector3 shutteredInnCenter = _streetCenter + _streetRight * 14.8f - _streetForward * 3.8f;
        Vector3 apothecaryCenter = _streetCenter - _streetRight * 14.5f + _streetForward * 8.6f;
        Vector3 collapsedResidenceCenter = _streetCenter + _streetRight * 14.8f + _streetForward * 15.8f;
        Vector3 sealedChapelCenter = _streetCenter - _streetRight * 14.6f + _streetForward * 23.4f;

        PlaceCompleteHeroHouseShell("Imported_GuardOfficeHouse", guardOfficeCenter, true, 0.94f, false);
        PlaceWestFacadeSet("Imported_GuardOfficeFacade", guardOfficeCenter, 0.94f, false);

        PlaceCompleteHeroHouseShell("Imported_TavernHouse", tavernCenter, true, 1.02f, false);
        PlaceWestFacadeSet("Imported_TavernFacade", tavernCenter, 1.02f, true);

        PlaceCompleteHeroHouseShell("Imported_ShutteredInnHouse", shutteredInnCenter, false, 1.04f, false);
        PlaceEastFacadeSet("Imported_ShutteredInnFacade", shutteredInnCenter, 1.04f, false);

        PlaceCompleteHeroHouseShell("Imported_ApothecaryHouse", apothecaryCenter, true, 0.96f, false);
        PlaceWestFacadeSet("Imported_ApothecaryFacade", apothecaryCenter, 0.96f, false);

        PlaceCompleteHeroHouseShell("Imported_CollapsedResidenceHouse", collapsedResidenceCenter, false, 1f, true);
        PlaceEastCollapsedSet("Imported_CollapsedResidenceFacade", collapsedResidenceCenter);

        PlaceCompleteHeroHouseShell("Imported_SealedChapelHouse", sealedChapelCenter, true, 1.06f, true);
        PlaceWestChapelSet("Imported_SealedChapelFacade", sealedChapelCenter);
    }

    private void PlaceCompleteHeroHouseShell(string prefix, Vector3 center, bool westSide, float scale, bool quarantineDistrict)
    {
        Vector3 streetFacing = westSide ? _streetRight : -_streetRight;
        Quaternion houseRotation = Quaternion.LookRotation(_streetForward, Vector3.up);
        Quaternion facadeRotation = Quaternion.LookRotation(streetFacing, Vector3.up);
        float bodyWidth = 4.5f * scale;
        float bodyHeight = quarantineDistrict ? 4.6f * scale : 4.9f * scale;
        float bodyDepth = 7.2f * scale;
        float annexWidth = 2.2f * scale;
        float annexHeight = 3.1f * scale;
        float annexDepth = 2.9f * scale;
        string wallTheme = quarantineDistrict ? "village-brick" : "village-plaster";
        string baseTheme = quarantineDistrict ? "village-brick" : "village-wood";
        string roofAsset = quarantineDistrict
            ? "Imported/Quaternius/MedievalVillage/FBX/Roof_Front_Brick8"
            : "Imported/Quaternius/MedievalVillage/FBX/Roof_RoundTiles_8x14";
        string annexRoofAsset = quarantineDistrict
            ? "Imported/Quaternius/MedievalVillage/FBX/Roof_Front_Brick4"
            : "Imported/Quaternius/MedievalVillage/FBX/Roof_RoundTiles_6x6";
        string overhangAsset = quarantineDistrict
            ? "Imported/Quaternius/MedievalVillage/FBX/Overhang_UnevenBrick_Long"
            : "Imported/Quaternius/MedievalVillage/FBX/Overhang_Plaster_Long";

        CreateImportedMaterialBlock(
            $"{prefix}_Foundation",
            center + streetFacing * 0.08f + Vector3.up * 0.18f,
            new Vector3(bodyWidth + 0.55f, 0.36f, bodyDepth + 0.55f),
            houseRotation,
            baseTheme).transform.SetParent(transform, true);
        CreateImportedMaterialBlock(
            $"{prefix}_Body",
            center + streetFacing * 0.08f + Vector3.up * (bodyHeight * 0.5f),
            new Vector3(bodyWidth, bodyHeight, bodyDepth),
            houseRotation,
            wallTheme).transform.SetParent(transform, true);
        CreateImportedMaterialBlock(
            $"{prefix}_RearAnnex",
            center - streetFacing * 1.28f + _streetForward * 1f + Vector3.up * (annexHeight * 0.5f),
            new Vector3(annexWidth, annexHeight, annexDepth),
            houseRotation,
            wallTheme).transform.SetParent(transform, true);
        CreateImportedMaterialBlock(
            $"{prefix}_PorchPad",
            center + streetFacing * (bodyWidth * 0.48f + 0.45f) + Vector3.up * 0.12f,
            new Vector3(1.8f * scale, 0.24f, 2.2f * scale),
            houseRotation,
            baseTheme).transform.SetParent(transform, true);

        RuntimeModelSpawner.Spawn(
            roofAsset,
            $"{prefix}_RoofMain",
            transform,
            center + streetFacing * 0.08f + Vector3.up * (bodyHeight + 0.65f),
            houseRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            annexRoofAsset,
            $"{prefix}_RoofRear",
            transform,
            center - streetFacing * 1.28f + _streetForward * 1f + Vector3.up * (annexHeight + 0.58f),
            houseRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            overhangAsset,
            $"{prefix}_Overhang",
            transform,
            center + streetFacing * (bodyWidth * 0.52f + 0.1f) + Vector3.up * 2.2f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            quarantineDistrict
                ? "Imported/Quaternius/MedievalVillage/FBX/Prop_Chimney2"
                : "Imported/Quaternius/MedievalVillage/FBX/Prop_Chimney",
            $"{prefix}_Chimney",
            transform,
            center - _streetForward * 1.1f + Vector3.up * (bodyHeight + 1.4f),
            houseRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            westSide
                ? "Imported/Quaternius/MedievalVillage/FBX/Stairs_Exterior_Straight_R"
                : "Imported/Quaternius/MedievalVillage/FBX/Stairs_Exterior_Straight_L",
            $"{prefix}_Steps",
            transform,
            center + streetFacing * (bodyWidth * 0.5f + 0.42f) + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * 0.92f * scale);
    }

    private void PlaceStoryAnchorImportedDressing(Quaternion forwardRotation)
    {
        Vector3 supplyCenter = _streetCenter + _streetRight * 7.4f + _streetForward * 4.1f;
        Vector3 memorialCenter = _streetCenter - _streetRight * 7.8f + _streetForward * 21.4f;
        Vector3 checkpointCenter = _streetCenter - _streetRight * 6.8f + _streetForward * 8.8f;

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Stall_Empty",
            "Imported_SupplyStall",
            transform,
            supplyCenter + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_streetRight, Vector3.up),
            Vector3.one * 0.95f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/FarmCrate_Apple",
            "Imported_SupplyApples",
            transform,
            supplyCenter - _streetForward * 0.9f + Vector3.up * 0.08f,
            forwardRotation,
            Vector3.one * 0.95f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/FarmCrate_Carrot",
            "Imported_SupplyCarrots",
            transform,
            supplyCenter + _streetForward * 0.85f + Vector3.up * 0.08f,
            forwardRotation,
            Vector3.one * 0.95f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Barrel_Holder",
            "Imported_SupplyBarrelRack",
            transform,
            supplyCenter + _streetRight * 1.2f + Vector3.up * 0.06f,
            Quaternion.LookRotation(_streetForward, Vector3.up),
            Vector3.one * 0.9f);

        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/altar-stone",
            "Imported_MemorialAltar",
            transform,
            memorialCenter + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.88f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/candle-multiple",
            "Imported_MemorialCandlesLeft",
            transform,
            memorialCenter - _streetRight * 0.9f + _streetForward * 0.6f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.88f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/candle-multiple",
            "Imported_MemorialCandlesRight",
            transform,
            memorialCenter + _streetRight * 0.85f + _streetForward * 0.55f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.88f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/grave-border",
            "Imported_MemorialBorder",
            transform,
            memorialCenter - _streetForward * 1.35f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.84f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/gravestone-decorative",
            "Imported_MemorialStone",
            transform,
            memorialCenter - _streetForward * 0.9f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.9f);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Workbench_Drawers",
            "Imported_CheckpointWorkbench",
            transform,
            checkpointCenter - _streetRight * 7.25f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_streetForward, Vector3.up),
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Shield_Wooden",
            "Imported_CheckpointShield",
            transform,
            checkpointCenter - _streetRight * 5.6f + _streetForward * 1.6f + Vector3.up * 1.35f,
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/WeaponStand",
            "Imported_CheckpointWeapons",
            transform,
            checkpointCenter + _streetRight * 6.1f + _streetForward * 1.2f + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_streetRight, Vector3.up),
            Vector3.one * 0.9f);
    }

    private void PlaceRowClusterImportedDressing()
    {
        Vector3 westEarly = _streetCenter - _streetRight * BuildingRowOffset - _streetForward * 18.6f;
        Vector3 westMid = _streetCenter - _streetRight * BuildingRowOffset + _streetForward * 0.2f;
        Vector3 westLate = _streetCenter - _streetRight * BuildingRowOffset + _streetForward * 18.6f;
        Vector3 eastEarly = _streetCenter + _streetRight * BuildingRowOffset - _streetForward * 12.4f;
        Vector3 eastMid = _streetCenter + _streetRight * BuildingRowOffset + _streetForward * 6.2f;
        Vector3 eastLate = _streetCenter + _streetRight * BuildingRowOffset + _streetForward * 18.6f;

        PlaceCompleteHeroHouseShell("Imported_WestRowEarlyHouse", westEarly, true, 0.88f, false);
        PlaceWestFacadeSet("Imported_WestRowEarlyFacade", westEarly, 0.88f, true);

        PlaceCompleteHeroHouseShell("Imported_WestRowMidHouse", westMid, true, 0.9f, false);
        PlaceWestFacadeSet("Imported_WestRowMidFacade", westMid, 0.9f, false);

        PlaceCompleteHeroHouseShell("Imported_WestRowLateHouse", westLate, true, 0.9f, true);
        PlaceWestFacadeSet("Imported_WestRowLateFacade", westLate, 0.9f, false);

        PlaceCompleteHeroHouseShell("Imported_EastRowEarlyHouse", eastEarly, false, 0.88f, false);
        PlaceEastFacadeSet("Imported_EastRowEarlyFacade", eastEarly, 0.88f, true);

        PlaceCompleteHeroHouseShell("Imported_EastRowMidHouse", eastMid, false, 0.9f, false);
        PlaceEastFacadeSet("Imported_EastRowMidFacade", eastMid, 0.9f, false);

        PlaceCompleteHeroHouseShell("Imported_EastRowLateHouse", eastLate, false, 0.9f, true);
        PlaceEastFacadeSet("Imported_EastRowLateFacade", eastLate, 0.9f, false);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_MetalFence_Simple",
            "Imported_WestRowFence",
            transform,
            _streetCenter - _streetRight * 10.6f + _streetForward * 17.1f + Vector3.up * 0.04f,
            Quaternion.LookRotation(_streetForward, Vector3.up),
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_MetalFence_Simple",
            "Imported_EastRowFence",
            transform,
            _streetCenter + _streetRight * 10.6f + _streetForward * 18.3f + Vector3.up * 0.04f,
            Quaternion.LookRotation(_streetForward, Vector3.up),
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Crate_Wooden",
            "Imported_WestRowCrate",
            transform,
            westMid + _streetRight * 2.4f - _streetForward * 1.1f + Vector3.up * 0.08f,
            Quaternion.LookRotation(_streetForward, Vector3.up),
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Barrel",
            "Imported_EastRowBarrel",
            transform,
            eastLate - _streetRight * 2.6f + _streetForward * 1.4f + Vector3.up * 0.08f,
            Quaternion.LookRotation(_streetForward, Vector3.up),
            Vector3.one * 0.84f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/gravestone-broken",
            "Imported_WestLateSealStone",
            transform,
            westLate + _streetRight * 2.2f + _streetForward * 1.3f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one * 0.82f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/gravestone-broken",
            "Imported_EastLateSealStone",
            transform,
            eastLate - _streetRight * 2.2f + _streetForward * 1f + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_streetRight, Vector3.up),
            Vector3.one * 0.82f);
    }

    private void PlacePixelHouse(
        string name,
        Vector3 center,
        bool westSide,
        float frontSpan,
        float thickness,
        float height,
        PixelFacadeFactory.FacadeStyle style)
    {
        PixelFacadeFactory.CreateHouse(
            name,
            transform,
            center,
            _streetForward,
            _streetRight,
            westSide,
            frontSpan,
            thickness,
            height,
            style);
    }

    private void PlaceCivicStructureImportedDressing()
    {
        PlaceWatchTowerImportedSet("Imported_WestWatchTowerSet", _streetCenter - _streetRight * 18.6f - _streetForward * 6.4f, 8.8f);
        PlaceWatchTowerImportedSet("Imported_EastWatchTowerSet", _streetCenter + _streetRight * 18.2f + _streetForward * 18.4f, 9.8f);
        PlaceBellHouseImportedSet("Imported_BellHouseSet", _streetCenter - _streetRight * 17.4f + _streetForward * 12.8f);
        PlaceCrossingImportedSet("Imported_MarketCrossingSet", _streetCenter + _streetForward * 6.8f, 4.8f, 11.6f);
        PlaceCrossingImportedSet("Imported_GateCrossingSet", _streetCenter + _streetForward * 21.8f, 4.2f, 10.4f);
        PlaceEdictImportedSet("Imported_EdictA", _streetCenter - _streetRight * 6.4f - _streetForward * 3.6f);
        PlaceEdictImportedSet("Imported_EdictB", _streetCenter + _streetRight * 6.1f + _streetForward * 11.8f);
        PlaceEdictImportedSet("Imported_EdictC", _streetCenter - _streetRight * 6.2f + _streetForward * 20.2f);
    }

    private void PlaceAlleyImportedDressing()
    {
        Vector3 westAlley = _streetCenter - _streetRight * 11.8f + _streetForward * 4.8f;
        Vector3 eastAlley = _streetCenter + _streetRight * 11.8f + _streetForward * 17.2f;
        Quaternion westAlleyRotation = Quaternion.LookRotation(-_streetRight, Vector3.up);
        Quaternion eastAlleyRotation = Quaternion.LookRotation(_streetRight, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/stone-wall-damaged",
            "Imported_WestAlleyWallA",
            transform,
            westAlley - _streetForward * 2f + Vector3.up * 0.02f,
            westAlleyRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/debris-wood",
            "Imported_WestAlleyDebris",
            transform,
            westAlley + _streetRight * 0.9f + Vector3.up * 0.02f,
            westAlleyRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/coffin-old",
            "Imported_WestAlleyCoffin",
            transform,
            westAlley - _streetRight * 0.5f + _streetForward * 1.3f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_streetForward, Vector3.up),
            Vector3.one * 0.84f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/gravestone-broken",
            "Imported_WestAlleyStone",
            transform,
            westAlley + _streetRight * 1.2f + _streetForward * 1.8f + Vector3.up * 0.02f,
            westAlleyRotation,
            Vector3.one * 0.82f);

        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/lightpost-single",
            "Imported_EastAlleyLightpost",
            transform,
            eastAlley + _streetRight * 1.1f - _streetForward * 2f + Vector3.up * 0.02f,
            eastAlleyRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Bench",
            "Imported_EastAlleyBench",
            transform,
            eastAlley - _streetForward * 1.1f + Vector3.up * 0.02f,
            eastAlleyRotation,
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Bucket_Wooden_1",
            "Imported_EastAlleyBucket",
            transform,
            eastAlley + _streetForward * 1.1f + Vector3.up * 0.02f,
            Quaternion.LookRotation(_streetForward, Vector3.up),
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Pot_1",
            "Imported_EastAlleyPot",
            transform,
            eastAlley + _streetRight * 0.85f + _streetForward * 1.7f + Vector3.up * 0.02f,
            eastAlleyRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_WoodenFence_Extension2",
            "Imported_EastAlleyFence",
            transform,
            eastAlley + _streetRight * 1.9f + Vector3.up * 0.02f,
            eastAlleyRotation,
            Vector3.one * 0.92f);
    }

    private void PlaceStreetBlockFoundationSet(string prefix, Vector3 center, bool westSide, float scale, bool quarantineDistrict)
    {
        Vector3 facadeOffset = _streetRight * (westSide ? 3.1f : -3.1f);
        Vector3 cornerOffset = _streetRight * (westSide ? 2.35f : -2.35f);
        Quaternion facadeRotation = Quaternion.LookRotation(westSide ? _streetRight : -_streetRight, Vector3.up);
        Quaternion oppositeFacadeRotation = facadeRotation * Quaternion.Euler(0f, 180f, 0f);
        Quaternion roofRotation = Quaternion.LookRotation(_streetForward, Vector3.up);

        string centerWallAsset = quarantineDistrict
            ? "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Door_Flat"
            : "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Door_Flat";
        string forwardWallAsset = quarantineDistrict
            ? "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Window_Wide_Flat"
            : "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Window_Wide_Flat";
        string rearWallAsset = quarantineDistrict
            ? "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Straight"
            : "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_WoodGrid";
        string cornerAsset = quarantineDistrict
            ? "Imported/Quaternius/MedievalVillage/FBX/Corner_ExteriorWide_Brick"
            : "Imported/Quaternius/MedievalVillage/FBX/Corner_ExteriorWide_Wood";
        string roofAsset = quarantineDistrict
            ? "Imported/Quaternius/MedievalVillage/FBX/Roof_RoundTiles_8x10"
            : "Imported/Quaternius/MedievalVillage/FBX/Roof_RoundTiles_8x12";
        string overhangAsset = quarantineDistrict
            ? "Imported/Quaternius/MedievalVillage/FBX/Overhang_UnevenBrick_Long"
            : "Imported/Quaternius/MedievalVillage/FBX/Overhang_Plaster_Long";

        RuntimeModelSpawner.Spawn(
            centerWallAsset,
            $"{prefix}_CenterWall",
            transform,
            center + facadeOffset + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            forwardWallAsset,
            $"{prefix}_ForwardWall",
            transform,
            center + facadeOffset + _streetForward * 2.35f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            rearWallAsset,
            $"{prefix}_RearWall",
            transform,
            center + facadeOffset - _streetForward * 2.35f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            cornerAsset,
            $"{prefix}_CornerFront",
            transform,
            center + cornerOffset + _streetForward * 3.8f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            cornerAsset,
            $"{prefix}_CornerRear",
            transform,
            center + cornerOffset - _streetForward * 3.8f + Vector3.up * 0.02f,
            oppositeFacadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            roofAsset,
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * (quarantineDistrict ? 5.1f : 5.25f),
            roofRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            overhangAsset,
            $"{prefix}_Overhang",
            transform,
            center + facadeOffset * 1.06f + Vector3.up * 2.05f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            quarantineDistrict
                ? "Imported/Quaternius/MedievalVillage/FBX/Prop_Chimney2"
                : "Imported/Quaternius/MedievalVillage/FBX/Prop_Chimney",
            $"{prefix}_Chimney",
            transform,
            center - _streetForward * 0.95f + Vector3.up * 6.15f,
            roofRotation,
            Vector3.one * scale);
    }

    private void PlacePerimeterWallImportedDressing()
    {
        float[] zSegments = { -18f, -10f, -2f, 6f, 14f, 22f, 30f };
        Quaternion forwardRotation = Quaternion.LookRotation(_streetForward, Vector3.up);

        for (int i = 0; i < zSegments.Length; i++)
        {
            bool lateDistrict = zSegments[i] >= 18f;
            string wallAsset = lateDistrict
                ? "Imported/Kenney/GraveyardKit/brick-wall"
                : "Imported/Kenney/GraveyardKit/stone-wall";
            Vector3 westCenter = _streetCenter - _streetRight * (BuildingRowOffset + 5.55f) + _streetForward * zSegments[i] + Vector3.up * 0.02f;
            Vector3 eastCenter = _streetCenter + _streetRight * (BuildingRowOffset + 5.55f) + _streetForward * zSegments[i] + Vector3.up * 0.02f;

            RuntimeModelSpawner.Spawn(
                wallAsset,
                $"Imported_WestPerimeterWall_{i + 1}",
                transform,
                westCenter,
                forwardRotation,
                new Vector3(1.18f, 1.02f, 1f));
            RuntimeModelSpawner.Spawn(
                wallAsset,
                $"Imported_EastPerimeterWall_{i + 1}",
                transform,
                eastCenter,
                forwardRotation,
                new Vector3(1.18f, 1.02f, 1f));
        }

        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/stone-wall-column",
            "Imported_WestPerimeterColumnA",
            transform,
            _streetCenter - _streetRight * (BuildingRowOffset + 5.55f) - _streetForward * 22.2f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/stone-wall-column",
            "Imported_WestPerimeterColumnB",
            transform,
            _streetCenter - _streetRight * (BuildingRowOffset + 5.55f) + _streetForward * 26.4f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/stone-wall-column",
            "Imported_EastPerimeterColumnA",
            transform,
            _streetCenter + _streetRight * (BuildingRowOffset + 5.55f) - _streetForward * 22.2f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/stone-wall-column",
            "Imported_EastPerimeterColumnB",
            transform,
            _streetCenter + _streetRight * (BuildingRowOffset + 5.55f) + _streetForward * 26.4f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one);
    }

    private void PlaceForegroundImportedDressing()
    {
        Quaternion forwardRotation = Quaternion.LookRotation(_streetForward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/pine",
            "Imported_ForegroundTreeLeft",
            transform,
            _streetCenter - _streetRight * 18.8f - _streetForward * 11.2f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 1.18f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/pine",
            "Imported_ForegroundTreeRight",
            transform,
            _streetCenter + _streetRight * 18.3f - _streetForward * 10.4f + Vector3.up * 0.02f,
            Quaternion.LookRotation(-_streetForward, Vector3.up),
            Vector3.one * 1.14f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/stone-wall-column",
            "Imported_ForegroundColumnLeft",
            transform,
            _streetCenter - _streetRight * 17.3f + _streetForward * 6.2f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/stone-wall-column",
            "Imported_ForegroundColumnRight",
            transform,
            _streetCenter + _streetRight * 17f + _streetForward * 15.5f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_MetalFence_Ornament",
            "Imported_ForegroundFenceLeft",
            transform,
            _streetCenter - _streetRight * 16.6f + _streetForward * 7.2f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_MetalFence_Ornament",
            "Imported_ForegroundFenceRight",
            transform,
            _streetCenter + _streetRight * 16.4f + _streetForward * 16.2f + Vector3.up * 0.02f,
            forwardRotation,
            Vector3.one * 0.92f);
    }

    private void HidePlaceholderVisuals()
    {
        HideRenderers(
            "StartThresholdLintel",
            "StartThresholdPostLeft",
            "StartThresholdPostRight",
            "StartThresholdBanner",
            "GatePillarLeft",
            "GatePillarRight",
            "GateLintel",
            "WatchReliefCanopy_Deck",
            "WatchReliefCanopy_Roof",
            "WatchReliefCanopy_PostLeft",
            "WatchReliefCanopy_PostRight",
            "WatchTentDeck",
            "WatchTentRoof",
            "WatchMapTable",
            "WatchSpearRack",
            "NoticeBenchLeft",
            "NoticeBenchRight",
            "NoticePlaza",
            "IncidentBundleA",
            "IncidentBundleB",
            "IncidentLampPost",
            "WatchBarrierA",
            "WatchBarrierB",
            "ArrivalCourtDeck",
            "ArrivalCourtBenchLeft",
            "ArrivalCourtBenchRight",
            "ArrivalCourtNoticeRack",
            "DistrictBand_Start",
            "DistrictBand_Notice",
            "DistrictBand_Incident",
            "DistrictBand_Gate",
            "DistrictBand_Fog",
            "SteleGuideLeft",
            "SteleGuideRight",
            "CrestFocusPatch",
            "GateFocusRunner",
            "GatePlaza",
            "GateWarningBannerLeft",
            "GateWarningBannerRight",
            "GatePressureCrateLeft",
            "GatePressureCrateRight",
            "GatePressurePoleLeft",
            "GatePressurePoleRight",
            "GatePressureCloth",
            "SupplyCheckpointDeck",
            "SupplyCheckpointRackA",
            "SupplyCheckpointRackB",
            "SupplyCheckpointTarp",
            "SupplyRegistryBoard",
            "MemorialShrine_Base",
            "MemorialShrine_Stele",
            "MemorialShrine_Offerings",
            "MemorialShrine_CandleLeft_Post",
            "MemorialShrine_CandleLeft_Bowl",
            "MemorialShrine_CandleLeft_Flame",
            "MemorialShrine_CandleRight_Post",
            "MemorialShrine_CandleRight_Bowl",
            "MemorialShrine_CandleRight_Flame",
            "SupplyPile_CrateA",
            "SupplyPile_CrateB",
            "SupplyPile_SackA",
            "SupplyPile_SackB",
            "WestWatchTower_Base",
            "WestWatchTower_Top",
            "WestWatchTower_Roof",
            "WestWatchTower_Banner",
            "EastWatchTower_Base",
            "EastWatchTower_Top",
            "EastWatchTower_Roof",
            "EastWatchTower_Banner",
            "BellHouse_Base",
            "BellHouse_Roof",
            "BellHouse_Bell",
            "MarketCrossing_Beam",
            "MarketCrossing_PostLeft",
            "MarketCrossing_PostRight",
            "MarketCrossing_Cloth",
            "GateCrossing_Beam",
            "GateCrossing_PostLeft",
            "GateCrossing_PostRight",
            "GateCrossing_Cloth",
            "RoyalEdictA_Post",
            "RoyalEdictA_Board",
            "RoyalEdictA_Ribbon",
            "RoyalEdictB_Post",
            "RoyalEdictB_Board",
            "RoyalEdictB_Ribbon",
            "RoyalEdictC_Post",
            "RoyalEdictC_Board",
            "RoyalEdictC_Ribbon",
            "WestBacklotWall_Base",
            "WestBacklotWall_Cap",
            "EastBacklotWall_Base",
            "EastBacklotWall_Cap",
            "Boulevard",
            "LeftCurb",
            "RightCurb",
            "LeftWalkway",
            "RightWalkway",
            "WestServiceLane",
            "EastServiceLane",
            "CrossStreet_Mid",
            "CrossStreet_Gate",
            "GuardOfficeLot_Base",
            "GuardOfficeLot_Roof",
            "GuardOfficeLot_Porch",
            "GuardOfficeLot_Door",
            "GuardOfficeLot_RecordBoard",
            "GuardOfficeLot_SignalBar",
            "ShutteredInnLot_Base",
            "ShutteredInnLot_Roof",
            "ShutteredInnLot_FrontCanopy",
            "ShutteredInnLot_SignBlade",
            "ShutteredInnLot_RearKitchen",
            "ShutteredInnLot_PorchRail",
            "ShutteredInnLot_BoardedWindowA",
            "ShutteredInnLot_BoardedWindowB",
            "ApothecaryLot_Base",
            "ApothecaryLot_Roof",
            "ApothecaryLot_SideLab",
            "ApothecaryLot_GlassBay",
            "ApothecaryLot_HerbFrame",
            "CollapsedResidenceLot_Base",
            "CollapsedResidenceLot_RemainingRoof",
            "CollapsedResidenceLot_CollapseMassA",
            "CollapsedResidenceLot_CollapseMassB",
            "CollapsedResidenceLot_ExposedFrame",
            "SealedChapelLot_Nave",
            "SealedChapelLot_Roof",
            "SealedChapelLot_FrontArch",
            "SealedChapelLot_SealPanel",
            "SealedChapelLot_SpireStub",
            "TavernAnchor_InnBoard",
            "TavernAnchor_ShutterA",
            "TavernAnchor_ShutterB",
            "ApothecaryAnchor_Cabinet",
            "ApothecaryAnchor_MortarSign",
            "ApothecaryAnchor_HerbRack",
            "ApothecaryAnchor_Satchel",
            "ForegroundLeftPost",
            "ForegroundRightPost",
            "ForegroundBannerLeft",
            "ForegroundBannerRight",
            "ForegroundMidLeft",
            "ForegroundMidRight",
            "ForegroundMidClothLeft",
            "ForegroundMidClothRight",
            "QuarantineWallLeft",
            "QuarantineWallRight",
            "QuarantineRailLeft",
            "QuarantineRailRight",
            "QuarantineChainA",
            "QuarantineChainB",
            "FogFunnelLeftA",
            "FogFunnelRightA",
            "FogFunnelLeftB",
            "FogFunnelRightB",
            "FogFunnelChain");

        HideRenderersByPrefix(
            "GuardOfficeLot_",
            "ShutteredInnLot_",
            "ApothecaryLot_",
            "CollapsedResidenceLot_",
            "SealedChapelLot_",
            "WestWatchTower_",
            "EastWatchTower_",
            "BellHouse_",
            "MarketCrossing_",
            "GateCrossing_",
            "RoyalEdict",
            "SkylineWest_",
            "SkylineEast_",
            "WestBacklotWall_",
            "EastBacklotWall_",
            "WestDrain_",
            "EastDrain_",
            "WestBlock_",
            "EastBlock_");
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

    private void HideRenderersByPrefix(params string[] prefixes)
    {
        foreach (Transform child in transform)
        {
            string childName = child.name;
            foreach (string prefix in prefixes)
            {
                if (string.IsNullOrWhiteSpace(prefix) || !childName.StartsWith(prefix))
                {
                    continue;
                }

                foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = false;
                }

                break;
            }
        }
    }

    private void PlaceWatchTowerImportedSet(string prefix, Vector3 center, float height)
    {
        Quaternion forwardRotation = Quaternion.LookRotation(_streetForward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_Tower_RoundTiles",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * height,
            forwardRotation,
            Vector3.one * 0.88f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_Support",
            $"{prefix}_SupportA",
            transform,
            center - _streetRight * 0.95f - _streetForward * 0.8f + Vector3.up * (height - 2.1f),
            forwardRotation,
            Vector3.one * 0.74f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_Support",
            $"{prefix}_SupportB",
            transform,
            center + _streetRight * 0.95f - _streetForward * 0.8f + Vector3.up * (height - 2.1f),
            forwardRotation,
            Vector3.one * 0.74f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Banner_1_Cloth",
            $"{prefix}_Banner",
            transform,
            center + Vector3.up * (height - 0.5f),
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one * 0.86f);
    }

    private void PlaceBellHouseImportedSet(string prefix, Vector3 center)
    {
        Quaternion forwardRotation = Quaternion.LookRotation(_streetForward, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_Tower_RoundTiles",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * 4.95f,
            forwardRotation,
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Chandelier",
            $"{prefix}_BellProxy",
            transform,
            center + Vector3.up * 3.2f,
            Quaternion.identity,
            Vector3.one * 0.7f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Banner_2_Cloth",
            $"{prefix}_Cloth",
            transform,
            center + _streetRight * 0.9f + Vector3.up * 4.05f,
            Quaternion.LookRotation(_streetRight, Vector3.up),
            Vector3.one * 0.74f);
    }

    private void PlaceCrossingImportedSet(string prefix, Vector3 center, float height, float width)
    {
        Quaternion crossRotation = Quaternion.LookRotation(_streetRight, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Balcony_Cross_Straight",
            $"{prefix}_Bridge",
            transform,
            center + Vector3.up * height,
            crossRotation,
            Vector3.one * (width / 11.6f));
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Banner_1_Cloth",
            $"{prefix}_Banner",
            transform,
            center + Vector3.up * (height - 0.85f),
            crossRotation,
            Vector3.one * 0.98f);
    }

    private void PlaceEdictImportedSet(string prefix, Vector3 center)
    {
        Quaternion sideRotation = Quaternion.LookRotation(_streetRight, Vector3.up);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/BookStand",
            $"{prefix}_Stand",
            transform,
            center + Vector3.up * 1.32f,
            sideRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/FantasyProps/FBX/Banner_2",
            $"{prefix}_Board",
            transform,
            center + Vector3.up * 2.28f,
            sideRotation,
            Vector3.one * 0.95f);
    }

    private void PlaceWestFacadeSet(string prefix, Vector3 center, float scale, bool addBalcony)
    {
        Quaternion facadeRotation = Quaternion.LookRotation(_streetRight, Vector3.up);
        Quaternion roofRotation = Quaternion.LookRotation(_streetForward, Vector3.up);
        float facadeOffset = 3.45f;

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Door_Round",
            $"{prefix}_Wall",
            transform,
            center + _streetRight * facadeOffset + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/DoorFrame_Round_WoodDark",
            $"{prefix}_DoorFrame",
            transform,
            center + _streetRight * facadeOffset + Vector3.up * 1.32f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Door_2_Round",
            $"{prefix}_Door",
            transform,
            center + _streetRight * (facadeOffset + 0.04f) + Vector3.up * 1.08f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Window_Wide_Round",
            $"{prefix}_WindowNorth",
            transform,
            center + _streetRight * facadeOffset + _streetForward * 1.95f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Window_Wide_Round",
            $"{prefix}_WindowSouth",
            transform,
            center + _streetRight * facadeOffset - _streetForward * 1.95f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/WindowShutters_Wide_Round_Open",
            $"{prefix}_ShuttersNorth",
            transform,
            center + _streetRight * (facadeOffset + 0.06f) + _streetForward * 1.95f + Vector3.up * 1.46f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/WindowShutters_Wide_Round_Open",
            $"{prefix}_ShuttersSouth",
            transform,
            center + _streetRight * (facadeOffset + 0.06f) - _streetForward * 1.95f + Vector3.up * 1.46f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_RoundTiles_6x8",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * 4.95f,
            roofRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_Chimney",
            $"{prefix}_Chimney",
            transform,
            center - _streetForward * 1.2f + Vector3.up * 6.2f,
            roofRotation,
            Vector3.one * scale);

        if (addBalcony)
        {
            RuntimeModelSpawner.Spawn(
                "Imported/Quaternius/MedievalVillage/FBX/Balcony_Simple_Straight",
                $"{prefix}_Balcony",
                transform,
                center + _streetRight * 3.65f + Vector3.up * 2.7f,
                facadeRotation,
                Vector3.one * scale);
        }
        else
        {
            RuntimeModelSpawner.Spawn(
                "Imported/Quaternius/MedievalVillage/FBX/Window_Roof_Wide",
                $"{prefix}_WindowRoof",
                transform,
                center + _streetRight * (facadeOffset + 0.05f) + _streetForward * 1.95f + Vector3.up * 3.1f,
                facadeRotation,
                Vector3.one * scale);
            RuntimeModelSpawner.Spawn(
                "Imported/Quaternius/MedievalVillage/FBX/Window_Roof_Wide",
                $"{prefix}_WindowRoofSouth",
                transform,
                center + _streetRight * (facadeOffset + 0.05f) - _streetForward * 1.95f + Vector3.up * 3.1f,
                facadeRotation,
                Vector3.one * scale);
        }
    }

    private void PlaceEastFacadeSet(string prefix, Vector3 center, float scale, bool addCanopy)
    {
        Quaternion facadeRotation = Quaternion.LookRotation(-_streetRight, Vector3.up);
        Quaternion roofRotation = Quaternion.LookRotation(_streetForward, Vector3.up);
        float facadeOffset = 3.45f;

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Door_Flat",
            $"{prefix}_Wall",
            transform,
            center - _streetRight * facadeOffset + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/DoorFrame_Flat_WoodDark",
            $"{prefix}_DoorFrame",
            transform,
            center - _streetRight * facadeOffset + Vector3.up * 1.3f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Door_4_Flat",
            $"{prefix}_Door",
            transform,
            center - _streetRight * (facadeOffset + 0.04f) + Vector3.up * 1.08f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Window_Wide_Flat2",
            $"{prefix}_WindowNorth",
            transform,
            center - _streetRight * facadeOffset + _streetForward * 1.95f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_Plaster_Window_Wide_Flat2",
            $"{prefix}_WindowSouth",
            transform,
            center - _streetRight * facadeOffset - _streetForward * 1.95f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/WindowShutters_Wide_Flat_Open",
            $"{prefix}_ShuttersNorth",
            transform,
            center - _streetRight * (facadeOffset + 0.06f) + _streetForward * 1.95f + Vector3.up * 1.46f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/WindowShutters_Wide_Flat_Open",
            $"{prefix}_ShuttersSouth",
            transform,
            center - _streetRight * (facadeOffset + 0.06f) - _streetForward * 1.95f + Vector3.up * 1.46f,
            facadeRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_RoundTiles_8x8",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * 5.35f,
            roofRotation,
            Vector3.one * scale);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Prop_Chimney2",
            $"{prefix}_Chimney",
            transform,
            center + _streetForward * 1.05f + Vector3.up * 6.15f,
            roofRotation,
            Vector3.one * scale);

        if (addCanopy)
        {
            RuntimeModelSpawner.Spawn(
                "Imported/Quaternius/MedievalVillage/FBX/Overhang_Plaster_Long",
                $"{prefix}_Canopy",
                transform,
                center - _streetRight * (facadeOffset + 0.22f) + Vector3.up * 2.05f,
                facadeRotation,
                Vector3.one * scale);
            RuntimeModelSpawner.Spawn(
                "Imported/Quaternius/MedievalVillage/FBX/Balcony_Simple_Straight",
                $"{prefix}_Balcony",
                transform,
                center - _streetRight * (facadeOffset + 0.18f) + Vector3.up * 2.85f,
                facadeRotation,
                Vector3.one * scale);
        }
        else
        {
            RuntimeModelSpawner.Spawn(
                "Imported/Quaternius/MedievalVillage/FBX/Window_Roof_Wide",
                $"{prefix}_WindowRoofNorth",
                transform,
                center - _streetRight * (facadeOffset + 0.05f) + _streetForward * 1.95f + Vector3.up * 3.08f,
                facadeRotation,
                Vector3.one * scale);
            RuntimeModelSpawner.Spawn(
                "Imported/Quaternius/MedievalVillage/FBX/Window_Roof_Wide",
                $"{prefix}_WindowRoofSouth",
                transform,
                center - _streetRight * (facadeOffset + 0.05f) - _streetForward * 1.95f + Vector3.up * 3.08f,
                facadeRotation,
                Vector3.one * scale);
        }
    }

    private void PlaceEastCollapsedSet(string prefix, Vector3 center)
    {
        Quaternion facadeRotation = Quaternion.LookRotation(-_streetRight, Vector3.up);
        Quaternion roofRotation = Quaternion.LookRotation(_streetForward, Vector3.up);
        Quaternion oppositeRotation = facadeRotation * Quaternion.Euler(0f, 180f, 0f);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Door_Flat",
            $"{prefix}_Wall",
            transform,
            center - _streetRight * 3.18f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/DoorFrame_Flat_Brick",
            $"{prefix}_DoorFrame",
            transform,
            center - _streetRight * 3.16f + Vector3.up * 1.34f,
            facadeRotation,
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Door_4_Flat",
            $"{prefix}_Door",
            transform,
            center - _streetRight * 3.1f + Vector3.up * 1.1f,
            facadeRotation,
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Window_Wide_Flat",
            $"{prefix}_FrontWing",
            transform,
            center - _streetRight * 3.18f + _streetForward * 2.4f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Straight",
            $"{prefix}_RearWing",
            transform,
            center - _streetRight * 3.18f - _streetForward * 2.3f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Corner_ExteriorWide_Brick",
            $"{prefix}_FrontCorner",
            transform,
            center - _streetRight * 2.4f + _streetForward * 4.05f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Corner_ExteriorWide_Brick",
            $"{prefix}_RearCorner",
            transform,
            center - _streetRight * 2.4f - _streetForward * 4.05f + Vector3.up * 0.02f,
            oppositeRotation,
            Vector3.one * 0.96f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_Front_Brick6",
            $"{prefix}_RoofFragment",
            transform,
            center - _streetForward * 0.9f + Vector3.up * 4.7f,
            roofRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Overhang_UnevenBrick_Long",
            $"{prefix}_Overhang",
            transform,
            center - _streetRight * 3.45f + Vector3.up * 2.1f,
            facadeRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/rocks-tall",
            $"{prefix}_CollapseRocks",
            transform,
            center + _streetRight * 2.6f + _streetForward * 1.9f + Vector3.up * 0.02f,
            roofRotation,
            Vector3.one * 0.95f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/gravestone-debris",
            $"{prefix}_Debris",
            transform,
            center + _streetRight * 2.2f + _streetForward * 0.8f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * 0.95f);
    }

    private void PlaceWestChapelSet(string prefix, Vector3 center)
    {
        Quaternion facadeRotation = Quaternion.LookRotation(_streetRight, Vector3.up);
        Quaternion roofRotation = Quaternion.LookRotation(_streetForward, Vector3.up);
        Quaternion oppositeRotation = facadeRotation * Quaternion.Euler(0f, 180f, 0f);

        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Door_Round",
            $"{prefix}_Wall",
            transform,
            center + _streetRight * 3.62f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one * 1.02f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/DoorFrame_Round_Brick",
            $"{prefix}_DoorFrame",
            transform,
            center + _streetRight * 3.58f + Vector3.up * 1.4f,
            facadeRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Door_1_Round",
            $"{prefix}_Door",
            transform,
            center + _streetRight * 3.5f + Vector3.up * 1.14f,
            facadeRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Window_Wide_Round",
            $"{prefix}_FrontWing",
            transform,
            center + _streetRight * 3.55f + _streetForward * 2.6f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Wall_UnevenBrick_Straight",
            $"{prefix}_RearWing",
            transform,
            center + _streetRight * 3.55f - _streetForward * 2.6f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Corner_ExteriorWide_Brick",
            $"{prefix}_FrontCorner",
            transform,
            center + _streetRight * 2.55f + _streetForward * 4.2f + Vector3.up * 0.02f,
            facadeRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Corner_ExteriorWide_Brick",
            $"{prefix}_RearCorner",
            transform,
            center + _streetRight * 2.55f - _streetForward * 4.2f + Vector3.up * 0.02f,
            oppositeRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_Tower_RoundTiles",
            $"{prefix}_Roof",
            transform,
            center + Vector3.up * 6.5f,
            roofRotation,
            Vector3.one * 0.95f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Roof_Front_Brick8",
            $"{prefix}_NaveRoof",
            transform,
            center + Vector3.up * 5.2f,
            roofRotation,
            Vector3.one * 0.92f);
        RuntimeModelSpawner.Spawn(
            "Imported/Quaternius/MedievalVillage/FBX/Overhang_UnevenBrick_Long",
            $"{prefix}_Overhang",
            transform,
            center + _streetRight * 3.92f + Vector3.up * 2.2f,
            facadeRotation,
            Vector3.one);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/crypt-small",
            $"{prefix}_Crypt",
            transform,
            center - _streetForward * 2.4f - _streetRight * 1.9f + Vector3.up * 0.02f,
            roofRotation,
            Vector3.one * 0.88f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/grave",
            $"{prefix}_GraveLeft",
            transform,
            center - _streetRight * 0.8f + _streetForward * 1.9f + Vector3.up * 0.02f,
            roofRotation,
            Vector3.one * 0.9f);
        RuntimeModelSpawner.Spawn(
            "Imported/Kenney/GraveyardKit/grave",
            $"{prefix}_GraveRight",
            transform,
            center - _streetForward * 0.4f + _streetRight * 0.5f + Vector3.up * 0.02f,
            roofRotation,
            Vector3.one * 0.9f);
    }

    private GameObject CreateBlock(string name, Vector3 position, Vector3 scale, Quaternion rotation, Color color)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.position = position;
        block.transform.rotation = rotation;
        block.transform.localScale = scale;
        Tint(block.GetComponent<Renderer>(), color);
        return block;
    }

    private GameObject CreateImportedMaterialBlock(string name, Vector3 position, Vector3 scale, Quaternion rotation, string themeKey)
    {
        GameObject block = CreateBlock(name, position, scale, rotation, Color.white);
        RuntimeImportedMaterialLibrary.ApplyTheme(themeKey, block);
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
