using UnityEngine;

public class PickupInteractable : InteractableBase
{
    [SerializeField] private string itemId = "old_token";
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private bool disableRendererOnPickup = true;

    private bool _picked;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(displayName) || displayName == "可交互对象")
        {
            displayName = "旧徽记";
        }

        if (string.IsNullOrWhiteSpace(promptText) || promptText == "Interact")
        {
            promptText = "拾取";
        }
    }

    public override void Interact(PlayerMover player)
    {
        if (_picked)
        {
            return;
        }

        _picked = true;
        promptText = "已拾取";

        if (TryGetComponent(out QuestObjectiveTarget objectiveTarget))
        {
            objectiveTarget.NotifyInteracted();
        }

        if (disableRendererOnPickup)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = false;
            }
        }

        foreach (Collider collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        Debug.Log($"[PickupInteractable] Picked item: {itemId}");

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }
}
