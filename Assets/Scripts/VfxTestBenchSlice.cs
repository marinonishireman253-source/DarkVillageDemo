using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class VfxTestBenchSlice : MonoBehaviour
{
    private const string RootName = "__VfxTestBenchSlice";
    private const int LayoutVersion = 1;

    private static readonly Color FloorColor = new Color(0.12f, 0.12f, 0.14f);
    private static readonly Color LaneColor = new Color(0.2f, 0.11f, 0.09f);
    private static readonly Color FocusLaneColor = new Color(0.14f, 0.13f, 0.11f);
    private static readonly Color AccentColor = new Color(0.78f, 0.42f, 0.22f);
    private static readonly Color DummyColor = new Color(0.42f, 0.17f, 0.14f);
    private static readonly Color WallColor = new Color(0.18f, 0.17f, 0.18f);
    private static readonly Color FogColor = new Color(0.2f, 0.1f, 0.09f, 1f);

    [SerializeField] private int _layoutVersion;

    private PlayerMover _player;
    private Vector3 _forward;
    private Vector3 _right;
    private Vector3 _center;

    public static void Ensure(PlayerMover player)
    {
        if (player == null || SceneManager.GetActiveScene().name != SceneLoader.VfxTestBenchSceneName)
        {
            return;
        }

        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            VfxTestBenchSlice existingSlice = existingRoot.GetComponent<VfxTestBenchSlice>();
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
        VfxTestBenchSlice slice = root.AddComponent<VfxTestBenchSlice>();
        slice.Build(player);
    }

    private void BindRuntimeState(PlayerMover player)
    {
        _player = player;
        ResolveAxes();
        PositionPlayer();
        ConfigureSceneLighting();
        EnsureOverlay();
    }

    private void Build(PlayerMover player)
    {
        _player = player;
        _layoutVersion = LayoutVersion;

        ResolveAxes();
        PositionPlayer();
        ConfigureSceneLighting();
        BuildArena();
        BuildCombatLane();
        BuildFocusLane();
        BuildAtmosphere();
        EnsureOverlay();
    }

    private bool IsLayoutCurrent()
    {
        return _layoutVersion == LayoutVersion
            && transform.Find("BenchFloor") != null
            && transform.Find("CombatLane") != null
            && transform.Find("FocusLane") != null;
    }

    private void ResolveAxes()
    {
        _forward = Camera.main != null
            ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up)
            : new Vector3(1f, 0f, 1f);

        if (_forward.sqrMagnitude <= 0.001f)
        {
            _forward = new Vector3(1f, 0f, 1f);
        }

        _forward.Normalize();
        _right = Vector3.Cross(Vector3.up, _forward).normalized;
        _center = Vector3.zero;
    }

    private void PositionPlayer()
    {
        if (_player == null)
        {
            return;
        }

        _player.transform.position = _center - _forward * 5.8f + Vector3.up;
        _player.transform.rotation = Quaternion.LookRotation(_forward, Vector3.up);

        CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(_player.transform, true);
        }
    }

    private void ConfigureSceneLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.2f, 0.2f);

        Light sun = FindFirstObjectByType<Light>();
        if (sun != null && sun.type == LightType.Directional)
        {
            sun.intensity = 0.85f;
            sun.color = new Color(0.86f, 0.78f, 0.72f);
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        }
    }

    private void BuildArena()
    {
        Quaternion facing = Quaternion.LookRotation(_forward, Vector3.up);

        CreateBlock("BenchFloor", _center, new Vector3(20f, 0.2f, 20f), facing, FloorColor).transform.SetParent(transform, true);
        CreateBlock("CombatLane", _center + _forward * 2.2f + Vector3.up * 0.02f, new Vector3(5.4f, 0.04f, 10.8f), facing, LaneColor).transform.SetParent(transform, true);
        CreateBlock("FocusLane", _center - _right * 5.9f + Vector3.up * 0.03f, new Vector3(4.6f, 0.06f, 11.6f), facing, FocusLaneColor).transform.SetParent(transform, true);
        CreateBlock("BackdropWall", _center + _forward * 9.8f + Vector3.up * 2.8f, new Vector3(18f, 5.6f, 0.6f), facing, WallColor).transform.SetParent(transform, true);
        CreateBlock("LeftWall", _center - _right * 10f + Vector3.up * 2.1f, new Vector3(0.6f, 4.2f, 20f), facing, WallColor).transform.SetParent(transform, true);
        CreateBlock("RightWall", _center + _right * 10f + Vector3.up * 2.1f, new Vector3(0.6f, 4.2f, 20f), facing, WallColor).transform.SetParent(transform, true);

        CreateBlock("PlayerMark", _center - _forward * 5.9f + Vector3.up * 0.04f, new Vector3(2.4f, 0.03f, 2.4f), facing, new Color(0.16f, 0.18f, 0.22f)).transform.SetParent(transform, true);
        CreateBlock("LaneDivider", _center - _right * 3.2f + Vector3.up * 0.04f, new Vector3(0.12f, 0.03f, 14f), facing, new Color(0.32f, 0.21f, 0.15f)).transform.SetParent(transform, true);
    }

    private void BuildCombatLane()
    {
        CreateTrainingDummy("TrainingDummy_Front", _center + _forward * 3.8f, DummyColor);
        CreateTrainingDummy("TrainingDummy_Left", _center + _forward * 5f - _right * 1.7f, new Color(0.48f, 0.2f, 0.16f));
        CreateTrainingDummy("TrainingDummy_Right", _center + _forward * 5.2f + _right * 1.7f, new Color(0.36f, 0.14f, 0.18f));

        CreateLantern("CombatLantern_Left", _center + _forward * 3.2f - _right * 3.3f);
        CreateLantern("CombatLantern_Right", _center + _forward * 3.2f + _right * 3.3f);
    }

    private void BuildFocusLane()
    {
        Vector3 laneOrigin = _center - _right * 5.9f;

        GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = "BenchNpc";
        npc.transform.SetParent(transform, true);
        npc.transform.position = laneOrigin - _forward * 3.4f + Vector3.up * 0.9f;
        npc.transform.rotation = Quaternion.LookRotation(_forward, Vector3.up);
        Tint(npc.GetComponent<Renderer>(), new Color(0.44f, 0.36f, 0.3f));
        TestNpcInteractable npcInteractable = npc.AddComponent<TestNpcInteractable>();
        npcInteractable.ConfigurePresentation("特效观测员", "交谈");
        npcInteractable.ConfigureFallbackDialogue(
            "欢迎来到特效测试台。",
            "前方三只训练傀儡不会移动，适合看挥砍起手和命中爆点。",
            "左侧这排交互物专门用来观察聚焦高亮和提示 UI。");

        GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stone.name = "BenchStone";
        stone.transform.SetParent(transform, true);
        stone.transform.position = laneOrigin - _forward * 0.7f + Vector3.up * 0.7f;
        stone.transform.localScale = new Vector3(1f, 1.4f, 0.8f);
        Tint(stone.GetComponent<Renderer>(), new Color(0.42f, 0.42f, 0.45f));
        TestStoneInteractable stoneInteractable = stone.AddComponent<TestStoneInteractable>();
        stoneInteractable.ConfigurePresentation("测试石碑", "查看");
        stoneInteractable.ConfigureFallbackDialogue("测试石碑", "查看", "这块石碑用于检查交互高亮、面板提示和静态观察距离。");

        GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pickup.name = "BenchPickup";
        pickup.transform.SetParent(transform, true);
        pickup.transform.position = laneOrigin + _forward * 1.8f + Vector3.up * 0.55f;
        pickup.transform.localScale = Vector3.one * 0.55f;
        Tint(pickup.GetComponent<Renderer>(), new Color(0.86f, 0.66f, 0.22f));
        PickupInteractable pickupInteractable = pickup.AddComponent<PickupInteractable>();
        pickupInteractable.Configure("vfx_test_token", "测试徽记", "拾取", false);

        GameObject doorRoot = new GameObject("BenchDoor");
        doorRoot.transform.SetParent(transform, true);
        doorRoot.transform.position = laneOrigin + _forward * 4.5f;
        doorRoot.transform.rotation = Quaternion.LookRotation(_right, Vector3.up);

        GameObject doorVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorVisual.name = "DoorVisual";
        doorVisual.transform.SetParent(doorRoot.transform, false);
        doorVisual.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        doorVisual.transform.localScale = new Vector3(1.2f, 2.4f, 0.18f);
        Tint(doorVisual.GetComponent<Renderer>(), new Color(0.28f, 0.18f, 0.12f));

        DoorInteractable doorInteractable = doorRoot.AddComponent<DoorInteractable>();
        doorInteractable.SetDoorVisual(doorVisual.transform);
        doorInteractable.ConfigurePresentation("测试门", "推门");
        doorInteractable.ConfigureMotion(new Vector3(0f, 95f, 0f), false);
    }

    private void BuildAtmosphere()
    {
        CreateFogBlob("Fog_Left", _center + _forward * 7f - _right * 5f + Vector3.up * 1.2f, 2.4f);
        CreateFogBlob("Fog_Right", _center + _forward * 6.4f + _right * 5.4f + Vector3.up * 1.1f, 2.1f);
        CreateFogBlob("Fog_Back", _center + _forward * 8.2f + Vector3.up * 1.6f, 2.9f);
    }

    private void EnsureOverlay()
    {
        if (FindFirstObjectByType<VfxTestBenchOverlay>() == null)
        {
            new GameObject("VfxTestBenchOverlay").AddComponent<VfxTestBenchOverlay>();
        }
    }

    private void CreateTrainingDummy(string objectName, Vector3 position, Color color)
    {
        GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        dummy.name = objectName;
        dummy.transform.SetParent(transform, true);
        dummy.transform.position = position + Vector3.up;
        dummy.transform.rotation = Quaternion.LookRotation(-_forward, Vector3.up);
        Tint(dummy.GetComponent<Renderer>(), color);

        CombatantHealth health = dummy.AddComponent<CombatantHealth>();
        SimpleEnemyController controller = dummy.AddComponent<SimpleEnemyController>();
        controller.Configure("训练傀儡", 999, 1);
        controller.enabled = false;

        if (health != null)
        {
            health.Configure(999);
        }

        GameObject baseBlock = CreateBlock($"{objectName}_Base", position + Vector3.up * 0.18f, new Vector3(1.5f, 0.36f, 1.5f), Quaternion.LookRotation(_forward, Vector3.up), new Color(0.22f, 0.15f, 0.14f));
        baseBlock.transform.SetParent(transform, true);
    }

    private void CreateLantern(string objectName, Vector3 position)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(transform, true);
        root.transform.position = position;

        GameObject post = CreateBlock("Post", position + Vector3.up * 1.45f, new Vector3(0.18f, 2.9f, 0.18f), Quaternion.identity, new Color(0.24f, 0.18f, 0.14f));
        post.transform.SetParent(root.transform, true);

        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = "Glow";
        glow.transform.SetParent(root.transform, true);
        glow.transform.position = position + Vector3.up * 2.45f;
        glow.transform.localScale = Vector3.one * 0.34f;
        Tint(glow.GetComponent<Renderer>(), new Color(1f, 0.82f, 0.44f));

        GameObject lightAnchor = new GameObject("PointLight");
        lightAnchor.transform.SetParent(root.transform, false);
        lightAnchor.transform.position = position + Vector3.up * 2.45f;

        Light pointLight = lightAnchor.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = new Color(1f, 0.76f, 0.42f);
        pointLight.intensity = 4.2f;
        pointLight.range = 8f;
        pointLight.shadows = LightShadows.None;
    }

    private void CreateFogBlob(string objectName, Vector3 position, float scale)
    {
        GameObject fog = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fog.name = objectName;
        fog.transform.SetParent(transform, true);
        fog.transform.position = position;
        fog.transform.localScale = Vector3.one * scale;
        Renderer renderer = fog.GetComponent<Renderer>();
        Tint(renderer, FogColor);
        if (renderer != null)
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private GameObject CreateBlock(string objectName, Vector3 position, Vector3 scale, Quaternion rotation, Color color)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = objectName;
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

public sealed class VfxTestBenchOverlay : MonoBehaviour
{
    private static Texture2D s_WhiteTexture;

    private PlayerMover _player;
    private PlayerCombat _combat;
    private GUIStyle _titleStyle;
    private GUIStyle _bodyStyle;
    private GUIStyle _buttonStyle;
    private bool _collapsed;

    private void Awake()
    {
        EnsureWhiteTexture();
    }

    private void Update()
    {
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerMover>();
            _combat = _player != null ? _player.GetComponent<PlayerCombat>() : null;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            _collapsed = !_collapsed;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            SpawnSlashPreview();
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            SpawnHitPreview();
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            ChapterState.ResetRuntime();
            SceneLoader.ReloadCurrent();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SceneLoader.LoadTitle();
        }
    }

    private void OnGUI()
    {
        if (SceneManager.GetActiveScene().name != SceneLoader.VfxTestBenchSceneName)
        {
            return;
        }

        EnsureWhiteTexture();
        EnsureStyles();

        float panelWidth = 370f;
        float panelHeight = _collapsed ? 42f : 188f;
        Rect panelRect = new Rect(Screen.width - panelWidth - 18f, 18f, panelWidth, panelHeight);
        DrawRect(panelRect, new Color(0.05f, 0.06f, 0.08f, 0.86f));
        DrawRect(new Rect(panelRect.x + 3f, panelRect.y + 3f, panelRect.width - 6f, panelRect.height - 6f), new Color(0.13f, 0.14f, 0.17f, 0.78f));

        GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 10f, panelRect.width - 28f, 24f), "VFX Test Bench", _titleStyle);

        if (_collapsed)
        {
            GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 24f, panelRect.width - 28f, 18f), "Tab 展开", _bodyStyle);
            return;
        }

        float buttonWidth = 104f;
        float buttonHeight = 30f;
        float buttonY = panelRect.y + 42f;

        if (GUI.Button(new Rect(panelRect.x + 14f, buttonY, buttonWidth, buttonHeight), "Slash (1)", _buttonStyle))
        {
            SpawnSlashPreview();
        }

        if (GUI.Button(new Rect(panelRect.x + 128f, buttonY, buttonWidth, buttonHeight), "Hit (2)", _buttonStyle))
        {
            SpawnHitPreview();
        }

        if (GUI.Button(new Rect(panelRect.x + 242f, buttonY, buttonWidth, buttonHeight), "Reload (R)", _buttonStyle))
        {
            ChapterState.ResetRuntime();
            SceneLoader.ReloadCurrent();
        }

        GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 82f, panelRect.width - 28f, 90f),
            "Space / J / 鼠标左键: 实战测试挥砍与命中\n1: 原地预览挥砍弧光\n2: 原地预览命中爆点\nR: 重置测试台\nEsc: 返回标题\nTab: 折叠面板",
            _bodyStyle);

        string stateLine = _combat != null && _combat.Health != null
            ? $"玩家 HP {_combat.Health.CurrentHealth}/{_combat.Health.MaxHealth} | 前方训练傀儡固定不动 | 左侧一排交互物用于看聚焦高亮"
            : "等待玩家和战斗组件初始化...";
        GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 152f, panelRect.width - 28f, 26f), stateLine, _bodyStyle);
    }

    private void SpawnSlashPreview()
    {
        if (_player == null)
        {
            return;
        }

        CombatVfxFactory.SpawnSlash(_player.transform, new Color(1f, 0.9f, 0.72f, 1f));
    }

    private void SpawnHitPreview()
    {
        if (_player == null)
        {
            return;
        }

        Vector3 position = _player.transform.position + _player.transform.forward * 1.3f + Vector3.up * 1f;
        CombatVfxFactory.SpawnHitBurst(position, _player.transform.forward, new Color(1f, 0.55f, 0.35f, 1f));
    }

    private void EnsureWhiteTexture()
    {
        if (s_WhiteTexture != null)
        {
            return;
        }

        s_WhiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        s_WhiteTexture.SetPixel(0, 0, Color.white);
        s_WhiteTexture.Apply(false, true);
    }

    private void EnsureStyles()
    {
        if (_titleStyle != null)
        {
            return;
        }

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        _titleStyle.normal.textColor = new Color(0.94f, 0.86f, 0.76f);

        _bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            wordWrap = true
        };
        _bodyStyle.normal.textColor = new Color(0.79f, 0.79f, 0.8f);

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, s_WhiteTexture, ScaleMode.StretchToFill);
        GUI.color = previousColor;
    }
}
