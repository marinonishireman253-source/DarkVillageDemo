using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public enum FloorVariant
{
    AshParlor = 0,
    MirrorCorridor = 1
}

public sealed class TowerInteriorSlice : MonoBehaviour
{
    private const string RootName = "__TowerInteriorSlice";
    private const int LayoutVersion = 19;
    private const float RoomZoneOverlap = 4.6f;
    private const float RoomZoneDepth = 5.2f;
    private const float RoomZoneCenterY = 2.35f;
    private const float RoomZoneBlendDuration = 0.32f;
    private const float PlayerReferenceHeight = 2.15f;
    private const float MonsterHeightRatio = 0.7f;
    private const int MonsterSortingOrder = 10;
    private const int LandingRoomIndex = 0;
    private const int RuleRoomIndex = 1;
    private const int PressureRoomIndex = 2;
    private const int ChoiceRoomIndex = 3;
    private const int FinaleRoomIndex = 4;
    private const float LandingRelicLocalX = -10.4f;
    private const float LandingGuideLocalX = 15.2f;
    private const float LandingGuideLocalZOffset = 0.22f;
    private const float LandingTestRoomLocalX = -16f;
    private const float LandingTestRiskStationLocalX = -1.45f;
    private const float LandingTestSafeStationLocalX = 1.45f;
    private const float FirstBrazierLocalX = 8.6f;
    private const float PressureEnemyLocalX = -4.4f;
    private const float ChoiceRiskyLocalX = -9.4f;
    private const float ChoiceSafeLocalX = 9.4f;
    private const float ChoiceAnchorLocalX = 0f;
    private const float ChoicePromptLocalY = 1.02f;
    private const float SecondBrazierLocalX = 7.8f;
    private const float FinalEnemyLocalX = -7.2f;
    private const float FinalRiskEnemyLocalX = -2.6f;
    private const float FinalRewardLocalX = -0.2f;
    private const float BrazierLocalZOffset = 0.18f;
    private const float ExitLocalX = 16.5f;
    private const float ExitLocalZOffset = 0.28f;
    private static readonly Color NightColor = new Color(0.06f, 0.07f, 0.09f);
    private static readonly Color ShellColor = new Color(0.18f, 0.19f, 0.21f);
    private static readonly Color FloorColor = new Color(0.3f, 0.25f, 0.21f);
    private static readonly Color CeilingColor = new Color(0.24f, 0.24f, 0.23f);
    private static readonly Color DividerColor = new Color(0.33f, 0.3f, 0.27f);
    private static readonly Color WindowColor = new Color(0.12f, 0.12f, 0.13f);
    private static readonly Color ForegroundTrimColor = new Color(0.22f, 0.2f, 0.18f);
    private static readonly Color MidDepthFloorColor = new Color(0.36f, 0.3f, 0.25f);
    private static readonly Color BackDepthFloorColor = new Color(0.26f, 0.22f, 0.19f);
    private static readonly Color CeilingBeamColor = new Color(0.2f, 0.19f, 0.18f);
    private static readonly Color LampColor = new Color(0.74f, 0.67f, 0.44f);
    private static readonly StandardRoomTemplate[] AshRoomTemplates =
    {
        CreateLivingRoomTemplate("Ash_Foyer"),
        CreateStudyRoomTemplate("Ash_Rule"),
        CreateLivingRoomTemplate("Ash_Pressure"),
        CreateStudyRoomTemplate("Ash_Choice"),
        CreateLivingRoomTemplate("Ash_Finale")
    };
    private static readonly StandardRoomTemplate[] MirrorRoomTemplates =
    {
        CreateStudyRoomTemplate("Mirror_Landing", 36f, 8f, 6.1f),
        CreateStudyRoomTemplate("Mirror_Rule", 34f, 8f, 6.1f),
        CreateLivingRoomTemplate("Mirror_Pressure", 38f, 8.1f, 6.1f),
        CreateStudyRoomTemplate("Mirror_Choice", 34f, 8f, 6.1f),
        CreateLivingRoomTemplate("Mirror_Finale", 38f, 8.1f, 6.1f)
    };
    private static StandardRoomTemplate[] RoomTemplates => GetTemplatesForFloor((FloorVariant)GameStateHub.CurrentFloorIndexRuntime);

    public static float WalkDepth => RoomTemplates[0].WalkDepth;
    public static Vector2 PlayableXRange => new Vector2(
        -GetHouseWidth() * 0.5f + RoomTemplates[0].WalkableInset,
        GetHouseWidth() * 0.5f - RoomTemplates[RoomTemplates.Length - 1].WalkableInset);

    public static Vector2 CameraTrackXRange => new Vector2(
        -GetHouseWidth() * 0.5f + RoomTemplates[0].CameraTrackInset,
        GetHouseWidth() * 0.5f - RoomTemplates[RoomTemplates.Length - 1].CameraTrackInset);

