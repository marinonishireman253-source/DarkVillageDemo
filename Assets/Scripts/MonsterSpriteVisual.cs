using UnityEngine;

[DisallowMultipleComponent]
public sealed class MonsterSpriteVisual : MonoBehaviour
{
    private const string IdleSpritePath = "Characters/Monsters/BeamVisitor/side";
    private const string AttackSpritePrefix = "Characters/Monsters/BeamVisitor/side_attack_";
    private const string VisualRootName = "MonsterSpriteVisualRoot";

    [SerializeField] private float desiredHeight = 1.08f;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.02f, 0f);
    [SerializeField] private int sortingOrder = 10;
    [Header("Animation")]
    [SerializeField] private float walkAnimationFps = 4.8f;
    [SerializeField] private float proximityWalkDistance = 13f;
    [SerializeField] private float previewAttackDistance = 3.4f;
    [SerializeField] private float previewAttackCooldown = 1.15f;
    [SerializeField] private float attackAnimationDuration = 0.42f;
    [SerializeField] private float previewMoveSpeed = 1.08f;
    [SerializeField] private float previewReturnSpeed = 1.45f;
    [SerializeField] private float previewMoveRange = 4.4f;
    [SerializeField] private float previewStopDistance = 2.15f;
    [Header("Motion")]
    [SerializeField] private float idleBreathAmount = 0.028f;
    [SerializeField] private float idleBreathSpeed = 1.3f;
    [SerializeField] private float twitchAmount = 0.014f;
    [SerializeField] private float twitchSpeed = 7.6f;
    [SerializeField] private float walkBobHeight = 0.014f;
    [SerializeField] private float attackWindupDistance = 0.06f;
    [SerializeField] private float attackLungeDistance = 0.24f;
    [SerializeField] private float attackLiftHeight = 0.06f;
    [SerializeField] private float attackStretchAmount = 0.2f;

    private Sprite _idleSprite;
    private Sprite[] _walkSprites;
    private Sprite[] _attackSprites;
    private Transform _visualRoot;
    private SpriteRenderer _spriteRenderer;
    private SpriteCharacterLighting _characterLighting;
    private PlayerMover _player;
    private SimpleEnemyController _enemyController;
    private float _baseScale = 1f;
    private float _lastAttackStartedAt = float.NegativeInfinity;
    private float _previewNextAttackAt;
    private Vector3 _previewAnchorPosition;
    private bool _previewAnchorInitialized;

    public void Configure(float newDesiredHeight, int newSortingOrder, Vector3 newLocalOffset)
    {
        desiredHeight = Mathf.Max(0.1f, newDesiredHeight);
        sortingOrder = newSortingOrder;
        localOffset = newLocalOffset;
        if (_spriteRenderer != null)
        {
            _spriteRenderer.sortingOrder = sortingOrder;
        }

        ApplySprite(_spriteRenderer != null && _spriteRenderer.sprite != null ? _spriteRenderer.sprite : _idleSprite);
    }

    private void Awake()
    {
        EnsureVisual();
    }

    private void OnEnable()
    {
        EnsureVisual();
        UpdateVisual();
    }

    private void LateUpdate()
    {
        UpdateVisual();
    }

    private void EnsureVisual()
    {
        LoadSprites();

        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerMover>();
        }

        if (_enemyController == null)
        {
            _enemyController = GetComponent<SimpleEnemyController>();
        }

        if (!_previewAnchorInitialized)
        {
            _previewAnchorPosition = transform.position;
            _previewAnchorInitialized = true;
        }

        if (_visualRoot == null)
        {
            Transform existing = transform.Find(VisualRootName);
            _visualRoot = existing != null ? existing : CreateVisualRoot();
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = _visualRoot.GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = _visualRoot.gameObject.AddComponent<SpriteRenderer>();
            }
        }

        _spriteRenderer.sortingOrder = sortingOrder;
        ApplySprite(_idleSprite);
        EnsureCharacterLighting();
    }

    private void LoadSprites()
    {
        if (_idleSprite == null)
        {
            _idleSprite = Resources.Load<Sprite>(IdleSpritePath);
        }

        _walkSprites ??= LoadSequence("Characters/Monsters/BeamVisitor/side_walk_", 4);
        _attackSprites ??= LoadSequence(AttackSpritePrefix, 4);
    }

    private static Sprite[] LoadSequence(string prefix, int count)
    {
        Sprite[] sprites = new Sprite[count];
        for (int index = 0; index < count; index++)
        {
            sprites[index] = Resources.Load<Sprite>($"{prefix}{index + 1:00}");
        }

        return sprites;
    }

    private Transform CreateVisualRoot()
    {
        GameObject visual = new GameObject(VisualRootName);
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        return visual.transform;
    }

    private void UpdateVisual()
    {
        if (_visualRoot == null || _spriteRenderer == null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerMover>();
        }

        if (_enemyController == null)
        {
            _enemyController = GetComponent<SimpleEnemyController>();
        }

        if (_enemyController == null)
        {
            float previewAttackElapsed = Time.time - _lastAttackStartedAt;
            bool previewAttackActive = previewAttackElapsed >= 0f && previewAttackElapsed <= attackAnimationDuration;
            UpdatePreviewMovement(Time.deltaTime, previewAttackActive);
        }

        Vector3 cameraForward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up);
        if (cameraForward.sqrMagnitude > 0.0001f)
        {
            _visualRoot.forward = cameraForward.normalized;
        }

        bool isWalking = false;
        bool isAttacking = false;
        float attackProgress = 0f;
        bool isAgitated = false;

        if (_enemyController != null)
        {
            isWalking = _enemyController.IsMoving;
            isAgitated = isWalking;

            if (_enemyController.LastAttackStartedAt > _lastAttackStartedAt)
            {
                _lastAttackStartedAt = _enemyController.LastAttackStartedAt;
            }

            float attackDuration = Mathf.Max(0.01f, _enemyController.AttackAnimationDuration);
            float elapsed = Time.time - _lastAttackStartedAt;
            isAttacking = elapsed >= 0f && elapsed <= attackDuration;
            attackProgress = isAttacking ? Mathf.Clamp01(elapsed / attackDuration) : 0f;
        }
        else if (_player != null)
        {
            float distanceToPlayer = Mathf.Abs(_player.transform.position.x - transform.position.x);
            isWalking = distanceToPlayer <= proximityWalkDistance;
            isAgitated = isWalking;

            if (distanceToPlayer <= previewAttackDistance && Time.time >= _previewNextAttackAt)
            {
                _lastAttackStartedAt = Time.time;
                _previewNextAttackAt = Time.time + previewAttackCooldown;
            }

            float elapsed = Time.time - _lastAttackStartedAt;
            isAttacking = elapsed >= 0f && elapsed <= attackAnimationDuration;
            attackProgress = isAttacking ? Mathf.Clamp01(elapsed / attackAnimationDuration) : 0f;
        }

        if (isAttacking)
        {
            isWalking = false;
            isAgitated = true;
            ApplySprite(GetAttackSprite(attackProgress));
        }
        else
        {
            ApplySprite(isWalking ? GetLoopSprite(_walkSprites, walkAnimationFps) : _idleSprite);
        }

        if (_player != null)
        {
            _spriteRenderer.flipX = _player.transform.position.x < transform.position.x;
        }

        float breath = Mathf.Sin(Time.time * idleBreathSpeed) * idleBreathAmount;
        float twitch = isAgitated ? Mathf.Max(0f, Mathf.Sin(Time.time * twitchSpeed)) * twitchAmount : 0f;
        float walkBob = isWalking ? Mathf.Abs(Mathf.Sin(Time.time * walkAnimationFps * Mathf.PI)) * walkBobHeight : 0f;
        float attackEase = isAttacking ? Mathf.Sin(attackProgress * Mathf.PI) : 0f;
        float attackLunge = GetAttackLunge(attackProgress, isAttacking);
        float lungeDirection = _spriteRenderer.flipX ? -1f : 1f;

        _visualRoot.position = transform.position
            + localOffset
            + Vector3.up * (breath + twitch + walkBob + attackEase * attackLiftHeight)
            + Vector3.right * (lungeDirection * attackLunge);

        float scaleX = _baseScale * (1f + breath * 0.25f + attackEase * attackStretchAmount);
        float scaleY = _baseScale * (1f + breath * 0.2f - attackEase * attackStretchAmount * 0.38f);
        _visualRoot.localScale = new Vector3(scaleX, scaleY, _baseScale);
        _characterLighting?.ApplyFrame(_spriteRenderer, _visualRoot, desiredHeight, isWalking ? 1f : 0f, attackEase);
    }

    private void UpdatePreviewMovement(float deltaTime, bool isAttacking)
    {
        if (_player == null || !_previewAnchorInitialized)
        {
            return;
        }

        Vector3 position = transform.position;
        position.y = _previewAnchorPosition.y;
        position.z = _previewAnchorPosition.z;

        float targetX = _previewAnchorPosition.x;
        float distanceToPlayer = Mathf.Abs(_player.transform.position.x - position.x);

        if (distanceToPlayer <= proximityWalkDistance)
        {
            float facingSign = _player.transform.position.x >= position.x ? 1f : -1f;
            targetX = _player.transform.position.x - facingSign * previewStopDistance;
            targetX = Mathf.Clamp(targetX, _previewAnchorPosition.x - previewMoveRange, _previewAnchorPosition.x + previewMoveRange);
        }

        if (isAttacking)
        {
            targetX = position.x;
        }

        float moveSpeed = Mathf.Abs(targetX - position.x) > 0.01f && distanceToPlayer <= proximityWalkDistance
            ? previewMoveSpeed
            : previewReturnSpeed;

        position.x = Mathf.MoveTowards(position.x, targetX, moveSpeed * deltaTime);
        transform.position = position;
    }

    private Sprite GetLoopSprite(Sprite[] sprites, float fps)
    {
        if (sprites == null || sprites.Length == 0)
        {
            return _idleSprite;
        }

        int frame = Mathf.FloorToInt(Time.time * Mathf.Max(0.1f, fps)) % sprites.Length;
        return sprites[frame] != null ? sprites[frame] : _idleSprite;
    }

    private Sprite GetAttackSprite(float progress)
    {
        if (_attackSprites == null || _attackSprites.Length == 0)
        {
            return _idleSprite;
        }

        int frame = Mathf.Clamp(Mathf.FloorToInt(progress * _attackSprites.Length), 0, _attackSprites.Length - 1);
        return _attackSprites[frame] != null ? _attackSprites[frame] : _idleSprite;
    }

    private float GetAttackLunge(float progress, bool isAttacking)
    {
        if (!isAttacking)
        {
            return 0f;
        }

        if (progress <= 0.3f)
        {
            return -Mathf.SmoothStep(0f, attackWindupDistance, progress / 0.3f);
        }

        float strikeProgress = Mathf.InverseLerp(0.3f, 1f, progress);
        return Mathf.Sin(strikeProgress * Mathf.PI) * attackLungeDistance;
    }

    private void ApplySprite(Sprite sprite)
    {
        if (_spriteRenderer == null || sprite == null)
        {
            return;
        }

        if (_spriteRenderer.sprite == sprite && _baseScale > 0f)
        {
            return;
        }

        _spriteRenderer.sprite = sprite;
        float spriteHeight = sprite.bounds.size.y;
        _baseScale = spriteHeight > 0.0001f ? desiredHeight / spriteHeight : 1f;
    }

    private void EnsureCharacterLighting()
    {
        if (_characterLighting == null)
        {
            _characterLighting = GetComponent<SpriteCharacterLighting>();
        }

        if (_characterLighting == null)
        {
            _characterLighting = gameObject.AddComponent<SpriteCharacterLighting>();
        }
    }
}
