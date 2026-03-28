using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 6f, -10f);
    [SerializeField] private float followSmoothTime = 0.16f;
    [SerializeField] private bool followOnStart = true;
    [SerializeField] private bool preserveInitialOffset = true;

    [Header("Sprint Feel")]
    [SerializeField] private PlayerMover playerMover;
    [SerializeField] private Vector3 sprintOffsetDelta = new Vector3(0f, 0.35f, -1.25f);
    [SerializeField] private float sprintCameraLerpSpeed = 5f;

    private Vector3 _velocity;
    private bool _initialized;
    private Vector3 _baseOffset;
    private Vector3 _currentOffset;

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
        _velocity = Vector3.zero;
        _initialized = true;
    }
}
