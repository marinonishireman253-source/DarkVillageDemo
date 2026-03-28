public interface IInteractable
{
    string PromptText { get; }
    string DisplayName { get; }

    void Interact(PlayerMover player);
    void OnFocusGained(PlayerMover player);
    void OnFocusLost(PlayerMover player);
}
