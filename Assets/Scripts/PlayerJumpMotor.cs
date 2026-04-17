using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CapsuleCollider))]
public sealed class PlayerJumpMotor : MonoBehaviour, IGroundProjectionProvider
{
    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.8f;
    [SerializeField] private float gravity = 24f;
    [SerializeField] private float fallGravityMultiplier = 1.85f;
    [SerializeField] private float maxFallSpeed = 18f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferDuration = 0.14f;

    [Header("Grounding")]
    [SerializeField] private bool useVirtualGroundPlane = true;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundProbeDistance = 0.18f;
    [SerializeField] private float landingSnapDistance = 0.12f;
    [SerializeField] private float collisionSkin = 0.02f;
    [SerializeField] [Range(0.1f, 1f)] private float minGroundNormalY = 0.55f;

    public bool IsGrounded => _isGrounded;
    public float VerticalVelocity => _verticalVelocity;
    public float HeightAboveGround => Mathf.Max(0f, transform.position.y - _groundHeight);

    public event System.Action OnJumped;
    public event System.Action OnLanded;

    private readonly RaycastHit[] _castHits = new RaycastHit[8];

    private CapsuleCollider _capsule;
    private float _verticalVelocity;
    private float _lastGroundedAt = float.NegativeInfinity;
    private float _jumpBufferedUntil = float.NegativeInfinity;
    private float _groundHeight;
    private bool _isGrounded;

    private void Awake()
    {
        _capsule = GetComponent<CapsuleCollider>();
    }

    public void QueueJump()
    {
        _jumpBufferedUntil = Time.time + Mathf.Max(0.01f, jumpBufferDuration);
    }

    public void SyncToPosition(Vector3 worldPosition)
    {
        Vector3 resolvedPosition = RefreshGroundState(worldPosition, snapToGround: true);
        if (!_isGrounded && useVirtualGroundPlane)
        {
            _groundHeight = worldPosition.y;
            _isGrounded = true;
            resolvedPosition.y = _groundHeight;
        }

        transform.position = resolvedPosition;
        if (_isGrounded)
        {
            _verticalVelocity = 0f;
        }
    }

    public Vector3 Simulate(Vector3 currentPosition, float deltaTime)
    {
        currentPosition = RefreshGroundState(currentPosition, snapToGround: true);

        if (CanConsumeBufferedJump())
        {
            StartJump();
        }

        if (_isGrounded && _verticalVelocity <= 0f)
        {
            _verticalVelocity = 0f;
            return currentPosition;
        }

        float appliedGravity = gravity * (_verticalVelocity <= 0f ? fallGravityMultiplier : 1f);
        _verticalVelocity = Mathf.Max(_verticalVelocity - appliedGravity * deltaTime, -Mathf.Max(1f, maxFallSpeed));
        float verticalDelta = _verticalVelocity * deltaTime;

        if (Mathf.Abs(verticalDelta) <= 0.0001f)
        {
            currentPosition = RefreshGroundState(currentPosition, snapToGround: true);
            return currentPosition;
        }

        Vector3 nextPosition = currentPosition;
        if (TryResolveVerticalCollision(currentPosition, verticalDelta, out Vector3 resolvedPosition, out RaycastHit hit))
        {
            nextPosition = resolvedPosition;

            if (verticalDelta > 0f)
            {
                _verticalVelocity = 0f;
            }
            else
            {
                _verticalVelocity = 0f;
                _jumpBufferedUntil = float.NegativeInfinity;
            }
        }
        else
        {
            nextPosition.y += verticalDelta;
        }

        nextPosition = RefreshGroundState(nextPosition, snapToGround: _verticalVelocity <= 0f);
        return nextPosition;
    }

    public Vector3 GetGroundProjection(Vector3 worldPosition)
    {
        return new Vector3(worldPosition.x, _groundHeight, worldPosition.z);
    }

    private bool CanConsumeBufferedJump()
    {
        if (Time.time > _jumpBufferedUntil)
        {
            return false;
        }

        return _isGrounded || Time.time <= _lastGroundedAt + Mathf.Max(0f, coyoteTime);
    }

    private void StartJump()
    {
        _jumpBufferedUntil = float.NegativeInfinity;
        _isGrounded = false;
        _verticalVelocity = Mathf.Sqrt(2f * Mathf.Max(0.01f, gravity) * Mathf.Max(0.1f, jumpHeight));
        OnJumped?.Invoke();
    }

