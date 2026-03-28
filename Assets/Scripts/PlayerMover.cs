using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4.2f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float acceleration = 22f;
    [SerializeField] private float deceleration = 20f;
    [SerializeField] private float turnSpeed = 14f;
    [SerializeField] private bool moveRelativeToCamera = true;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string sprintActionName = "Sprint";
    [SerializeField] private string interactActionName = "Interact";

    [Header("Interaction")]
    [SerializeField] private Vector3 interactionOffset = new Vector3(0f, 0.9f, 0.6f);
    [SerializeField] private float interactionRadius = 1.2f;
    [SerializeField] private LayerMask interactionMask = ~0;

    public Vector3 Velocity => _currentVelocity;
    public bool HasInteractableTarget => _currentInteractable != null;
    public IInteractable CurrentInteractable => _currentInteractable;
    public bool IsSprintActive => !SimpleDialogueUI.IsOpen && IsSprinting() && _moveInput.sqrMagnitude > 0.0001f;

    private InputAction _moveAction;
    private InputAction _sprintAction;
    private InputAction _interactAction;

    private Vector2 _moveInput;
    private Vector3 _currentVelocity;
    private IInteractable _currentInteractable;
    private bool _usingFallbackInput;
    private bool _fallbackInteractRequested;

    private void OnEnable()
    {
        BindInput();
        EnableActions();
    }

    private void OnDisable()
    {
        DisableActions();
        UnbindInput();
    }

    private void Update()
    {
        RefreshMoveInput();
        UpdateMovement(Time.deltaTime);
        UpdateInteractionTarget();
        ProcessActionInteraction();
        ProcessFallbackInteraction();
    }

    private void BindInput()
    {
        _usingFallbackInput = inputActions == null;

        if (inputActions == null)
        {
            return;
        }

        var actionMap = inputActions.FindActionMap(actionMapName, false);
        if (actionMap == null)
        {
            _usingFallbackInput = true;
            Debug.LogWarning($"{nameof(PlayerMover)} could not find action map '{actionMapName}'. Falling back to keyboard polling.", this);
            return;
        }

        _moveAction = actionMap.FindAction(moveActionName, false);
        _sprintAction = actionMap.FindAction(sprintActionName, false);
        _interactAction = actionMap.FindAction(interactActionName, false);

        if (_moveAction == null)
        {
            _usingFallbackInput = true;
            Debug.LogWarning($"{nameof(PlayerMover)} could not find move action '{moveActionName}'. Falling back to keyboard polling.", this);
            return;
        }

        if (_moveAction != null)
        {
            _moveAction.performed += OnMovePerformed;
            _moveAction.canceled += OnMoveCanceled;
        }

    }

    private void UnbindInput()
    {
        if (_moveAction != null)
        {
            _moveAction.performed -= OnMovePerformed;
            _moveAction.canceled -= OnMoveCanceled;
        }

        _moveAction = null;
        _sprintAction = null;
        _interactAction = null;
    }

    private void EnableActions()
    {
        _moveAction?.Enable();
        _sprintAction?.Enable();
        _interactAction?.Enable();
    }

    private void DisableActions()
    {
        _moveAction?.Disable();
        _sprintAction?.Disable();
        _interactAction?.Disable();
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        _moveInput = Vector2.zero;
    }

    private void UpdateMovement(float deltaTime)
    {
        if (SimpleDialogueUI.IsOpen)
        {
            _currentVelocity = Vector3.zero;
            return;
        }

        Vector3 desiredDirection = GetDesiredDirection();

        float targetSpeed = IsSprinting() ? sprintSpeed : walkSpeed;
        Vector3 targetVelocity = desiredDirection * targetSpeed;

        float rate = desiredDirection.sqrMagnitude > 0.0001f ? acceleration : deceleration;
        _currentVelocity = Vector3.MoveTowards(_currentVelocity, targetVelocity, rate * deltaTime);
        transform.position += _currentVelocity * deltaTime;

        Vector3 planarVelocity = new Vector3(_currentVelocity.x, 0f, _currentVelocity.z);
        if (planarVelocity.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(planarVelocity.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * deltaTime);
        }
    }

    private bool IsSprinting()
    {
        if (_sprintAction != null)
        {
            return _sprintAction.IsPressed();
        }

        return _usingFallbackInput && Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
    }

    private Vector3 GetDesiredDirection()
    {
        Vector3 inputDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);
        if (inputDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        if (!moveRelativeToCamera || Camera.main == null)
        {
            return Vector3.ClampMagnitude(inputDirection, 1f);
        }

        Vector3 cameraForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up);
        Vector3 cameraRight = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up);

        if (cameraForward.sqrMagnitude <= 0.0001f || cameraRight.sqrMagnitude <= 0.0001f)
        {
            return Vector3.ClampMagnitude(inputDirection, 1f);
        }

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 worldDirection = cameraRight * _moveInput.x + cameraForward * _moveInput.y;
        return Vector3.ClampMagnitude(worldDirection, 1f);
    }

    private void UpdateInteractionTarget()
    {
        Vector3 origin = transform.position + transform.rotation * interactionOffset;
        Collider[] hits = Physics.OverlapSphere(origin, interactionRadius, interactionMask, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestScore = float.NegativeInfinity;

        foreach (Collider hit in hits)
        {
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                continue;
            }

            if (interactable is Object unityObject && !unityObject)
            {
                continue;
            }

            Vector3 toTarget = hit.bounds.center - transform.position;
            float distanceScore = -toTarget.sqrMagnitude;
            float facingScore = Vector3.Dot(transform.forward, toTarget.normalized) * 2f;
            float score = distanceScore + facingScore;

            if (score > bestScore)
            {
                bestScore = score;
                best = interactable;
            }
        }

        if (!ReferenceEquals(best, _currentInteractable))
        {
            _currentInteractable?.OnFocusLost(this);
            _currentInteractable = best;
            _currentInteractable?.OnFocusGained(this);
        }
    }

    private void RefreshMoveInput()
    {
        if (_moveAction != null && _moveAction.enabled)
        {
            _moveInput = Vector2.ClampMagnitude(_moveAction.ReadValue<Vector2>(), 1f);
            return;
        }

        Vector2 fallbackMove = Vector2.zero;
        bool hasFallbackInput = false;

        if (Gamepad.current != null)
        {
            fallbackMove = Gamepad.current.leftStick.ReadValue();
            hasFallbackInput = fallbackMove.sqrMagnitude > 0.0001f;
        }

        if (!hasFallbackInput && Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                fallbackMove.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                fallbackMove.x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                fallbackMove.y -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                fallbackMove.y += 1f;

            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                _fallbackInteractRequested = true;
            }
        }

        _moveInput = Vector2.ClampMagnitude(fallbackMove, 1f);
    }

    private void ProcessActionInteraction()
    {
        if (_interactAction == null || !_interactAction.enabled)
        {
            return;
        }

        if (_interactAction.WasPressedThisFrame())
        {
            _currentInteractable?.Interact(this);
        }
    }

    private void ProcessFallbackInteraction()
    {
        if (!_fallbackInteractRequested)
        {
            return;
        }

        _fallbackInteractRequested = false;
        _currentInteractable?.Interact(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = HasInteractableTarget ? Color.green : Color.cyan;
        Vector3 origin = transform.position + transform.rotation * interactionOffset;
        Gizmos.DrawWireSphere(origin, interactionRadius);
    }
}
