using System;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MirrorCorridorPlaythroughCheck
{
    private const string SkipInitialMenuRedirectKey = "DarkVillage.SkipInitialMenuRedirect";
    private const string RunningKey = "DarkVillage.MirrorCorridorPlaythroughCheck.Running";
    private const string BatchModeKey = "DarkVillage.MirrorCorridorPlaythroughCheck.BatchMode";
    private const string RunStartKey = "DarkVillage.MirrorCorridorPlaythroughCheck.RunStart";
    private const string PlayStartKey = "DarkVillage.MirrorCorridorPlaythroughCheck.PlayStart";
    private const string PhaseKey = "DarkVillage.MirrorCorridorPlaythroughCheck.Phase";
    private const string RouteKey = "DarkVillage.MirrorCorridorPlaythroughCheck.Route";
    private const double TimeoutSeconds = 28d;
    private const double ValidationDelaySeconds = 1.5d;
    private const float FloatTolerance = 0.0001f;
    private const float PressureEnemyMoveMultiplier = 1.22f;
    private const float PressureEnemyAttackRangeMultiplier = 1.08f;
    private const float PressureEnemyAttackCooldownMultiplier = 0.8f;
    private const float FinalEnemyRiskMoveMultiplier = 1.26f;
    private const float FinalEnemyRiskAttackRangeMultiplier = 1.18f;
    private const float FinalEnemyRiskAttackCooldownMultiplier = 0.76f;
    private const float FinalEnemySafeMoveMultiplier = 0.9f;
    private const float FinalEnemySafeAttackRangeMultiplier = 0.98f;
    private const float FinalEnemySafeAttackCooldownMultiplier = 1.16f;
    private const string MirrorRewardItemId = "mirror_corridor_shard";

    private static readonly FieldInfo RoomBoundsField = typeof(MirrorCorridorRunController).GetField("_roomBounds", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo RoomBoundsRegisteredField = typeof(MirrorCorridorRunController).GetField("_roomBoundsRegistered", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo MoveSpeedMultiplierField = typeof(SimpleEnemyController).GetField("_moveSpeedMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo AttackRangeMultiplierField = typeof(SimpleEnemyController).GetField("_attackRangeMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo AttackCooldownMultiplierField = typeof(SimpleEnemyController).GetField("_attackCooldownMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo SelectRiskMethod = typeof(AshParlorChoiceOverlay).GetMethod("SelectRisk", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo SelectSafeMethod = typeof(AshParlorChoiceOverlay).GetMethod("SelectSafe", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SummaryTitleField = typeof(FloorSummaryPanel).GetField("_titleText", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SummaryCollectionField = typeof(FloorSummaryPanel).GetField("_collectionValueText", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SummaryChoiceField = typeof(FloorSummaryPanel).GetField("_choiceValueText", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SummaryNarrativeField = typeof(FloorSummaryPanel).GetField("_narrativeText", BindingFlags.Instance | BindingFlags.NonPublic);

    private static bool s_CallbacksRegistered;

    private enum Route
    {
        Risk = 0,
        Safe = 1
    }

    private enum Phase
    {
        Boot = 0,
        LightFirstBrazier = 1,
        MoveToPressureRoom = 2,
        VerifyPressureEncounter = 3,
        MoveToChoiceRoom = 4,
        OpenChoicePrompt = 5,
        ResolveChoice = 6,
        MoveToFinalRoom = 7,
        VerifyFinalRoom = 8,
        AttemptLockedExit = 9,
        LightSecondBrazier = 10,
        VerifyRewardState = 11,
        UseExit = 12,
        VerifySummary = 13
    }

    [MenuItem("Tools/DarkVillage/Playthrough Test Mirror Corridor/Risk Route")]
    public static void RunRiskFromMenu()
    {
        Start(runInBatchMode: false, Route.Risk);
    }

    [MenuItem("Tools/DarkVillage/Playthrough Test Mirror Corridor/Safe Route")]
    public static void RunSafeFromMenu()
    {
        Start(runInBatchMode: false, Route.Safe);
    }

    public static void RunRiskInBatchMode()
    {
        Start(runInBatchMode: true, Route.Risk);
    }

    public static void RunSafeInBatchMode()
    {
        Start(runInBatchMode: true, Route.Safe);
    }

    private static void Start(bool runInBatchMode, Route route)
    {
        if (SessionState.GetBool(RunningKey, false))
        {
            Debug.LogWarning("[MirrorCorridorPlaythroughCheck] Playthrough check is already running.");
            return;
        }

        SessionState.SetBool(RunningKey, true);
        SessionState.SetBool(BatchModeKey, runInBatchMode);
        SessionState.SetString(RunStartKey, EditorApplication.timeSinceStartup.ToString("R"));
        SessionState.EraseString(PlayStartKey);
        SessionState.SetInt(PhaseKey, (int)Phase.Boot);
        SessionState.SetInt(RouteKey, (int)route);
        RegisterCallbacks();

        PlayerPrefs.SetInt(SkipInitialMenuRedirectKey, 1);
        PlayerPrefs.Save();
        GameStateHub.SetCurrentFloorIndexRuntime(1);
        EditorSceneManager.OpenScene(SceneLoader.MainScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void RegisterCallbacks()
    {
        if (s_CallbacksRegistered)
        {
            return;
        }

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += Update;
        s_CallbacksRegistered = true;
    }

    private static void UnregisterCallbacks()
    {
        if (!s_CallbacksRegistered)
        {
            return;
        }

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= Update;
        s_CallbacksRegistered = false;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(RunningKey, false))
        {
            return;
        }

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetString(PlayStartKey, EditorApplication.timeSinceStartup.ToString("R"));
        }
    }

    private static void Update()
    {
        if (!SessionState.GetBool(RunningKey, false))
        {
            UnregisterCallbacks();
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        double runStart = ReadTime(RunStartKey);
        if (runStart > 0d && now - runStart > TimeoutSeconds)
        {
            Fail("Timed out waiting for Mirror Corridor playthrough check to complete.");
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            return;
        }

        double playStart = ReadTime(PlayStartKey);
        if (playStart <= 0d)
        {
            playStart = now;
            SessionState.SetString(PlayStartKey, playStart.ToString("R"));
        }

        if (now - playStart < ValidationDelaySeconds)
        {
            return;
        }

        switch ((Phase)SessionState.GetInt(PhaseKey, (int)Phase.Boot))
        {
            case Phase.Boot:
                AdvanceBootPhase();
                break;
            case Phase.LightFirstBrazier:
                AdvanceLightFirstBrazierPhase();
                break;
            case Phase.MoveToPressureRoom:
                AdvanceMoveToPressureRoomPhase();
                break;
            case Phase.VerifyPressureEncounter:
                AdvanceVerifyPressureEncounterPhase();
                break;
            case Phase.MoveToChoiceRoom:
                AdvanceMoveToChoiceRoomPhase();
                break;
            case Phase.OpenChoicePrompt:
                AdvanceOpenChoicePromptPhase();
                break;
            case Phase.ResolveChoice:
                AdvanceResolveChoicePhase();
                break;
            case Phase.MoveToFinalRoom:
                AdvanceMoveToFinalRoomPhase();
                break;
            case Phase.VerifyFinalRoom:
                AdvanceVerifyFinalRoomPhase();
                break;
            case Phase.AttemptLockedExit:
                AdvanceAttemptLockedExitPhase();
                break;
            case Phase.LightSecondBrazier:
                AdvanceLightSecondBrazierPhase();
                break;
            case Phase.VerifyRewardState:
                AdvanceVerifyRewardStatePhase();
                break;
            case Phase.UseExit:
                AdvanceUseExitPhase();
                break;
            case Phase.VerifySummary:
                AdvanceVerifySummaryPhase();
                break;
        }
    }

    private static void AdvanceBootPhase()
    {
        MirrorCorridorRunController controller = UnityEngine.Object.FindFirstObjectByType<MirrorCorridorRunController>();
        PlayerMover player = UnityEngine.Object.FindFirstObjectByType<PlayerMover>();
        if (controller == null || player == null)
        {
            return;
        }

        if (SceneManager.GetActiveScene().path != SceneLoader.MainScenePath)
        {
            Fail("Unexpected active scene during Mirror Corridor boot phase.");
            return;
        }

        if (GameStateHub.CurrentFloorIndexRuntime != 1)
        {
            Fail($"CurrentFloorIndexRuntime should be 1 but was {GameStateHub.CurrentFloorIndexRuntime}.");
            return;
        }

        SimpleEnemyController[] pressureEnemies = FindPressureEnemies();
        if (pressureEnemies.Length != 2)
        {
            Fail($"Expected 2 pressure enemies, found {pressureEnemies.Length}.");
            return;
        }

        for (int i = 0; i < pressureEnemies.Length; i++)
        {
            SimpleEnemyController enemy = pressureEnemies[i];
            if (enemy.gameObject.activeSelf || enemy.IsEncounterEnabled)
            {
                Fail("Pressure enemies should stay dormant before the pressure room encounter starts.");
                return;
            }

            if (!ValidateEncounterProfile(enemy, PressureEnemyMoveMultiplier, PressureEnemyAttackRangeMultiplier, PressureEnemyAttackCooldownMultiplier, "pressure enemy"))
            {
                return;
            }
        }

        if (GameStateHub.Instance == null || !GameStateHub.Instance.IsCurrentObjective("mirror_corridor_first_brazier"))
        {
            return;
        }

        AshParlorChoicePromptInteractable choicePrompt = UnityEngine.Object.FindFirstObjectByType<AshParlorChoicePromptInteractable>();
        if (choicePrompt == null || choicePrompt.PromptText != "直面铜镜")
        {
            Fail("Mirror choice prompt is missing or prompt text is incorrect.");
            return;
        }

        SessionState.SetInt(PhaseKey, (int)Phase.LightFirstBrazier);
    }

    private static void AdvanceLightFirstBrazierPhase()
    {
        if (!TryGetPlaythroughContext(out MirrorCorridorRunController controller, out PlayerMover player, out AshParlorBrazierInteractable firstBrazier, out _, out _, out _, out _, out _))
        {
            return;
        }

        SimpleDialogueUI.Instance?.Hide();
        MovePlayerToRoom(player, controller, 1);
        firstBrazier.Interact(player);
        if (!firstBrazier.IsLit)
        {
            return;
        }

        AshParlorSealBarrier pressureSeal = FindBarrier("Seal_PressureDoor");
        if (pressureSeal == null || pressureSeal.IsLocked)
        {
            Fail("Pressure seal should unlock after lighting the first brazier.");
            return;
        }

        LightZoneEffect lightZone = FindLightZone("刻痕亮区");
        if (lightZone == null || !lightZone.IsLit)
        {
            Fail("The rule room should become lit after the first brazier.");
            return;
        }

        if (!GameStateHub.Instance.IsCurrentObjective("mirror_corridor_choice"))
        {
            Fail("Objective should advance to the mirror choice after the first brazier.");
            return;
        }

        SessionState.SetInt(PhaseKey, (int)Phase.MoveToPressureRoom);
    }

    private static void AdvanceMoveToPressureRoomPhase()
    {
        if (!TryGetPlaythroughContext(out MirrorCorridorRunController controller, out PlayerMover player, out _, out _, out _, out _, out _, out _))
        {
            return;
        }

        SimpleDialogueUI.Instance?.Hide();
        MovePlayerToRoom(player, controller, 2);
        SessionState.SetInt(PhaseKey, (int)Phase.VerifyPressureEncounter);
    }

    private static void AdvanceVerifyPressureEncounterPhase()
    {
        SimpleEnemyController[] pressureEnemies = FindPressureEnemies();
        if (pressureEnemies.Length != 2)
        {
            Fail($"Expected 2 pressure enemies during the encounter, found {pressureEnemies.Length}.");
            return;
        }

        for (int i = 0; i < pressureEnemies.Length; i++)
        {
            SimpleEnemyController enemy = pressureEnemies[i];
            if (!enemy.gameObject.activeSelf || !enemy.IsEncounterEnabled)
            {
                return;
            }
        }

        SessionState.SetInt(PhaseKey, (int)Phase.MoveToChoiceRoom);
    }

    private static void AdvanceMoveToChoiceRoomPhase()
    {
        if (!TryGetPlaythroughContext(out MirrorCorridorRunController controller, out PlayerMover player, out _, out _, out _, out _, out _, out _))
        {
            return;
        }

        SimpleDialogueUI.Instance?.Hide();
        MovePlayerToRoom(player, controller, 3);
        SessionState.SetInt(PhaseKey, (int)Phase.OpenChoicePrompt);
    }

    private static void AdvanceOpenChoicePromptPhase()
    {
        if (!TryGetPlaythroughContext(out _, out PlayerMover player, out _, out _, out AshParlorChoicePromptInteractable choicePrompt, out _, out _, out _))
        {
            return;
        }

        SimpleDialogueUI.Instance?.Hide();
        choicePrompt.Interact(player);
        if (!AshParlorChoiceOverlay.IsVisible || AshParlorChoiceOverlay.Instance == null)
        {
            return;
        }

        if (!Mathf.Approximately(Time.timeScale, 0f))
        {
            Fail("Opening the mirror choice should pause time.");
            return;
        }

        SessionState.SetInt(PhaseKey, (int)Phase.ResolveChoice);
    }

    private static void AdvanceResolveChoicePhase()
    {
        if (!TryGetPlaythroughContext(out _, out _, out _, out _, out _, out _, out _, out SimpleEnemyController finalEnemy))
        {
            return;
        }

        Route route = CurrentRoute;
        MethodInfo method = route == Route.Risk ? SelectRiskMethod : SelectSafeMethod;
        if (method == null || AshParlorChoiceOverlay.Instance == null)
        {
            Fail("Could not resolve mirror choice overlay callbacks.");
            return;
        }

        method.Invoke(AshParlorChoiceOverlay.Instance, null);
        if (AshParlorChoiceOverlay.IsVisible)
        {
            return;
        }

        if (!Mathf.Approximately(Time.timeScale, 1f))
        {
            Fail("Resolving the mirror choice should restore time scale.");
            return;
        }

        ChapterState.ChoiceResult expectedChoice = route == Route.Risk
            ? ChapterState.ChoiceResult.Risk
            : ChapterState.ChoiceResult.Safe;
        if (GameStateHub.Instance == null || GameStateHub.Instance.CurrentChoiceResult != expectedChoice)
        {
            Fail("Mirror choice result did not persist correctly.");
            return;
        }

        AshParlorSealBarrier finaleSeal = FindBarrier("Seal_FinaleDoor");
        if (finaleSeal == null || finaleSeal.IsLocked)
        {
            Fail("Finale seal should unlock after the mirror choice.");
            return;
        }

        if (!GameStateHub.Instance.IsCurrentObjective("mirror_corridor_second_brazier"))
        {
            Fail("Objective should advance to the second brazier after the mirror choice.");
            return;
        }

        bool profileValid = route == Route.Risk
            ? ValidateEncounterProfile(finalEnemy, FinalEnemyRiskMoveMultiplier, FinalEnemyRiskAttackRangeMultiplier, FinalEnemyRiskAttackCooldownMultiplier, "final enemy risk profile")
            : ValidateEncounterProfile(finalEnemy, FinalEnemySafeMoveMultiplier, FinalEnemySafeAttackRangeMultiplier, FinalEnemySafeAttackCooldownMultiplier, "final enemy safe profile");
        if (!profileValid)
        {
            return;
        }

        SessionState.SetInt(PhaseKey, (int)Phase.MoveToFinalRoom);
    }

    private static void AdvanceMoveToFinalRoomPhase()
    {
        if (!TryGetPlaythroughContext(out MirrorCorridorRunController controller, out PlayerMover player, out _, out _, out _, out _, out _, out _))
        {
            return;
        }

        SimpleDialogueUI.Instance?.Hide();
        MovePlayerToRoom(player, controller, 4);
        SessionState.SetInt(PhaseKey, (int)Phase.VerifyFinalRoom);
    }

    private static void AdvanceVerifyFinalRoomPhase()
    {
        if (!TryGetPlaythroughContext(out _, out _, out _, out _, out _, out _, out PickupInteractable rewardPickup, out SimpleEnemyController finalEnemy))
        {
            return;
        }

        if (finalEnemy == null || !finalEnemy.IsEncounterEnabled)
        {
            return;
        }

        bool rewardAvailable = HasActivePickupPresentation(rewardPickup);
        if (CurrentRoute == Route.Risk && !rewardAvailable)
        {
            Fail("Risk route should expose the mirror shard reward in the finale room.");
            return;
        }

        if (CurrentRoute == Route.Safe && rewardAvailable)
        {
            Fail("Safe route should keep the mirror shard reward unavailable.");
            return;
        }

        SessionState.SetInt(PhaseKey, (int)Phase.AttemptLockedExit);
    }

    private static void AdvanceAttemptLockedExitPhase()
    {
        if (!TryGetPlaythroughContext(out _, out PlayerMover player, out _, out _, out _, out AshParlorExitInteractable exitInteractable, out _, out _))
        {
            return;
        }

        SimpleDialogueUI.Instance?.Hide();
        exitInteractable.Interact(player);
        if (FloorSummaryPanel.IsVisible)
        {
            Fail("Exit should stay locked until the second brazier is lit.");
            return;
        }

        SessionState.SetInt(PhaseKey, (int)Phase.LightSecondBrazier);
    }

    private static void AdvanceLightSecondBrazierPhase()
    {
        if (!TryGetPlaythroughContext(out _, out PlayerMover player, out _, out AshParlorBrazierInteractable secondBrazier, out _, out AshParlorExitInteractable exitInteractable, out _, out _))
        {
            return;
        }

        SimpleDialogueUI.Instance?.Hide();
        secondBrazier.Interact(player);
        if (!secondBrazier.IsLit)
        {
            return;
        }

        LightZoneEffect lightZone = FindLightZone("终局镜门");
        if (lightZone == null || !lightZone.IsLit)
        {
            Fail("The finale room should become lit after the second brazier.");
            return;
        }

        if (exitInteractable.PromptText != "攀上塔梯")
        {
            Fail("Exit prompt should switch to the unlocked state after the second brazier.");
            return;
        }

        if (!GameStateHub.Instance.IsCurrentObjective("mirror_corridor_exit"))
        {
            Fail("Objective should advance to the exit after the second brazier.");
            return;
        }

        SessionState.SetInt(PhaseKey, (int)Phase.VerifyRewardState);
    }

    private static void AdvanceVerifyRewardStatePhase()
    {
        if (!TryGetPlaythroughContext(out _, out PlayerMover player, out _, out _, out _, out _, out PickupInteractable rewardPickup, out _))
        {
            return;
        }

        if (CurrentRoute == Route.Risk)
        {
            if (rewardPickup == null)
            {
                Fail("Risk route reward pickup is missing.");
                return;
            }

            rewardPickup.Interact(player);
            InventoryController.FloorCollectionSummary collectionSummary = InventoryController.GetCurrentFloorCollectionSummary(new[] { MirrorRewardItemId });
            if (!GameStateHub.Instance.HasCollectedItem(MirrorRewardItemId) || collectionSummary.CollectedCount != 1 || collectionSummary.TotalCollectibleCount != 1)
            {
                Fail("Risk route should collect the mirror shard reward before exiting.");
                return;
            }
        }
        else
        {
            InventoryController.FloorCollectionSummary collectionSummary = InventoryController.GetCurrentFloorCollectionSummary(new[] { MirrorRewardItemId });
            if (GameStateHub.Instance.HasCollectedItem(MirrorRewardItemId) || collectionSummary.CollectedCount != 0 || collectionSummary.TotalCollectibleCount != 1)
            {
                Fail("Safe route should leave the mirror shard reward uncollected.");
                return;
            }
        }

        SessionState.SetInt(PhaseKey, (int)Phase.UseExit);
    }

    private static void AdvanceUseExitPhase()
    {
        if (!TryGetPlaythroughContext(out _, out PlayerMover player, out _, out _, out _, out AshParlorExitInteractable exitInteractable, out _, out _))
        {
            return;
        }

        SimpleDialogueUI.Instance?.Hide();
        exitInteractable.Interact(player);
        if (!FloorSummaryPanel.IsVisible || FloorSummaryPanel.Instance == null)
        {
            return;
        }

        SessionState.SetInt(PhaseKey, (int)Phase.VerifySummary);
    }

    private static void AdvanceVerifySummaryPhase()
    {
        if (!FloorSummaryPanel.IsVisible || FloorSummaryPanel.Instance == null)
        {
            return;
        }

        TMP_Text titleText = SummaryTitleField?.GetValue(FloorSummaryPanel.Instance) as TMP_Text;
        TMP_Text collectionText = SummaryCollectionField?.GetValue(FloorSummaryPanel.Instance) as TMP_Text;
        TMP_Text choiceText = SummaryChoiceField?.GetValue(FloorSummaryPanel.Instance) as TMP_Text;
        TMP_Text narrativeText = SummaryNarrativeField?.GetValue(FloorSummaryPanel.Instance) as TMP_Text;
        if (titleText == null || collectionText == null || choiceText == null || narrativeText == null)
        {
            Fail("Could not read Mirror Corridor summary texts.");
            return;
        }

        if (titleText.text != "—— 铜镜长廊 · 通过 ——")
        {
            Fail($"Unexpected Mirror Corridor summary title: {titleText.text}");
            return;
        }

        if (CurrentRoute == Route.Risk)
        {
            if (collectionText.text != "1 / 1"
                || choiceText.text != "选择了打碎铜镜"
                || narrativeText.text != "你打碎了镜子。碎片映出的不是过去——而是还没到来的未来。")
            {
                Fail("Risk route summary text did not match the Mirror Corridor design.");
                return;
            }
        }
        else
        {
            if (collectionText.text != "0 / 1"
                || choiceText.text != "选择了绕过铜镜"
                || narrativeText.text != "镜子完好无损。你也完好无损。在这种地方，完好无损从来不是免费的。")
            {
                Fail("Safe route summary text did not match the Mirror Corridor design.");
                return;
            }
        }

        Succeed();
    }

    private static bool TryGetPlaythroughContext(
        out MirrorCorridorRunController controller,
        out PlayerMover player,
        out AshParlorBrazierInteractable firstBrazier,
        out AshParlorBrazierInteractable secondBrazier,
        out AshParlorChoicePromptInteractable choicePrompt,
        out AshParlorExitInteractable exitInteractable,
        out PickupInteractable rewardPickup,
        out SimpleEnemyController finalEnemy)
    {
        controller = UnityEngine.Object.FindFirstObjectByType<MirrorCorridorRunController>();
        player = UnityEngine.Object.FindFirstObjectByType<PlayerMover>();
        firstBrazier = null;
        secondBrazier = null;
        choicePrompt = UnityEngine.Object.FindFirstObjectByType<AshParlorChoicePromptInteractable>();
        exitInteractable = UnityEngine.Object.FindFirstObjectByType<AshParlorExitInteractable>();
        rewardPickup = FindMirrorRewardPickup();
        finalEnemy = FindFinalEnemy();

        AshParlorBrazierInteractable[] braziers = UnityEngine.Object.FindObjectsByType<AshParlorBrazierInteractable>(FindObjectsSortMode.None);
        for (int i = 0; i < braziers.Length; i++)
        {
            AshParlorBrazierInteractable brazier = braziers[i];
            if (brazier == null)
            {
                continue;
            }

            if (brazier.BrazierIndex == 1)
            {
                firstBrazier = brazier;
            }
            else if (brazier.BrazierIndex == 2)
            {
                secondBrazier = brazier;
            }
        }

        return controller != null && player != null;
    }

    private static SimpleEnemyController[] FindPressureEnemies()
    {
        SimpleEnemyController[] allEnemies = UnityEngine.Object.FindObjectsByType<SimpleEnemyController>(FindObjectsSortMode.None);
        int count = 0;
        for (int i = 0; i < allEnemies.Length; i++)
        {
            if (allEnemies[i] != null
                && (allEnemies[i].gameObject.name == "Monster_MirrorEcho_A" || allEnemies[i].gameObject.name == "Monster_MirrorEcho_B"))
            {
                count++;
            }
        }

        SimpleEnemyController[] pressureEnemies = new SimpleEnemyController[count];
        int writeIndex = 0;
        for (int i = 0; i < allEnemies.Length; i++)
        {
            SimpleEnemyController enemy = allEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            if (enemy.gameObject.name == "Monster_MirrorEcho_A" || enemy.gameObject.name == "Monster_MirrorEcho_B")
            {
                pressureEnemies[writeIndex++] = enemy;
            }
        }

        return pressureEnemies;
    }

    private static SimpleEnemyController FindFinalEnemy()
    {
        SimpleEnemyController[] enemies = UnityEngine.Object.FindObjectsByType<SimpleEnemyController>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            SimpleEnemyController enemy = enemies[i];
            if (enemy != null && enemy.gameObject.name == "Monster_MirrorFinalEcho")
            {
                return enemy;
            }
        }

        return null;
    }

    private static PickupInteractable FindMirrorRewardPickup()
    {
        PickupInteractable[] pickups = UnityEngine.Object.FindObjectsByType<PickupInteractable>(FindObjectsSortMode.None);
        for (int i = 0; i < pickups.Length; i++)
        {
            PickupInteractable pickup = pickups[i];
            if (pickup != null && pickup.ItemId == MirrorRewardItemId)
            {
                return pickup;
            }
        }

        return null;
    }

    private static LightZoneEffect FindLightZone(string label)
    {
        LightZoneEffect[] zones = UnityEngine.Object.FindObjectsByType<LightZoneEffect>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            LightZoneEffect zone = zones[i];
            if (zone != null && zone.ZoneLabel == label)
            {
                return zone;
            }
        }

        return null;
    }

    private static AshParlorSealBarrier FindBarrier(string objectName)
    {
        AshParlorSealBarrier[] barriers = UnityEngine.Object.FindObjectsByType<AshParlorSealBarrier>(FindObjectsSortMode.None);
        for (int i = 0; i < barriers.Length; i++)
        {
            AshParlorSealBarrier barrier = barriers[i];
            if (barrier != null && barrier.gameObject.name == objectName)
            {
                return barrier;
            }
        }

        return null;
    }

    private static void MovePlayerToRoom(PlayerMover player, MirrorCorridorRunController controller, int roomIndex)
    {
        if (player == null || controller == null || RoomBoundsField == null || RoomBoundsRegisteredField == null)
        {
            return;
        }

        Vector2[] roomBounds = RoomBoundsField.GetValue(controller) as Vector2[];
        bool[] roomBoundsRegistered = RoomBoundsRegisteredField.GetValue(controller) as bool[];
        if (roomBounds == null || roomBoundsRegistered == null || roomIndex < 0 || roomIndex >= roomBounds.Length || !roomBoundsRegistered[roomIndex])
        {
            return;
        }

        Vector2 bounds = roomBounds[roomIndex];
        Vector3 position = player.transform.position;
        position.x = (bounds.x + bounds.y) * 0.5f;
        player.transform.position = position;
    }

    private static bool ValidateEncounterProfile(SimpleEnemyController enemy, float expectedMove, float expectedAttackRange, float expectedAttackCooldown, string label)
    {
        if (enemy == null || MoveSpeedMultiplierField == null || AttackRangeMultiplierField == null || AttackCooldownMultiplierField == null)
        {
            Fail($"{label} could not be inspected.");
            return false;
        }

        float move = (float)MoveSpeedMultiplierField.GetValue(enemy);
        float attackRange = (float)AttackRangeMultiplierField.GetValue(enemy);
        float attackCooldown = (float)AttackCooldownMultiplierField.GetValue(enemy);
        if (!Approximately(move, expectedMove)
            || !Approximately(attackRange, expectedAttackRange)
            || !Approximately(attackCooldown, expectedAttackCooldown))
        {
            Fail($"{label} profile mismatch. Got ({move:F2}, {attackRange:F2}, {attackCooldown:F2}).");
            return false;
        }

        return true;
    }

    private static bool HasActivePickupPresentation(PickupInteractable pickup)
    {
        if (pickup == null)
        {
            return false;
        }

        Collider[] colliders = pickup.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].enabled)
            {
                return true;
            }
        }

        Renderer[] renderers = pickup.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].enabled)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Approximately(float left, float right)
    {
        return Mathf.Abs(left - right) <= FloatTolerance;
    }

    private static Route CurrentRoute => (Route)SessionState.GetInt(RouteKey, (int)Route.Risk);

    private static void Succeed()
    {
        bool runInBatchMode = SessionState.GetBool(BatchModeKey, false);
        Route route = CurrentRoute;
        Cleanup();
        GameStateHub.SetCurrentFloorIndexRuntime(0);
        Debug.Log($"[MirrorCorridorPlaythroughCheck] Mirror Corridor {route} route playthrough passed.");
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        if (runInBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    private static void Fail(string message)
    {
        bool runInBatchMode = SessionState.GetBool(BatchModeKey, false);
        Cleanup();
        GameStateHub.SetCurrentFloorIndexRuntime(0);
        Debug.LogError("[MirrorCorridorPlaythroughCheck] " + message);
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }

        if (runInBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void Cleanup()
    {
        SessionState.EraseBool(RunningKey);
        SessionState.EraseBool(BatchModeKey);
        SessionState.EraseString(RunStartKey);
        SessionState.EraseString(PlayStartKey);
        SessionState.EraseInt(PhaseKey);
        SessionState.EraseInt(RouteKey);
        PlayerPrefs.DeleteKey(SkipInitialMenuRedirectKey);
        UnregisterCallbacks();
    }

    private static double ReadTime(string key)
    {
        string rawValue = SessionState.GetString(key, string.Empty);
        return double.TryParse(rawValue, out double value) ? value : -1d;
    }
}
