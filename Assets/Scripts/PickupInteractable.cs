using UnityEngine;

public class PickupInteractable : InteractableBase
{
    [SerializeField] private string itemId = "old_token";
    [SerializeField] private string inventoryCategory = "遗物";
    [SerializeField] private Sprite inventoryIcon;
    [SerializeField] private WeaponData weaponData;
    [TextArea(2, 4)]
    [SerializeField] private string inventoryDescription;
    [SerializeField] private string pickupSpeaker = "伊尔萨恩";
    [SerializeField] [TextArea(2, 4)] private string[] pickupLines;
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private bool disableRendererOnPickup = true;
    [SerializeField] private float interactionTriggerRadius = 1.25f;

    private bool _picked;
    private bool _pickupEnabled = true;

    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
    public bool IsCollected => _picked || (GameStateHub.Instance != null && GameStateHub.Instance.HasCollectedItem(itemId));

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
        RegisterInventoryDefinition();
        RefreshCollectedState();
    }

    public void Configure(
        string newItemId,
        string newDisplayName,
        string newPromptText,
        bool shouldDestroyOnPickup,
        string newInventoryCategory = null,
        string newInventoryDescription = null,
        string newPickupSpeaker = null,
        string[] newPickupLines = null,
        Sprite newInventoryIcon = null,
        WeaponData newWeaponData = null)
    {
        if (!string.IsNullOrWhiteSpace(newItemId))
        {
            itemId = newItemId.Trim();
        }

        ConfigurePresentation(newDisplayName, newPromptText);
        destroyOnPickup = shouldDestroyOnPickup;

        if (!string.IsNullOrWhiteSpace(newInventoryCategory))
        {
            inventoryCategory = newInventoryCategory.Trim();
        }

        if (!string.IsNullOrWhiteSpace(newInventoryDescription))
        {
            inventoryDescription = newInventoryDescription.Trim();
        }

        if (!string.IsNullOrWhiteSpace(newPickupSpeaker))
        {
            pickupSpeaker = newPickupSpeaker.Trim();
        }

        if (newInventoryIcon != null)
        {
            inventoryIcon = newInventoryIcon;
        }

        if (newWeaponData != null)
        {
            weaponData = newWeaponData;
        }

        if (newPickupLines != null && newPickupLines.Length > 0)
        {
            pickupLines = newPickupLines;
        }

        RegisterInventoryDefinition();
        RefreshCollectedState();
    }

    public override void Interact(PlayerMover player)
    {
        if (_picked || !_pickupEnabled)
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
        GameStateHub.Instance?.CollectItem(itemId);

        if (pickupLines != null && pickupLines.Length > 0 && !SimpleDialogueUI.IsOpen)
        {
            SimpleDialogueUI.Instance?.Show(string.IsNullOrWhiteSpace(pickupSpeaker) ? "伊尔萨恩" : pickupSpeaker, pickupLines);
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }

    public void SetPickupEnabled(bool enabled)
    {
        _pickupEnabled = enabled;
        ApplyCollectedState();
    }

    public void RefreshFromRuntimeState()
    {
        RefreshCollectedState();
    }

    private void Start()
    {
        RegisterInventoryDefinition();
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
        _picked = GameStateHub.Instance != null && GameStateHub.Instance.HasCollectedItem(itemId);
        if (_picked)
        {
            promptText = "已拾取";
        }

        ApplyCollectedState();
    }

    private void RegisterInventoryDefinition()
    {
        InventoryItemCatalog.RegisterDefinition(
            itemId,
            DisplayName,
            inventoryDescription,
            inventoryCategory,
            inventoryIcon != null ? inventoryIcon : weaponData != null ? weaponData.Icon : null,
            weaponData);
    }

    private void ApplyCollectedState()
    {
        bool shouldShowVisual = !_picked && _pickupEnabled;
        bool shouldEnableColliders = !_picked && _pickupEnabled;

        if (disableRendererOnPickup)
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = shouldShowVisual;
            }
        }

        foreach (Light lightComponent in GetComponentsInChildren<Light>())
        {
            lightComponent.enabled = shouldShowVisual;
        }

        foreach (Collider collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = shouldEnableColliders;
        }
    }
}
