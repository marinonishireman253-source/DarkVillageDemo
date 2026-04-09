using System.Collections;
using UnityEngine;

public class DoorInteractable : InteractableBase
{
    [SerializeField] private Transform doorVisual;
    [SerializeField] private Vector3 closedEulerAngles;
    [SerializeField] private Vector3 openedEulerAngles = new Vector3(0f, 90f, 0f);
    [SerializeField] private bool startsOpened;
    [SerializeField] private bool lockAfterOpen;
    [SerializeField] private float openDuration = 0.18f;

    private bool _isOpen;
    private bool _isLocked;
    private Coroutine _doorAnimation;

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

        bool targetOpen = lockAfterOpen ? true : !_isOpen;
        if (targetOpen == _isOpen)
        {
            return;
        }

        SetDoorState(targetOpen);

        if (_isOpen && lockAfterOpen)
        {
            _isLocked = true;
        }

        RefreshPrompt();

        if (_isOpen && TryGetComponent(out QuestObjectiveTarget objectiveTarget))
        {
            objectiveTarget.NotifyInteracted();
        }
    }

    public void SetDoorVisual(Transform target)
    {
        doorVisual = target;
        ApplyVisual();
    }

    public void ConfigureMotion(Vector3 openedAngles, bool shouldLockAfterOpen)
    {
        openedEulerAngles = openedAngles;
        lockAfterOpen = shouldLockAfterOpen;
        RefreshPrompt();
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

    private void SetDoorState(bool isOpen)
    {
        _isOpen = isOpen;

        if (doorVisual == null)
        {
            return;
        }

        if (_doorAnimation != null)
        {
            StopCoroutine(_doorAnimation);
        }

        if (!isActiveAndEnabled || openDuration <= 0.01f)
        {
            ApplyVisual();
            return;
        }

        Vector3 targetEulerAngles = _isOpen ? openedEulerAngles : closedEulerAngles;
        _doorAnimation = StartCoroutine(AnimateDoor(targetEulerAngles));
    }

    private IEnumerator AnimateDoor(Vector3 targetEulerAngles)
    {
        Quaternion startRotation = doorVisual.localRotation;
        Quaternion targetRotation = Quaternion.Euler(targetEulerAngles);
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            doorVisual.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        doorVisual.localRotation = targetRotation;
        _doorAnimation = null;
    }

    private void RefreshPrompt()
    {
        if (_isLocked)
        {
            promptText = "通过";
            return;
        }

        promptText = _isOpen ? "关闭" : "推门";
    }
}
