using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerJumpMotor))]
public class PlayerMover : MonoBehaviour
{
    public enum MovementMode
    {
        GroundPlaneXZ,
        SideScrollerX
    }

    public static PlayerMover LocalInstance { get; private set; }
    public static event Action<PlayerMover> OnLocalInstanceChanged;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4.2f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float acceleration = 22f;
    [SerializeField] private float deceleration = 20f;
    [SerializeField] private float turnSpeed = 14f;
    [SerializeField] private float externalVelocityDecay = 18f;
    [SerializeField] private bool moveRelativeToCamera = true;
    [SerializeField] private MovementMode movementMode = MovementMode.GroundPlaneXZ;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string sprintActionName = "Sprint";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string interactActionName = "Interact";

    [Header("Interaction")]
    [SerializeField] private Vector3 interactionOffset = new Vector3(0f, 0.9f, 0.6f);
    [SerializeField] private float interactionRadius = 1.2f;
    [SerializeField] private LayerMask interactionMask = ~0;

    public Vector3 Velocity => _currentVelocity;
    public Vector3 ExternalVelocity => _externalVelocity;
    public bool IsGrounded => _jumpMotor == null || _jumpMotor.IsGrounded;
    public bool HasInteractableTarget => _currentInteractable != null;
    public IInteractable CurrentInteractable => _currentInteractable;
    public bool IsSprintActive => !UiStateCoordinator.BlocksPlayerMovementForMode(_currentUiMode) && IsSprinting() && _moveInput.sqrMagnitude > 0.0001f;
    public bool IsControlLocked => Time.time < _controlLockedUntil;
    public float VerticalVelocity => _jumpMotor != null ? _jumpMotor.VerticalVelocity : 0f;

    public event Action<PlayerMover, IInteractable> OnInteractableTargetChanged;

    private InputAction _moveAction;
    private InputAction _sprintAction;
    private InputAction _jumpAction;
    private InputAction _interactAction;

    private Vector2 _moveInput;
    private Vector3 _currentVelocity;
    private IInteractable _currentInteractable;
    private bool _usingFallbackInput;
    private bool _fallbackInteractRequested;
    private float _sideScrollDepth;
    private Vector2 _sideScrollHorizontalRange;
    private bool _useSideScrollHorizontalRange;
    private Vector2 _groundPlaneXRange;
    private Vector2 _groundPlaneZRange;
    private bool _useGroundPlaneBounds;
    private Vector3 _externalVelocity;
    private float _controlLockedUntil;
    private float _speedMultiplier = 1f;
    private float _lightZoneSprintMultiplier = 1f;
    private float _lightZoneInteractionMultiplier = 1f;
    private UiStateCoordinator _stateCoordinator;
    private UiStateCoordinator.UiMode _currentUiMode = UiStateCoordinator.UiMode.Exploration;
    private PlayerJumpMotor _jumpMotor;

    private void Awake()
    {
        _jumpMotor = GetComponent<PlayerJumpMotor>();
        if (_jumpMotor == null)
        {
            _jumpMotor = gameObject.AddComponent<PlayerJumpMotor>();
        }

        LocalInstance = this;
        OnLocalInstanceChanged?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (LocalInstance == this)
        {
            LocalInstance = null;
            OnLocalInstanceChanged?.Invoke(null);
        }
    }

    public void ConfigureGroundPlane()
    {
        movementMode = MovementMode.GroundPlaneXZ;
        moveRelativeToCamera = true;
        _useSideScrollHorizontalRange = false;
        _useGroundPlaneBounds = false;
        _currentVelocity = Vector3.zero;
        _externalVelocity = Vector3.zero;
        _jumpMotor?.SyncToPosition(transform.position);
    }

    public void ConfigureGroundPlane(Vector2 xRange, Vector2 zRange, bool useCameraRelativeMovement)
    {
        movementMode = MovementMode.GroundPlaneXZ;
        moveRelativeToCamera = useCameraRelativeMovement;
        _groundPlaneXRange = xRange;
        _groundPlaneZRange = zRange;
        _useGroundPlaneBounds = xRange.x < xRange.y && zRange.x < zRange.y;
        _useSideScrollHorizontalRange = false;
        _currentVelocity = Vector3.zero;
        _externalVelocity = Vector3.zero;
        _jumpMotor?.SyncToPosition(transform.position);
    }