    private PlayerMover _player;
    private RoomCameraZone _startingRoomZone;
    private AshParlorRunController _ashParlorController;
    private MirrorCorridorRunController _mirrorCorridorController;
    private FloorRunController _floorController;
    private FloorVariant _floorVariant;
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
            TowerInteriorSlice existingSlice = existingRoot.GetComponent<TowerInteriorSlice>();
            if (existingSlice != null && existingSlice._layoutVersion == LayoutVersion)
            {
                existingSlice.Bind(player);
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(existingRoot);
            }
            else
            {
                DestroyImmediate(existingRoot);
            }
        }

        GameObject root = new GameObject(RootName);
        TowerInteriorSlice slice = root.AddComponent<TowerInteriorSlice>();
        slice.Build(player);
    }

    public static RoomCameraZone FindBestZone(PlayerMover player)
    {
        if (player == null)
        {
            return null;
        }

        RoomCameraZone[] zones = FindObjectsByType<RoomCameraZone>(FindObjectsSortMode.None);
        if (zones == null || zones.Length == 0)
        {
            return null;
        }

        float playerX = player.transform.position.x;
        RoomCameraZone bestZone = null;
        float bestDistance = float.PositiveInfinity;

        foreach (RoomCameraZone zone in zones)
        {
            Vector2 bounds = zone.HorizontalBounds;
            if (playerX >= bounds.x && playerX <= bounds.y)
            {
                return zone;
            }

            float distance = playerX < bounds.x ? bounds.x - playerX : playerX - bounds.y;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestZone = zone;
            }
        }

        return bestZone;
    }

    private void Bind(PlayerMover player)
    {
        _player = player;
        PositionPlayer();
    }

    private void Build(PlayerMover player)
    {
        _player = player;
        _layoutVersion = LayoutVersion;
        _floorVariant = (FloorVariant)GameStateHub.CurrentFloorIndexRuntime;

        BuildFloorState();
        BuildShell();
        BuildRooms();
        BuildRoomDividers();
        BuildFloorSeals();
        PositionPlayer();
    }

    private void BuildFloorState()
    {
        if (_floorVariant == FloorVariant.MirrorCorridor)
        {
            Transform stateRoot = CreateGroup(transform, "MirrorCorridorState");
            _mirrorCorridorController = stateRoot.gameObject.AddComponent<MirrorCorridorRunController>();
            _floorController = _mirrorCorridorController;
            return;
        }

        Transform ashStateRoot = CreateGroup(transform, "AshParlorState");
        _ashParlorController = ashStateRoot.gameObject.AddComponent<AshParlorRunController>();
        _floorController = _ashParlorController;
    }

    private void PositionPlayer()
    {
        if (_player == null)
        {
            return;
        }

        float spawnX = -GetHouseWidth() * 0.5f + RoomTemplates[0].PlayerSpawnOffsetX;
        _player.transform.position = new Vector3(spawnX, RoomTemplates[0].PlayerSpawnHeight, WalkDepth);
        _player.transform.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);

        CameraFollow follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (follow != null)
        {
            if (_startingRoomZone != null)
            {
                follow.ConfigureRoomZone(_startingRoomZone, true);
            }

            follow.SetTarget(_player.transform, true);
        }
    }

    private void BuildShell()
    {
        StandardRoomTemplate firstRoom = RoomTemplates[0];
        float houseWidth = GetHouseWidth();
        float totalFloorDepth = firstRoom.Depth + firstRoom.FrontFloorExtension;
        float floorCenterZ = (firstRoom.Depth - firstRoom.FrontFloorExtension) * 0.5f;
        float frontShellExtension = 1.9f;
        float sideWallDepth = firstRoom.Depth + frontShellExtension;
        float sideWallCenterZ = (firstRoom.Depth - frontShellExtension) * 0.5f;
        float frontMaskDepth = 2.35f;
        float frontMaskCenterZ = -0.2f;
        float frontTopBandDepth = 1.05f;
        float frontTopBandCenterZ = 0.16f;

        Transform shellRoot = CreateGroup(transform, "Shell");

        CreateLocalBlock(shellRoot, "NightBackdrop", new Vector3(0f, firstRoom.WallHeight * 0.5f, firstRoom.Depth + 1.65f), new Vector3(houseWidth + 6f, firstRoom.WallHeight + 0.2f, 0.35f), NightColor);
        CreateLocalBlock(shellRoot, "InteriorFloor", new Vector3(0f, firstRoom.FloorThickness * 0.5f, floorCenterZ), new Vector3(houseWidth, firstRoom.FloorThickness, totalFloorDepth), FloorColor);
        CreateLocalBlock(shellRoot, "InteriorCeiling", new Vector3(0f, firstRoom.WallHeight - firstRoom.FloorThickness * 0.5f, firstRoom.Depth * 0.5f), new Vector3(houseWidth, firstRoom.FloorThickness, firstRoom.Depth), CeilingColor);
        CreateLocalBlock(shellRoot, "BackWall", new Vector3(0f, firstRoom.WallHeight * 0.5f, firstRoom.Depth), new Vector3(houseWidth, firstRoom.WallHeight, 0.18f), ShellColor);
        CreateLocalBlock(shellRoot, "LeftWall", new Vector3(-houseWidth * 0.5f, firstRoom.WallHeight * 0.5f, sideWallCenterZ), new Vector3(firstRoom.WallThickness, firstRoom.WallHeight, sideWallDepth), ShellColor);
        CreateLocalBlock(shellRoot, "RightWall", new Vector3(houseWidth * 0.5f, firstRoom.WallHeight * 0.5f, sideWallCenterZ), new Vector3(firstRoom.WallThickness, firstRoom.WallHeight, sideWallDepth), ShellColor);
        CreateLocalBlock(shellRoot, "RoofCap", new Vector3(0f, firstRoom.WallHeight + 0.2f, sideWallCenterZ), new Vector3(houseWidth + 0.6f, 0.18f, sideWallDepth + 0.26f), new Color(0.15f, 0.14f, 0.14f));
        CreateLocalBlock(shellRoot, "FrontCeilingMask", new Vector3(0f, firstRoom.WallHeight - 0.26f, frontMaskCenterZ), new Vector3(houseWidth + 1f, 0.52f, frontMaskDepth), ShellColor);
        CreateLocalBlock(shellRoot, "FrontLeftReveal", new Vector3(-houseWidth * 0.5f + 0.24f, firstRoom.WallHeight * 0.5f, frontMaskCenterZ), new Vector3(0.48f, firstRoom.WallHeight, frontMaskDepth), ShellColor);
        CreateLocalBlock(shellRoot, "FrontRightReveal", new Vector3(houseWidth * 0.5f - 0.24f, firstRoom.WallHeight * 0.5f, frontMaskCenterZ), new Vector3(0.48f, firstRoom.WallHeight, frontMaskDepth), ShellColor);
        CreateLocalBlock(shellRoot, "FrontTopBand", new Vector3(0f, firstRoom.WallHeight - 0.7f, frontTopBandCenterZ), new Vector3(houseWidth + 0.5f, 0.16f, frontTopBandDepth), ForegroundTrimColor);
        CreateLocalBlock(shellRoot, "FrontFloorLip", new Vector3(0f, 0.16f, -2.72f), new Vector3(houseWidth + 1.2f, 0.32f, 5.9f), ForegroundTrimColor);
        CreateLocalBlock(shellRoot, "FrontFloorSkirt", new Vector3(0f, 0.09f, -5.05f), new Vector3(houseWidth + 1.5f, 0.18f, 0.42f), new Color(0.17f, 0.15f, 0.13f));
    }

    private void BuildRooms()
    {
        float houseWidth = GetHouseWidth();
        float roomStartX = -houseWidth * 0.5f;

        for (int roomIndex = 0; roomIndex < RoomTemplates.Length; roomIndex++)
        {
            StandardRoomTemplate template = RoomTemplates[roomIndex];
            float roomEndX = roomStartX + template.Width;
            float roomCenterX = roomStartX + template.Width * 0.5f;

            Transform roomRoot = CreateGroup(transform, $"Room_{roomIndex + 1}_{template.TemplateId}", new Vector3(roomCenterX, 0f, 0f));
            BuildRoom(roomRoot, template, roomIndex);
            ConfigureRoomLighting(roomRoot, roomStartX, roomEndX, roomIndex);
            CreateRoomLightZoneEffect(roomRoot, template, roomStartX, roomEndX, roomIndex);
            CreateRoomLightRig(roomRoot, roomIndex);
            if (roomIndex == PressureRoomIndex)
            {
                _floorController?.RegisterPressureRoomLights(roomRoot.GetComponentsInChildren<Light>(true));
            }

            RoomCameraZone roomZone = CreateRoomCameraZone(roomRoot, template, roomStartX, roomEndX, roomIndex);
            if (roomIndex == 0)
            {
                _startingRoomZone = roomZone;
            }

            _floorController?.RegisterRoomBounds(roomIndex, roomStartX, roomEndX);

            roomStartX = roomEndX;
        }
    }

    private void BuildRoom(Transform roomRoot, StandardRoomTemplate template, int roomIndex)
    {
        Transform structureRoot = CreateGroup(roomRoot, "Structure");
        Transform foregroundRoot = CreateGroup(roomRoot, "Foreground");
        Transform gameplayRoot = CreateGroup(roomRoot, "Gameplay");
        Transform backgroundRoot = CreateGroup(roomRoot, "Background");
        Transform ceilingRoot = CreateGroup(roomRoot, "Ceiling");
        Transform accentRoot = CreateGroup(roomRoot, "Accents");

        CreateLocalBlocks(structureRoot, template.StructureBlocks, roomIndex);
        CreateLocalBlocks(foregroundRoot, template.ForegroundBlocks, roomIndex);
        CreateLocalBlocks(gameplayRoot, template.GameplayBlocks, roomIndex);
        CreateLocalBlocks(backgroundRoot, template.BackgroundBlocks, roomIndex);
        CreateLocalBlocks(ceilingRoot, template.CeilingBlocks, roomIndex);
        CreateLocalBlocks(accentRoot, template.AccentBlocks, roomIndex);

        if (_floorVariant == FloorVariant.MirrorCorridor)
        {
            BuildMirrorRoom(roomRoot, template, roomIndex);
            return;
        }

        if (roomIndex == LandingRoomIndex)
        {
            BuildLandingRoom(roomRoot, template);
        }

        if (roomIndex == RuleRoomIndex)
        {
            BuildAshParlorFirstBrazier(roomRoot, template);
        }

        if (roomIndex == PressureRoomIndex)
        {
            BuildPressureEnemy(roomRoot, template);
        }

        if (roomIndex == ChoiceRoomIndex)
        {
            BuildChoiceRoom(roomRoot, template);
        }

        if (roomIndex == FinaleRoomIndex)
        {
            BuildFinalEnemy(roomRoot, template);
            BuildRiskRewardPickup(roomRoot, template);
            BuildAshParlorSecondBrazier(roomRoot, template);
            BuildAshParlorExit(roomRoot, template);
        }

    }

    private void BuildMirrorRoom(Transform roomRoot, StandardRoomTemplate template, int roomIndex)
    {
        switch (roomIndex)
        {
            case LandingRoomIndex:
                BuildMirrorLandingRoom(roomRoot, template);
                break;
            case RuleRoomIndex:
                BuildMirrorRuleRoom(roomRoot, template);
                break;
            case PressureRoomIndex:
                BuildMirrorPressureRoom(roomRoot, template);
                break;
            case ChoiceRoomIndex:
                BuildMirrorChoiceRoom(roomRoot, template);
                break;
            case FinaleRoomIndex:
                BuildMirrorFinalRoom(roomRoot, template);
                break;
        }
    }

    private void BuildLandingRoom(Transform roomRoot, StandardRoomTemplate template)
    {
        BuildLandingRelic(roomRoot, template);
        BuildLandingGuide(roomRoot, template);
        BuildLandingTestRoom(roomRoot, template);
    }

    private void BuildMirrorLandingRoom(Transform roomRoot, StandardRoomTemplate template)
    {
        Transform mirrorRoot = CreateGroup(
            roomRoot,
            "MirrorLandingRelic",
            new Vector3(LandingRelicLocalX + 1.6f, 0f, template.WalkDepth + 0.12f));

        CreateDecorBlock(mirrorRoot, "Base", new Vector3(0f, 0.24f, 0f), new Vector3(1.36f, 0.48f, 1f), new Color(0.18f, 0.17f, 0.18f));
        CreateDecorBlock(mirrorRoot, "Frame", new Vector3(0f, 1.34f, 0.06f), new Vector3(0.9f, 1.92f, 0.18f), new Color(0.42f, 0.3f, 0.18f));
        CreateDecorBlock(mirrorRoot, "MirrorFace", new Vector3(0f, 1.34f, 0.16f), new Vector3(0.62f, 1.56f, 0.04f), new Color(0.38f, 0.46f, 0.52f));
        CreatePointLight(mirrorRoot, "MirrorGlow", new Vector3(0f, 1.46f, 0.22f), new Color(0.68f, 0.76f, 0.86f, 1f), 0.38f, 3.2f, LightRenderMode.ForcePixel);

        InspectionInteractable inspectable = mirrorRoot.gameObject.AddComponent<InspectionInteractable>();
        inspectable.Configure(
            "完好的铜镜",
            "查看",
            "伊尔萨恩",
            new[]
            {
                "镜面上没有灰，像有人一直在擦。",
                "这里的反光太慢了，像在等你先动。"
            },
            new[]
            {
                "别在这里盯太久。镜子会记住你停留的时间。"
            },
            new Vector3(0f, 1.2f, 0.1f),
            new Vector3(1.8f, 2.4f, 1.4f));
    }

    private void BuildMirrorRuleRoom(Transform roomRoot, StandardRoomTemplate template)
    {
        Transform etchingRoot = CreateGroup(
            roomRoot,
            "MirrorRuleEtching",
            new Vector3(-9.8f, 0f, template.WalkDepth + 0.28f));
        CreateDecorBlock(etchingRoot, "Stone", new Vector3(0f, 1.22f, 0f), new Vector3(1.56f, 2.28f, 0.44f), new Color(0.22f, 0.21f, 0.22f));
        CreateDecorBlock(etchingRoot, "Etching", new Vector3(0f, 1.36f, 0.24f), new Vector3(1.02f, 1.46f, 0.06f), new Color(0.56f, 0.48f, 0.34f));

        InspectionInteractable inspectable = etchingRoot.gameObject.AddComponent<InspectionInteractable>();
        inspectable.Configure(
            "墙上刻痕",
            "查看",
            "铜镜长廊",
            new[]
            {
                "走廊的尽头不是出口，是镜子。",
                "不要相信镜中所见。也不要不信。"
            },
            new[]
            {
                "刻痕像是被人反复刻深过。"
            },
            new Vector3(0f, 1.18f, 0.08f),
            new Vector3(2.2f, 2.6f, 1.4f));

        AshParlorBrazierInteractable brazier = BuildAshParlorBrazier(roomRoot, template, "MirrorBrazier_1", FirstBrazierLocalX - 0.8f, 1);
        _floorController?.RegisterFirstBrazier(brazier);
    }

    private void BuildMirrorPressureRoom(Transform roomRoot, StandardRoomTemplate template)
    {
        SimpleEnemyController firstEnemy = BuildEnemy(
            roomRoot,
            "Monster_MirrorEcho_A",
            new Vector3(-6.6f, template.PlayerSpawnHeight, template.WalkDepth),
            4,
            1);
        _floorController?.RegisterPressureEnemy(firstEnemy);

        SimpleEnemyController secondEnemy = BuildEnemy(
            roomRoot,
            "Monster_MirrorEcho_B",
            new Vector3(3.8f, template.PlayerSpawnHeight, template.WalkDepth),
            4,
            1);
        _floorController?.RegisterPressureEnemy(secondEnemy);
    }

    private void BuildMirrorChoiceRoom(Transform roomRoot, StandardRoomTemplate template)
    {
        Transform mirrorRoot = CreateGroup(
            roomRoot,
            "MirrorChoiceAnchor",
            new Vector3(ChoiceAnchorLocalX, 0f, template.WalkDepth + 0.2f));
        CreateDecorBlock(mirrorRoot, "MirrorBase", new Vector3(0f, 0.28f, 0f), new Vector3(2.4f, 0.56f, 1.18f), new Color(0.18f, 0.17f, 0.18f));
        CreateDecorBlock(mirrorRoot, "MirrorFrame", new Vector3(0f, 1.74f, 0.08f), new Vector3(2.1f, 2.92f, 0.24f), new Color(0.48f, 0.34f, 0.2f));
        CreateDecorBlock(mirrorRoot, "MirrorFace", new Vector3(0f, 1.74f, 0.2f), new Vector3(1.62f, 2.36f, 0.05f), new Color(0.32f, 0.38f, 0.44f));
        CreatePointLight(mirrorRoot, "MirrorBeacon", new Vector3(0f, 1.86f, 0.28f), new Color(0.76f, 0.72f, 0.58f, 1f), 0.62f, 4.4f, LightRenderMode.ForcePixel);

        BoxCollider interaction = mirrorRoot.gameObject.AddComponent<BoxCollider>();
        interaction.isTrigger = true;
        interaction.center = new Vector3(0f, 1.46f, 0.08f);
        interaction.size = new Vector3(3f, 3.4f, 1.6f);

        AshParlorChoicePromptInteractable prompt = mirrorRoot.gameObject.AddComponent<AshParlorChoicePromptInteractable>();
        prompt.Configure(_floorController, "铜镜", "直面铜镜");
        _floorController?.RegisterChoicePair(null, null, prompt, mirrorRoot);
    }

    private void BuildMirrorFinalRoom(Transform roomRoot, StandardRoomTemplate template)
    {
        SimpleEnemyController enemy = BuildEnemy(
            roomRoot,
            "Monster_MirrorFinalEcho",
            new Vector3(FinalEnemyLocalX + 0.6f, template.PlayerSpawnHeight, template.WalkDepth),
            5,
            1);
        _floorController?.RegisterFinalEnemy(enemy);

        BuildMirrorRewardPickup(roomRoot, template);

        AshParlorBrazierInteractable brazier = BuildAshParlorBrazier(roomRoot, template, "MirrorBrazier_2", SecondBrazierLocalX, 2);
        _floorController?.RegisterSecondBrazier(brazier);

        BuildMirrorExit(roomRoot, template);
    }

    private void BuildLandingTestRoom(Transform roomRoot, StandardRoomTemplate template)
    {
        Transform testRoomRoot = CreateGroup(
            roomRoot,
            "AshFoyer_TestRoom",
            new Vector3(LandingTestRoomLocalX, 0f, template.WalkDepth + 0.12f));

        Renderer floor = CreateDecorBlock(testRoomRoot, "TestFloor", new Vector3(0f, 0.08f, 0f), new Vector3(6.8f, 0.14f, 1.6f), new Color(0.23f, 0.2f, 0.18f));
        Renderer rearWall = CreateDecorBlock(testRoomRoot, "TestRearWall", new Vector3(0f, 1.7f, 0.62f), new Vector3(6.8f, 3.4f, 0.14f), new Color(0.17f, 0.15f, 0.14f));
        Renderer leftWall = CreateDecorBlock(testRoomRoot, "TestLeftWall", new Vector3(-3.18f, 1.7f, 0.24f), new Vector3(0.18f, 3.4f, 0.92f), new Color(0.18f, 0.16f, 0.15f));
        Renderer rightWall = CreateDecorBlock(testRoomRoot, "TestRightWall", new Vector3(3.18f, 1.7f, 0.24f), new Vector3(0.18f, 3.4f, 0.92f), new Color(0.18f, 0.16f, 0.15f));
        Renderer lintel = CreateDecorBlock(testRoomRoot, "TestLintel", new Vector3(0f, 3.18f, 0.24f), new Vector3(6.5f, 0.16f, 0.82f), new Color(0.22f, 0.2f, 0.18f));
        Renderer sign = CreateDecorBlock(testRoomRoot, "TestSign", new Vector3(0f, 2.46f, 0.52f), new Vector3(3.8f, 0.64f, 0.08f), new Color(0.34f, 0.28f, 0.22f));

        DisableDecorCollider(floor);
        DisableDecorCollider(rearWall);
        DisableDecorCollider(leftWall);
        DisableDecorCollider(rightWall);
        DisableDecorCollider(lintel);
        DisableDecorCollider(sign);

        CreatePointLight(
            testRoomRoot,
            "TestRoomGlow",
            new Vector3(0f, 2.36f, 0.18f),
            new Color(0.86f, 0.74f, 0.54f, 1f),
            0.42f,
            4.4f,
            LightRenderMode.ForcePixel);

        BuildTestScenarioInteractable(
            testRoomRoot,
            "RiskSummaryTest",
            LandingTestRiskStationLocalX,
            AshParlorRunController.FloorSummaryTestPreset.RiskSummary,
            "风险结算测试",
            "准备 Risk");

        BuildTestScenarioInteractable(
            testRoomRoot,
            "SafeSummaryTest",
            LandingTestSafeStationLocalX,
            AshParlorRunController.FloorSummaryTestPreset.SafeSummary,
            "安全结算测试",
            "准备 Safe");
    }

    private void BuildLandingIntroTrigger(Transform roomRoot, StandardRoomTemplate template)
    {
        Transform triggerRoot = CreateGroup(roomRoot, "LandingIntroTrigger");
        NarrationTrigger trigger = triggerRoot.gameObject.AddComponent<NarrationTrigger>();
        trigger.Configure(
            "伊尔萨恩",
            new[]
            {
                "这间客厅像被烧毁过，却又被谁按原样拼了回来。",
                "灰封正往更深处收紧。先找到第一盏封印烛台。"
            },
            0,
            new Vector3(-2.6f, 1.08f, template.WalkDepth + 0.16f),
            new Vector3(7.2f, 2.2f, 2.1f));
    }

    private void BuildLandingRelic(Transform roomRoot, StandardRoomTemplate template)
    {
        Transform relicRoot = CreateGroup(
            roomRoot,
            "AshFoyerRelic",
            new Vector3(LandingRelicLocalX, 0f, template.WalkDepth + 0.16f));

        CreateDecorBlock(relicRoot, "Base", new Vector3(0f, 0.28f, 0f), new Vector3(1.22f, 0.56f, 0.92f), new Color(0.25f, 0.22f, 0.2f));
        CreateDecorBlock(relicRoot, "Slab", new Vector3(0f, 1.1f, 0.08f), new Vector3(0.82f, 1.48f, 0.26f), new Color(0.3f, 0.26f, 0.23f));
        CreateDecorBlock(relicRoot, "AshSeal", new Vector3(0f, 1.28f, 0.26f), new Vector3(0.46f, 0.52f, 0.08f), new Color(0.42f, 0.31f, 0.22f));
        CreateDecorBlock(relicRoot, "AshBowl", new Vector3(0.36f, 0.94f, 0.08f), new Vector3(0.32f, 0.18f, 0.32f), new Color(0.34f, 0.24f, 0.18f));

        Transform glowRoot = CreateGroup(relicRoot, "Glow", new Vector3(0.18f, 1.18f, 0.18f));
        Light relicLight = glowRoot.gameObject.AddComponent<Light>();
        relicLight.type = LightType.Point;
        relicLight.shadows = LightShadows.None;
        relicLight.range = 3.6f;
        relicLight.intensity = 0.46f;
        relicLight.color = new Color(0.96f, 0.58f, 0.28f, 1f);

        InspectionInteractable inspectable = relicRoot.gameObject.AddComponent<InspectionInteractable>();
        inspectable.Configure(
            "焦痕誓牌",
            "查看",
            "伊尔萨恩",
            new[]
            {
                "这块誓牌不像被火烧黑的，更像是有人把整层的灰都按在了它上面。",
                "裂痕一路往右边延过去。第一盏封印烛台大概就在前面。"
            },
            new[]
            {
                "灰痕还在往里层裂开。",
                "先把第一盏封印烛台点亮。"
            },
            new Vector3(0f, 1.02f, 0.06f),
            new Vector3(1.65f, 2.05f, 1.35f));
    }

    private void BuildLandingGuide(Transform roomRoot, StandardRoomTemplate template)
    {
        Transform guideRoot = CreateGroup(
            roomRoot,
            "AshFoyerGuide",
            new Vector3(LandingGuideLocalX, 0f, template.WalkDepth + LandingGuideLocalZOffset));

        CreateDecorBlock(guideRoot, "GuideStone", new Vector3(0f, 0.22f, 0f), new Vector3(0.76f, 0.44f, 0.58f), new Color(0.28f, 0.23f, 0.2f));
        CreateDecorBlock(guideRoot, "GuideAsh_0", new Vector3(-0.5f, 0.26f, 0.04f), new Vector3(0.18f, 0.08f, 0.18f), new Color(0.56f, 0.34f, 0.18f));
        CreateDecorBlock(guideRoot, "GuideAsh_1", new Vector3(0f, 0.3f, 0.06f), new Vector3(0.24f, 0.1f, 0.22f), new Color(0.68f, 0.4f, 0.2f));
        CreateDecorBlock(guideRoot, "GuideAsh_2", new Vector3(0.44f, 0.34f, 0.08f), new Vector3(0.18f, 0.08f, 0.18f), new Color(0.84f, 0.48f, 0.24f));

        CreatePointLight(
            guideRoot,
            "GuideGlow",
            new Vector3(0.08f, 1.08f, 0.16f),
            new Color(1f, 0.62f, 0.3f, 1f),
            0.58f,
            4.6f,
            LightRenderMode.ForcePixel);

        CreateSpotLight(
            guideRoot,
            "GuideSpill",
            new Vector3(0.1f, 1.16f, 0.12f),
            new Vector3(0.88f, -0.42f, -0.06f),
            new Color(1f, 0.7f, 0.42f, 1f),
            0.72f,
            5.2f,
            74f,
            46f,
            LightRenderMode.ForcePixel);
    }

    private void BuildTestScenarioInteractable(
        Transform parent,
        string objectName,
        float localX,
        AshParlorRunController.FloorSummaryTestPreset preset,
        string displayName,
        string promptText)
    {
        Transform stationRoot = CreateGroup(
            parent,
            objectName,
            new Vector3(localX, 0f, 0.02f));

        List<Renderer> renderers = new List<Renderer>();
        renderers.Add(CreateDecorBlock(stationRoot, "Base", new Vector3(0f, 0.28f, 0f), new Vector3(1.08f, 0.56f, 0.96f), new Color(0.22f, 0.2f, 0.19f)));
        renderers.Add(CreateDecorBlock(stationRoot, "Column", new Vector3(0f, 1.04f, 0.04f), new Vector3(0.58f, 0.96f, 0.52f), new Color(0.32f, 0.26f, 0.22f)));
        renderers.Add(CreateDecorBlock(stationRoot, "Head", new Vector3(0f, 1.74f, 0.06f), new Vector3(0.94f, 0.36f, 0.16f), new Color(0.4f, 0.31f, 0.23f)));

        Transform lightRoot = CreateGroup(stationRoot, "Accent", new Vector3(0f, 1.3f, 0.08f));
        Light stationLight = lightRoot.gameObject.AddComponent<Light>();
        stationLight.type = LightType.Point;
        stationLight.shadows = LightShadows.None;
        stationLight.range = 3.6f;
        stationLight.intensity = 0.9f;

        BoxCollider interaction = stationRoot.gameObject.AddComponent<BoxCollider>();
        interaction.isTrigger = true;
        interaction.center = new Vector3(0f, 1.02f, 0.02f);
        interaction.size = new Vector3(1.7f, 2.1f, 1.3f);

        AshParlorTestRoomInteractable interactable = stationRoot.gameObject.AddComponent<AshParlorTestRoomInteractable>();
        interactable.Configure(_ashParlorController, preset, stationLight, renderers.ToArray(), displayName, promptText);
    }

    private void BuildAshParlorFirstBrazier(Transform roomRoot, StandardRoomTemplate template)
    {
        AshParlorBrazierInteractable brazier = BuildAshParlorBrazier(roomRoot, template, "AshBrazier_1", FirstBrazierLocalX, 1);
        _floorController?.RegisterFirstBrazier(brazier);
    }

    private void BuildPressureEnemy(Transform roomRoot, StandardRoomTemplate template)
    {
        SimpleEnemyController enemy = BuildEnemy(
            roomRoot,
            "Monster_PressureEcho",
            new Vector3(PressureEnemyLocalX, template.PlayerSpawnHeight, template.WalkDepth),
            4,
            1);
        _floorController?.RegisterPressureEnemy(enemy);
    }

    private void BuildFinalEnemy(Transform roomRoot, StandardRoomTemplate template)
    {
        SimpleEnemyController enemy = BuildEnemy(
            roomRoot,
            "Monster_FinalEcho",
            new Vector3(FinalEnemyLocalX, template.PlayerSpawnHeight, template.WalkDepth),
            5,
            1);
        _floorController?.RegisterFinalEnemy(enemy);

        SimpleEnemyController riskEnemy = BuildEnemy(
            roomRoot,
            "Monster_FinalEcho_Risk",
            new Vector3(FinalRiskEnemyLocalX, template.PlayerSpawnHeight, template.WalkDepth),
            4,
            1);
        _floorController?.RegisterRiskBonusEnemy(riskEnemy);
    }

    private SimpleEnemyController BuildEnemy(Transform roomRoot, string objectName, Vector3 localPosition, int healthPoints, int damage)
    {
        CorePrefabCatalog catalog = CorePrefabCatalog.Load();
        GameObject enemyPrefab = catalog != null ? catalog.StandardEnemyPrefab : null;
        if (enemyPrefab == null)
        {
            Debug.LogError("[TowerInteriorSlice] Missing StandardEnemy prefab in CorePrefabCatalog.");
            return null;
        }

        float desiredMonsterHeight = PlayerReferenceHeight * MonsterHeightRatio;
        GameObject enemyObject = Instantiate(enemyPrefab, roomRoot);
        enemyObject.name = objectName;
        enemyObject.transform.localPosition = localPosition;
        enemyObject.transform.localRotation = Quaternion.identity;
        enemyObject.transform.localScale = Vector3.one;

        MonsterSpriteVisual visual = enemyObject.GetComponent<MonsterSpriteVisual>();
        if (visual != null)
        {
            visual.Configure(desiredMonsterHeight, MonsterSortingOrder, new Vector3(0f, 0.01f, 0f));
        }

        CombatantHealth health = enemyObject.GetComponent<CombatantHealth>();
        if (health != null)
        {
            health.Configure(healthPoints);
        }

        SimpleEnemyController enemy = enemyObject.GetComponent<SimpleEnemyController>();
        if (enemy == null)
        {
            Debug.LogError($"[TowerInteriorSlice] StandardEnemy prefab is missing {nameof(SimpleEnemyController)}.", enemyObject);
            return null;
        }

        enemy.Configure("仪式回响", healthPoints, damage);
        return enemy;
    }

    private void BuildChoiceRoom(Transform roomRoot, StandardRoomTemplate template)
    {
        Transform anchor = CreateGroup(
            roomRoot,
            "ChoiceAnchor",
            new Vector3(ChoiceAnchorLocalX, 1.2f, template.WalkDepth + BrazierLocalZOffset));

        AshParlorChoicePromptInteractable prompt = BuildChoicePromptInteractable(roomRoot, template);

        AshParlorChoiceInteractable risky = BuildChoiceInteractable(
            roomRoot,
            template,
            "Choice_Risky",
            ChoiceRiskyLocalX,
            AshParlorChoiceInteractable.ChoiceKind.Risky,
            "低吼回廊",
            "走向低吼");

        AshParlorChoiceInteractable safe = BuildChoiceInteractable(
            roomRoot,
            template,
            "Choice_Safe",
            ChoiceSafeLocalX,
            AshParlorChoiceInteractable.ChoiceKind.Safe,
            "沉寂回廊",
            "走向沉寂");

        _floorController?.RegisterChoicePair(risky, safe, prompt, prompt != null ? prompt.transform : anchor);
    }

    private AshParlorChoicePromptInteractable BuildChoicePromptInteractable(Transform roomRoot, StandardRoomTemplate template)
    {
        Transform promptRoot = CreateGroup(
            roomRoot,
            "ChoicePrompt",
            new Vector3(ChoiceAnchorLocalX, 0f, template.WalkDepth + 0.14f));

        CreateDecorBlock(promptRoot, "Base", new Vector3(0f, 0.24f, 0f), new Vector3(1.4f, 0.48f, 1.2f), new Color(0.24f, 0.22f, 0.2f));
        CreateDecorBlock(promptRoot, "Tablet", new Vector3(0f, ChoicePromptLocalY, 0.02f), new Vector3(0.82f, 1.32f, 0.16f), new Color(0.34f, 0.28f, 0.22f));
        CreateDecorBlock(promptRoot, "MarkerBar", new Vector3(0f, 1.82f, 0.06f), new Vector3(1.6f, 0.08f, 0.1f), new Color(0.76f, 0.63f, 0.34f));

        Transform lightRoot = CreateGroup(promptRoot, "ChoiceBeacon", new Vector3(0f, 1.48f, 0.14f));
        Light promptLight = lightRoot.gameObject.AddComponent<Light>();
        promptLight.type = LightType.Point;
        promptLight.shadows = LightShadows.None;
        promptLight.range = 3.4f;
        promptLight.intensity = 0.96f;
        promptLight.color = new Color(0.92f, 0.78f, 0.46f, 1f);

        BoxCollider interaction = promptRoot.gameObject.AddComponent<BoxCollider>();
        interaction.isTrigger = true;
        interaction.center = new Vector3(0f, 1.08f, 0.02f);
        interaction.size = new Vector3(2.2f, 2.2f, 1.8f);

        AshParlorChoicePromptInteractable prompt = promptRoot.gameObject.AddComponent<AshParlorChoicePromptInteractable>();
        prompt.Configure(_floorController, "抉择台", "做出选择");
        return prompt;
    }

    private AshParlorChoiceInteractable BuildChoiceInteractable(
        Transform roomRoot,
        StandardRoomTemplate template,
        string objectName,
        float localX,
        AshParlorChoiceInteractable.ChoiceKind choiceKind,
        string displayName,
        string promptText)
    {
        Transform choiceRoot = CreateGroup(
            roomRoot,
            objectName,
            new Vector3(localX, 0f, template.WalkDepth + BrazierLocalZOffset));

        List<Renderer> renderers = new List<Renderer>();
        renderers.Add(CreateDecorBlock(choiceRoot, "Pedestal", new Vector3(0f, 0.32f, 0f), new Vector3(0.92f, 0.64f, 0.92f), new Color(0.22f, 0.2f, 0.19f)));
        renderers.Add(CreateDecorBlock(choiceRoot, "Head", new Vector3(0f, 1.02f, 0.04f), new Vector3(0.66f, 0.52f, 0.66f), new Color(0.3f, 0.24f, 0.2f)));

        Transform lightRoot = CreateGroup(choiceRoot, "Accent", new Vector3(0f, 1.26f, 0.06f));
        Light choiceLight = lightRoot.gameObject.AddComponent<Light>();
        choiceLight.type = LightType.Point;
        choiceLight.shadows = LightShadows.None;
        choiceLight.range = 4.2f;
        choiceLight.intensity = 1.1f;
        choiceLight.color = choiceKind == AshParlorChoiceInteractable.ChoiceKind.Risky
            ? new Color(0.86f, 0.38f, 0.18f, 1f)
            : new Color(0.58f, 0.68f, 0.78f, 1f);

        AshParlorChoiceInteractable interactable = choiceRoot.gameObject.AddComponent<AshParlorChoiceInteractable>();
        interactable.Configure(_floorController, choiceKind, choiceLight, renderers.ToArray(), displayName, promptText);
        return interactable;
    }

    private void BuildAshParlorSecondBrazier(Transform roomRoot, StandardRoomTemplate template)
    {
        AshParlorBrazierInteractable brazier = BuildAshParlorBrazier(roomRoot, template, "AshBrazier_2", SecondBrazierLocalX, 2);
        _floorController?.RegisterSecondBrazier(brazier);
    }

    private void BuildRiskRewardPickup(Transform roomRoot, StandardRoomTemplate template)
    {
        Transform rewardRoot = CreateGroup(
            roomRoot,
            "AshParlor_RiskReward",
            new Vector3(FinalRewardLocalX, 0f, template.WalkDepth + 0.28f));

        CreateDecorBlock(rewardRoot, "CrystalCore", new Vector3(0f, 0.78f, 0.02f), new Vector3(0.34f, 0.68f, 0.34f), new Color(0.42f, 0.56f, 0.88f));
        CreateDecorBlock(rewardRoot, "CrystalShard_Left", new Vector3(-0.22f, 0.5f, 0.04f), new Vector3(0.18f, 0.34f, 0.18f), new Color(0.3f, 0.42f, 0.78f));
        CreateDecorBlock(rewardRoot, "CrystalShard_Right", new Vector3(0.2f, 0.44f, 0.03f), new Vector3(0.16f, 0.28f, 0.16f), new Color(0.34f, 0.45f, 0.82f));

        Transform lightRoot = CreateGroup(rewardRoot, "Glow", new Vector3(0f, 0.86f, 0.06f));
        Light rewardLight = lightRoot.gameObject.AddComponent<Light>();
        rewardLight.type = LightType.Point;
        rewardLight.shadows = LightShadows.None;
        rewardLight.range = 2.8f;
        rewardLight.intensity = 0.8f;
        rewardLight.color = new Color(0.46f, 0.58f, 0.96f, 1f);

        PickupInteractable pickup = rewardRoot.gameObject.AddComponent<PickupInteractable>();
        pickup.Configure(
            "ash_parlor_rift_whisper",
            "裂隙中的低语",
            "拾起结晶",
            true,
            "叙事线索",
            "一块从残响体内掉落的结晶。凑近耳边，能听到模糊的声音：'……第三层……门后面……不要打开……'",
            "伊尔萨恩",
            new[]
            {
                "这块结晶还是温热的。里面有人在说话。",
                "第三层……记住了。"
            });
        pickup.SetPickupEnabled(false);
        _floorController?.RegisterRiskRewardPickup(pickup);
    }

    private void BuildMirrorRewardPickup(Transform roomRoot, StandardRoomTemplate template)
    {
        Transform rewardRoot = CreateGroup(
            roomRoot,
            "MirrorCorridor_RiskReward",
            new Vector3(FinalRewardLocalX + 0.4f, 0f, template.WalkDepth + 0.26f));

        CreateDecorBlock(rewardRoot, "Shard_A", new Vector3(-0.18f, 0.82f, 0.04f), new Vector3(0.18f, 0.72f, 0.08f), new Color(0.52f, 0.62f, 0.72f));
        CreateDecorBlock(rewardRoot, "Shard_B", new Vector3(0.12f, 0.64f, 0.08f), new Vector3(0.14f, 0.46f, 0.06f), new Color(0.6f, 0.7f, 0.82f));
        CreateDecorBlock(rewardRoot, "Shard_C", new Vector3(0.28f, 0.52f, -0.04f), new Vector3(0.12f, 0.34f, 0.06f), new Color(0.46f, 0.58f, 0.7f));
        CreatePointLight(rewardRoot, "ShardGlow", new Vector3(0.02f, 0.86f, 0.12f), new Color(0.72f, 0.78f, 0.9f, 1f), 0.58f, 2.8f, LightRenderMode.ForcePixel);

        PickupInteractable pickup = rewardRoot.gameObject.AddComponent<PickupInteractable>();
        pickup.Configure(
            "mirror_corridor_shard",
            "铜镜碎片",
            "拾起碎片",
            true,
            "叙事线索",
            "一块边缘还在反光的铜镜碎片。凑近时，碎片里映出一扇刻着你名字的门。",
            "伊尔萨恩",
            new[]
            {
                "这不是过去的倒影。",
                "它像是在提前演给我看。"
            });
        pickup.SetPickupEnabled(false);
        _floorController?.RegisterRiskRewardPickup(pickup);
    }

    private AshParlorBrazierInteractable BuildAshParlorBrazier(Transform roomRoot, StandardRoomTemplate template, string objectName, float localX, int index)
    {
        CorePrefabCatalog catalog = CorePrefabCatalog.Load();
        GameObject brazierPrefab = catalog != null ? catalog.BrazierPrefab : null;
        if (brazierPrefab == null)
        {
            Debug.LogError("[TowerInteriorSlice] Missing Brazier prefab in CorePrefabCatalog.");
            return null;
        }

        GameObject brazierObject = Instantiate(brazierPrefab, roomRoot);
        brazierObject.name = objectName;
        brazierObject.transform.localPosition = new Vector3(localX, 0f, template.WalkDepth + BrazierLocalZOffset);
        brazierObject.transform.localRotation = Quaternion.identity;
        brazierObject.transform.localScale = Vector3.one;

        AshParlorBrazierInteractable brazier = brazierObject.GetComponent<AshParlorBrazierInteractable>();
        if (brazier == null)
        {
            Debug.LogError($"[TowerInteriorSlice] Brazier prefab is missing {nameof(AshParlorBrazierInteractable)}.", brazierObject);
            return null;
        }

        brazier.Configure(_floorController, index);
        return brazier;
    }

    private void BuildAshParlorExit(Transform roomRoot, StandardRoomTemplate template)
    {
        Transform exitRoot = CreateGroup(
            roomRoot,
            "AshParlor_Exit",
            new Vector3(ExitLocalX, 0f, template.WalkDepth + ExitLocalZOffset));

        List<Renderer> renderers = new List<Renderer>();
        renderers.Add(CreateDecorBlock(exitRoot, "Frame_Left", new Vector3(-0.76f, 1.65f, 0f), new Vector3(0.24f, 3.3f, 0.54f), new Color(0.24f, 0.22f, 0.21f)));
        renderers.Add(CreateDecorBlock(exitRoot, "Frame_Right", new Vector3(0.76f, 1.65f, 0f), new Vector3(0.24f, 3.3f, 0.54f), new Color(0.24f, 0.22f, 0.21f)));
        renderers.Add(CreateDecorBlock(exitRoot, "Frame_Top", new Vector3(0f, 3.18f, 0f), new Vector3(1.84f, 0.22f, 0.54f), new Color(0.24f, 0.22f, 0.21f)));
        renderers.Add(CreateDecorBlock(exitRoot, "Seal", new Vector3(0f, 1.52f, 0.04f), new Vector3(1.22f, 2.7f, 0.18f), new Color(0.2f, 0.19f, 0.2f)));
        renderers.Add(CreateDecorBlock(exitRoot, "LadderGlow", new Vector3(0f, 1.48f, -0.08f), new Vector3(0.72f, 2.2f, 0.08f), new Color(0.28f, 0.22f, 0.18f)));

        Transform lightRoot = CreateGroup(exitRoot, "SealLight", new Vector3(0f, 1.8f, 0.12f));
        Light exitLight = lightRoot.gameObject.AddComponent<Light>();
        exitLight.type = LightType.Point;
        exitLight.shadows = LightShadows.None;
        exitLight.range = 2f;
        exitLight.intensity = 0.12f;
        exitLight.color = new Color(0.24f, 0.18f, 0.16f, 1f);

        BoxCollider interaction = exitRoot.gameObject.AddComponent<BoxCollider>();
        interaction.isTrigger = true;
        interaction.center = new Vector3(0f, 1.55f, 0f);
        interaction.size = new Vector3(2.4f, 3.5f, 1.4f);

        AshParlorExitInteractable exitInteractable = exitRoot.gameObject.AddComponent<AshParlorExitInteractable>();
        exitInteractable.Configure(_floorController, exitLight, renderers.ToArray());
        _floorController?.RegisterExit(exitInteractable);
    }

    private void BuildMirrorExit(Transform roomRoot, StandardRoomTemplate template)
    {
        Transform exitRoot = CreateGroup(
            roomRoot,
            "MirrorCorridor_Exit",
            new Vector3(ExitLocalX - 0.8f, 0f, template.WalkDepth + ExitLocalZOffset));

        List<Renderer> renderers = new List<Renderer>();
        renderers.Add(CreateDecorBlock(exitRoot, "Frame_Left", new Vector3(-0.72f, 1.65f, 0f), new Vector3(0.24f, 3.3f, 0.54f), new Color(0.22f, 0.22f, 0.24f)));
        renderers.Add(CreateDecorBlock(exitRoot, "Frame_Right", new Vector3(0.72f, 1.65f, 0f), new Vector3(0.24f, 3.3f, 0.54f), new Color(0.22f, 0.22f, 0.24f)));
        renderers.Add(CreateDecorBlock(exitRoot, "Lintel", new Vector3(0f, 3.18f, 0f), new Vector3(1.8f, 0.22f, 0.54f), new Color(0.22f, 0.22f, 0.24f)));
        renderers.Add(CreateDecorBlock(exitRoot, "DoorGlow", new Vector3(0f, 1.52f, -0.04f), new Vector3(1.08f, 2.62f, 0.08f), new Color(0.38f, 0.32f, 0.18f)));
        renderers.Add(CreateDecorBlock(exitRoot, "MirrorSeal", new Vector3(0f, 1.52f, 0.06f), new Vector3(1.16f, 2.72f, 0.14f), new Color(0.2f, 0.22f, 0.26f)));

        Transform lightRoot = CreateGroup(exitRoot, "SealLight", new Vector3(0f, 1.82f, 0.12f));
        Light exitLight = lightRoot.gameObject.AddComponent<Light>();
        exitLight.type = LightType.Point;
        exitLight.shadows = LightShadows.None;
        exitLight.range = 2.2f;
        exitLight.intensity = 0.14f;
        exitLight.color = new Color(0.3f, 0.28f, 0.18f, 1f);

        BoxCollider interaction = exitRoot.gameObject.AddComponent<BoxCollider>();
        interaction.isTrigger = true;
        interaction.center = new Vector3(0f, 1.55f, 0f);
        interaction.size = new Vector3(2.4f, 3.5f, 1.4f);

        AshParlorExitInteractable exitInteractable = exitRoot.gameObject.AddComponent<AshParlorExitInteractable>();
        exitInteractable.Configure(_floorController, exitLight, renderers.ToArray());
        _floorController?.RegisterExit(exitInteractable);
    }

    private static Renderer CreateDecorBlock(Transform parent, string name, Vector3 localCenter, Vector3 size, Color color)
    {
        GameObject block = CreatePrimitiveBlock(name, size, color);
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localCenter;
        return block.GetComponent<Renderer>();
    }

    private static void DisableDecorCollider(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Collider colliderComponent = renderer.GetComponent<Collider>();
        if (colliderComponent != null)
        {
            colliderComponent.enabled = false;
        }
    }

    private RoomCameraZone CreateRoomCameraZone(Transform roomRoot, StandardRoomTemplate template, float roomStartX, float roomEndX, int roomIndex)
    {
        Transform zoneRoot = CreateGroup(roomRoot, $"CameraZone_{roomIndex + 1}");
        RoomCameraZone zone = zoneRoot.gameObject.AddComponent<RoomCameraZone>();

        float overlapLeft = roomIndex == 0 ? 0f : RoomZoneOverlap;
        float overlapRight = roomIndex == RoomTemplates.Length - 1 ? 0f : RoomZoneOverlap;
        float triggerWidth = template.Width + overlapLeft + overlapRight;
        float localCenterX = (overlapRight - overlapLeft) * 0.5f;

        zone.Configure(
            new Vector3(localCenterX, RoomZoneCenterY, template.WalkDepth),
            new Vector3(triggerWidth, template.WallHeight, RoomZoneDepth),
            new Vector2(roomStartX + template.CameraTrackInset, roomEndX - template.CameraTrackInset),
            template.CameraOffsetDelta,
            template.RoomBlendDuration);

        return zone;
    }

    private void BuildRoomDividers()
    {
        if (RoomTemplates.Length <= 1)
        {
            return;
        }

        Transform dividerRoot = CreateGroup(transform, "Dividers");
        float houseWidth = GetHouseWidth();
        float cursorX = -houseWidth * 0.5f;

        for (int dividerIndex = 0; dividerIndex < RoomTemplates.Length - 1; dividerIndex++)
        {
            StandardRoomTemplate left = RoomTemplates[dividerIndex];
            StandardRoomTemplate right = RoomTemplates[dividerIndex + 1];
            cursorX += left.Width;

            float dividerX = cursorX;
            float depth = left.Depth;
            float doorHeight = Mathf.Min(left.DoorHeight, right.DoorHeight);
            float upperSectionHeight = left.WallHeight - doorHeight;
            float upperSectionCenterY = doorHeight + upperSectionHeight * 0.5f;
            string dividerPrefix = $"Divider_{dividerIndex + 1}";

            CreateLocalBlock(dividerRoot, $"{dividerPrefix}_Upper", new Vector3(dividerX, upperSectionCenterY, depth * 0.5f), new Vector3(left.WallThickness, upperSectionHeight, depth), DividerColor);
            CreateLocalBlock(dividerRoot, $"{dividerPrefix}_Header", new Vector3(dividerX, doorHeight - 0.08f, depth * 0.5f), new Vector3(left.WallThickness + 0.02f, 0.16f, depth), new Color(0.29f, 0.26f, 0.23f));
            CreateLocalBlock(dividerRoot, $"{dividerPrefix}_Jamb_Front", new Vector3(dividerX, doorHeight * 0.5f, 0.96f), new Vector3(left.WallThickness + 0.04f, doorHeight, 0.3f), ForegroundTrimColor);
            CreateLocalBlock(dividerRoot, $"{dividerPrefix}_Jamb_Back", new Vector3(dividerX, doorHeight * 0.5f, depth - 0.28f), new Vector3(left.WallThickness + 0.02f, doorHeight, 0.28f), DividerColor);
        }
    }

    private void BuildFloorSeals()
    {
        BuildSealBarrier(1, "Seal_PressureDoor", registerPressure: true);
        BuildSealBarrier(3, "Seal_FinaleDoor", registerPressure: false);
    }

    private void BuildSealBarrier(int dividerIndex, string objectName, bool registerPressure)
    {
        if (dividerIndex < 0 || dividerIndex >= RoomTemplates.Length - 1)
        {
            return;
        }

        float houseWidth = GetHouseWidth();
        float cursorX = -houseWidth * 0.5f;

        for (int i = 0; i <= dividerIndex; i++)
        {
            cursorX += RoomTemplates[i].Width;
        }

        StandardRoomTemplate left = RoomTemplates[dividerIndex];
        StandardRoomTemplate right = RoomTemplates[dividerIndex + 1];
        float doorHeight = Mathf.Min(left.DoorHeight, right.DoorHeight);
        float barrierDepth = 1.24f;

        Transform sealRoot = CreateGroup(transform, objectName, new Vector3(cursorX, 0f, left.WalkDepth + 0.18f));
        GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrier.name = "Barrier";
        barrier.transform.SetParent(sealRoot, false);
        barrier.transform.localPosition = new Vector3(0f, doorHeight * 0.5f, 0f);
        barrier.transform.localScale = new Vector3(0.34f, doorHeight, barrierDepth);

        Renderer barrierRenderer = barrier.GetComponent<Renderer>();
        if (barrierRenderer != null)
        {
            Tint(barrierRenderer, new Color(0.56f, 0.3f, 0.18f));
        }

        Transform lightRoot = CreateGroup(sealRoot, "Glow", new Vector3(0f, doorHeight * 0.6f, 0f));
        Light glow = lightRoot.gameObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.shadows = LightShadows.None;
        glow.range = 3.4f;
        glow.intensity = 1.15f;
        glow.color = new Color(0.82f, 0.44f, 0.22f, 1f);

        AshParlorSealBarrier seal = sealRoot.gameObject.AddComponent<AshParlorSealBarrier>();
        seal.Configure(new[] { barrierRenderer }, new[] { barrier.GetComponent<Collider>() }, glow);

        if (registerPressure)
        {
            _floorController?.RegisterPressureSeal(seal);
        }
        else
        {
            _floorController?.RegisterFinaleSeal(seal);
        }
    }

    private void ConfigureRoomLighting(Transform roomRoot, float roomStartX, float roomEndX, int roomIndex)
    {
        if (roomRoot == null)
        {
            return;
        }

        RoomLightingZone lightingZone = roomRoot.GetComponent<RoomLightingZone>();
        if (lightingZone == null)
        {
            lightingZone = roomRoot.gameObject.AddComponent<RoomLightingZone>();
        }

        if (_floorVariant == FloorVariant.MirrorCorridor)
        {
            ConfigureMirrorRoomLighting(lightingZone, roomStartX, roomEndX, roomIndex);
            return;
        }

        switch (roomIndex)
        {
            case LandingRoomIndex:
                lightingZone.Configure(
                    new Vector2(roomStartX, roomEndX),
                    new Color(0.28f, 0.24f, 0.2f, 1f),
                    0.24f,
                    new Color(0.9f, 0.72f, 0.46f, 1f),
                    0.12f,
                    new Vector3(0.02f, -1f, -0.1f),
                    new Color(0.92f, 0.8f, 0.66f, 1f),
                    0.08f,
                    0.74f,
                    0.2f,
                    new Color(0.034f, 0.028f, 0.024f, 1f),
                    0.82f,
                    new RoomLightingZone.LocalLightConfig(new Vector3(2.4f, 4.28f, 2.72f), new Color(1f, 0.82f, 0.58f, 1f), 1.38f, 8.2f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(-8.8f, 1.56f, 2.22f), new Color(0.95f, 0.67f, 0.42f, 1f), 0.48f, 3.4f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(0.2f, 1.62f, 7.04f), new Color(0.82f, 0.58f, 0.38f, 1f), 0.36f, 3.4f));
                break;
            case RuleRoomIndex:
                lightingZone.Configure(
                    new Vector2(roomStartX, roomEndX),
                    new Color(0.22f, 0.19f, 0.17f, 1f),
                    0.18f,
                    new Color(0.96f, 0.56f, 0.28f, 1f),
                    0.12f,
                    new Vector3(0.2f, -0.98f, -0.04f),
                    new Color(1f, 0.82f, 0.64f, 1f),
                    0.07f,
                    0.76f,
                    0.14f,
                    new Color(0.035f, 0.026f, 0.02f, 1f),
                    0.8f,
                    new RoomLightingZone.LocalLightConfig(new Vector3(8.6f, 1.18f, 0.6f), new Color(1f, 0.58f, 0.22f, 1f), 1.95f, 6.4f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(-7.2f, 1.56f, 2.5f), new Color(0.78f, 0.64f, 0.46f, 1f), 0.46f, 3.4f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(-12.2f, 2.02f, 6.92f), new Color(0.66f, 0.5f, 0.34f, 1f), 0.24f, 2.8f));
                break;
            case PressureRoomIndex:
                lightingZone.Configure(
                    new Vector2(roomStartX, roomEndX),
                    new Color(0.17f, 0.145f, 0.14f, 1f),
                    0.2f,
                    new Color(0.68f, 0.34f, 0.22f, 1f),
                    0.14f,
                    new Vector3(-0.12f, -0.99f, -0.05f),
                    new Color(0.72f, 0.56f, 0.48f, 1f),
                    0.08f,
                    0.78f,
                    0.14f,
                    new Color(0.03f, 0.02f, 0.018f, 1f),
                    0.84f,
                    new RoomLightingZone.LocalLightConfig(new Vector3(-1.2f, 3.48f, 3.18f), new Color(0.72f, 0.4f, 0.28f, 1f), 0.95f, 11.2f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(2.8f, 4.24f, 3.52f), new Color(0.84f, 0.48f, 0.32f, 1f), 0.88f, 6.2f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(-12.6f, 4.02f, 4.42f), new Color(0.54f, 0.28f, 0.2f, 1f), 0.34f, 4.4f));
                break;
            case ChoiceRoomIndex:
                lightingZone.Configure(
                    new Vector2(roomStartX, roomEndX),
                    new Color(0.21f, 0.19f, 0.18f, 1f),
                    0.18f,
                    new Color(0.82f, 0.74f, 0.62f, 1f),
                    0.09f,
                    new Vector3(0f, -1f, -0.08f),
                    new Color(0.9f, 0.84f, 0.74f, 1f),
                    0.07f,
                    0.72f,
                    0.16f,
                    new Color(0.03f, 0.028f, 0.028f, 1f),
                    0.74f,
                    new RoomLightingZone.LocalLightConfig(new Vector3(0.6f, 4.18f, 3.44f), new Color(0.96f, 0.82f, 0.6f, 1f), 0.72f, 6.6f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(-9.4f, 1.26f, 0.48f), new Color(0.82f, 0.38f, 0.2f, 1f), 1.25f, 5.4f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(9.4f, 1.26f, 0.48f), new Color(0.56f, 0.66f, 0.74f, 1f), 1.1f, 5.4f));
                break;
            default:
                lightingZone.Configure(
                    new Vector2(roomStartX, roomEndX),
                    new Color(0.19f, 0.17f, 0.16f, 1f),
                    0.15f,
                    new Color(0.92f, 0.54f, 0.3f, 1f),
                    0.09f,
                    new Vector3(0.08f, -0.99f, -0.08f),
                    new Color(1f, 0.76f, 0.6f, 1f),
                    0.08f,
                    0.78f,
                    0.12f,
                    new Color(0.03f, 0.022f, 0.022f, 1f),
                    0.84f,
                    new RoomLightingZone.LocalLightConfig(new Vector3(7.8f, 1.18f, 0.6f), new Color(1f, 0.58f, 0.22f, 1f), 1.85f, 6.2f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(15.8f, 4.26f, 1.48f), new Color(0.92f, 0.74f, 0.48f, 1f), 0.84f, 5.8f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(16.5f, 1.8f, 0.54f), new Color(0.9f, 0.66f, 0.32f, 1f), 1.28f, 6f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(-11.6f, 4.04f, 4.36f), new Color(0.68f, 0.4f, 0.24f, 1f), 0.14f, 2.8f));
                break;
        }
    }

    private void CreateRoomLightRig(Transform roomRoot, int roomIndex)
    {
        if (roomRoot == null)
        {
            return;
        }

        Transform lightRoot = CreateGroup(roomRoot, "Lights");

        if (_floorVariant == FloorVariant.MirrorCorridor)
        {
            CreateMirrorRoomLightRig(lightRoot, roomIndex);
            return;
        }

        switch (roomIndex)
        {
            case LandingRoomIndex:
                CreatePendantRig(lightRoot, "TablePendant", new Vector3(2.4f, 4.28f, 2.72f), new Color(1f, 0.82f, 0.58f, 1f), 0.88f, 6.8f, new Color(1f, 0.78f, 0.52f, 1f), 1.48f, 7.8f);
                CreateDeskLampRig(lightRoot, "DeskLamp", new Vector3(-8.8f, 1.56f, 2.22f), new Vector3(0.12f, -0.98f, -0.12f), new Color(0.95f, 0.67f, 0.42f, 1f), 0.4f, 2.9f, 0.44f, 3.2f);
                CreateDeskLampRig(lightRoot, "RearConsoleLamp", new Vector3(0.2f, 1.62f, 7.04f), new Vector3(-0.04f, -0.98f, -0.16f), new Color(0.82f, 0.58f, 0.38f, 1f), 0.26f, 2.8f, 0.3f, 3.1f);
                break;
            case RuleRoomIndex:
                CreateDeskLampRig(lightRoot, "ArchiveTaskLamp", new Vector3(-7.2f, 1.56f, 2.5f), new Vector3(0.18f, -0.98f, -0.1f), new Color(0.78f, 0.64f, 0.46f, 1f), 0.32f, 2.8f, 0.42f, 3.2f);
                CreateDeskLampRig(lightRoot, "BackShelfLamp", new Vector3(-12.2f, 2.02f, 6.92f), new Vector3(0.18f, -0.96f, -0.12f), new Color(0.66f, 0.5f, 0.34f, 1f), 0.18f, 2.2f, 0.22f, 2.6f);
                break;
            case PressureRoomIndex:
                CreatePointLight(lightRoot, "RoomWarningWash", new Vector3(-1.2f, 3.48f, 3.18f), new Color(0.72f, 0.4f, 0.28f, 1f), 0.72f, 13.2f, LightRenderMode.ForcePixel);
                CreateSpotLight(lightRoot, "RoomWarningDown", new Vector3(-0.8f, 4.18f, 2.96f), new Vector3(-0.04f, -0.98f, -0.12f), new Color(0.76f, 0.42f, 0.28f, 1f), 1.18f, 13.8f, 112f, 82f, LightRenderMode.ForcePixel);
                CreatePendantRig(lightRoot, "BrokenPendant", new Vector3(2.8f, 4.24f, 3.52f), new Color(0.84f, 0.48f, 0.32f, 1f), 0.48f, 5.6f, new Color(0.86f, 0.5f, 0.32f, 1f), 0.92f, 6.8f);
                CreateSconceRig(lightRoot, "RearEmber", new Vector3(-12.6f, 4.02f, 4.42f), new Vector3(0.42f, -0.88f, -0.2f), new Color(0.54f, 0.28f, 0.2f, 1f), 0.3f, 4.2f, 0.26f, 4.4f);
                break;
            case ChoiceRoomIndex:
                CreatePendantRig(lightRoot, "ChoicePendant", new Vector3(0.6f, 4.18f, 3.44f), new Color(0.96f, 0.84f, 0.62f, 1f), 0.56f, 5.8f, new Color(0.98f, 0.84f, 0.62f, 1f), 0.86f, 6.4f);
                break;
            default:
                CreatePendantRig(lightRoot, "StairPendant", new Vector3(15.8f, 4.26f, 1.48f), new Color(0.92f, 0.74f, 0.48f, 1f), 0.48f, 4.8f, new Color(0.96f, 0.78f, 0.54f, 1f), 0.86f, 5.6f);
                CreateSconceRig(lightRoot, "FinalLeftSconce", new Vector3(-11.6f, 4.04f, 4.36f), new Vector3(0.34f, -0.92f, -0.18f), new Color(0.68f, 0.4f, 0.24f, 1f), 0.14f, 2.8f, 0.12f, 2.6f);
                break;
        }
    }

    private void CreateRoomLightZoneEffect(Transform roomRoot, StandardRoomTemplate template, float roomStartX, float roomEndX, int roomIndex)
    {
        if (roomRoot == null)
        {
            return;
        }

        LightZoneEffect lightZone = roomRoot.GetComponent<LightZoneEffect>();
        if (lightZone == null)
        {
            lightZone = roomRoot.gameObject.AddComponent<LightZoneEffect>();
        }

        lightZone.Configure(
            new Vector2(roomStartX, roomEndX),
            template.WalkDepth,
            template.WallHeight,
            GetRoomLightZoneLabel(roomIndex),
            IsRoomLitByDefault(roomIndex));

        _floorController?.RegisterRoomLightZone(roomIndex, lightZone);
    }

    private static void ConfigureMirrorRoomLighting(RoomLightingZone lightingZone, float roomStartX, float roomEndX, int roomIndex)
    {
        switch (roomIndex)
        {
            case LandingRoomIndex:
                lightingZone.Configure(
                    new Vector2(roomStartX, roomEndX),
                    new Color(0.16f, 0.18f, 0.22f, 1f),
                    0.18f,
                    new Color(0.66f, 0.74f, 0.82f, 1f),
                    0.08f,
                    new Vector3(0.02f, -1f, -0.08f),
                    new Color(0.82f, 0.86f, 0.92f, 1f),
                    0.05f,
                    0.68f,
                    0.18f,
                    new Color(0.02f, 0.024f, 0.03f, 1f),
                    0.76f,
                    new RoomLightingZone.LocalLightConfig(new Vector3(1.4f, 4.08f, 3.24f), new Color(0.72f, 0.78f, 0.88f, 1f), 0.62f, 6.8f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(-9.4f, 1.68f, 1.88f), new Color(0.52f, 0.6f, 0.72f, 1f), 0.32f, 3.1f));
                break;
            case RuleRoomIndex:
                lightingZone.Configure(
                    new Vector2(roomStartX, roomEndX),
                    new Color(0.15f, 0.17f, 0.2f, 1f),
                    0.16f,
                    new Color(0.8f, 0.64f, 0.34f, 1f),
                    0.08f,
                    new Vector3(0.04f, -1f, -0.08f),
                    new Color(0.9f, 0.82f, 0.64f, 1f),
                    0.06f,
                    0.7f,
                    0.14f,
                    new Color(0.022f, 0.024f, 0.028f, 1f),
                    0.76f,
                    new RoomLightingZone.LocalLightConfig(new Vector3(7.8f, 1.18f, 0.6f), new Color(1f, 0.64f, 0.26f, 1f), 1.78f, 6.2f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(-8.8f, 2.12f, 4.9f), new Color(0.6f, 0.66f, 0.78f, 1f), 0.22f, 2.8f));
                break;
            case PressureRoomIndex:
                lightingZone.Configure(
                    new Vector2(roomStartX, roomEndX),
                    new Color(0.1f, 0.12f, 0.16f, 1f),
                    0.18f,
                    new Color(0.44f, 0.5f, 0.62f, 1f),
                    0.1f,
                    new Vector3(-0.08f, -0.99f, -0.06f),
                    new Color(0.6f, 0.68f, 0.8f, 1f),
                    0.06f,
                    0.82f,
                    0.1f,
                    new Color(0.016f, 0.018f, 0.024f, 1f),
                    0.86f,
                    new RoomLightingZone.LocalLightConfig(new Vector3(-2.4f, 3.86f, 3.22f), new Color(0.42f, 0.5f, 0.62f, 1f), 0.58f, 9.8f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(5.8f, 3.74f, 3.34f), new Color(0.34f, 0.42f, 0.56f, 1f), 0.52f, 8.6f));
                break;
            case ChoiceRoomIndex:
                lightingZone.Configure(
                    new Vector2(roomStartX, roomEndX),
                    new Color(0.14f, 0.16f, 0.18f, 1f),
                    0.16f,
                    new Color(0.72f, 0.68f, 0.56f, 1f),
                    0.08f,
                    new Vector3(0f, -1f, -0.08f),
                    new Color(0.84f, 0.8f, 0.7f, 1f),
                    0.05f,
                    0.7f,
                    0.14f,
                    new Color(0.02f, 0.022f, 0.026f, 1f),
                    0.78f,
                    new RoomLightingZone.LocalLightConfig(new Vector3(0f, 2.12f, 0.82f), new Color(0.88f, 0.78f, 0.5f, 1f), 1.12f, 4.6f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(0f, 4.08f, 3.48f), new Color(0.62f, 0.7f, 0.82f, 1f), 0.42f, 5.4f));
                break;
            default:
                lightingZone.Configure(
                    new Vector2(roomStartX, roomEndX),
                    new Color(0.12f, 0.14f, 0.18f, 1f),
                    0.16f,
                    new Color(0.84f, 0.72f, 0.44f, 1f),
                    0.08f,
                    new Vector3(0.06f, -0.99f, -0.06f),
                    new Color(0.92f, 0.84f, 0.62f, 1f),
                    0.06f,
                    0.76f,
                    0.12f,
                    new Color(0.018f, 0.02f, 0.028f, 1f),
                    0.84f,
                    new RoomLightingZone.LocalLightConfig(new Vector3(7.4f, 1.18f, 0.6f), new Color(1f, 0.68f, 0.28f, 1f), 1.82f, 6f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(14.2f, 4.18f, 1.8f), new Color(0.88f, 0.82f, 0.58f, 1f), 0.52f, 5.2f));
                break;
        }
    }

    private static void CreateMirrorRoomLightRig(Transform lightRoot, int roomIndex)
    {
        switch (roomIndex)
        {
            case LandingRoomIndex:
                CreatePendantRig(lightRoot, "MirrorPendant", new Vector3(1.4f, 4.08f, 3.24f), new Color(0.72f, 0.78f, 0.88f, 1f), 0.46f, 5.2f, new Color(0.78f, 0.84f, 0.92f, 1f), 0.74f, 6.2f);
                break;
            case RuleRoomIndex:
                CreateDeskLampRig(lightRoot, "EtchingLamp", new Vector3(-8.8f, 2.12f, 4.9f), new Vector3(0.22f, -0.96f, -0.16f), new Color(0.58f, 0.64f, 0.76f, 1f), 0.18f, 2.4f, 0.24f, 2.8f);
                break;
            case PressureRoomIndex:
                CreatePointLight(lightRoot, "PressureWash_A", new Vector3(-2.4f, 3.86f, 3.22f), new Color(0.42f, 0.5f, 0.62f, 1f), 0.38f, 10.2f, LightRenderMode.ForcePixel);
                CreatePointLight(lightRoot, "PressureWash_B", new Vector3(5.8f, 3.74f, 3.34f), new Color(0.34f, 0.42f, 0.56f, 1f), 0.34f, 9.6f, LightRenderMode.ForcePixel);
                break;
            case ChoiceRoomIndex:
                CreatePointLight(lightRoot, "MirrorHalo", new Vector3(0f, 2.12f, 0.82f), new Color(0.88f, 0.78f, 0.5f, 1f), 1.08f, 4.9f, LightRenderMode.ForcePixel);
                break;
            default:
                CreatePendantRig(lightRoot, "ExitPendant", new Vector3(14.2f, 4.18f, 1.8f), new Color(0.88f, 0.82f, 0.58f, 1f), 0.52f, 4.9f, new Color(0.94f, 0.86f, 0.62f, 1f), 0.88f, 5.8f);
                break;
        }
    }

    private string GetRoomLightZoneLabel(int roomIndex)
    {
        if (_floorVariant == FloorVariant.MirrorCorridor)
        {
            switch (roomIndex)
            {
                case LandingRoomIndex:
                    return "镜廊前厅";
                case RuleRoomIndex:
                    return "刻痕亮区";
                case PressureRoomIndex:
                    return "镜廊暗区";
                case ChoiceRoomIndex:
                    return "铜镜暗区";
                default:
                    return "终局镜门";
            }
        }

        switch (roomIndex)
        {
            case LandingRoomIndex:
                return "前厅亮区";
            case RuleRoomIndex:
                return "封印烛台";
            case PressureRoomIndex:
                return "压迫暗区";
            case ChoiceRoomIndex:
                return "抉择暗区";
            default:
                return "终局封印";
        }
    }

    private bool IsRoomLitByDefault(int roomIndex)
    {
        return roomIndex == LandingRoomIndex;
    }

    private static void CreatePointLight(Transform parent, string name, Vector3 localPosition, Color color, float intensity, float range, LightRenderMode renderMode = LightRenderMode.Auto)
    {
        Transform lightTransform = CreateGroup(parent, name, localPosition);
        Light light = lightTransform.gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.renderMode = renderMode;
        light.bounceIntensity = 0.2f;
        light.cullingMask = ~0;
    }

    private static void CreateSpotLight(Transform parent, string name, Vector3 localPosition, Vector3 localDirection, Color color, float intensity, float range, float spotAngle, float innerSpotAngle, LightRenderMode renderMode = LightRenderMode.Auto)
    {
        Transform lightTransform = CreateGroup(parent, name, localPosition);
        Vector3 direction = localDirection.sqrMagnitude > 0.0001f ? localDirection.normalized : Vector3.forward;
        lightTransform.localRotation = Quaternion.LookRotation(direction, Vector3.up);

        Light light = lightTransform.gameObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.spotAngle = spotAngle;
        light.innerSpotAngle = Mathf.Min(innerSpotAngle, spotAngle - 0.5f);
        light.shadows = LightShadows.None;
        light.renderMode = renderMode;
        light.bounceIntensity = 0.15f;
        light.cullingMask = ~0;
    }

    private static void CreatePendantRig(Transform parent, string name, Vector3 localPosition, Color fillColor, float fillIntensity, float fillRange, Color downColor, float downIntensity, float downRange)
    {
        Transform rig = CreateGroup(parent, name, localPosition);
        CreatePointLight(rig, "Fill", Vector3.zero, fillColor, fillIntensity, fillRange, LightRenderMode.ForcePixel);
        CreateSpotLight(rig, "Down", Vector3.zero, new Vector3(0f, -1f, -0.08f), downColor, downIntensity, downRange, 96f, 64f, LightRenderMode.ForcePixel);
    }

    private static void CreateSconceRig(Transform parent, string name, Vector3 localPosition, Vector3 direction, Color color, float pointIntensity, float pointRange, float spotIntensity, float spotRange)
    {
        Transform rig = CreateGroup(parent, name, localPosition);
        CreatePointLight(rig, "Glow", Vector3.zero, color, pointIntensity, pointRange, LightRenderMode.Auto);
        CreateSpotLight(rig, "Throw", Vector3.zero, direction, color, spotIntensity, spotRange, 82f, 54f, LightRenderMode.ForcePixel);
    }

    private static void CreateDeskLampRig(Transform parent, string name, Vector3 localPosition, Vector3 direction, Color color, float pointIntensity, float pointRange, float spotIntensity, float spotRange)
    {
        Transform rig = CreateGroup(parent, name, localPosition);
        CreatePointLight(rig, "Glow", Vector3.zero, color, pointIntensity, pointRange, LightRenderMode.Auto);
        CreateSpotLight(rig, "Down", Vector3.zero, direction, color, spotIntensity, spotRange, 68f, 42f, LightRenderMode.ForcePixel);
    }

    private static float GetHouseWidth()
    {
        float width = 0f;
        foreach (StandardRoomTemplate template in RoomTemplates)
        {
            width += template.Width;
        }

        return width;
    }

    private static StandardRoomTemplate[] GetTemplatesForFloor(FloorVariant floorVariant)
    {
        return floorVariant == FloorVariant.MirrorCorridor ? MirrorRoomTemplates : AshRoomTemplates;
    }

    private static StandardRoomTemplate CreateLivingRoomTemplate(string templateId, float width = 42f, float depth = 8.4f, float wallHeight = 6.4f)
    {
        float floorThickness = 0.24f;

        return new StandardRoomTemplate(
            templateId: templateId,
            width: width,
            depth: depth,
            wallHeight: wallHeight,
            floorThickness: floorThickness,
            wallThickness: 0.3f,
            doorHeight: 3.35f,
            walkDepth: 0.42f,
            walkableInset: 2.4f,
            cameraTrackInset: 6.2f,
            frontFloorExtension: 5.6f,
            playerSpawnHeight: floorThickness + 0.04f,
            playerSpawnOffsetX: 7.5f,
            cameraOffsetDelta: Vector3.zero,
            roomBlendDuration: RoomZoneBlendDuration,
            structureBlocks: new[]
            {
                new RoomBlockSpec("Backdrop", new Vector3(0f, wallHeight * 0.47f, depth - 0.09f), new Vector3(width - 0.1f, wallHeight - 0.35f, 0.14f), new Color(0.24f, 0.2f, 0.18f)),
                new RoomBlockSpec("Skirting", new Vector3(0f, 0.27f, depth - 0.14f), new Vector3(width - 0.16f, 0.16f, 0.12f), DividerColor),
                new RoomBlockSpec("CeilingBand", new Vector3(0f, wallHeight - 0.52f, depth - 0.12f), new Vector3(width - 0.16f, 0.14f, 0.12f), DividerColor),
                new RoomBlockSpec("WallPanel_Left", new Vector3(-8.6f, wallHeight - 1.7f, depth - 0.05f), new Vector3(2.8f, 1.7f, 0.1f), new Color(0.19f, 0.15f, 0.13f)),
                new RoomBlockSpec("WallInset_Left", new Vector3(-8.6f, wallHeight - 1.7f, depth - 0.09f), new Vector3(2.34f, 1.28f, 0.03f), WindowColor),
                new RoomBlockSpec("WallPanel_Right", new Vector3(8.6f, wallHeight - 1.7f, depth - 0.05f), new Vector3(2.8f, 1.7f, 0.1f), new Color(0.19f, 0.15f, 0.13f)),
                new RoomBlockSpec("WallInset_Right", new Vector3(8.6f, wallHeight - 1.7f, depth - 0.09f), new Vector3(2.34f, 1.28f, 0.03f), WindowColor)
            },
            foregroundBlocks: new[]
            {
                new RoomBlockSpec("FloorBand_Front", new Vector3(0f, 0.15f, 0.52f), new Vector3(width - 0.4f, 0.05f, 1.95f), new Color(0.24f, 0.2f, 0.17f)),
                new RoomBlockSpec("ForegroundColumn_Left", new Vector3(-width * 0.5f + 2.4f, 2.2f, 0.92f), new Vector3(0.5f, 4.4f, 0.72f), ForegroundTrimColor),
                new RoomBlockSpec("ForegroundColumn_Right", new Vector3(width * 0.5f - 2.4f, 2.2f, 0.92f), new Vector3(0.5f, 4.4f, 0.72f), ForegroundTrimColor),
                new RoomBlockSpec("ForegroundHeader_Left", new Vector3(-width * 0.25f, wallHeight - 0.55f, 0.9f), new Vector3(width * 0.34f, 0.28f, 0.46f), ForegroundTrimColor),
                new RoomBlockSpec("ForegroundHeader_Right", new Vector3(width * 0.25f, wallHeight - 0.55f, 0.9f), new Vector3(width * 0.34f, 0.28f, 0.46f), ForegroundTrimColor)
            },
            gameplayBlocks: new[]
            {
                new RoomBlockSpec("FloorBand_Mid", new Vector3(0f, 0.13f, 3.1f), new Vector3(width - 0.9f, 0.04f, 1.95f), MidDepthFloorColor),
                new RoomBlockSpec("FloorRunner", new Vector3(0f, 0.15f, 3.55f), new Vector3(width * 0.52f, 0.02f, 6.2f), new Color(0.46f, 0.34f, 0.28f)),
                new RoomBlockSpec("GrandDesk", new Vector3(-8.8f, 0.62f, 2.25f), new Vector3(4.8f, 0.24f, 1.15f), new Color(0.39f, 0.3f, 0.23f)),
                new RoomBlockSpec("GrandSofaLeft", new Vector3(-1.8f, 0.56f, 1.95f), new Vector3(4.2f, 0.56f, 0.98f), new Color(0.28f, 0.21f, 0.18f)),
                new RoomBlockSpec("GrandTable", new Vector3(2.4f, 0.46f, 2.65f), new Vector3(2.8f, 0.2f, 1.45f), new Color(0.43f, 0.32f, 0.24f)),
                new RoomBlockSpec("GrandBench", new Vector3(9.5f, 0.46f, 2.15f), new Vector3(5.4f, 0.24f, 0.86f), new Color(0.38f, 0.29f, 0.22f))
            },
            backgroundBlocks: new[]
            {
                new RoomBlockSpec("FloorBand_Back", new Vector3(0f, 0.12f, 6.45f), new Vector3(width - 1.4f, 0.04f, 1.5f), BackDepthFloorColor),
                new RoomBlockSpec("GrandWardrobe", new Vector3(-13.2f, 1.34f, 4.22f), new Vector3(3.0f, 2.68f, 0.56f), new Color(0.31f, 0.25f, 0.19f)),
                new RoomBlockSpec("GrandShelf", new Vector3(14f, 1.42f, 4.2f), new Vector3(2.8f, 2.84f, 0.56f), new Color(0.29f, 0.24f, 0.19f)),
                new RoomBlockSpec("RearPlant_Left", new Vector3(-12.4f, 0.78f, 7.05f), new Vector3(1.02f, 1.56f, 0.72f), new Color(0.22f, 0.28f, 0.2f)),
                new RoomBlockSpec("RearPlant_Right", new Vector3(12.1f, 0.78f, 7f), new Vector3(1.02f, 1.56f, 0.72f), new Color(0.22f, 0.28f, 0.2f)),
                new RoomBlockSpec("RearConsole", new Vector3(0f, 0.86f, 7.06f), new Vector3(8.6f, 1.72f, 0.56f), new Color(0.32f, 0.25f, 0.2f))
            },
            ceilingBlocks: new[]
            {
                new RoomBlockSpec("CeilingBeam_Front", new Vector3(0f, wallHeight - 0.76f, 1.2f), new Vector3(width - 0.8f, 0.2f, 0.72f), CeilingBeamColor),
                new RoomBlockSpec("CeilingBeam_Mid", new Vector3(0f, wallHeight - 0.98f, 3.55f), new Vector3(width - 1.2f, 0.18f, 0.62f), CeilingBeamColor),
                new RoomBlockSpec("CeilingBeam_Back", new Vector3(0f, wallHeight - 1.18f, 6.55f), new Vector3(width - 1.6f, 0.16f, 0.5f), CeilingBeamColor),
                new RoomBlockSpec("CeilingBeam_Left", new Vector3(-width * 0.24f, wallHeight - 0.9f, 3.65f), new Vector3(width * 0.18f, 0.14f, 3.8f), CeilingBeamColor),
                new RoomBlockSpec("CeilingBeam_Right", new Vector3(width * 0.24f, wallHeight - 0.9f, 3.65f), new Vector3(width * 0.18f, 0.14f, 3.8f), CeilingBeamColor)
            },
            accentBlocks: new[]
            {
                new RoomBlockSpec("GrandLamp_Left", new Vector3(-15.5f, 4.18f, 4.3f), new Vector3(0.24f, 1.14f, 0.24f), LampColor),
                new RoomBlockSpec("GrandLamp_Right", new Vector3(15.6f, 4.18f, 4.3f), new Vector3(0.24f, 1.14f, 0.24f), new Color(0.72f, 0.67f, 0.49f)),
                new RoomBlockSpec("CenterPendant", new Vector3(0f, 4.34f, 3.82f), new Vector3(0.34f, 1.46f, 0.34f), new Color(0.74f, 0.66f, 0.44f))
            });
    }

    private static StandardRoomTemplate CreateStudyRoomTemplate(string templateId, float width = 42f, float depth = 8.4f, float wallHeight = 6.4f)
    {
        float floorThickness = 0.24f;

        return new StandardRoomTemplate(
            templateId: templateId,
            width: width,
            depth: depth,
            wallHeight: wallHeight,
            floorThickness: floorThickness,
            wallThickness: 0.3f,
            doorHeight: 3.2f,
            walkDepth: 0.42f,
            walkableInset: 2.4f,
            cameraTrackInset: 6.2f,
            frontFloorExtension: 5.6f,
            playerSpawnHeight: floorThickness + 0.04f,
            playerSpawnOffsetX: 7.5f,
            cameraOffsetDelta: new Vector3(0.35f, 0f, 0f),
            roomBlendDuration: RoomZoneBlendDuration,
            structureBlocks: new[]
            {
                new RoomBlockSpec("Backdrop", new Vector3(0f, wallHeight * 0.47f, depth - 0.09f), new Vector3(width - 0.1f, wallHeight - 0.35f, 0.14f), new Color(0.18f, 0.2f, 0.23f)),
                new RoomBlockSpec("Skirting", new Vector3(0f, 0.27f, depth - 0.14f), new Vector3(width - 0.16f, 0.16f, 0.12f), new Color(0.27f, 0.28f, 0.3f)),
                new RoomBlockSpec("CeilingBand", new Vector3(0f, wallHeight - 0.52f, depth - 0.12f), new Vector3(width - 0.16f, 0.14f, 0.12f), new Color(0.27f, 0.28f, 0.3f)),
                new RoomBlockSpec("WallPanel_Left", new Vector3(-9.4f, wallHeight - 1.78f, depth - 0.05f), new Vector3(3.2f, 1.85f, 0.1f), new Color(0.15f, 0.16f, 0.18f)),
                new RoomBlockSpec("WallInset_Left", new Vector3(-9.4f, wallHeight - 1.78f, depth - 0.09f), new Vector3(2.72f, 1.48f, 0.03f), WindowColor),
                new RoomBlockSpec("NoticeBoard", new Vector3(9.8f, 3.2f, depth - 0.07f), new Vector3(3.4f, 1.6f, 0.06f), new Color(0.39f, 0.31f, 0.22f))
            },
            foregroundBlocks: new[]
            {
                new RoomBlockSpec("FloorBand_Front", new Vector3(0f, 0.15f, 0.54f), new Vector3(width - 0.5f, 0.05f, 2.1f), new Color(0.2f, 0.18f, 0.17f)),
                new RoomBlockSpec("ForegroundCabinet_Left", new Vector3(-width * 0.5f + 3.2f, 1.05f, 1.08f), new Vector3(1.45f, 2.1f, 0.82f), new Color(0.22f, 0.21f, 0.2f)),
                new RoomBlockSpec("ForegroundColumn_Right", new Vector3(width * 0.5f - 2.4f, 2.2f, 0.92f), new Vector3(0.5f, 4.4f, 0.72f), ForegroundTrimColor),
                new RoomBlockSpec("ForegroundHeader_Left", new Vector3(-width * 0.2f, wallHeight - 0.55f, 0.9f), new Vector3(width * 0.28f, 0.28f, 0.46f), ForegroundTrimColor),
                new RoomBlockSpec("ForegroundHeader_Right", new Vector3(width * 0.28f, wallHeight - 0.55f, 0.9f), new Vector3(width * 0.26f, 0.28f, 0.46f), ForegroundTrimColor)
            },
            gameplayBlocks: new[]
            {
                new RoomBlockSpec("FloorBand_Mid", new Vector3(0f, 0.13f, 3.2f), new Vector3(width - 1.1f, 0.04f, 2.1f), new Color(0.29f, 0.27f, 0.26f)),
                new RoomBlockSpec("FloorRunner", new Vector3(2.2f, 0.15f, 3.7f), new Vector3(width * 0.38f, 0.02f, 5.4f), new Color(0.27f, 0.31f, 0.34f)),
                new RoomBlockSpec("StudyDesk", new Vector3(-7.4f, 0.64f, 2.32f), new Vector3(5.4f, 0.24f, 1.28f), new Color(0.31f, 0.29f, 0.24f)),
                new RoomBlockSpec("DeskScreen", new Vector3(-7f, 1.15f, 2.62f), new Vector3(1.15f, 0.92f, 0.16f), new Color(0.16f, 0.22f, 0.28f)),
                new RoomBlockSpec("ReadingChair", new Vector3(-1.9f, 0.56f, 2.1f), new Vector3(1.3f, 1.12f, 0.9f), new Color(0.24f, 0.22f, 0.19f)),
                new RoomBlockSpec("MeetingTable", new Vector3(4.8f, 0.48f, 2.85f), new Vector3(4.1f, 0.22f, 1.62f), new Color(0.36f, 0.33f, 0.28f)),
                new RoomBlockSpec("BenchRight", new Vector3(11.3f, 0.46f, 2.2f), new Vector3(4.4f, 0.24f, 0.84f), new Color(0.27f, 0.25f, 0.22f))
            },
            backgroundBlocks: new[]
            {
                new RoomBlockSpec("FloorBand_Back", new Vector3(0f, 0.12f, 6.45f), new Vector3(width - 1.5f, 0.04f, 1.55f), new Color(0.2f, 0.21f, 0.22f)),
                new RoomBlockSpec("TallCabinet_Left", new Vector3(-13f, 1.46f, 4.28f), new Vector3(2.55f, 2.92f, 0.56f), new Color(0.24f, 0.23f, 0.21f)),
                new RoomBlockSpec("ArchiveShelf_Center", new Vector3(2.4f, 1.34f, 4.36f), new Vector3(6.8f, 2.68f, 0.58f), new Color(0.26f, 0.27f, 0.24f)),
                new RoomBlockSpec("Cabinet_Right", new Vector3(14.2f, 1.3f, 4.25f), new Vector3(2.25f, 2.6f, 0.54f), new Color(0.22f, 0.24f, 0.22f)),
                new RoomBlockSpec("RearLamp_Left", new Vector3(-10.2f, 2.55f, 6.9f), new Vector3(0.22f, 2.1f, 0.22f), new Color(0.62f, 0.66f, 0.74f)),
                new RoomBlockSpec("RearLamp_Right", new Vector3(10.6f, 2.55f, 6.9f), new Vector3(0.22f, 2.1f, 0.22f), new Color(0.62f, 0.66f, 0.74f))
            },
            ceilingBlocks: new[]
            {
                new RoomBlockSpec("CeilingBeam_Front", new Vector3(0f, wallHeight - 0.76f, 1.22f), new Vector3(width - 0.8f, 0.2f, 0.74f), CeilingBeamColor),
                new RoomBlockSpec("CeilingBeam_Mid", new Vector3(0f, wallHeight - 1.02f, 3.52f), new Vector3(width - 1.3f, 0.18f, 0.62f), CeilingBeamColor),
                new RoomBlockSpec("CeilingBeam_Back", new Vector3(0f, wallHeight - 1.22f, 6.5f), new Vector3(width - 1.7f, 0.16f, 0.5f), CeilingBeamColor),
                new RoomBlockSpec("CableTray_Left", new Vector3(-width * 0.22f, wallHeight - 0.86f, 3.7f), new Vector3(width * 0.16f, 0.1f, 3.6f), new Color(0.17f, 0.18f, 0.19f)),
                new RoomBlockSpec("CableTray_Right", new Vector3(width * 0.22f, wallHeight - 0.86f, 3.7f), new Vector3(width * 0.16f, 0.1f, 3.6f), new Color(0.17f, 0.18f, 0.19f))
            },
            accentBlocks: new[]
            {
                new RoomBlockSpec("TaskLamp_Left", new Vector3(-15.2f, 4.1f, 4.36f), new Vector3(0.24f, 1.2f, 0.24f), new Color(0.63f, 0.67f, 0.82f)),
                new RoomBlockSpec("TaskLamp_Right", new Vector3(15.4f, 4.1f, 4.36f), new Vector3(0.24f, 1.2f, 0.24f), new Color(0.63f, 0.67f, 0.82f)),
                new RoomBlockSpec("CenterPendant", new Vector3(0f, 4.3f, 3.9f), new Vector3(0.34f, 1.52f, 0.34f), new Color(0.72f, 0.7f, 0.58f))
            });
    }

    private static void CreateLocalBlocks(Transform parent, RoomBlockSpec[] blockSpecs, int roomIndex)
    {
        if (blockSpecs == null)
        {
            return;
        }

        string roomPrefix = $"R{roomIndex + 1}_";
        foreach (RoomBlockSpec blockSpec in blockSpecs)
        {
            CreateLocalBlock(parent, $"{roomPrefix}{blockSpec.Name}", blockSpec.LocalCenter, blockSpec.Size, blockSpec.Color);
        }
    }

    private static Transform CreateGroup(Transform parent, string name, Vector3? localPosition = null)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        group.transform.localPosition = localPosition ?? Vector3.zero;
        group.transform.localRotation = Quaternion.identity;
        group.transform.localScale = Vector3.one;
        return group.transform;
    }

    private static void CreateLocalBlock(Transform parent, string name, Vector3 localCenter, Vector3 size, Color color)
    {
        GameObject block = CreatePrimitiveBlock(name, size, color);
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localCenter;
    }

    private static GameObject CreatePrimitiveBlock(string name, Vector3 size, Color color)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.localScale = size;

        Collider collider = block.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }

        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
        {
            Tint(renderer, color);
        }

        return block;
    }

    private static void Tint(Renderer renderer, Color color)
    {
        if (renderer == null || renderer.sharedMaterial == null)
        {
            return;
        }

        Material material = renderer.material;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        ApplyMatteSurface(material);
    }

    private static void ApplyMatteSurface(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.02f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        if (material.HasProperty("_SpecularHighlights"))
        {
            material.SetFloat("_SpecularHighlights", 0f);
        }

        if (material.HasProperty("_EnvironmentReflections"))
        {
            material.SetFloat("_EnvironmentReflections", 0f);
        }
    }

    private readonly struct RoomBlockSpec
    {
        public RoomBlockSpec(string name, Vector3 localCenter, Vector3 size, Color color)
        {
            Name = name;
            LocalCenter = localCenter;
            Size = size;
            Color = color;
        }

        public string Name { get; }
        public Vector3 LocalCenter { get; }
        public Vector3 Size { get; }
        public Color Color { get; }
    }

    private sealed class StandardRoomTemplate
    {
        public StandardRoomTemplate(
            string templateId,
            float width,
            float depth,
            float wallHeight,
            float floorThickness,
            float wallThickness,
            float doorHeight,
            float walkDepth,
            float walkableInset,
            float cameraTrackInset,
            float frontFloorExtension,
            float playerSpawnHeight,
            float playerSpawnOffsetX,
            Vector3 cameraOffsetDelta,
            float roomBlendDuration,
            RoomBlockSpec[] structureBlocks,
            RoomBlockSpec[] foregroundBlocks,
            RoomBlockSpec[] gameplayBlocks,
            RoomBlockSpec[] backgroundBlocks,
            RoomBlockSpec[] ceilingBlocks,
            RoomBlockSpec[] accentBlocks)
        {
            TemplateId = templateId;
            Width = width;
            Depth = depth;
            WallHeight = wallHeight;
            FloorThickness = floorThickness;
            WallThickness = wallThickness;
            DoorHeight = doorHeight;
            WalkDepth = walkDepth;
            WalkableInset = walkableInset;
            CameraTrackInset = cameraTrackInset;
            FrontFloorExtension = frontFloorExtension;
            PlayerSpawnHeight = playerSpawnHeight;
            PlayerSpawnOffsetX = playerSpawnOffsetX;
            CameraOffsetDelta = cameraOffsetDelta;
            RoomBlendDuration = roomBlendDuration;
            StructureBlocks = structureBlocks;
            ForegroundBlocks = foregroundBlocks;
            GameplayBlocks = gameplayBlocks;
            BackgroundBlocks = backgroundBlocks;
            CeilingBlocks = ceilingBlocks;
            AccentBlocks = accentBlocks;
        }

        public string TemplateId { get; }
        public float Width { get; }
        public float Depth { get; }
        public float WallHeight { get; }
        public float FloorThickness { get; }
        public float WallThickness { get; }
        public float DoorHeight { get; }
        public float WalkDepth { get; }
        public float WalkableInset { get; }
        public float CameraTrackInset { get; }
        public float FrontFloorExtension { get; }
        public float PlayerSpawnHeight { get; }
        public float PlayerSpawnOffsetX { get; }
        public Vector3 CameraOffsetDelta { get; }
        public float RoomBlendDuration { get; }
        public RoomBlockSpec[] StructureBlocks { get; }
        public RoomBlockSpec[] ForegroundBlocks { get; }
        public RoomBlockSpec[] GameplayBlocks { get; }
        public RoomBlockSpec[] BackgroundBlocks { get; }
        public RoomBlockSpec[] CeilingBlocks { get; }
        public RoomBlockSpec[] AccentBlocks { get; }
    }
}
