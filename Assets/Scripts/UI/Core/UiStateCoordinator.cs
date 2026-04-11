using System;
using UnityEngine;

public sealed class UiStateCoordinator : MonoBehaviour
{
    public enum UiMode
    {
        Exploration,
        InteractionFocus,
        Dialogue,
        Combat,
        ChapterComplete,
        Loading,
        Paused
    }

    public static UiStateCoordinator Instance { get; private set; }

    public UiMode CurrentMode { get; private set; } = UiMode.Exploration;

    public event Action<UiMode> OnModeChanged;

    private PlayerMover _player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerMover>();
        }

        UiMode nextMode = ResolveMode();
        if (nextMode == CurrentMode)
        {
            return;
        }

        CurrentMode = nextMode;
        OnModeChanged?.Invoke(CurrentMode);
    }

    private UiMode ResolveMode()
    {
        if (ChapterCompleteOverlay.IsVisible)
        {
            return UiMode.ChapterComplete;
        }

        if (SimpleDialogueUI.IsOpen || DialogueRunner.IsActive)
        {
            return UiMode.Dialogue;
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
}
