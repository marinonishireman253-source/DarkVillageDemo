using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class FloorRunController : MonoBehaviour
{
    public readonly struct ChoiceOverlayConfig
    {
        public ChoiceOverlayConfig(
            string title,
            string body,
            string hint,
            string riskTitle,
            string riskDetail,
            string safeTitle,
            string safeDetail)
        {
            Title = string.IsNullOrWhiteSpace(title) ? "选择房分支" : title.Trim();
            Body = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();
            Hint = string.IsNullOrWhiteSpace(hint) ? string.Empty : hint.Trim();
            RiskTitle = string.IsNullOrWhiteSpace(riskTitle) ? "风险" : riskTitle.Trim();
            RiskDetail = string.IsNullOrWhiteSpace(riskDetail) ? string.Empty : riskDetail.Trim();
            SafeTitle = string.IsNullOrWhiteSpace(safeTitle) ? "保守" : safeTitle.Trim();
            SafeDetail = string.IsNullOrWhiteSpace(safeDetail) ? string.Empty : safeDetail.Trim();
        }

        public string Title { get; }
        public string Body { get; }
        public string Hint { get; }
        public string RiskTitle { get; }
        public string RiskDetail { get; }
        public string SafeTitle { get; }
        public string SafeDetail { get; }
    }

    public abstract string ExitUnlockedFlagId { get; }

    public virtual void RegisterRoomBounds(int roomIndex, float startX, float endX) { }
    public virtual void RegisterRoomLightZone(int roomIndex, LightZoneEffect lightZone) { }
    public virtual void RegisterFirstBrazier(AshParlorBrazierInteractable brazier) { }
    public virtual void RegisterSecondBrazier(AshParlorBrazierInteractable brazier) { }
    public virtual void RegisterChoicePair(AshParlorChoiceInteractable riskyChoice, AshParlorChoiceInteractable safeChoice, AshParlorChoicePromptInteractable choicePrompt, Transform choiceAnchor) { }
    public virtual void RegisterExit(AshParlorExitInteractable exitInteractable) { }
    public virtual void RegisterPressureSeal(AshParlorSealBarrier barrier) { }
    public virtual void RegisterFinaleSeal(AshParlorSealBarrier barrier) { }
    public virtual void RegisterPressureEnemy(SimpleEnemyController enemy) { }
    public virtual void RegisterPressureRoomLights(Light[] lights) { }
    public virtual void RegisterFinalEnemy(SimpleEnemyController enemy) { }
    public virtual void RegisterRiskBonusEnemy(SimpleEnemyController enemy) { }
    public virtual void RegisterRiskRewardPickup(PickupInteractable pickup) { }

    public abstract bool TryLightBrazier(AshParlorBrazierInteractable brazier, PlayerMover player);
    public abstract void TryOpenChoicePrompt(PlayerMover player);
    public abstract bool TryResolveChoice(AshParlorChoiceInteractable.ChoiceKind choiceKind, PlayerMover player);
    public abstract void TryUseExit(PlayerMover player);
    public abstract ChoiceOverlayConfig GetChoiceOverlayConfig();

    protected static void ShowLines(string speaker, params string[] lines)
    {
        if (SimpleDialogueUI.Instance == null)
        {
            return;
        }

        SimpleDialogueUI.Instance.Show(speaker, lines);
    }

    protected FloorSummaryPanel.SummaryData BuildSummaryData(
        string title,
        IEnumerable<string> floorItemIds,
        string riskChoiceLabel,
        string safeChoiceLabel,
        string riskNarrative,
        string safeNarrative,
        string continueLabel = "继续")
    {
        InventoryController.FloorCollectionSummary collectionSummary = GameStateHub.Instance != null
            ? GameStateHub.Instance.GetFloorCollectionSummary(floorItemIds)
            : InventoryController.GetCurrentFloorCollectionSummary(floorItemIds);
        ChapterState.ChoiceResult choiceResult = GameStateHub.Instance != null
            ? GameStateHub.Instance.CurrentChoiceResult
            : ChapterState.ChoiceResult.None;

        string choiceLine;
        string narrativeLine;
        switch (choiceResult)
        {
            case ChapterState.ChoiceResult.Risk:
                choiceLine = riskChoiceLabel;
                narrativeLine = riskNarrative;
                break;
            case ChapterState.ChoiceResult.Safe:
                choiceLine = safeChoiceLabel;
                narrativeLine = safeNarrative;
                break;
            default:
                choiceLine = "尚未留下明确的选择记录";
                narrativeLine = "这一层已经结束，但它留下的重量还没有名字。";
                break;
        }

        return new FloorSummaryPanel.SummaryData(
            title,
            $"{collectionSummary.CollectedCount} / {collectionSummary.TotalCollectibleCount}",
            choiceLine,
            narrativeLine,
            continueLabel);
    }

    protected void ShowFloorSummary(FloorSummaryPanel.SummaryData summary, Action onContinue)
    {
        if (UiBootstrap.TryGetFloorSummaryView(out FloorSummaryPanel summaryPanel))
        {
            Time.timeScale = 0f;
            summaryPanel.Show(summary, onContinue);
            return;
        }

        onContinue?.Invoke();
    }

    protected void ContinueToFloor(int floorIndex)
    {
        Time.timeScale = 1f;
        GameStateHub.SetCurrentFloorIndexRuntime(floorIndex);
        GameStateHub.Instance?.ResetRuntimeState();
        DialogueEventSystem.ClearFlags();
        SceneLoader.ReloadCurrent();
    }
}