    public void ConfigureSideScroller(float lockedDepth, Vector2 horizontalRange)
    {
        movementMode = MovementMode.SideScrollerX;
        moveRelativeToCamera = false;
        _sideScrollDepth = lockedDepth;
        _sideScrollHorizontalRange = horizontalRange;
        _useSideScrollHorizontalRange = horizontalRange.x < horizontalRange.y;
        _useGroundPlaneBounds = false;
        _currentVelocity = Vector3.zero;
        _externalVelocity = Vector3.zero;

        Vector3 position = transform.position;
        position.z = lockedDepth;
        transform.position = position;
        _jumpMotor?.SyncToPosition(transform.position);
    }

    public void AddImpulse(Vector3 impulse)
    {
        if (movementMode == MovementMode.SideScrollerX)
        {
            _externalVelocity.x += impulse.x;
            return;
        }

        _externalVelocity += new Vector3(impulse.x, 0f, impulse.z);
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Clamp(multiplier, 0.1f, 2f);
    }

    public void ResetSpeedMultiplier()
    {
        _speedMultiplier = 1f;
    }

    public void SetLightZoneSprintMultiplier(float multiplier)
    {
        _lightZoneSprintMultiplier = Mathf.Clamp(multiplier, 0.25f, 2f);
    }

    public void SetLightZoneInteractionMultiplier(float multiplier)
    {
        _lightZoneInteractionMultiplier = Mathf.Clamp(multiplier, 0.25f, 2f);
        UpdateInteractionTarget();
    }

    public void ResetLightZoneMultipliers()
    {
        _lightZoneSprintMultiplier = 1f;
        _lightZoneInteractionMultiplier = 1f;
        UpdateInteractionTarget();
    }

    public void LockControls(float duration)
    {
        _controlLockedUntil = Mathf.Max(_controlLockedUntil, Time.time + Mathf.Max(0f, duration));
    }

    private void OnEnable()
    {
        UiStateCoordinator.OnInstanceChanged += HandleStateCoordinatorChanged;
        BindStateCoordinator(UiStateCoordinator.Instance);
        BindInput();
        EnableActions();
        SyncLightZoneEffects();
        _jumpMotor?.SyncToPosition(transform.position);
    }

    private void OnDisable()
    {
        UiStateCoordinator.OnInstanceChanged -= HandleStateCoordinatorChanged;
        BindStateCoordinator(null);
        DisableActions();
        UnbindInput();
    }

    private void Update()
    {
        if (ShouldBlockInteractionInput())
        {
            _fallbackInteractRequested = false;
            ClearInteractionTarget();
            _currentVelocity = Vector3.zero;
            _externalVelocity = Vector3.zero;
            return;
        }

        RefreshMoveInput();
        ProcessJumpInput();

        if (UiStateCoordinator.BlocksPlayerMovementForMode(_currentUiMode))
        {
            ClearInteractionTarget();
            _currentVelocity = Vector3.zero;
            _externalVelocity = Vector3.zero;
            return;
        }

        UpdateMovement(Time.deltaTime);
        UpdateInteractionTarget();
        ProcessActionInteraction();
        ProcessFallbackInteraction();
    }

    private bool ShouldBlockInteractionInput()
    {
        return UiStateCoordinator.BlocksPlayerInteractionForMode(_currentUiMode)
            || Time.frameCount <= SimpleDialogueUI.LastClosedFrame;
    }

    private void HandleStateCoordinatorChanged(UiStateCoordinator stateCoordinator)
    {
        BindStateCoordinator(stateCoordinator);
    }

    private void HandleUiModeChanged(UiStateCoordinator.UiMode mode)
    {
        _currentUiMode = mode;
    }

    private void BindStateCoordinator(UiStateCoordinator stateCoordinator)
    {
        if (_stateCoordinator == stateCoordinator)
        {
            return;
        }

        if (_stateCoordinator != null)
        {
            _stateCoordinator.OnModeChanged -= HandleUiModeChanged;
        }

        _stateCoordinator = stateCoordinator;
        _currentUiMode = _stateCoordinator != null ? _stateCoordinator.CurrentMode : UiStateCoordinator.UiMode.Exploration;

        if (_stateCoordinator != null)
        {
            _stateCoordinator.OnModeChanged += HandleUiModeChanged;
        }
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
        _jumpAction = actionMap.FindAction(jumpActionName, false);
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
        _jumpAction = null;
        _interactAction = null;
    }

    private void EnableActions()
    {
        _moveAction?.Enable();
        _sprintAction?.Enable();
        _jumpAction?.Enable();
        _interactAction?.Enable();
    }

