using UnityEngine;

public sealed class AshParlorChoicePromptInteractable : InteractableBase
{
    private FloorRunController _controller;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName == "可交互对象")
        {
            displayName = "抉择台";
        }

        if (string.IsNullOrWhiteSpace(promptText) || promptText == "Interact")
        {
            promptText = "做出选择";
        }
    }

    public void Configure(FloorRunController controller, string newDisplayName, string newPromptText)
    {
        _controller = controller;
        ConfigurePresentation(newDisplayName, newPromptText);
    }

    public override void Interact(PlayerMover player)
    {
        _controller?.TryOpenChoicePrompt(player);
    }
}
