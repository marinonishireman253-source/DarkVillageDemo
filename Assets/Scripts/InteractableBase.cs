using UnityEngine;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [SerializeField] protected string promptText = "Interact";
    [SerializeField] protected string displayName = "可交互对象";

    public string PromptText => string.IsNullOrWhiteSpace(promptText) ? "交互" : promptText.Trim();
    public virtual string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName.Trim();

    public abstract void Interact(PlayerMover player);

    public virtual void OnFocusGained(PlayerMover player)
    {
    }

    public virtual void OnFocusLost(PlayerMover player)
    {
    }
}