    private bool TryResolveVerticalCollision(Vector3 currentPosition, float verticalDelta, out Vector3 resolvedPosition, out RaycastHit resolvedHit)
    {
        resolvedPosition = currentPosition;
        resolvedHit = default;

        Vector3 direction = verticalDelta > 0f ? Vector3.up : Vector3.down;
        float travelDistance = Mathf.Abs(verticalDelta);
        if (travelDistance <= 0.0001f)
        {
            return false;
        }

        int hitCount = Physics.CapsuleCastNonAlloc(
            GetCapsuleTop(currentPosition),
            GetCapsuleBottom(currentPosition),
            GetCapsuleRadius(),
            direction,
            _castHits,
            travelDistance + collisionSkin,
            groundMask,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.PositiveInfinity;
        bool foundHit = false;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _castHits[i];
            bool isValidHit = verticalDelta > 0f
                ? IsValidCollisionHit(hit)
                : IsValidGroundHit(hit);
            if (!isValidHit)
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                resolvedHit = hit;
                foundHit = true;
            }
        }

        if (!foundHit)
        {
            return false;
        }

        float allowedDistance = Mathf.Max(0f, bestDistance - collisionSkin);
        resolvedPosition += direction * allowedDistance;
        return true;
    }

    private Vector3 RefreshGroundState(Vector3 currentPosition, bool snapToGround)
    {
        bool wasGrounded = _isGrounded;
        if (TryGetGroundHit(currentPosition, groundProbeDistance + landingSnapDistance + collisionSkin, out RaycastHit hit))
        {
            _groundHeight = hit.point.y;
            float distanceToGround = currentPosition.y - _groundHeight;
            bool shouldGround = distanceToGround <= groundProbeDistance
                || (_verticalVelocity <= 0f && distanceToGround <= landingSnapDistance);

            if (shouldGround)
            {
                _isGrounded = true;
                _lastGroundedAt = Time.time;

                if (!wasGrounded)
                {
                    OnLanded?.Invoke();
                }

                if (snapToGround)
                {
                    currentPosition.y = _groundHeight;
                }

                if (_verticalVelocity <= 0f)
                {
                    _verticalVelocity = 0f;
                }

                return currentPosition;
            }
        }

        if (useVirtualGroundPlane)
        {
            float distanceToGround = currentPosition.y - _groundHeight;
            bool shouldGround = _verticalVelocity <= 0f
                && (distanceToGround <= groundProbeDistance
                    || currentPosition.y <= _groundHeight + landingSnapDistance);
            if (shouldGround)
            {
                _isGrounded = true;
                _lastGroundedAt = Time.time;

                if (!wasGrounded)
                {
                    OnLanded?.Invoke();
                }

                if (snapToGround)
                {
                    currentPosition.y = _groundHeight;
                }

                if (_verticalVelocity <= 0f)
                {
                    _verticalVelocity = 0f;
                }

                return currentPosition;
            }
        }

        _isGrounded = false;
        return currentPosition;
    }

    private bool TryGetGroundHit(Vector3 currentPosition, float probeDistance, out RaycastHit resolvedHit)
    {
        resolvedHit = default;
        int hitCount = Physics.CapsuleCastNonAlloc(
            GetCapsuleTop(currentPosition),
            GetCapsuleBottom(currentPosition),
            GetCapsuleRadius(),
            Vector3.down,
            _castHits,
            Mathf.Max(0.01f, probeDistance),
            groundMask,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.PositiveInfinity;
        bool foundHit = false;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _castHits[i];
            if (!IsValidGroundHit(hit))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                resolvedHit = hit;
                foundHit = true;
            }
        }

        return foundHit;
    }

    private bool IsValidGroundHit(RaycastHit hit)
    {
        Collider hitCollider = hit.collider;
        if (hitCollider == null)
        {
            return false;
        }

        if (hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform))
        {
            return false;
        }

        return hit.normal.y >= minGroundNormalY;
    }

    private bool IsValidCollisionHit(RaycastHit hit)
    {
        Collider hitCollider = hit.collider;
        if (hitCollider == null)
        {
            return false;
        }

        return hitCollider.transform != transform && !hitCollider.transform.IsChildOf(transform);
    }

    private Vector3 GetCapsuleTop(Vector3 position)
    {
        GetCapsuleExtents(out float radius, out float verticalOffset);
        Vector3 center = position + _capsule.center;
        return center + Vector3.up * verticalOffset;
    }

    private Vector3 GetCapsuleBottom(Vector3 position)
    {
        GetCapsuleExtents(out float radius, out float verticalOffset);
        Vector3 center = position + _capsule.center;
        return center - Vector3.up * verticalOffset;
    }

    private float GetCapsuleRadius()
    {
        GetCapsuleExtents(out float radius, out _);
        return radius;
    }

    private void GetCapsuleExtents(out float radius, out float verticalOffset)
    {
        if (_capsule == null)
        {
            _capsule = GetComponent<CapsuleCollider>();
        }

        Vector3 lossyScale = transform.lossyScale;
        radius = _capsule != null
            ? _capsule.radius * Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.z))
            : 0.3f;
        float height = _capsule != null
            ? Mathf.Max(_capsule.height * Mathf.Abs(lossyScale.y), radius * 2f + 0.01f)
            : 2f;
        verticalOffset = Mathf.Max(0f, height * 0.5f - radius);
    }
}
