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

    private Vector3 _velocity;
    private bool _initialized;
    private Vector3 _baseOffset;
    private Vector3 _currentOffset;

    public Transform Target => target;

    public void Configure(Vector3 newOffset, bool useInitialOffset, Vector3 newLookAtOffset, Vector3 newFixedEulerAngles)
    {
        offset = newOffset;
        preserveInitialOffset = useInitialOffset;
        lookAtOffset = newLookAtOffset;
        fixedEulerAngles = newFixedEulerAngles;
        useFixedRotation = true;
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
        Vector3 targetOffset = _baseOffset;

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
        _currentOffset = offset;
        transform.position = target.position + _currentOffset;
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
