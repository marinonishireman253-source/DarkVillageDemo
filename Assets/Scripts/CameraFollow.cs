using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(-7f, 8f, -7f);
    [SerializeField] private float followSmoothTime = 0.16f;
    [SerializeField] private bool followOnStart = true;
    [SerializeField] private bool preserveInitialOffset = true;
    [SerializeField] private bool useFixedRotation = true;
    [SerializeField] private Vector3 fixedEulerAngles = new Vector3(42f, 45f, 0f);
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private Vector3 lookAtOffset = Vector3.zero;
    [SerializeField] private float rotationLerpSpeed = 8f;

    [Header("Sprint Feel")]
    [SerializeField] private PlayerMover playerMover;
    [SerializeField] private Vector3 sprintOffsetDelta = new Vector3(-0.3f, 0.15f, -0.3f);
    [SerializeField] private float sprintCameraLerpSpeed = 5f;
    [Header("Bounds")]
    [SerializeField] private bool useHorizontalBounds;
    [SerializeField] private Vector2 horizontalBounds;
    [Header("Room Blend")]
    [SerializeField] private Vector3 roomOffsetDelta = Vector3.zero;
    [SerializeField] private float roomBlendDuration = 0.28f;

    private Vector3 _velocity;
    private bool _initialized;
    private Vector3 _baseOffset;
    private Vector3 _currentOffset;
    private Vector2 _currentHorizontalBounds;
    private Vector2 _targetHorizontalBounds;
    private Vector3 _currentRoomOffsetDelta;
    private Vector3 _targetRoomOffsetDelta;
    private RoomCameraZone _activeRoomZone;

    public Transform Target => target;

    public void Configure(Vector3 newOffset, bool useInitialOffset, Vector3 newLookAtOffset, Vector3 newFixedEulerAngles)
    {
        offset = newOffset;
        preserveInitialOffset = useInitialOffset;
        lookAtOffset = newLookAtOffset;
        fixedEulerAngles = newFixedEulerAngles;
        useFixedRotation = true;
    }

    public void ConfigureSprintFeel(Vector3 newSprintOffsetDelta, float newSprintCameraLerpSpeed)
    {
        sprintOffsetDelta = newSprintOffsetDelta;
        sprintCameraLerpSpeed = newSprintCameraLerpSpeed;
    }

    public void ConfigureHorizontalBounds(Vector2 newHorizontalBounds)
    {
        horizontalBounds = newHorizontalBounds;
        useHorizontalBounds = newHorizontalBounds.x < newHorizontalBounds.y;
        _currentHorizontalBounds = newHorizontalBounds;
        _targetHorizontalBounds = newHorizontalBounds;
    }

    public void ConfigureRoomZone(RoomCameraZone roomZone, bool snap = false)
    {
        if (roomZone == null)
        {
            return;
        }

        if (!snap && ReferenceEquals(_activeRoomZone, roomZone))
        {
            return;
        }

        _activeRoomZone = roomZone;
        roomBlendDuration = Mathf.Max(0.01f, roomZone.BlendDuration);
        useHorizontalBounds = roomZone.HorizontalBounds.x < roomZone.HorizontalBounds.y;
        _targetHorizontalBounds = roomZone.HorizontalBounds;
        _targetRoomOffsetDelta = roomZone.CameraOffsetDelta;

        if (snap)
        {
            _currentHorizontalBounds = _targetHorizontalBounds;
            _currentRoomOffsetDelta = _targetRoomOffsetDelta;
        }
    }

    private void Start()
    {
        if (target == null)
        {
            PlayerMover player = FindFirstObjectByType<PlayerMover>();
            if (player != null)
            {
                target = player.transform;
                if (playerMover == null)
                {
                    playerMover = player;
                }
            }
        }

        if (target == null)
        {
            return;
        }

        if (playerMover == null)
        {
            playerMover = target.GetComponent<PlayerMover>();
        }

        if (preserveInitialOffset)
        {
            offset = transform.position - target.position;
        }

        _baseOffset = offset;
        _currentOffset = offset;
        if (_currentHorizontalBounds == Vector2.zero && _targetHorizontalBounds == Vector2.zero)
        {
            _currentHorizontalBounds = horizontalBounds;
            _targetHorizontalBounds = horizontalBounds;
        }

        if (_currentRoomOffsetDelta == Vector3.zero && _targetRoomOffsetDelta == Vector3.zero)
        {
            _currentRoomOffsetDelta = roomOffsetDelta;
            _targetRoomOffsetDelta = roomOffsetDelta;
        }

        if (followOnStart)
        {
            SnapToTarget();
        }
        else
        {
            _initialized = true;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (!_initialized)
        {
            SnapToTarget();
            return;
        }

        UpdateDynamicOffset(Time.deltaTime);

        Vector3 desiredPosition = target.position + _currentOffset;
        if (useHorizontalBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, _currentHorizontalBounds.x, _currentHorizontalBounds.y);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, followSmoothTime);
        UpdateRotation(Time.deltaTime);
    }

    public void SetTarget(Transform newTarget, bool snap = false)
    {
        target = newTarget;
        if (target != null && playerMover == null)
        {
            playerMover = target.GetComponent<PlayerMover>();
        }

        if (snap)
        {
            SnapToTarget();
        }
    }

    private void UpdateDynamicOffset(float deltaTime)
    {
        float roomBlendT = 1f - Mathf.Exp(-deltaTime / Mathf.Max(0.01f, roomBlendDuration));
        _currentHorizontalBounds = Vector2.Lerp(_currentHorizontalBounds, _targetHorizontalBounds, roomBlendT);
        _currentRoomOffsetDelta = Vector3.Lerp(_currentRoomOffsetDelta, _targetRoomOffsetDelta, roomBlendT);

        Vector3 targetOffset = _baseOffset + _currentRoomOffsetDelta;

        if (playerMover != null && playerMover.IsSprintActive)
        {
            targetOffset += sprintOffsetDelta;
        }

        _currentOffset = Vector3.Lerp(_currentOffset, targetOffset, 1f - Mathf.Exp(-sprintCameraLerpSpeed * deltaTime));
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        _baseOffset = offset;
        _currentOffset = offset + _currentRoomOffsetDelta;
        transform.position = target.position + _currentOffset;
        if (useHorizontalBounds)
        {
            Vector3 clampedPosition = transform.position;
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, _currentHorizontalBounds.x, _currentHorizontalBounds.y);
            transform.position = clampedPosition;
        }

        SnapRotation();
        _velocity = Vector3.zero;
        _initialized = true;
    }

    private void UpdateRotation(float deltaTime)
    {
        if (useFixedRotation)
        {
            transform.rotation = Quaternion.Euler(fixedEulerAngles);
            return;
        }

        if (!lookAtTarget)
        {
            return;
        }

        Quaternion desiredRotation = GetDesiredRotation();
        float t = 1f - Mathf.Exp(-rotationLerpSpeed * deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, t);
    }

    private void SnapRotation()
    {
        if (useFixedRotation)
        {
            transform.rotation = Quaternion.Euler(fixedEulerAngles);
            return;
        }

        if (!lookAtTarget)
        {
            return;
        }

        transform.rotation = GetDesiredRotation();
    }

    private Quaternion GetDesiredRotation()
    {
        Vector3 focusPoint = target.position + lookAtOffset;
        Vector3 lookDirection = focusPoint - transform.position;

        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return transform.rotation;
        }

        return Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }
}
