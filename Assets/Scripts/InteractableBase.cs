using UnityEngine;

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [SerializeField] protected string promptText = "Interact";
    [SerializeField] protected string displayName = "可交互对象";
    [SerializeField] private InteractableFocusVisual focusVisual;

    public string PromptText => string.IsNullOrWhiteSpace(promptText) ? "交互" : promptText.Trim();
    public virtual string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName.Trim();

    public void ConfigurePresentation(string newDisplayName, string newPromptText)
    {
        if (!string.IsNullOrWhiteSpace(newDisplayName))
        {
            displayName = newDisplayName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(newPromptText))
        {
            promptText = newPromptText.Trim();
        }
    }

    public abstract void Interact(PlayerMover player);

    public virtual void OnFocusGained(PlayerMover player)
    {
        GetOrCreateFocusVisual().SetFocused(true);
    }

    public virtual void OnFocusLost(PlayerMover player)
    {
        if (focusVisual != null)
        {
            focusVisual.SetFocused(false);
        }
    }

    private InteractableFocusVisual GetOrCreateFocusVisual()
    {
        if (focusVisual != null)
        {
            return focusVisual;
        }

        focusVisual = GetComponent<InteractableFocusVisual>();
        if (focusVisual == null)
        {
            focusVisual = gameObject.AddComponent<InteractableFocusVisual>();
        }

        return focusVisual;
    }
}
