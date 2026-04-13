using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSpriteVisual : MonoBehaviour
{
    private enum FacingBucket
    {
        Front,
        Side,
        Back
    }

    private const string FrontIdleSpritePath = "Characters/SagiriRuntime/front";
    private const string SideIdleSpritePath = "Characters/SagiriRuntime/side";
    private const string BackIdleSpritePath = "Characters/SagiriRuntime/back";
    private const string VisualRootName = "SpriteVisual";

    [SerializeField] private float desiredHeight = 2.15f;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.02f, 0f);
    [SerializeField] private int sortingOrder = 10;
    [Header("Animation")]
    [SerializeField] private float frontWalkAnimationFps = 4.25f;
    [SerializeField] private float sideWalkAnimationFps = 7.1f;
    [SerializeField] private float backWalkAnimationFps = 4.6f;
    [SerializeField] private float walkAnimationThreshold = 0.14f;
    [Header("Motion")]
    [SerializeField] private float idleBreathAmount = 0.008f;
    [SerializeField] private float walkBobHeight = 0.015f;
    [SerializeField] private float walkSquashAmount = 0.01f;
    [SerializeField] private float sideLeanAngle = 0.25f;
    [SerializeField] private float forwardLeanAngle = 0.2f;
    [Header("Hit Reaction")]
    [SerializeField] private float hitVisualDuration = 0.24f;
    [SerializeField] private float hitFlashDuration = 0.12f;
    [SerializeField] private float hitRecoilDistance = 0.12f;
    [SerializeField] private float hitTiltAngle = 5.5f;
    [SerializeField] private float hitSquashAmount = 0.02f;
    [SerializeField] private Color hitTint = new Color(1f, 0.58f, 0.58f, 1f);
    [Header("Attack")]
    [SerializeField] private float frontAttackVisualDuration = 0.46f;
    [SerializeField] private float sideAttackVisualDuration = 0.28f;
    [SerializeField] private float backAttackVisualDuration = 0.38f;
    [SerializeField] private float attackLungeDistance = 0.08f;
    [SerializeField] private float attackStretchAmount = 0.015f;
    [SerializeField] private Color attackTint = new Color(1f, 0.98f, 0.95f, 1f);

    private PlayerMover _playerMover;
    private PlayerCombat _playerCombat;
    private CombatantHealth _health;
    private Sprite _frontIdleSprite;
    private Sprite _sideIdleSprite;
    private Sprite _backIdleSprite;
    private Sprite[] _frontWalkSprites;
    private Sprite[] _sideWalkSprites;
    private Sprite[] _backWalkSprites;
    private Sprite[] _frontAttackSprites;
    private Sprite[] _sideAttackSprites;
    private Sprite[] _backAttackSprites;
    private Transform _visualRoot;
    private SpriteRenderer _spriteRenderer;
    private SpriteCharacterLighting _characterLighting;
    private float _baseScale = 1f;
    private float _attackStartedAt = float.NegativeInfinity;
    private float _hitStartedAt = float.NegativeInfinity;
    private FacingBucket _attackFacing = FacingBucket.Front;
    private bool _attackFlipX;
    private Vector3 _attackLungeDirection = Vector3.forward;
    private Vector3 _hitRecoilDirection = Vector3.back;

    private void Awake()
    {
        EnsureVisual();
    }

    private void OnEnable()
    {
        EnsureVisual();
        SubscribeCombatEvents();
        SubscribeHealthEvents();
        UpdateVisual();
    }

    private void OnDisable()
    {
        UnsubscribeCombatEvents();
        UnsubscribeHealthEvents();
    }

    private void LateUpdate()
    {
        UpdateVisual();
    }

    private void EnsureVisual()
    {
        DisableLegacyMesh();
        RemoveLegacyVisualRoot();
        LoadSprites();

        if (_playerMover == null)
        {
            _playerMover = GetComponent<PlayerMover>();
        }

        if (_playerCombat == null)
        {
            _playerCombat = GetComponent<PlayerCombat>();
        }

        if (_health == null)
        {
            _health = GetComponent<CombatantHealth>();
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
        ApplySprite(_frontIdleSprite ?? _sideIdleSprite ?? _backIdleSprite);
        EnsureCharacterLighting();
    }

    private void LoadSprites()
    {
        if (_frontIdleSprite == null)
        {
            _frontIdleSprite = Resources.Load<Sprite>(FrontIdleSpritePath);
        }

        if (_sideIdleSprite == null)
        {
            _sideIdleSprite = Resources.Load<Sprite>(SideIdleSpritePath);
        }

        if (_backIdleSprite == null)
        {
            _backIdleSprite = Resources.Load<Sprite>(BackIdleSpritePath);
        }

        _frontWalkSprites ??= LoadSequence("Characters/SagiriRuntime/front_walk_", 6);
        _sideWalkSprites ??= LoadSequence("Characters/SagiriRuntime/side_walk_", 4);
        _backWalkSprites ??= LoadSequence("Characters/SagiriRuntime/back_walk_", 4);
        _frontAttackSprites ??= LoadSequence("Characters/SagiriRuntime/front_attack_", 4);
        _sideAttackSprites ??= LoadSequence("Characters/SagiriRuntime/side_attack_", 4);
        _backAttackSprites ??= LoadSequence("Characters/SagiriRuntime/back_attack_", 4);
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

        Vector3 cameraForward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up);
        Vector3 cameraRight = Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up);
        _visualRoot.position = transform.position + localOffset;
        if (cameraForward.sqrMagnitude > 0.0001f)
        {
            _visualRoot.forward = cameraForward.normalized;
        }

        Vector3 facing = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Vector3 planarVelocity = _playerMover != null ? _playerMover.Velocity : Vector3.zero;
        planarVelocity.y = 0f;

        float moveSpeed = planarVelocity.magnitude;
        float moveBlend = Mathf.Clamp01(moveSpeed / 4.2f);
        float attackDuration = GetAttackDuration(_attackFacing);
        float attackElapsed = GetAttackElapsed(attackDuration);
        float attackBlend = GetAttackBlend(attackElapsed, attackDuration);
        float hitElapsed = GetHitElapsed();
        float hitBlend = GetHitBlend(hitElapsed);
        float hitFlashBlend = GetHitFlashBlend(hitElapsed);
        bool isWalking = moveBlend >= walkAnimationThreshold;
        bool hasFacingData = TryResolveFacingState(
            cameraForward,
            cameraRight,
            facing,
            out FacingBucket facingBucket,
            out bool flipX,
            out float lateralFacing,
            out float forwardFacing);

        if (hitElapsed >= 0f)
        {
            ApplyHitVisual(hasFacingData, facingBucket, flipX, lateralFacing, forwardFacing, hitBlend, hitFlashBlend);
            _characterLighting?.ApplyFrame(_spriteRenderer, _visualRoot, desiredHeight, 0f, hitBlend * 0.25f);
            return;
        }

        if (attackElapsed >= 0f)
        {
            ApplyAttackVisual(moveBlend, attackElapsed, attackBlend);
            _characterLighting?.ApplyFrame(_spriteRenderer, _visualRoot, desiredHeight, moveBlend, attackBlend);
            return;
        }

        if (!hasFacingData)
        {
            ApplySprite(SelectLoopSprite(_frontIdleSprite, _frontWalkSprites, isWalking, frontWalkAnimationFps));
            _spriteRenderer.flipX = false;
            ApplyMotion(moveBlend, Time.time * frontWalkAnimationFps * Mathf.PI, attackBlend, 0f, 0f, 0f, 0f);
            _characterLighting?.ApplyFrame(_spriteRenderer, _visualRoot, desiredHeight, moveBlend, attackBlend);
            return;
        }

        switch (facingBucket)
        {
            case FacingBucket.Back:
                ApplySprite(SelectLoopSprite(_backIdleSprite, _backWalkSprites, isWalking, backWalkAnimationFps));
                _spriteRenderer.flipX = false;
                ApplyMotion(moveBlend, Time.time * backWalkAnimationFps * Mathf.PI, attackBlend, 0f, forwardFacing, 0f, 0f);
                _characterLighting?.ApplyFrame(_spriteRenderer, _visualRoot, desiredHeight, moveBlend, attackBlend);
                return;
            case FacingBucket.Side:
                ApplySprite(SelectLoopSprite(_sideIdleSprite, _sideWalkSprites, isWalking, sideWalkAnimationFps));
                _spriteRenderer.flipX = flipX;
                ApplyMotion(moveBlend, Time.time * sideWalkAnimationFps * Mathf.PI, attackBlend, lateralFacing, 0f, 0f, 0f);
                _characterLighting?.ApplyFrame(_spriteRenderer, _visualRoot, desiredHeight, moveBlend, attackBlend);
                return;
            default:
                ApplySprite(SelectLoopSprite(_frontIdleSprite, _frontWalkSprites, isWalking, frontWalkAnimationFps));
                _spriteRenderer.flipX = false;
                ApplyMotion(moveBlend, Time.time * frontWalkAnimationFps * Mathf.PI, attackBlend, 0f, forwardFacing, 0f, 0f);
                _characterLighting?.ApplyFrame(_spriteRenderer, _visualRoot, desiredHeight, moveBlend, attackBlend);
                return;
        }
    }

    private void ApplyAttackVisual(float moveBlend, float attackElapsed, float attackBlend)
    {
        float motionBlend = Mathf.Min(moveBlend, 0.35f);

        switch (_attackFacing)
        {
            case FacingBucket.Back:
                ApplySprite(SelectActionSprite(_backIdleSprite, _backWalkSprites, backWalkAnimationFps, _backAttackSprites, motionBlend >= walkAnimationThreshold, attackElapsed, GetAttackDuration(FacingBucket.Back)));
                _spriteRenderer.flipX = false;
                ApplyMotion(motionBlend, Time.time * backWalkAnimationFps * Mathf.PI, attackBlend, 0f, 1f, 0f, 0f);
                return;
            case FacingBucket.Side:
                ApplySprite(SelectActionSprite(_sideIdleSprite, _sideWalkSprites, sideWalkAnimationFps, _sideAttackSprites, motionBlend >= walkAnimationThreshold, attackElapsed, GetAttackDuration(FacingBucket.Side)));
                _spriteRenderer.flipX = _attackFlipX;
                ApplyMotion(motionBlend, Time.time * sideWalkAnimationFps * Mathf.PI, attackBlend, _attackFlipX ? -1f : 1f, 0f, 0f, 0f);
                return;
            default:
                ApplySprite(SelectActionSprite(_frontIdleSprite, _frontWalkSprites, frontWalkAnimationFps, _frontAttackSprites, motionBlend >= walkAnimationThreshold, attackElapsed, GetAttackDuration(FacingBucket.Front)));
                _spriteRenderer.flipX = false;
                ApplyMotion(motionBlend, Time.time * frontWalkAnimationFps * Mathf.PI, attackBlend, 0f, -1f, 0f, 0f);
                return;
        }
    }

    private void ApplyHitVisual(bool hasFacingData, FacingBucket facingBucket, bool flipX, float lateralFacing, float forwardFacing, float hitBlend, float hitFlashBlend)
    {
        if (!hasFacingData)
        {
            ApplySprite(_frontIdleSprite ?? _sideIdleSprite ?? _backIdleSprite);
            _spriteRenderer.flipX = false;
            ApplyMotion(0f, Time.time * frontWalkAnimationFps * Mathf.PI, 0f, 0f, 0f, hitBlend, hitFlashBlend);
            return;
        }

        switch (facingBucket)
        {
            case FacingBucket.Back:
                ApplySprite(_backIdleSprite ?? GetLoopFrame(_backWalkSprites, backWalkAnimationFps));
                _spriteRenderer.flipX = false;
                ApplyMotion(0f, Time.time * backWalkAnimationFps * Mathf.PI, 0f, 0f, forwardFacing, hitBlend, hitFlashBlend);
                return;
            case FacingBucket.Side:
                ApplySprite(_sideIdleSprite ?? GetLoopFrame(_sideWalkSprites, sideWalkAnimationFps));
                _spriteRenderer.flipX = flipX;
                ApplyMotion(0f, Time.time * sideWalkAnimationFps * Mathf.PI, 0f, lateralFacing, 0f, hitBlend, hitFlashBlend);
                return;
            default:
                ApplySprite(_frontIdleSprite ?? GetLoopFrame(_frontWalkSprites, frontWalkAnimationFps));
                _spriteRenderer.flipX = false;
                ApplyMotion(0f, Time.time * frontWalkAnimationFps * Mathf.PI, 0f, 0f, forwardFacing, hitBlend, hitFlashBlend);
                return;
        }
    }

    private Sprite SelectActionSprite(Sprite idle, Sprite[] walkSprites, float walkAnimationFps, Sprite[] attackSprites, bool isWalking, float attackElapsed, float attackDuration)
    {
        if (attackElapsed >= 0f)
        {
            Sprite attack = GetAttackFrame(attackSprites, attackElapsed, attackDuration);
            if (attack != null)
            {
                return attack;
            }
        }

        return SelectLoopSprite(idle, walkSprites, isWalking, walkAnimationFps);
    }

    private Sprite SelectLoopSprite(Sprite idle, Sprite[] walkSprites, bool isWalking, float animationFps)
    {
        if (isWalking)
        {
            Sprite walk = GetLoopFrame(walkSprites, animationFps);
            if (walk != null)
            {
                return walk;
            }
        }

        return idle ?? GetLoopFrame(walkSprites, animationFps);
    }

    private Sprite GetLoopFrame(Sprite[] sprites, float animationFps)
    {
        if (sprites == null || sprites.Length == 0)
        {
            return null;
        }

        int frameIndex = Mathf.FloorToInt(Time.time * animationFps);
        for (int attempts = 0; attempts < sprites.Length; attempts++)
        {
            Sprite sprite = sprites[(frameIndex + attempts) % sprites.Length];
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private Sprite GetAttackFrame(Sprite[] sprites, float attackElapsed, float attackDuration)
    {
        if (sprites == null || sprites.Length == 0 || attackElapsed < 0f || attackDuration <= 0.0001f)
        {
            return null;
        }

        float normalized = Mathf.Clamp01(attackElapsed / attackDuration);
        int frameIndex = Mathf.Clamp(Mathf.FloorToInt(normalized * sprites.Length), 0, sprites.Length - 1);

        for (int index = frameIndex; index >= 0; index--)
        {
            if (sprites[index] != null)
            {
                return sprites[index];
            }
        }

        for (int index = frameIndex + 1; index < sprites.Length; index++)
        {
            if (sprites[index] != null)
            {
                return sprites[index];
            }
        }

        return null;
    }

    private void ApplySprite(Sprite sprite)
    {
        if (_spriteRenderer == null || sprite == null)
        {
            return;
        }

        if (_spriteRenderer.sprite != sprite)
        {
            _spriteRenderer.sprite = sprite;
        }

        float spriteHeight = sprite.bounds.size.y;
        if (spriteHeight <= 0.0001f)
        {
            _visualRoot.localScale = Vector3.one;
            return;
        }

        _baseScale = desiredHeight / spriteHeight;
    }

    private void ApplyMotion(float moveBlend, float cycle, float attackBlend, float lateralFacing, float forwardFacing, float hitBlend, float hitFlashBlend)
    {
        if (_visualRoot == null)
        {
            return;
        }

        float bob = Mathf.Sin(cycle * 2f) * walkBobHeight * moveBlend;
        float breathe = Mathf.Sin(Time.time * 1.8f) * idleBreathAmount * (1f - moveBlend);
        float yOffset = Mathf.Max(0f, bob + breathe);

        float squash = Mathf.Abs(Mathf.Sin(cycle * 2f)) * walkSquashAmount * moveBlend;
        float attackStretch = attackStretchAmount * attackBlend;
        float hitSquash = hitSquashAmount * hitBlend;
        float scaleX = _baseScale * (1f + squash * 0.55f - attackStretch * 0.2f + hitSquash);
        float scaleY = _baseScale * (1f - squash + attackStretch - hitSquash * 0.75f);

        float zTilt = -Mathf.Sign(lateralFacing) * sideLeanAngle * moveBlend;
        float xTilt = -Mathf.Sign(forwardFacing) * forwardLeanAngle * moveBlend;
        Vector3 lungeDirection = attackBlend > 0.001f ? _attackLungeDirection : transform.forward;
        lungeDirection = Vector3.ProjectOnPlane(lungeDirection, Vector3.up);
        if (lungeDirection.sqrMagnitude > 0.0001f)
        {
            lungeDirection.Normalize();
        }
        else
        {
            lungeDirection = Vector3.forward;
        }

        Vector3 lungeOffset = lungeDirection * (attackLungeDistance * attackBlend);
        Quaternion billboardRotation = GetUprightBillboardRotation(Camera.main);
        Vector3 hitOffset = _hitRecoilDirection * (hitRecoilDistance * hitBlend);
        float hitTilt = 0f;

        if (Camera.main != null)
        {
            Vector3 cameraRight = Vector3.ProjectOnPlane(Camera.main.transform.right, Vector3.up);
            if (cameraRight.sqrMagnitude > 0.0001f && _hitRecoilDirection.sqrMagnitude > 0.0001f)
            {
                cameraRight.Normalize();
                hitTilt = Vector3.Dot(_hitRecoilDirection.normalized, cameraRight) * hitTiltAngle * hitBlend;
            }
        }

        _visualRoot.position = transform.position + localOffset + new Vector3(0f, yOffset, 0f) + lungeOffset + hitOffset;
        _visualRoot.localScale = new Vector3(scaleX, scaleY, 1f);
        _visualRoot.rotation = billboardRotation * Quaternion.Euler(xTilt, 0f, zTilt + hitTilt);
        Color attackColor = Color.Lerp(Color.white, attackTint, attackBlend * 0.6f);
        _spriteRenderer.color = Color.Lerp(attackColor, hitTint, hitFlashBlend);
    }

    private static Quaternion GetUprightBillboardRotation(Camera mainCamera)
    {
        if (mainCamera == null)
        {
            return Quaternion.identity;
        }

        Vector3 cameraForward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up);
        if (cameraForward.sqrMagnitude <= 0.0001f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(cameraForward.normalized, Vector3.up);
    }

    private float GetAttackElapsed(float attackDuration)
    {
        if (attackDuration <= 0.0001f)
        {
            return -1f;
        }

        float elapsed = Time.time - _attackStartedAt;
        if (elapsed < 0f || elapsed > attackDuration)
        {
            return -1f;
        }

        return elapsed;
    }

    private float GetAttackBlend(float attackElapsed, float attackDuration)
    {
        if (attackElapsed < 0f || attackDuration <= 0.0001f)
        {
            return 0f;
        }

        float normalized = attackElapsed / attackDuration;
        return Mathf.Sin(normalized * Mathf.PI);
    }

    private float GetHitElapsed()
    {
        if (hitVisualDuration <= 0.0001f)
        {
            return -1f;
        }

        float elapsed = Time.time - _hitStartedAt;
        if (elapsed < 0f || elapsed > hitVisualDuration)
        {
            return -1f;
        }

        return elapsed;
    }

    private float GetHitBlend(float hitElapsed)
    {
        if (hitElapsed < 0f || hitVisualDuration <= 0.0001f)
        {
            return 0f;
        }

        float normalized = hitElapsed / hitVisualDuration;
        return Mathf.Sin(normalized * Mathf.PI);
    }

    private float GetHitFlashBlend(float hitElapsed)
    {
        if (hitElapsed < 0f || hitFlashDuration <= 0.0001f)
        {
            return 0f;
        }

        float normalized = 1f - Mathf.Clamp01(hitElapsed / hitFlashDuration);
        return normalized * normalized;
    }

    private float GetAttackDuration(FacingBucket facing)
    {
        return facing switch
        {
            FacingBucket.Front => frontAttackVisualDuration,
            FacingBucket.Back => backAttackVisualDuration,
            _ => sideAttackVisualDuration
        };
    }

    private void SubscribeCombatEvents()
    {
        if (_playerCombat == null)
        {
            _playerCombat = GetComponent<PlayerCombat>();
        }

        if (_playerCombat != null)
        {
            _playerCombat.OnAttackStarted -= HandleAttackStarted;
            _playerCombat.OnAttackStarted += HandleAttackStarted;
        }
    }

    private void SubscribeHealthEvents()
    {
        if (_health == null)
        {
            _health = GetComponent<CombatantHealth>();
        }

        if (_health != null)
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnDamaged += HandleDamaged;
        }
    }

    private void UnsubscribeCombatEvents()
    {
        if (_playerCombat != null)
        {
            _playerCombat.OnAttackStarted -= HandleAttackStarted;
        }
    }

    private void UnsubscribeHealthEvents()
    {
        if (_health != null)
        {
            _health.OnDamaged -= HandleDamaged;
        }
    }

    private void HandleAttackStarted()
    {
        _attackStartedAt = Time.time;
        CacheAttackFacing();
    }

    private void HandleDamaged(CombatantHealth health, int damage)
    {
        _hitStartedAt = Time.time;
        CacheHitReactionDirection();
    }

    private void CacheAttackFacing()
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (forward.sqrMagnitude > 0.0001f)
        {
            _attackLungeDirection = forward.normalized;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            _attackFacing = FacingBucket.Front;
            _attackFlipX = false;
            return;
        }

        Vector3 cameraForward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up);
        Vector3 cameraRight = Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up);
        Vector3 facing = Vector3.ProjectOnPlane(transform.forward, Vector3.up);

        if (cameraForward.sqrMagnitude <= 0.0001f || cameraRight.sqrMagnitude <= 0.0001f || facing.sqrMagnitude <= 0.0001f)
        {
            _attackFacing = FacingBucket.Front;
            _attackFlipX = false;
            return;
        }

        cameraForward.Normalize();
        cameraRight.Normalize();
        facing.Normalize();

        float forwardDot = Vector3.Dot(facing, cameraForward);
        if (forwardDot <= -0.45f)
        {
            _attackFacing = FacingBucket.Front;
            _attackFlipX = false;
            return;
        }

        if (forwardDot >= 0.45f)
        {
            _attackFacing = FacingBucket.Back;
            _attackFlipX = false;
            return;
        }

        _attackFacing = FacingBucket.Side;
        _attackFlipX = Vector3.Dot(facing, cameraRight) < 0f;
    }

    private void CacheHitReactionDirection()
    {
        Vector3 recoilDirection = -Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        float bestDistance = 16f;
        SimpleEnemyController[] enemies = FindObjectsByType<SimpleEnemyController>(FindObjectsSortMode.None);

        for (int index = 0; index < enemies.Length; index++)
        {
            SimpleEnemyController enemy = enemies[index];
            if (enemy == null || !enemy.IsAlive || !enemy.IsEncounterEnabled)
            {
                continue;
            }

            Vector3 awayFromEnemy = transform.position - enemy.transform.position;
            awayFromEnemy.y = 0f;
            float sqrDistance = awayFromEnemy.sqrMagnitude;
            if (sqrDistance <= 0.0001f || sqrDistance > bestDistance)
            {
                continue;
            }

            bestDistance = sqrDistance;
            recoilDirection = awayFromEnemy.normalized;
        }

        recoilDirection = Vector3.ProjectOnPlane(recoilDirection, Vector3.up);
        if (recoilDirection.sqrMagnitude <= 0.0001f)
        {
            recoilDirection = Vector3.back;
        }

        _hitRecoilDirection = recoilDirection.normalized;
    }

    private static bool TryResolveFacingState(
        Vector3 cameraForward,
        Vector3 cameraRight,
        Vector3 facing,
        out FacingBucket facingBucket,
        out bool flipX,
        out float lateralFacing,
        out float forwardFacing)
    {
        facingBucket = FacingBucket.Front;
        flipX = false;
        lateralFacing = 0f;
        forwardFacing = 0f;

        if (cameraForward.sqrMagnitude <= 0.0001f || cameraRight.sqrMagnitude <= 0.0001f || facing.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        cameraForward.Normalize();
        cameraRight.Normalize();
        facing.Normalize();

        float forwardDot = Vector3.Dot(facing, cameraForward);
        if (forwardDot <= -0.45f)
        {
            facingBucket = FacingBucket.Front;
            forwardFacing = -forwardDot;
            return true;
        }

        if (forwardDot >= 0.45f)
        {
            facingBucket = FacingBucket.Back;
            forwardFacing = forwardDot;
            return true;
        }

        float rightDot = Vector3.Dot(facing, cameraRight);
        facingBucket = FacingBucket.Side;
        flipX = rightDot < 0f;
        lateralFacing = rightDot;
        return true;
    }

    private Transform CreateVisualRoot()
    {
        GameObject root = new GameObject(VisualRootName);
        root.transform.SetParent(transform, false);
        return root.transform;
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

    private void DisableLegacyMesh()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
    }

    private void RemoveLegacyVisualRoot()
    {
        Transform legacy = transform.Find("CharacterVisual");
        if (legacy == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(legacy.gameObject);
        }
        else
        {
            DestroyImmediate(legacy.gameObject);
        }
    }
}
