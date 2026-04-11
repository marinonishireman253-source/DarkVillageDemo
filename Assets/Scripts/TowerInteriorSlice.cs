using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TowerInteriorSlice : MonoBehaviour
{
    private const string RootName = "__TowerInteriorSlice";
    private const int LayoutVersion = 11;
    private const float RoomZoneOverlap = 4.6f;
    private const float RoomZoneDepth = 5.2f;
    private const float RoomZoneCenterY = 2.35f;
    private const float RoomZoneBlendDuration = 0.32f;
    private const float PlayerReferenceHeight = 2.15f;
    private const float MonsterHeightRatio = 0.7f;
    private const float MonsterPreviewX = -11.6f;
    private const float MonsterPreviewDepthOffset = 0f;
    private const int MonsterSortingOrder = 10;

    private static readonly Color NightColor = new Color(0.06f, 0.07f, 0.09f);
    private static readonly Color ShellColor = new Color(0.18f, 0.19f, 0.21f);
    private static readonly Color FloorColor = new Color(0.3f, 0.25f, 0.21f);
    private static readonly Color CeilingColor = new Color(0.24f, 0.24f, 0.23f);
    private static readonly Color DividerColor = new Color(0.33f, 0.3f, 0.27f);
    private static readonly Color WindowColor = new Color(0.19f, 0.25f, 0.32f);
    private static readonly Color ForegroundTrimColor = new Color(0.22f, 0.2f, 0.18f);
    private static readonly Color MidDepthFloorColor = new Color(0.36f, 0.3f, 0.25f);
    private static readonly Color BackDepthFloorColor = new Color(0.26f, 0.22f, 0.19f);
    private static readonly Color CeilingBeamColor = new Color(0.2f, 0.19f, 0.18f);
    private static readonly Color LampColor = new Color(0.74f, 0.67f, 0.44f);
    private const int MonsterPreviewRoomIndex = 1;
    private static readonly StandardRoomTemplate[] RoomTemplates =
    {
        CreateLivingRoomTemplate(),
        CreateStudyRoomTemplate()
    };

    public static float WalkDepth => RoomTemplates[0].WalkDepth;
    public static Vector2 PlayableXRange => new Vector2(
        -GetHouseWidth() * 0.5f + RoomTemplates[0].WalkableInset,
        GetHouseWidth() * 0.5f - RoomTemplates[RoomTemplates.Length - 1].WalkableInset);

    public static Vector2 CameraTrackXRange => new Vector2(
        -GetHouseWidth() * 0.5f + RoomTemplates[0].CameraTrackInset,
        GetHouseWidth() * 0.5f - RoomTemplates[RoomTemplates.Length - 1].CameraTrackInset);

    private PlayerMover _player;
    private RoomCameraZone _startingRoomZone;
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

        BuildShell();
        BuildRooms();
        BuildRoomDividers();
        PositionPlayer();
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
            CreateRoomLightRig(roomRoot, roomIndex);

            RoomCameraZone roomZone = CreateRoomCameraZone(roomRoot, template, roomStartX, roomEndX, roomIndex);
            if (roomIndex == 0)
            {
                _startingRoomZone = roomZone;
            }

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

        if (roomIndex == MonsterPreviewRoomIndex)
        {
            BuildMonsterPreview(roomRoot, template);
        }
    }

    private void BuildMonsterPreview(Transform roomRoot, StandardRoomTemplate template)
    {
        float desiredMonsterHeight = PlayerReferenceHeight * MonsterHeightRatio;
        float monsterDepth = template.WalkDepth + MonsterPreviewDepthOffset;
        Transform monsterRoot = CreateGroup(
            roomRoot,
            "Monster_BeamVisitor",
            new Vector3(MonsterPreviewX, template.PlayerSpawnHeight, monsterDepth));
        MonsterSpriteVisual visual = monsterRoot.gameObject.AddComponent<MonsterSpriteVisual>();
        visual.Configure(desiredMonsterHeight, MonsterSortingOrder, new Vector3(0f, 0.01f, 0f));
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

    private static void ConfigureRoomLighting(Transform roomRoot, float roomStartX, float roomEndX, int roomIndex)
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

        switch (roomIndex)
        {
            case 0:
                lightingZone.Configure(
                    new Vector2(roomStartX, roomEndX),
                    new Color(0.87f, 0.79f, 0.69f, 1f),
                    0.7f,
                    new Color(1f, 0.78f, 0.52f, 1f),
                    0.28f,
                    new Vector3(-0.16f, -0.84f, -0.52f),
                    new Color(1f, 0.92f, 0.8f, 1f),
                    0.24f,
                    0.56f,
                    0.48f,
                    new Color(0.07f, 0.055f, 0.05f, 1f),
                    0.82f,
                    new RoomLightingZone.LocalLightConfig(new Vector3(-15.5f, 4.18f, 4.3f), new Color(1f, 0.76f, 0.48f, 1f), 1.2f, 8.5f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(15.6f, 4.18f, 4.3f), new Color(1f, 0.76f, 0.48f, 1f), 1.2f, 8.5f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(-8.6f, 4.72f, 8.05f), new Color(0.5f, 0.64f, 0.82f, 1f), 0.45f, 6.8f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(8.6f, 4.72f, 8.05f), new Color(0.5f, 0.64f, 0.82f, 1f), 0.45f, 6.8f));
                break;
            default:
                lightingZone.Configure(
                    new Vector2(roomStartX, roomEndX),
                    new Color(0.73f, 0.79f, 0.88f, 1f),
                    0.66f,
                    new Color(0.62f, 0.79f, 1f, 1f),
                    0.32f,
                    new Vector3(0.12f, -0.82f, -0.56f),
                    new Color(0.8f, 0.9f, 1f, 1f),
                    0.28f,
                    0.62f,
                    0.45f,
                    new Color(0.045f, 0.05f, 0.065f, 1f),
                    0.74f,
                    new RoomLightingZone.LocalLightConfig(new Vector3(-15.2f, 4.1f, 4.36f), new Color(0.7f, 0.84f, 1f, 1f), 0.95f, 8.2f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(15.4f, 4.1f, 4.36f), new Color(0.7f, 0.84f, 1f, 1f), 0.95f, 8.2f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(-7f, 1.15f, 2.62f), new Color(0.54f, 0.82f, 1f, 1f), 0.85f, 4.9f),
                    new RoomLightingZone.LocalLightConfig(new Vector3(-9.4f, 4.82f, 8.02f), new Color(0.46f, 0.64f, 0.86f, 1f), 0.38f, 6.6f));
                break;
        }
    }

    private static void CreateRoomLightRig(Transform roomRoot, int roomIndex)
    {
        if (roomRoot == null)
        {
            return;
        }

        Transform lightRoot = CreateGroup(roomRoot, "Lights");

        switch (roomIndex)
        {
            case 0:
                CreatePointLight(lightRoot, "Lamp_Left", new Vector3(-15.5f, 4.18f, 4.3f), new Color(1f, 0.76f, 0.48f, 1f), 1.55f, 7f);
                CreatePointLight(lightRoot, "Lamp_Right", new Vector3(15.6f, 4.18f, 4.3f), new Color(1f, 0.76f, 0.48f, 1f), 1.55f, 7f);
                CreateSpotLight(lightRoot, "WindowFill_Left", new Vector3(-8.6f, 4.72f, 7.9f), new Vector3(0.18f, -0.2f, -0.96f), new Color(0.48f, 0.62f, 0.82f, 1f), 0.68f, 5.8f, 88f, 62f);
                CreateSpotLight(lightRoot, "WindowFill_Right", new Vector3(8.6f, 4.72f, 7.9f), new Vector3(-0.18f, -0.2f, -0.96f), new Color(0.48f, 0.62f, 0.82f, 1f), 0.68f, 5.8f, 88f, 62f);
                break;
            default:
                CreatePointLight(lightRoot, "TaskLamp_Left", new Vector3(-15.2f, 4.1f, 4.36f), new Color(0.7f, 0.84f, 1f, 1f), 1.25f, 6.7f);
                CreatePointLight(lightRoot, "TaskLamp_Right", new Vector3(15.4f, 4.1f, 4.36f), new Color(0.7f, 0.84f, 1f, 1f), 1.25f, 6.7f);
                CreateSpotLight(lightRoot, "DeskScreenGlow", new Vector3(-7f, 1.15f, 2.62f), new Vector3(0.16f, 0.04f, -0.99f), new Color(0.54f, 0.82f, 1f, 1f), 0.82f, 3.9f, 74f, 50f);
                CreateSpotLight(lightRoot, "RearWindowFill", new Vector3(-9.4f, 4.82f, 7.96f), new Vector3(0.22f, -0.18f, -0.96f), new Color(0.46f, 0.64f, 0.86f, 1f), 0.56f, 5.7f, 84f, 58f);
                break;
        }
    }

    private static void CreatePointLight(Transform parent, string name, Vector3 localPosition, Color color, float intensity, float range)
    {
        Transform lightTransform = CreateGroup(parent, name, localPosition);
        Light light = lightTransform.gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;
        light.bounceIntensity = 0.2f;
        light.cullingMask = ~0;
    }

    private static void CreateSpotLight(Transform parent, string name, Vector3 localPosition, Vector3 localDirection, Color color, float intensity, float range, float spotAngle, float innerSpotAngle)
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
        light.renderMode = LightRenderMode.Auto;
        light.bounceIntensity = 0.15f;
        light.cullingMask = ~0;
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

    private static StandardRoomTemplate CreateLivingRoomTemplate()
    {
        float width = 42f;
        float depth = 8.4f;
        float wallHeight = 6.4f;
        float floorThickness = 0.24f;

        return new StandardRoomTemplate(
            templateId: "Living_A",
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
                new RoomBlockSpec("WindowFrame_Left", new Vector3(-8.6f, wallHeight - 1.7f, depth - 0.03f), new Vector3(2.8f, 1.7f, 0.08f), new Color(0.17f, 0.14f, 0.12f)),
                new RoomBlockSpec("WindowGlass_Left", new Vector3(-8.6f, wallHeight - 1.7f, depth - 0.08f), new Vector3(2.36f, 1.34f, 0.04f), WindowColor),
                new RoomBlockSpec("WindowFrame_Right", new Vector3(8.6f, wallHeight - 1.7f, depth - 0.03f), new Vector3(2.8f, 1.7f, 0.08f), new Color(0.17f, 0.14f, 0.12f)),
                new RoomBlockSpec("WindowGlass_Right", new Vector3(8.6f, wallHeight - 1.7f, depth - 0.08f), new Vector3(2.36f, 1.34f, 0.04f), WindowColor)
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
                new RoomBlockSpec("GrandLamp_Right", new Vector3(15.6f, 4.18f, 4.3f), new Vector3(0.24f, 1.14f, 0.24f), new Color(0.72f, 0.67f, 0.49f))
            });
    }

    private static StandardRoomTemplate CreateStudyRoomTemplate()
    {
        float width = 42f;
        float depth = 8.4f;
        float wallHeight = 6.4f;
        float floorThickness = 0.24f;

        return new StandardRoomTemplate(
            templateId: "Study_B",
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
                new RoomBlockSpec("WindowFrame_Left", new Vector3(-9.4f, wallHeight - 1.78f, depth - 0.03f), new Vector3(3.2f, 1.85f, 0.08f), new Color(0.15f, 0.16f, 0.18f)),
                new RoomBlockSpec("WindowGlass_Left", new Vector3(-9.4f, wallHeight - 1.78f, depth - 0.08f), new Vector3(2.72f, 1.48f, 0.04f), new Color(0.17f, 0.24f, 0.31f)),
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
                new RoomBlockSpec("TaskLamp_Right", new Vector3(15.4f, 4.1f, 4.36f), new Vector3(0.24f, 1.2f, 0.24f), new Color(0.63f, 0.67f, 0.82f))
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
