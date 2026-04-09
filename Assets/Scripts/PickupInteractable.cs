using UnityEngine;

public class PickupInteractable : InteractableBase
{
    [SerializeField] private string itemId = "old_token";
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private bool disableRendererOnPickup = true;
    [SerializeField] private float interactionTriggerRadius = 1.25f;

    private bool _picked;

    public bool IsCollected => _picked || ChapterState.HasItem(itemId);

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

        EnsureInteractionTrigger();
        RefreshCollectedState();
    }

    public void Configure(string newItemId, string newDisplayName, string newPromptText, bool shouldDestroyOnPickup)
    {
        if (!string.IsNullOrWhiteSpace(newItemId))
        {
            itemId = newItemId.Trim();
        }

        ConfigurePresentation(newDisplayName, newPromptText);
        destroyOnPickup = shouldDestroyOnPickup;
        RefreshCollectedState();
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
        ChapterState.CollectItem(itemId);

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        RefreshCollectedState();
    }

    private void EnsureInteractionTrigger()
    {
        SphereCollider[] colliders = GetComponents<SphereCollider>();
        foreach (SphereCollider collider in colliders)
        {
            if (collider != null && collider.isTrigger && collider.radius >= interactionTriggerRadius - 0.01f)
            {
                return;
            }
        }

        SphereCollider triggerCollider = gameObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = interactionTriggerRadius;
        triggerCollider.center = Vector3.zero;
    }

    private void RefreshCollectedState()
    {
        if (!ChapterState.HasItem(itemId))
        {
            return;
        }

        _picked = true;
        promptText = "已拾取";

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
    }
}
