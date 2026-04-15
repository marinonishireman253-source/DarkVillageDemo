using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class AshParlorRunController : FloorRunController
{
    public enum FloorSummaryTestPreset
    {
        RiskSummary,
        SafeSummary
    }

    private const int RoomCount = 5;
    private const string FirstBrazierObjectiveId = "ash_parlor_first_brazier";
    private const string ChoiceObjectiveId = "ash_parlor_choice";
    private const string SecondBrazierObjectiveId = "ash_parlor_second_brazier";
    private const string ExitObjectiveId = "ash_parlor_exit";
    private const string ExitUnlockedFlagValue = "ash_parlor_exit_unlocked";
    private const string FloorSummaryTitle = "—— 灰烬客厅 · 通过 ——";
    private const string RiskChoiceLabel = "选择了风险之路";
    private const string SafeChoiceLabel = "选择了安全之路";
    private const string RiskNarrative = "你选择直面灰烬。代价刻在身上，但你带走了一片真相的碎片。";
    private const string SafeNarrative = "你绕过了深渊的边缘。安全抵达——却把一些东西永远地留在了身后。";
    private const string DefaultPressureWarningClipPath = "Audio/Sfx/Cello_at_First_Light";
    private static readonly Color FinaleBurstColor = new Color(1f, 0.843f, 0f, 1f);

    private readonly struct LightIntensityState
    {
        public LightIntensityState(Light light, float baseIntensity)
        {
            Light = light;
            BaseIntensity = Mathf.Max(0f, baseIntensity);
        }

        public Light Light { get; }
        public float BaseIntensity { get; }
    }

    private readonly struct AmbientLightingState
    {
        public AmbientLightingState(Color ambientLight, float ambientIntensity)
        {
            AmbientLight = ambientLight;
            AmbientIntensity = ambientIntensity;
        }

        public Color AmbientLight { get; }
        public float AmbientIntensity { get; }
    }

    [Header("Landing Beat")]
    [SerializeField] private string landingMonologueResourcePath = "Dialogue/AshParlor_LandingMonologue";
    [SerializeField] private float landingMonologueDelay = 1f;

    [Header("Pressure Warning")]
    [SerializeField] private float pressureWarningDuration = 1.5f;
    [SerializeField] private float pressureWarningDimDuration = 0.32f;
    [SerializeField] private float pressureWarningMinLightMultiplier = 0f;
    [SerializeField] private float pressureWarningRecoveryLightMultiplier = 0.6f;
    [SerializeField] private float pressureWarningRecoveryDuration = 0.28f;
    [SerializeField] private float pressureWarningSceneDipMultiplier = 0f;
    [SerializeField] private float pressureWarningAmbientMultiplier = 0f;
    [SerializeField] private AudioClip warningClip;

    [Header("Finale Feedback")]
    [SerializeField] private float finaleFlashDuration = 0.3f;
    [SerializeField] private float finaleFlashPeakMultiplier = 1.2f;
    [SerializeField] private Vector3 finaleBurstOffset = new Vector3(0f, 1.18f, 0.04f);

    [Header("Enemy Pressure")]
    [SerializeField] private float pressureEnemySpeed = 1.2f;
    [SerializeField] private float pressureEnemyRange = 1.08f;
    [SerializeField] private float pressureEnemyCooldown = 0.9f;
    [SerializeField] private float finalEnemyBaseSpeed = 1.02f;
    [SerializeField] private float finalEnemyBaseRange = 1.04f;
    [SerializeField] private float finalEnemyBaseCooldown = 0.96f;
    [SerializeField] private float finalEnemyRiskySpeed = 1.24f;
    [SerializeField] private float finalEnemyRiskyRange = 1.12f;
    [SerializeField] private float finalEnemyRiskyCooldown = 0.88f;
    [SerializeField] private float finalEnemySafeSpeed = 0.88f;
    [SerializeField] private float finalEnemySafeRange = 0.96f;
    [SerializeField] private float finalEnemySafeCooldown = 1.12f;

    private readonly Vector2[] _roomBounds = new Vector2[RoomCount];
    private readonly bool[] _roomBoundsRegistered = new bool[RoomCount];
    private readonly bool[] _roomBeatPlayed = new bool[RoomCount];
    private readonly HashSet<string> _floorCollectibleIds = new HashSet<string>();
    private readonly LightZoneEffect[] _roomLightZones = new LightZoneEffect[RoomCount];

    private AshParlorBrazierInteractable _firstBrazier;
    private AshParlorBrazierInteractable _secondBrazier;
    private AshParlorChoiceInteractable _riskyChoice;
    private AshParlorChoiceInteractable _safeChoice;
    private AshParlorChoicePromptInteractable _choicePrompt;
    private AshParlorExitInteractable _exit;
    private AshParlorSealBarrier _pressureSeal;
    private AshParlorSealBarrier _finaleSeal;
    private SimpleEnemyController _pressureEnemy;
    private SimpleEnemyController _finalEnemy;
    private SimpleEnemyController _riskBonusEnemy;
    private PickupInteractable _riskRewardPickup;
    private Transform _choiceAnchor;
    private Light[] _pressureRoomLights = System.Array.Empty<Light>();
    private AudioSource _warningAudioSource;
    private DialogueNode _landingMonologueNode;

    private int _litBraziers;
    private bool _completionSequenceStarted;
    private bool _finalEnemyAwakened;
    private bool _landingMonologueStarted;
    private bool _pressureWarningStarted;
    private ChapterState.ChoiceResult _choiceState;

    public override string ExitUnlockedFlagId => ExitUnlockedFlagValue;

    public override void RegisterRoomBounds(int roomIndex, float startX, float endX)
    {
        if (roomIndex < 0 || roomIndex >= RoomCount)
        {
            return;
        }

        _roomBounds[roomIndex] = new Vector2(startX, endX);
        _roomBoundsRegistered[roomIndex] = true;
    }

    public override void RegisterRoomLightZone(int roomIndex, LightZoneEffect lightZone)
    {
        if (roomIndex < 0 || roomIndex >= RoomCount)
        {
            return;
        }

        _roomLightZones[roomIndex] = lightZone;

        if (lightZone == null)
        {
            return;
        }

        bool shouldBeLit = roomIndex == 0
            || (roomIndex == 1 && _firstBrazier != null && _firstBrazier.IsLit)
            || (roomIndex == 4 && _secondBrazier != null && _secondBrazier.IsLit);
        lightZone.SetLit(shouldBeLit);
    }

    public override void RegisterFirstBrazier(AshParlorBrazierInteractable brazier)
    {
        _firstBrazier = brazier;
        SetRoomLightZoneLit(1, brazier != null && brazier.IsLit);
    }

    public override void RegisterSecondBrazier(AshParlorBrazierInteractable brazier)
    {
        _secondBrazier = brazier;
        SetRoomLightZoneLit(4, brazier != null && brazier.IsLit);
    }

    public override void RegisterChoicePair(
        AshParlorChoiceInteractable riskyChoice,
        AshParlorChoiceInteractable safeChoice,
        AshParlorChoicePromptInteractable choicePrompt,
        Transform choiceAnchor)
    {
        _riskyChoice = riskyChoice;
        _safeChoice = safeChoice;
        _choicePrompt = choicePrompt;
        _choiceAnchor = choiceAnchor;
    }

    public override void RegisterExit(AshParlorExitInteractable exitInteractable)
    {
        _exit = exitInteractable;
        SetExitUnlocked(false);
    }

    public override void RegisterPressureSeal(AshParlorSealBarrier barrier)
    {
        _pressureSeal = barrier;
        _pressureSeal?.SetLocked(true);
    }

    public override void RegisterFinaleSeal(AshParlorSealBarrier barrier)
    {
        _finaleSeal = barrier;
        _finaleSeal?.SetLocked(true);
    }

    public override void RegisterPressureEnemy(SimpleEnemyController enemy)
    {
        _pressureEnemy = enemy;
        if (_pressureEnemy == null)
        {
            return;
        }

        _pressureEnemy.SetEncounterEnabled(false);
        _pressureEnemy.SetEncounterProfile(pressureEnemySpeed, pressureEnemyRange, pressureEnemyCooldown);
        _pressureEnemy.gameObject.SetActive(false);
    }

    public override void RegisterPressureRoomLights(Light[] lights)
    {
        _pressureRoomLights = lights ?? System.Array.Empty<Light>();
    }

    public override void RegisterFinalEnemy(SimpleEnemyController enemy)
    {
        _finalEnemy = enemy;
        _finalEnemy?.SetEncounterEnabled(false);
        _finalEnemy?.SetEncounterProfile(finalEnemyBaseSpeed, finalEnemyBaseRange, finalEnemyBaseCooldown);
    }

    public override void RegisterRiskBonusEnemy(SimpleEnemyController enemy)
    {
        _riskBonusEnemy = enemy;
        if (_riskBonusEnemy == null)
        {
            return;
        }

        _riskBonusEnemy.SetEncounterEnabled(false);
        _riskBonusEnemy.SetEncounterProfile(finalEnemyRiskySpeed, finalEnemyRiskyRange, finalEnemyRiskyCooldown);
        _riskBonusEnemy.gameObject.SetActive(false);
    }

    public override void RegisterRiskRewardPickup(PickupInteractable pickup)
    {
        _riskRewardPickup = pickup;
        if (_riskRewardPickup != null && !string.IsNullOrWhiteSpace(_riskRewardPickup.ItemId))
        {
            _floorCollectibleIds.Add(_riskRewardPickup.ItemId);
        }

        _riskRewardPickup?.SetPickupEnabled(false);
    }

    public void PrepareFloorSummaryTest(FloorSummaryTestPreset preset, PlayerMover player)
    {
        GameStateHub gameStateHub = GameStateHub.Instance;
        ChapterState.ChoiceResult choiceResult = preset == FloorSummaryTestPreset.RiskSummary
            ? ChapterState.ChoiceResult.Risk
            : ChapterState.ChoiceResult.Safe;
        bool collectReward = preset == FloorSummaryTestPreset.RiskSummary;

        Time.timeScale = 1f;

        if (UiBootstrap.TryGetFloorSummaryView(out FloorSummaryPanel summaryPanel))
        {
            summaryPanel.Hide();
        }

        gameStateHub?.ResetRuntimeState();
        DialogueEventSystem.ClearFlags();

        _completionSequenceStarted = false;
        _pressureWarningStarted = true;
        _landingMonologueStarted = true;
        _finalEnemyAwakened = true;
        _litBraziers = 2;

        _firstBrazier?.SetLit(true);
        _secondBrazier?.SetLit(true);
        SetRoomLightZoneLit(1, true);
        SetRoomLightZoneLit(4, true);
        _pressureSeal?.SetLocked(false);
        _finaleSeal?.SetLocked(false);
        SetExitUnlocked(true);

        _choiceState = choiceResult;
        if (gameStateHub != null)
        {
            gameStateHub.CurrentChoiceResult = choiceResult;
        }
        ApplyChoiceConsequences(false);

        if (collectReward && _riskRewardPickup != null && !string.IsNullOrWhiteSpace(_riskRewardPickup.ItemId))
        {
            gameStateHub?.CollectItem(_riskRewardPickup.ItemId);
            _riskRewardPickup.RefreshFromRuntimeState();
        }
        else
        {
            _riskRewardPickup?.RefreshFromRuntimeState();
        }

        DisableEnemyForTest(_pressureEnemy);
        DisableEnemyForTest(_finalEnemy);
        DisableEnemyForTest(_riskBonusEnemy);

        RefreshObjective(true);
        TeleportPlayerToExit(player);
    }

    private void Update()
    {
        SynchronizeChoiceState();
        RefreshObjective();
        UpdateRoomBeats();
    }

    public override bool TryLightBrazier(AshParlorBrazierInteractable brazier, PlayerMover player)
    {
        GameStateHub gameStateHub = GameStateHub.Instance;
        if (brazier == null || _completionSequenceStarted)
        {
            return false;
        }

        if (brazier == _firstBrazier)
        {
            if (_litBraziers > 0)
            {
                ShowLines("灰烬客厅", "第一盏烛台已经稳住了余烬。");
                return false;
            }

            _litBraziers = 1;
            brazier.SetLit(true);
            SetRoomLightZoneLit(1, true);
            gameStateHub?.CompleteObjective(FirstBrazierObjectiveId);
            _pressureSeal?.SetLocked(false);
            RefreshObjective(true);
            return true;
        }

        if (brazier == _secondBrazier)
        {
            if (_choiceState == ChapterState.ChoiceResult.None)
            {
                ShowLines("灰烬客厅", "第二盏烛台前的余烬还没有定形。", "先在前面的房间做出选择。");
                return false;
            }

            if (_litBraziers > 1)
            {
                ShowLines("灰烬客厅", "第二盏烛台已经点亮，塔梯封印正在松动。");
                return false;
            }

            _litBraziers = 2;
            brazier.SetLit(true);
            SetRoomLightZoneLit(4, true);
            SetExitUnlocked(true);
            gameStateHub?.CompleteObjective(SecondBrazierObjectiveId);
            StartCoroutine(PlayFinaleFeedback(brazier.transform.position + finaleBurstOffset));

            if (_pressureEnemy != null)
            {
                _pressureEnemy.SetEncounterProfile(0.94f, 0.98f, 1.08f);
            }

            ApplyFinalEncounterProfile();

            RefreshObjective(true);
            return true;
        }

        return false;
    }

    public override void TryOpenChoicePrompt(PlayerMover player)
    {
        if (_completionSequenceStarted)
        {
            return;
        }

        if (_litBraziers < 1)
        {
            ShowLines("灰烬客厅", "余烬还没有被第一盏灯驯服。", "先让前面的烛台亮起来。");
            return;
        }

        if (_choiceState != ChapterState.ChoiceResult.None)
        {
            ShowLines("灰烬客厅", "灰烬客厅已经记住了你的决定。");
            return;
        }

        AshParlorChoiceOverlay.Instance?.Show(this, player);
    }

    public override bool TryResolveChoice(AshParlorChoiceInteractable.ChoiceKind choiceKind, PlayerMover player)
    {
        if (_completionSequenceStarted || _choiceState != ChapterState.ChoiceResult.None)
        {
            return false;
        }

        _choiceState = choiceKind == AshParlorChoiceInteractable.ChoiceKind.Risky
            ? ChapterState.ChoiceResult.Risk
            : ChapterState.ChoiceResult.Safe;
        if (GameStateHub.Instance != null)
        {
            GameStateHub.Instance.CurrentChoiceResult = _choiceState;
        }
        ApplyChoiceConsequences(true);

        RefreshObjective(true);
        return true;
    }

    public override void TryUseExit(PlayerMover player)
    {
        if (_completionSequenceStarted)
        {
            return;
        }

        if (_litBraziers < 2)
        {
            ShowLines("灰烬客厅", "塔梯还被灰封着。", "第二盏烛台没亮之前，它不会让路。");
            return;
        }

        _completionSequenceStarted = true;
        RewardPlayer(player);
        GameStateHub.Instance?.CompleteObjective(ExitObjectiveId);
        ShowFloorSummary();
    }

    private void UpdateRoomBeats()
    {
        PlayerMover player = FindFirstObjectByType<PlayerMover>();
        if (player == null || SimpleDialogueUI.IsOpen)
        {
            return;
        }

        int roomIndex = GetRoomIndex(player.transform.position.x);
        if (roomIndex < 0 || _roomBeatPlayed[roomIndex])
        {
            return;
        }

        _roomBeatPlayed[roomIndex] = true;

        switch (roomIndex)
        {
            case 0:
                if (!_landingMonologueStarted)
                {
                    _landingMonologueStarted = true;
                    StartCoroutine(PlayLandingMonologueAfterDelay());
                }
                break;
            case 3:
                ShowLines(
                    "伊尔萨恩",
                    "前方的路断成两条。",
                    "左边传来低吼和微弱的光。右边是彻底的沉寂。");
                break;
            case 2:
                if (!_pressureWarningStarted)
                {
                    _pressureWarningStarted = true;
                    StartCoroutine(PlayPressureWarningBeforeEncounter());
                }
                break;
            case 4:
                if (!_finalEnemyAwakened)
                {
                    _finalEnemyAwakened = true;
                    _finalEnemy?.SetEncounterEnabled(true);
                    if (_choiceState == ChapterState.ChoiceResult.Risk && _riskBonusEnemy != null && _riskBonusEnemy.gameObject.activeSelf)
                    {
                        _riskBonusEnemy.SetEncounterEnabled(true);
                    }
                }
                break;
        }
    }

    private int GetRoomIndex(float playerX)
    {
        for (int i = 0; i < RoomCount; i++)
        {
            if (!_roomBoundsRegistered[i])
            {
                continue;
            }

            Vector2 bounds = _roomBounds[i];
            if (playerX >= bounds.x && playerX <= bounds.y)
            {
                return i;
            }
        }

        return -1;
    }

    private void RefreshObjective(bool force = false)
    {
        GameStateHub gameStateHub = GameStateHub.Instance;
        if (_completionSequenceStarted || gameStateHub == null)
        {
            return;
        }

        string desiredId;
        string desiredText;
        string desiredMarker;
        Transform desiredTarget;

        if (_litBraziers <= 0)
        {
            desiredId = FirstBrazierObjectiveId;
            desiredText = "找到第一盏封印烛台";
            desiredMarker = "余烬";
            desiredTarget = _firstBrazier != null ? _firstBrazier.transform : null;
        }
        else if (_choiceState == ChapterState.ChoiceResult.None)
        {
            desiredId = ChoiceObjectiveId;
            desiredText = "走到抉择台前，决定要面对哪一条路";
            desiredMarker = "抉择";
            desiredTarget = _choicePrompt != null ? _choicePrompt.transform : _choiceAnchor != null ? _choiceAnchor : null;
        }
        else if (_litBraziers == 1)
        {
            desiredId = SecondBrazierObjectiveId;
            desiredText = "抵达终局房，点亮第二盏封印烛台";
            desiredMarker = "烛台";
            desiredTarget = _secondBrazier != null ? _secondBrazier.transform : null;
        }
        else
        {
            desiredId = ExitObjectiveId;
            desiredText = "攀上解封后的塔梯";
            desiredMarker = "出口";
            desiredTarget = _exit != null ? _exit.transform : null;
        }

        if (!force
            && gameStateHub.CurrentObjectiveId == desiredId
            && gameStateHub.CurrentObjectiveTarget == desiredTarget
            && !gameStateHub.IsCurrentObjectiveCompleted)
        {
            return;
        }

        gameStateHub.SetObjective(desiredId, desiredText, desiredTarget, desiredMarker);
    }

    private void RewardPlayer(PlayerMover player)
    {
        PlayerCombat combat = player != null ? player.GetComponent<PlayerCombat>() : FindFirstObjectByType<PlayerCombat>();
        if (combat == null || combat.Health == null || combat.Health.IsDead)
        {
            return;
        }

        int targetHealth = Mathf.Min(combat.Health.MaxHealth, combat.Health.CurrentHealth + 1);
        combat.Health.RestoreTo(targetHealth);
    }

    private void SynchronizeChoiceState()
    {
        ChapterState.ChoiceResult storedChoice = GameStateHub.Instance != null
            ? GameStateHub.Instance.CurrentChoiceResult
            : ChapterState.ChoiceResult.None;
        if (storedChoice == ChapterState.ChoiceResult.None || storedChoice == _choiceState)
        {
            return;
        }

        _choiceState = storedChoice;
        ApplyChoiceConsequences(false);
    }

    private void ApplyChoiceConsequences(bool showDialogue)
    {
        _riskyChoice?.SetResolved(true);
        _safeChoice?.SetResolved(true);
        _finaleSeal?.SetLocked(false);
        GameStateHub.Instance?.CompleteObjective(ChoiceObjectiveId);

        ApplyFinalEncounterProfile();
        SetRiskBonusEnemyActive(_choiceState == ChapterState.ChoiceResult.Risk);
        _riskRewardPickup?.SetPickupEnabled(_choiceState == ChapterState.ChoiceResult.Risk);

        if (!showDialogue)
        {
            return;
        }

        if (_choiceState == ChapterState.ChoiceResult.Risk)
        {
            ShowLines(
                "伊尔萨恩",
                "直面它。",
                "这条路从来不让人后悔——但总让人付出代价。");
        }
        else
        {
            ShowLines(
                "伊尔萨恩",
                "沉默是最安全的方向。",
                "但沉默也意味着，有些东西你永远不会知道。");
        }
    }

    private void ShowFloorSummary()
    {
        ShowFloorSummary(
            BuildSummaryData(
                FloorSummaryTitle,
                _floorCollectibleIds,
                RiskChoiceLabel,
                SafeChoiceLabel,
                RiskNarrative,
                SafeNarrative,
                "继续"),
            ContinueFromFloorSummary);
    }

    private void ContinueFromFloorSummary()
    {
        ContinueToFloor(1);
    }

    private void SetExitUnlocked(bool unlocked)
    {
        _exit?.SetUnlocked(unlocked);
        GameStateHub.Instance?.SetChapterFlag(ExitUnlockedFlagValue, unlocked ? "true" : "false");
    }

    private void SetRoomLightZoneLit(int roomIndex, bool isLit)
    {
        if (roomIndex < 0 || roomIndex >= _roomLightZones.Length)
        {
            return;
        }

        _roomLightZones[roomIndex]?.SetLit(isLit);
    }

    private void DisableEnemyForTest(SimpleEnemyController enemy)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.SetEncounterEnabled(false);
        enemy.gameObject.SetActive(false);
    }

    private void TeleportPlayerToExit(PlayerMover player)
    {
        PlayerMover targetPlayer = player != null ? player : FindFirstObjectByType<PlayerMover>();
        if (targetPlayer == null || _exit == null)
        {
            return;
        }

        Vector3 exitPosition = _exit.transform.position;
        targetPlayer.transform.position = new Vector3(exitPosition.x - 2.35f, targetPlayer.transform.position.y, exitPosition.z);
        targetPlayer.transform.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
        targetPlayer.LockControls(0.08f);

        CameraFollow follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (follow != null)
        {
            RoomCameraZone roomZone = TowerInteriorSlice.FindBestZone(targetPlayer);
            if (roomZone != null)
            {
                follow.ConfigureRoomZone(roomZone, true);
            }

            follow.SetTarget(targetPlayer.transform, true);
        }
    }

    private void ApplyFinalEncounterProfile()
    {
        if (_finalEnemy != null)
        {
            switch (_choiceState)
            {
                case ChapterState.ChoiceResult.Risk:
                    _finalEnemy.SetEncounterProfile(finalEnemyRiskySpeed, finalEnemyRiskyRange, finalEnemyRiskyCooldown);
                    break;
                case ChapterState.ChoiceResult.Safe:
                    _finalEnemy.SetEncounterProfile(finalEnemySafeSpeed, finalEnemySafeRange, finalEnemySafeCooldown);
                    break;
                default:
                    _finalEnemy.SetEncounterProfile(finalEnemyBaseSpeed, finalEnemyBaseRange, finalEnemyBaseCooldown);
                    break;
            }
        }

        if (_riskBonusEnemy != null)
        {
            _riskBonusEnemy.SetEncounterProfile(finalEnemyRiskySpeed, finalEnemyRiskyRange, finalEnemyRiskyCooldown);
        }
    }

    private void SetRiskBonusEnemyActive(bool enabled)
    {
        if (_riskBonusEnemy == null)
        {
            return;
        }

        _riskBonusEnemy.gameObject.SetActive(enabled);
        _riskBonusEnemy.SetEncounterEnabled(enabled && _finalEnemyAwakened);
    }

    private IEnumerator PlayLandingMonologueAfterDelay()
    {
        if (landingMonologueDelay > 0f)
        {
            yield return new WaitForSeconds(landingMonologueDelay);
        }

        while (DialogueRunner.Instance == null || SimpleDialogueUI.IsOpen || DialogueRunner.IsActive)
        {
            yield return null;
        }

        DialogueNode node = GetLandingMonologueNode();
        if (node != null)
        {
            DialogueRunner.Instance.StartDialogue(node);
            yield break;
        }

        ShowLines(
            "伊尔萨恩",
            "又是灰烬……比上一层还要浓。像有人刚烧完了什么。",
            "安静得不正常。这种安静，通常意味着“还没开始”。",
            "烛台在更深处。走。");
    }

    private DialogueNode GetLandingMonologueNode()
    {
        if (_landingMonologueNode != null)
        {
            return _landingMonologueNode;
        }

        if (string.IsNullOrWhiteSpace(landingMonologueResourcePath))
        {
            return null;
        }

        _landingMonologueNode = Resources.Load<DialogueNode>(landingMonologueResourcePath);
        if (_landingMonologueNode == null)
        {
            Debug.LogWarning($"[AshParlorRunController] 无法加载落地房独白资源: {landingMonologueResourcePath}", this);
        }

        return _landingMonologueNode;
    }

    private IEnumerator PlayPressureWarningBeforeEncounter()
    {
        PlayWarningClip();

        LightIntensityState[] pressureLights = CaptureLightStates(_pressureRoomLights);
        LightIntensityState[] sceneLights = CaptureLightStates(FindObjectsByType<Light>(FindObjectsSortMode.None));
        LightIntensityState[] sceneOnlyLights = ExcludeLights(sceneLights, pressureLights);
        AmbientLightingState ambientState = CaptureAmbientState();

        if (sceneOnlyLights.Length > 0)
        {
            StartCoroutine(AnimateLightsToMultiplier(sceneOnlyLights, pressureWarningSceneDipMultiplier, pressureWarningDimDuration));
        }

        StartCoroutine(AnimateAmbientToMultiplier(ambientState, pressureWarningAmbientMultiplier, pressureWarningDimDuration));

        if (pressureLights.Length > 0)
        {
            yield return AnimateLightsToMultiplier(pressureLights, pressureWarningMinLightMultiplier, pressureWarningDimDuration);
        }

        float remainingWarningTime = Mathf.Max(0f, pressureWarningDuration - pressureWarningDimDuration);
        if (remainingWarningTime > 0f)
        {
            yield return new WaitForSeconds(remainingWarningTime);
        }

        if (_pressureEnemy != null)
        {
            if (!_pressureEnemy.gameObject.activeSelf)
            {
                _pressureEnemy.gameObject.SetActive(true);
            }

            _pressureEnemy.SetEncounterEnabled(true);
        }

        if (sceneOnlyLights.Length > 0)
        {
            StartCoroutine(AnimateLightsToMultiplier(sceneOnlyLights, 1f, pressureWarningRecoveryDuration));
        }

        StartCoroutine(AnimateAmbientToMultiplier(ambientState, 1f, pressureWarningRecoveryDuration));

        if (pressureLights.Length > 0)
        {
            yield return AnimateLightsToMultiplier(pressureLights, pressureWarningRecoveryLightMultiplier, pressureWarningRecoveryDuration);
        }
    }

    private IEnumerator PlayFinaleFeedback(Vector3 burstPosition)
    {
        CombatVfxFactory.SpawnRisingEmberBurst(burstPosition, FinaleBurstColor);

        LightIntensityState[] allLights = CaptureLightStates(FindObjectsByType<Light>(FindObjectsSortMode.None));
        if (allLights.Length == 0)
        {
            yield break;
        }

        float halfDuration = Mathf.Max(0.01f, finaleFlashDuration * 0.5f);
        yield return AnimateLightsToMultiplier(allLights, finaleFlashPeakMultiplier, halfDuration);
        yield return AnimateLightsToMultiplier(allLights, 1f, halfDuration);
    }

    private void PlayWarningClip()
    {
        AudioClip clip = ResolveWarningClip();
        if (clip == null)
        {
            return;
        }

        if (_warningAudioSource == null)
        {
            _warningAudioSource = GetComponent<AudioSource>();
            if (_warningAudioSource == null)
            {
                _warningAudioSource = gameObject.AddComponent<AudioSource>();
            }

            _warningAudioSource.playOnAwake = false;
            _warningAudioSource.loop = false;
            _warningAudioSource.spatialBlend = 0f;
            _warningAudioSource.volume = 0.9f;
        }

        _warningAudioSource.Stop();
        _warningAudioSource.clip = clip;
        _warningAudioSource.Play();
    }

    private AudioClip ResolveWarningClip()
    {
        if (warningClip != null)
        {
            return warningClip;
        }

        warningClip = Resources.Load<AudioClip>(DefaultPressureWarningClipPath);
        if (warningClip == null)
        {
            Debug.LogWarning($"[AshParlorRunController] Missing pressure warning clip: {DefaultPressureWarningClipPath}", this);
        }

        return warningClip;
    }

    private static LightIntensityState[] CaptureLightStates(IReadOnlyList<Light> lights)
    {
        if (lights == null || lights.Count == 0)
        {
            return System.Array.Empty<LightIntensityState>();
        }

        List<LightIntensityState> states = new List<LightIntensityState>(lights.Count);
        HashSet<Light> seenLights = new HashSet<Light>();

        for (int index = 0; index < lights.Count; index++)
        {
            Light light = lights[index];
            if (light == null || !light.enabled || !seenLights.Add(light))
            {
                continue;
            }

            states.Add(new LightIntensityState(light, light.intensity));
        }

        return states.ToArray();
    }

    private static LightIntensityState[] ExcludeLights(IReadOnlyList<LightIntensityState> source, IReadOnlyList<LightIntensityState> excluded)
    {
        if (source == null || source.Count == 0)
        {
            return System.Array.Empty<LightIntensityState>();
        }

        if (excluded == null || excluded.Count == 0)
        {
            LightIntensityState[] copy = new LightIntensityState[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }

        HashSet<Light> excludedLights = new HashSet<Light>();
        for (int index = 0; index < excluded.Count; index++)
        {
            Light light = excluded[index].Light;
            if (light != null)
            {
                excludedLights.Add(light);
            }
        }

        List<LightIntensityState> result = new List<LightIntensityState>(source.Count);
        for (int index = 0; index < source.Count; index++)
        {
            LightIntensityState state = source[index];
            if (state.Light == null || excludedLights.Contains(state.Light))
            {
                continue;
            }

            result.Add(state);
        }

        return result.ToArray();
    }

    private static AmbientLightingState CaptureAmbientState()
    {
        return new AmbientLightingState(RenderSettings.ambientLight, RenderSettings.ambientIntensity);
    }

    private static IEnumerator AnimateLightsToMultiplier(IReadOnlyList<LightIntensityState> lights, float targetMultiplier, float duration)
    {
        if (lights == null || lights.Count == 0)
        {
            yield break;
        }

        targetMultiplier = Mathf.Max(0f, targetMultiplier);

        float[] startIntensities = new float[lights.Count];
        for (int index = 0; index < lights.Count; index++)
        {
            Light light = lights[index].Light;
            startIntensities[index] = light != null ? light.intensity : 0f;
        }

        if (duration <= 0f)
        {
            for (int index = 0; index < lights.Count; index++)
            {
                Light light = lights[index].Light;
                if (light == null)
                {
                    continue;
                }

                light.intensity = lights[index].BaseIntensity * targetMultiplier;
            }

            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            for (int index = 0; index < lights.Count; index++)
            {
                Light light = lights[index].Light;
                if (light == null)
                {
                    continue;
                }

                float targetIntensity = lights[index].BaseIntensity * targetMultiplier;
                light.intensity = Mathf.Lerp(startIntensities[index], targetIntensity, eased);
            }

            yield return null;
        }

        for (int index = 0; index < lights.Count; index++)
        {
            Light light = lights[index].Light;
            if (light == null)
            {
                continue;
            }

            light.intensity = lights[index].BaseIntensity * targetMultiplier;
        }
    }

    private static IEnumerator AnimateAmbientToMultiplier(AmbientLightingState ambientState, float targetMultiplier, float duration)
    {
        targetMultiplier = Mathf.Max(0f, targetMultiplier);

        Color startColor = RenderSettings.ambientLight;
        float startIntensity = RenderSettings.ambientIntensity;
        Color targetColor = ambientState.AmbientLight * targetMultiplier;
        float targetIntensity = ambientState.AmbientIntensity * targetMultiplier;

        if (duration <= 0f)
        {
            RenderSettings.ambientLight = targetColor;
            RenderSettings.ambientIntensity = targetIntensity;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            RenderSettings.ambientLight = Color.Lerp(startColor, targetColor, eased);
            RenderSettings.ambientIntensity = Mathf.Lerp(startIntensity, targetIntensity, eased);
            yield return null;
        }

        RenderSettings.ambientLight = targetColor;
        RenderSettings.ambientIntensity = targetIntensity;
    }

    public override ChoiceOverlayConfig GetChoiceOverlayConfig()
    {
        return new ChoiceOverlayConfig(
            "选择房分支",
            "前方的路断成两条。直接在下面两个按钮里选一条路。",
            "A / ← / 1 与 D / → / 2 切换    Enter / E 确认    Esc 返回",
            "风险",
            "走向低吼\n终局更危险，但可获得线索结晶。",
            "保守",
            "走向沉寂\n终局更平稳，但不会获得额外线索。");
    }

    private static new void ShowLines(string speaker, params string[] lines)
    {
        if (SimpleDialogueUI.Instance == null)
        {
            return;
        }

        SimpleDialogueUI.Instance.Show(speaker, lines);
    }
}
