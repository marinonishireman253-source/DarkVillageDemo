using System;
using UnityEngine;

public sealed class UiStateCoordinator : MonoBehaviour
{
    public enum UiMode
    {
        Exploration,
        InteractionFocus,
        Dialogue,
        Inventory,
        Combat,
        ChapterComplete,
        Loading,
        Paused
    }

    public static UiStateCoordinator Instance { get; private set; }
    public static event Action<UiStateCoordinator> OnInstanceChanged;

    public UiMode CurrentMode { get; private set; } = UiMode.Exploration;
    public bool BlocksPlayerMovement => BlocksPlayerMovementForMode(CurrentMode);
    public bool BlocksPlayerActions => BlocksPlayerActionsForMode(CurrentMode);
    public bool BlocksPlayerInteraction => BlocksPlayerInteractionForMode(CurrentMode);
    public bool PausesEnemyBehavior => PausesEnemyBehaviorForMode(CurrentMode);
    public bool AllowsInteractionPrompt => AllowsInteractionPromptForMode(CurrentMode);

    public event Action<UiMode> OnModeChanged;

    private PlayerMover _player;
    private bool _isDialogueOpen;
    private bool _isInventoryOpen;
    private bool _isFloorSummaryVisible;
    private bool _isChoiceOverlayVisible;
    private bool _isChapterCompleteVisible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        OnInstanceChanged?.Invoke(this);
    }

    private void OnEnable()
    {
        PlayerMover.OnLocalInstanceChanged += HandlePlayerChanged;
        SimpleDialogueUI.OnOpenStateChanged += HandleDialogueOpenStateChanged;
        InventoryController.OnOpenStateChanged += HandleInventoryOpenStateChanged;
        FloorSummaryPanel.OnVisibilityChanged += HandleFloorSummaryVisibilityChanged;
        AshParlorChoiceOverlay.OnVisibilityChanged += HandleChoiceOverlayVisibilityChanged;
        ChapterCompleteOverlay.OnVisibilityChanged += HandleChapterCompleteVisibilityChanged;
        HandlePlayerChanged(PlayerMover.LocalInstance);
        HandleDialogueOpenStateChanged(SimpleDialogueUI.IsOpen || DialogueRunner.IsActive);
        HandleInventoryOpenStateChanged(InventoryController.IsOpen);
        HandleFloorSummaryVisibilityChanged(FloorSummaryPanel.IsVisible);
        HandleChoiceOverlayVisibilityChanged(AshParlorChoiceOverlay.IsVisible);
        HandleChapterCompleteVisibilityChanged(ChapterCompleteOverlay.IsVisible);
    }

    private void OnDisable()
    {
        PlayerMover.OnLocalInstanceChanged -= HandlePlayerChanged;
        SimpleDialogueUI.OnOpenStateChanged -= HandleDialogueOpenStateChanged;
        InventoryController.OnOpenStateChanged -= HandleInventoryOpenStateChanged;
        FloorSummaryPanel.OnVisibilityChanged -= HandleFloorSummaryVisibilityChanged;
        AshParlorChoiceOverlay.OnVisibilityChanged -= HandleChoiceOverlayVisibilityChanged;
        ChapterCompleteOverlay.OnVisibilityChanged -= HandleChapterCompleteVisibilityChanged;
        _player = null;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            OnInstanceChanged?.Invoke(null);
        }
    }

    private void Update()
    {
        UiMode nextMode = ResolveMode();
        if (nextMode == CurrentMode)
        {
            return;
        }

        CurrentMode = nextMode;
        OnModeChanged?.Invoke(CurrentMode);
    }

    private void HandlePlayerChanged(PlayerMover player)
    {
        _player = player;
    }

    private void HandleDialogueOpenStateChanged(bool isOpen)
    {
        _isDialogueOpen = isOpen;
    }

    private void HandleInventoryOpenStateChanged(bool isOpen)
    {
        _isInventoryOpen = isOpen;
    }

    private void HandleFloorSummaryVisibilityChanged(bool isVisible)
    {
        _isFloorSummaryVisible = isVisible;
    }

    private void HandleChoiceOverlayVisibilityChanged(bool isVisible)
    {
        _isChoiceOverlayVisible = isVisible;
    }

    private void HandleChapterCompleteVisibilityChanged(bool isVisible)
    {
        _isChapterCompleteVisible = isVisible;
    }

    private UiMode ResolveMode()
    {
        if (_isChapterCompleteVisible)
        {
            return UiMode.ChapterComplete;
        }

        if (_isFloorSummaryVisible)
        {
            return UiMode.Paused;
        }

        if (_isChoiceOverlayVisible)
        {
            return UiMode.Paused;
        }

        if (_isDialogueOpen || DialogueRunner.IsActive)
        {
            return UiMode.Dialogue;
        }

        if (_isInventoryOpen)
        {
            return UiMode.Inventory;
        }

        if (CombatEncounterTrigger.ActiveEncounter != null)
        {
            return UiMode.Combat;
        }

        if (_player != null && _player.HasInteractableTarget)
        {
            return UiMode.InteractionFocus;
        }

        return UiMode.Exploration;
    }

    public static bool BlocksPlayerMovementForMode(UiMode mode)
    {
        return mode == UiMode.Dialogue
            || mode == UiMode.Inventory
            || mode == UiMode.ChapterComplete
            || mode == UiMode.Loading
            || mode == UiMode.Paused;
    }

    public static bool BlocksPlayerActionsForMode(UiMode mode)
    {
        return BlocksPlayerMovementForMode(mode);
    }

    public static bool BlocksPlayerInteractionForMode(UiMode mode)
    {
        return BlocksPlayerMovementForMode(mode);
    }

    public static bool PausesEnemyBehaviorForMode(UiMode mode)
    {
        return mode == UiMode.Dialogue
            || mode == UiMode.Inventory
            || mode == UiMode.ChapterComplete
            || mode == UiMode.Loading
            || mode == UiMode.Paused;
    }

    public static bool AllowsInteractionPromptForMode(UiMode mode)
    {
        return !BlocksPlayerInteractionForMode(mode);
    }

    public static bool AllowsInventoryForMode(UiMode mode)
    {
        return mode == UiMode.Exploration
            || mode == UiMode.InteractionFocus
            || mode == UiMode.Combat;
    }
}