    private void DisableActions()
    {
        _moveAction?.Disable();
        _sprintAction?.Disable();
        _jumpAction?.Disable();
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
        if (UiStateCoordinator.BlocksPlayerMovementForMode(_currentUiMode))
        {
            _currentVelocity = Vector3.zero;
            _externalVelocity = Vector3.zero;
            return;
        }

        Vector3 desiredDirection = IsControlLocked ? Vector3.zero : GetDesiredDirection();

        float movementSpeed = IsSprinting() ? sprintSpeed * _lightZoneSprintMultiplier : walkSpeed;
        float targetSpeed = movementSpeed * _speedMultiplier;
        Vector3 targetVelocity = desiredDirection * targetSpeed;

        float rate = desiredDirection.sqrMagnitude > 0.0001f ? acceleration : deceleration;
        _currentVelocity = Vector3.MoveTowards(_currentVelocity, targetVelocity, rate * deltaTime);
        _externalVelocity = Vector3.MoveTowards(_externalVelocity, Vector3.zero, externalVelocityDecay * deltaTime);
        Vector3 totalVelocity = _currentVelocity + _externalVelocity;
        Vector3 position = transform.position;

        if (movementMode == MovementMode.SideScrollerX)
        {
            position += Vector3.right * (totalVelocity.x * deltaTime);
            position.z = _sideScrollDepth;

            if (_useSideScrollHorizontalRange)
            {
                position.x = Mathf.Clamp(position.x, _sideScrollHorizontalRange.x, _sideScrollHorizontalRange.y);
            }

            if (_jumpMotor != null)
            {
                position = _jumpMotor.Simulate(position, deltaTime);
                position.z = _sideScrollDepth;
            }

            transform.position = position;

            if (Mathf.Abs(totalVelocity.x) > 0.0001f)
            {
                Vector3 facing = totalVelocity.x >= 0f ? Vector3.right : Vector3.left;
                Quaternion targetRotation = Quaternion.LookRotation(facing, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * deltaTime);
            }

            return;
        }

        position += totalVelocity * deltaTime;

        if (_useGroundPlaneBounds)
        {
            position.x = Mathf.Clamp(position.x, _groundPlaneXRange.x, _groundPlaneXRange.y);
            position.z = Mathf.Clamp(position.z, _groundPlaneZRange.x, _groundPlaneZRange.y);
        }

        if (_jumpMotor != null)
        {
            position = _jumpMotor.Simulate(position, deltaTime);
        }

        transform.position = position;

        Vector3 planarVelocity = new Vector3(totalVelocity.x, 0f, totalVelocity.z);
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
        if (movementMode == MovementMode.SideScrollerX)
        {
            return new Vector3(Mathf.Clamp(_moveInput.x, -1f, 1f), 0f, 0f);
        }

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
        float currentInteractionRadius = interactionRadius * _lightZoneInteractionMultiplier;
        Collider[] hits = Physics.OverlapSphere(origin, currentInteractionRadius, interactionMask, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestScore = float.NegativeInfinity;

        foreach (Collider hit in hits)
        {
            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                continue;
            }

            if (interactable is UnityEngine.Object unityObject && !unityObject)
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
            SetInteractionTarget(best);
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

    private void ProcessJumpInput()
    {
        bool useFallbackJump = _usingFallbackInput || _jumpAction == null;
        bool jumpPressed = (_jumpAction != null && _jumpAction.enabled && _jumpAction.WasPressedThisFrame())
            || (useFallbackJump && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            || (useFallbackJump && Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if (!jumpPressed)
        {
            return;
        }

        _jumpMotor?.QueueJump();
    }

    private void ProcessActionInteraction()
    {
        if (ShouldBlockInteractionInput())
        {
            return;
        }

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
        if (ShouldBlockInteractionInput())
        {
            _fallbackInteractRequested = false;
            return;
        }

        if (!_fallbackInteractRequested)
        {
            return;
        }

        _fallbackInteractRequested = false;
        _currentInteractable?.Interact(this);
    }

    private void ClearInteractionTarget()
    {
        SetInteractionTarget(null);
    }

    private void SetInteractionTarget(IInteractable nextInteractable)
    {
        if (ReferenceEquals(_currentInteractable, nextInteractable))
        {
            return;
        }

        IInteractable previousInteractable = _currentInteractable;
        previousInteractable?.OnFocusLost(this);

        _currentInteractable = nextInteractable;
        _currentInteractable?.OnFocusGained(this);
        OnInteractableTargetChanged?.Invoke(this, _currentInteractable);
    }

    private void SyncLightZoneEffects()
    {
        LightZoneEffect zone = LightZoneEffect.FindBest(transform.position);
        if (zone != null)
        {
            zone.ApplyCurrentEffects(this);
            return;
        }

        ResetLightZoneMultipliers();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = HasInteractableTarget ? Color.green : Color.cyan;
        Vector3 origin = transform.position + transform.rotation * interactionOffset;
        Gizmos.DrawWireSphere(origin, interactionRadius);
    }
}
