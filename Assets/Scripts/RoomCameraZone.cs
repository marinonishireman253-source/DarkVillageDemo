using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class RoomCameraZone : MonoBehaviour
{
    [SerializeField] private Vector2 horizontalBounds;
    [SerializeField] private Vector3 cameraOffsetDelta = Vector3.zero;
    [SerializeField] private float blendDuration = 0.28f;

    private BoxCollider _trigger;

    public Vector2 HorizontalBounds => horizontalBounds;
    public Vector3 CameraOffsetDelta => cameraOffsetDelta;
    public float BlendDuration => blendDuration;

    public void Configure(Vector3 triggerCenter, Vector3 triggerSize, Vector2 newHorizontalBounds, Vector3 newCameraOffsetDelta, float newBlendDuration)
    {
        EnsureTrigger();
        _trigger.center = triggerCenter;
        _trigger.size = triggerSize;
        horizontalBounds = newHorizontalBounds;
        cameraOffsetDelta = newCameraOffsetDelta;
        blendDuration = Mathf.Max(0.01f, newBlendDuration);
    }

    private void Awake()
    {
        EnsureTrigger();
    }

    private void OnValidate()
    {
        EnsureTrigger();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryActivate(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryActivate(other);
    }

    private void TryActivate(Collider other)
    {
        if (other == null)
        {
            return;
        }

        PlayerMover player = other.GetComponentInParent<PlayerMover>();
        if (player == null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        CameraFollow follow = mainCamera.GetComponent<CameraFollow>();
        if (follow == null)
        {
            return;
        }

        RoomCameraZone bestZone = TowerInteriorSlice.FindBestZone(player);
        if (bestZone == null || !ReferenceEquals(bestZone, this))
        {
            return;
        }

        follow.ConfigureRoomZone(this);
    }

    private void EnsureTrigger()
    {
        if (_trigger == null)
        {
            _trigger = GetComponent<BoxCollider>();
        }

        if (_trigger != null)
        {
            _trigger.isTrigger = true;
        }
    }
}
