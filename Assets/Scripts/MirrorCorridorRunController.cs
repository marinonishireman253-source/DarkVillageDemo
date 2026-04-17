using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class MirrorCorridorRunController : FloorRunController
{
    private const float PressureEnemyMoveMultiplier = 1.22f;
    private const float PressureEnemyAttackRangeMultiplier = 1.08f;
    private const float PressureEnemyAttackCooldownMultiplier = 0.8f;
    private const float FinalEnemyDefaultMoveMultiplier = 1.05f;
    private const float FinalEnemyDefaultAttackRangeMultiplier = 1.1f;
    private const float FinalEnemyDefaultAttackCooldownMultiplier = 0.96f;
    private const float FinalEnemyRiskMoveMultiplier = 1.26f;
    private const float FinalEnemyRiskAttackRangeMultiplier = 1.18f;
    private const float FinalEnemyRiskAttackCooldownMultiplier = 0.76f;
    private const float FinalEnemySafeMoveMultiplier = 0.9f;
    private const float FinalEnemySafeAttackRangeMultiplier = 0.98f;
    private const float FinalEnemySafeAttackCooldownMultiplier = 1.16f;

    private const int RoomCount = 5;
    private const string FirstBrazierObjectiveId = "mirror_corridor_first_brazier";
    private const string ChoiceObjectiveId = "mirror_corridor_choice";
    private const string SecondBrazierObjectiveId = "mirror_corridor_second_brazier";
    private const string ExitObjectiveId = "mirror_corridor_exit";
    private const string ExitUnlockedFlagValue = "mirror_corridor_exit_unlocked";
    private const string FloorSummaryTitle = "—— 铜镜长廊 · 通过 ——";
    private const string RiskChoiceLabel = "选择了打碎铜镜";
    private const string SafeChoiceLabel = "选择了绕过铜镜";
    private const string RiskNarrative = "你打碎了镜子。碎片映出的不是过去——而是还没到来的未来。";
    private const string SafeNarrative = "镜子完好无损。你也完好无损。在这种地方，完好无损从来不是免费的。";
    private const string DialogueCatalogPath = "Dialogue/MirrorCorridorDialogueSet";

    private readonly Vector2[] _roomBounds = new Vector2[RoomCount];
    private readonly bool[] _roomBoundsRegistered = new bool[RoomCount];
    private readonly bool[] _roomBeatPlayed = new bool[RoomCount];
    private readonly HashSet<string> _floorCollectibleIds = new HashSet<string>();
    private readonly LightZoneEffect[] _roomLightZones = new LightZoneEffect[RoomCount];
    private readonly List<SimpleEnemyController> _pressureEnemies = new List<SimpleEnemyController>(2);

    private AshParlorBrazierInteractable _firstBrazier;
    private AshParlorBrazierInteractable _secondBrazier;
    private AshParlorChoiceInteractable _riskyChoice;
    private AshParlorChoiceInteractable _safeChoice;
    private AshParlorChoicePromptInteractable _choicePrompt;
    private AshParlorExitInteractable _exit;
    private AshParlorSealBarrier _pressureSeal;
    private AshParlorSealBarrier _finaleSeal;
    private SimpleEnemyController _finalEnemy;
    private PickupInteractable _riskRewardPickup;
    private Transform _choiceAnchor;
    private FloorDialogueSet _dialogueSet;

    private int _litBraziers;
    private bool _completionSequenceStarted;
    private bool _landingMonologueStarted;
    private bool _pressureEncounterStarted;
    private bool _finalEnemyAwakened;
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
        if (enemy == null)
        {
            return;
        }

        _pressureEnemies.Add(enemy);
        enemy.SetEncounterEnabled(false);
        enemy.gameObject.SetActive(false);
        enemy.SetEncounterProfile(
            PressureEnemyMoveMultiplier,
            PressureEnemyAttackRangeMultiplier,
            PressureEnemyAttackCooldownMultiplier);
    }

    public override void RegisterFinalEnemy(SimpleEnemyController enemy)
    {
        _finalEnemy = enemy;
        _finalEnemy?.SetEncounterEnabled(false);
        _finalEnemy?.SetEncounterProfile(
            FinalEnemyDefaultMoveMultiplier,
            FinalEnemyDefaultAttackRangeMultiplier,
            FinalEnemyDefaultAttackCooldownMultiplier);
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
                ShowLines("铜镜长廊", "第一盏灯已经把镜廊的边缘钉住了。");
                return false;
            }

            _litBraziers = 1;
            brazier.SetLit(true);
            SetRoomLightZoneLit(1, true);
            _pressureSeal?.SetLocked(false);
            gameStateHub?.CompleteObjective(FirstBrazierObjectiveId);
            RefreshObjective(true);
            return true;
        }

        if (brazier == _secondBrazier)
        {
            if (_choiceState == ChapterState.ChoiceResult.None)
            {
                ShowLines("铜镜长廊", "第二盏灯前的影子还没有定形。", "先在镜子前做出选择。");
                return false;
            }

            if (_litBraziers > 1)
            {
                ShowLines("铜镜长廊", "第二盏灯已经亮着，长廊的尽头正在松动。");
                return false;
            }

            _litBraziers = 2;
            brazier.SetLit(true);
            SetRoomLightZoneLit(4, true);
            SetExitUnlocked(true);
            gameStateHub?.CompleteObjective(SecondBrazierObjectiveId);
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
            ShowLines("铜镜长廊", "镜面还太暗。先让前面的灯稳住这一层。");
            return;
        }

        if (_choiceState != ChapterState.ChoiceResult.None)
        {
            ShowLines("铜镜长廊", "镜子已经记住了你的决定。");
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
            ShowLines("铜镜长廊", "尽头还没有让路。第二盏灯没亮，它只会把你送回自己面前。");
            return;
        }

        _completionSequenceStarted = true;
        RewardPlayer(player);
        GameStateHub.Instance?.CompleteObjective(ExitObjectiveId);
        ShowFloorSummary(
            BuildSummaryData(
                FloorSummaryTitle,
                _floorCollectibleIds,
                RiskChoiceLabel,
                SafeChoiceLabel,
                RiskNarrative,
                SafeNarrative,
                "继续前行"),
            ContinueFromFloorSummary);
    }

    public override ChoiceOverlayConfig GetChoiceOverlayConfig()
    {
        return new ChoiceOverlayConfig(
            "铜镜前",
            "走廊正中立着一面完好无损的铜镜。镜中映出穿旧制服的人影，始终背对着你。",
            "A / ← / 1 与 D / → / 2 切换    Enter / E 确认    Esc 返回",
            "风险",
            "打碎铜镜\n会引来更暴烈的回声，但你能带走一块镜片。",
            "保守",
            "绕过铜镜\n终局会稳一些，但那道人影会一路跟到门前。");
    }

    private void ContinueFromFloorSummary()
    {
        ContinueFromCompletedFloor();
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
            case 2:
                if (!_pressureEncounterStarted)
                {
                    _pressureEncounterStarted = true;
                    ShowLines("伊尔萨恩", "不止一个。", "它们学会了结伴。");
                    ActivatePressureEnemies();
                }
                break;
            case 3:
                ShowLines("伊尔萨恩", "尽头不是出口，是镜子。", "别急着相信它，也别急着否认它。");
                break;
            case 4:
                if (!_finalEnemyAwakened)
                {
                    _finalEnemyAwakened = true;
                    if (_choiceState == ChapterState.ChoiceResult.Risk)
                    {
                        ShowLines("伊尔萨恩", "碎裂的回声先到了。", "它知道你带着一块镜片。");
                    }
                    else if (_choiceState == ChapterState.ChoiceResult.Safe)
                    {
                        ShowLines("伊尔萨恩", "它没有立刻扑过来。", "它只是比上一间更近了。");
                    }

                    _finalEnemy?.SetEncounterEnabled(true);
                }
                break;
        }
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
            desiredText = "找到第一盏灯，让铜镜长廊定形";
            desiredMarker = "灯";
            desiredTarget = _firstBrazier != null ? _firstBrazier.transform : null;
        }
        else if (_choiceState == ChapterState.ChoiceResult.None)
        {
            desiredId = ChoiceObjectiveId;
            desiredText = "走到铜镜前，决定是打碎它还是绕过它";
            desiredMarker = "镜";
            desiredTarget = _choicePrompt != null ? _choicePrompt.transform : _choiceAnchor;
        }
        else if (_litBraziers == 1)
        {
            desiredId = SecondBrazierObjectiveId;
            desiredText = "深入终局房，点亮第二盏灯";
            desiredMarker = "灯";
            desiredTarget = _secondBrazier != null ? _secondBrazier.transform : null;
        }
        else
        {
            desiredId = ExitObjectiveId;
            desiredText = "穿过走廊尽头那道真正打开的门";
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

    private void ApplyChoiceConsequences(bool showDialogue)
    {
        _riskyChoice?.SetResolved(true);
        _safeChoice?.SetResolved(true);
        _finaleSeal?.SetLocked(false);
        GameStateHub.Instance?.CompleteObjective(ChoiceObjectiveId);
        _riskRewardPickup?.SetPickupEnabled(_choiceState == ChapterState.ChoiceResult.Risk);

        if (_finalEnemy != null)
        {
            switch (_choiceState)
            {
                case ChapterState.ChoiceResult.Risk:
                    _finalEnemy.SetEncounterProfile(
                        FinalEnemyRiskMoveMultiplier,
                        FinalEnemyRiskAttackRangeMultiplier,
                        FinalEnemyRiskAttackCooldownMultiplier);
                    break;
                case ChapterState.ChoiceResult.Safe:
                    _finalEnemy.SetEncounterProfile(
                        FinalEnemySafeMoveMultiplier,
                        FinalEnemySafeAttackRangeMultiplier,
                        FinalEnemySafeAttackCooldownMultiplier);
                    break;
                default:
                    _finalEnemy.SetEncounterProfile(
                        FinalEnemyDefaultMoveMultiplier,
                        FinalEnemyDefaultAttackRangeMultiplier,
                        FinalEnemyDefaultAttackCooldownMultiplier);
                    break;
            }
        }

        if (!showDialogue)
        {
            return;
        }

        if (_choiceState == ChapterState.ChoiceResult.Risk)
        {
            ShowLines(
                "铜镜长廊",
                "镜面碎开的一瞬间，你看见了门缝里的金光。",
                "碎片划破了手掌，但你带走了它不肯给你的东西。",
                "尽头房里会留下其中一块，别把它留给回声。");
        }
        else
        {
            ShowLines(
                "铜镜长廊",
                "你从镜子旁边走了过去。",
                "快离开时，余光里那个人影转过了身。",
                "你没有受伤，只是把它也一起带向了尽头。");
        }
    }

    private void ActivatePressureEnemies()
    {
        for (int i = 0; i < _pressureEnemies.Count; i++)
        {
            SimpleEnemyController enemy = _pressureEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            if (!enemy.gameObject.activeSelf)
            {
                enemy.gameObject.SetActive(true);
            }

            enemy.SetEncounterEnabled(true);
        }
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

    private IEnumerator PlayLandingMonologueAfterDelay()
    {
        yield return new WaitForSeconds(1f);

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
            "空气变稠了。灰烬不再飘落——它们悬浮着，像凝固的雨。",
            "这一层更窄。墙壁在往里挤。",
            "第二盏灯……藏在更深的地方。");
    }

    private DialogueNode GetLandingMonologueNode()
    {
        if (_dialogueSet == null)
        {
            _dialogueSet = Resources.Load<FloorDialogueSet>(DialogueCatalogPath);
        }

        return _dialogueSet != null ? _dialogueSet.LandingMonologueNode : null;
    }
}
