using UnityEngine;

public class DoorInteractable : InteractableBase
{
    [SerializeField] private Transform doorVisual;
    [SerializeField] private Vector3 closedEulerAngles;
    [SerializeField] private Vector3 openedEulerAngles = new Vector3(0f, 90f, 0f);
    [SerializeField] private bool startsOpened;
    [SerializeField] private bool lockAfterOpen;

    private bool _isOpen;
    private bool _isLocked;

    private void Awake()
    {
        if (doorVisual == null)
        {
            doorVisual = transform;
        }

        if (string.IsNullOrWhiteSpace(displayName) || displayName == "可交互对象")
        {
            displayName = "木门";
        }

        _isOpen = startsOpened;
        RefreshPrompt();
        ApplyVisual();
    }

    public override void Interact(PlayerMover player)
    {
        if (_isLocked)
        {
            return;
        }

        _isOpen = !_isOpen;
        ApplyVisual();
        RefreshPrompt();

        if (_isOpen && lockAfterOpen)
        {
            _isLocked = true;
            promptText = "已开启";
        }

        if (TryGetComponent(out QuestObjectiveTarget objectiveTarget))
        {
            objectiveTarget.NotifyInteracted();
        }
    }

    public void SetDoorVisual(Transform target)
    {
        doorVisual = target;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (doorVisual == null)
        {
            return;
        }

        doorVisual.localEulerAngles = _isOpen ? openedEulerAngles : closedEulerAngles;
    }

    private void RefreshPrompt()
    {
        if (_isLocked)
        {
            promptText = "已开启";
            return;
        }

        promptText = _isOpen ? "关闭" : "开启";
    }
}
