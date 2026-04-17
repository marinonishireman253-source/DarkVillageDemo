using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CombatantHealth))]
[RequireComponent(typeof(CapsuleCollider))]
public class SimpleEnemyController : MonoBehaviour
{
    public enum EnemyState
    {
        Inactive,
        Idle,
        Chasing,
        Attacking,
        Recovering,
        Dead
    }

    [SerializeField] private string enemyName = "仪式回响";
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float moveSpeed = 2.15f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float attackRange = 1.15f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackAnimationDuration = 0.42f;
    [SerializeField] private float attackHitDelay = 0.16f;
    [SerializeField] private float attackRecoveryDuration = 0.28f;

    public string EnemyName => enemyName;
    public CombatantHealth Health { get; private set; }
    public bool IsAlive => CurrentState != EnemyState.Dead && Health != null && !Health.IsDead;
    public bool IsMoving => CurrentState == EnemyState.Chasing;
    public bool IsEncounterEnabled => _encounterEnabled;
    public float LastAttackStartedAt => _lastAttackStartedAt;
    public float AttackAnimationDuration => attackAnimationDuration;
    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

    public event System.Action<SimpleEnemyController> OnDefeated;
    public event System.Action<SimpleEnemyController, EnemyState> OnStateChanged;

    private PlayerMover _player;
    private PlayerCombat _playerCombat;
    private Renderer _renderer;
    private float _nextAttackTime;
    private float _lastAttackStartedAt = float.NegativeInfinity;
    private float _moveSpeedMultiplier = 1f;
    private float _attackRangeMultiplier = 1f;
    private float _attackCooldownMultiplier = 1f;
    private float _lightZoneMoveSpeedMultiplier = 1f;
    private float _lightZoneAttackCooldownMultiplier = 1f;
    private bool _encounterEnabled = true;
    private bool _pendingAttackDamage;
    private float _attackDamageAt;
    private float _attackRecoverUntil;
    private UiStateCoordinator _stateCoordinator;
    private UiStateCoordinator.UiMode _currentUiMode = UiStateCoordinator.UiMode.Exploration;

    private void Awake()
    {
        Health = GetComponent<CombatantHealth>();
        Health.Configure(maxHealth);
        Health.OnDied += HandleDeath;

        _renderer = GetComponentInChildren<SpriteRenderer>();
        if (_renderer == null)
        {
            _renderer = GetComponentInChildren<Renderer>();
        }

        EnsureColliderDefaults();
    }

    private void OnEnable()
    {
        PlayerMover.OnLocalInstanceChanged += HandlePlayerChanged;
        PlayerCombat.OnLocalInstanceChanged += HandlePlayerCombatChanged;
        UiStateCoordinator.OnInstanceChanged += HandleStateCoordinatorChanged;

        BindPlayer(PlayerMover.LocalInstance);
        BindPlayerCombat(PlayerCombat.LocalInstance);
        BindStateCoordinator(UiStateCoordinator.Instance);
        SyncLightZoneEffects();
    }

    private void OnDisable()
    {
        PlayerMover.OnLocalInstanceChanged -= HandlePlayerChanged;
        PlayerCombat.OnLocalInstanceChanged -= HandlePlayerCombatChanged;
        UiStateCoordinator.OnInstanceChanged -= HandleStateCoordinatorChanged;

        BindPlayer(null);
        BindPlayerCombat(null);
        BindStateCoordinator(null);
    }

    private void OnDestroy()
    {
        if (Health != null)
        {
            Health.OnDied -= HandleDeath;
        }
    }

    public void Configure(string displayName, int healthPoints, int damage)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            enemyName = displayName.Trim();
        }

        maxHealth = Mathf.Max(1, healthPoints);
        contactDamage = Mathf.Max(1, damage);

        if (Health != null)
        {
            Health.Configure(maxHealth);
        }
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive)
        {
            return;
        }

        Health.TakeDamage(amount);
        FlashDamageColor();
    }

    public void SetEncounterProfile(float moveSpeedMultiplier, float attackRangeMultiplier, float attackCooldownMultiplier)
    {
        _moveSpeedMultiplier = Mathf.Clamp(moveSpeedMultiplier, 0.3f, 3f);
        _attackRangeMultiplier = Mathf.Clamp(attackRangeMultiplier, 0.5f, 2f);
        _attackCooldownMultiplier = Mathf.Clamp(attackCooldownMultiplier, 0.35f, 3f);
    }

    public void SetLightZoneMoveSpeedMultiplier(float multiplier)
    {
        _lightZoneMoveSpeedMultiplier = Mathf.Clamp(multiplier, 0.4f, 2f);
    }

    public void SetLightZoneAttackCooldownMultiplier(float multiplier)
    {
        float nextMultiplier = Mathf.Clamp(multiplier, 0.35f, 2f);
        if (Mathf.Approximately(_lightZoneAttackCooldownMultiplier, nextMultiplier))
        {
            return;
        }

        float previousMultiplier = _lightZoneAttackCooldownMultiplier;
        _lightZoneAttackCooldownMultiplier = nextMultiplier;
        RecalculatePendingAttackWindow(previousMultiplier);
    }

    public void ResetLightZoneMultipliers()
    {
        SetLightZoneMoveSpeedMultiplier(1f);
        SetLightZoneAttackCooldownMultiplier(1f);
    }

    public void SetEncounterEnabled(bool enabled)
    {
        _encounterEnabled = enabled;
        if (!enabled)
        {
            TransitionToState(EnemyState.Inactive);
            return;
        }

        if (CurrentState == EnemyState.Inactive)
        {
            TransitionToState(EnemyState.Idle);
        }
    }

    private void Update()
    {
        if (!IsAlive)
        {
            return;
        }

        if (!_encounterEnabled || ShouldSuspendBehavior())
        {
            TransitionToState(EnemyState.Inactive);
            return;
        }

        if (_player == null || _playerCombat == null || _playerCombat.Health == null || _playerCombat.Health.IsDead)
        {
            TransitionToState(EnemyState.Idle);
            return;
        }

        Vector3 toPlayer = _player.transform.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude <= 0.001f)
        {
            TransitionToState(EnemyState.Idle);
            return;
        }

        FaceTowards(toPlayer.normalized);

        float distance = toPlayer.magnitude;
        float currentAttackRange = attackRange * _attackRangeMultiplier;

        switch (CurrentState)
        {
            case EnemyState.Inactive:
            case EnemyState.Idle:
                ResolveIdle(distance, currentAttackRange, toPlayer.normalized);
                break;

            case EnemyState.Chasing:
                ResolveChasing(distance, currentAttackRange, toPlayer.normalized);
                break;

            case EnemyState.Attacking:
                ResolveAttacking(distance, currentAttackRange);
                break;

            case EnemyState.Recovering:
                ResolveRecovering(distance, currentAttackRange, toPlayer.normalized);
                break;

            case EnemyState.Dead:
                break;
        }
    }

    private void HandlePlayerChanged(PlayerMover player)
    {
        BindPlayer(player);
    }

    private void HandlePlayerCombatChanged(PlayerCombat playerCombat)
    {
        BindPlayerCombat(playerCombat);
    }

    private void HandleStateCoordinatorChanged(UiStateCoordinator stateCoordinator)
    {
        BindStateCoordinator(stateCoordinator);
    }

    private void HandleUiModeChanged(UiStateCoordinator.UiMode mode)
    {
        _currentUiMode = mode;
    }

    private void BindPlayer(PlayerMover player)
    {
        _player = player;
    }

    private void BindPlayerCombat(PlayerCombat playerCombat)
    {
        _playerCombat = playerCombat;
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

    private bool ShouldSuspendBehavior()
    {
        return UiStateCoordinator.PausesEnemyBehaviorForMode(_currentUiMode);
    }

    private void ResolveIdle(float distance, float currentAttackRange, Vector3 directionToPlayer)
    {
        if (distance > currentAttackRange)
        {
            TransitionToState(EnemyState.Chasing);
            ResolveChasing(distance, currentAttackRange, directionToPlayer);
            return;
        }

        if (Time.time < _nextAttackTime)
        {
            TransitionToState(EnemyState.Idle);
            return;
        }

        StartAttack();
    }

    private void ResolveChasing(float distance, float currentAttackRange, Vector3 directionToPlayer)
    {
        if (distance <= currentAttackRange)
        {
            TransitionToState(EnemyState.Idle);
            ResolveIdle(distance, currentAttackRange, directionToPlayer);
            return;
        }

        TransitionToState(EnemyState.Chasing);
        float currentMoveSpeed = moveSpeed * _moveSpeedMultiplier * _lightZoneMoveSpeedMultiplier;
        transform.position += directionToPlayer * (currentMoveSpeed * Time.deltaTime);
    }

    private void ResolveAttacking(float distance, float currentAttackRange)
    {
        TransitionToState(EnemyState.Attacking);

        if (_pendingAttackDamage && Time.time >= _attackDamageAt)
        {
            _pendingAttackDamage = false;

            if (_playerCombat != null && distance <= currentAttackRange + 0.08f)
            {
                _playerCombat.ReceiveHit(contactDamage, transform.position);
            }
        }

        if (Time.time < _attackRecoverUntil)
        {
            return;
        }

        TransitionToState(EnemyState.Recovering);
    }

    private void ResolveRecovering(float distance, float currentAttackRange, Vector3 directionToPlayer)
    {
        if (Time.time < _nextAttackTime)
        {
            return;
        }

        TransitionToState(EnemyState.Idle);
        ResolveIdle(distance, currentAttackRange, directionToPlayer);
    }

    private void FaceTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    private void StartAttack()
    {
        float animationDuration = Mathf.Max(0.01f, attackAnimationDuration);
        float hitDelay = Mathf.Clamp(attackHitDelay, 0.01f, animationDuration);
        float recoveryDuration = Mathf.Max(0f, attackRecoveryDuration);

        _pendingAttackDamage = true;
        _lastAttackStartedAt = Time.time;
        _attackDamageAt = Time.time + hitDelay;
        _attackRecoverUntil = Time.time + animationDuration + recoveryDuration;
        _nextAttackTime = _attackRecoverUntil + attackCooldown * _attackCooldownMultiplier * _lightZoneAttackCooldownMultiplier;
        TransitionToState(EnemyState.Attacking);
    }

    private void RecalculatePendingAttackWindow(float previousLightZoneMultiplier)
    {
        if (_nextAttackTime <= Time.time)
        {
            return;
        }

        float cooldownAnchor = Mathf.Max(Time.time, _attackRecoverUntil);
        float previousFullCooldown = attackCooldown * _attackCooldownMultiplier * Mathf.Max(0.01f, previousLightZoneMultiplier);
        float nextFullCooldown = attackCooldown * _attackCooldownMultiplier * Mathf.Max(0.01f, _lightZoneAttackCooldownMultiplier);

        if (previousFullCooldown <= 0.0001f)
        {
            _nextAttackTime = cooldownAnchor + nextFullCooldown;
            return;
        }

        float remainingCooldown = Mathf.Max(0f, _nextAttackTime - cooldownAnchor);
        float remainingRatio = Mathf.Clamp01(remainingCooldown / previousFullCooldown);
        _nextAttackTime = cooldownAnchor + nextFullCooldown * remainingRatio;
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

    private void HandleDeath(CombatantHealth health)
    {
        TransitionToState(EnemyState.Dead);

        foreach (Collider colliderComponent in GetComponentsInChildren<Collider>())
        {
            colliderComponent.enabled = false;
        }

        OnDefeated?.Invoke(this);
        StartCoroutine(FadeOutAndDestroy());
    }

    private void TransitionToState(EnemyState nextState)
    {
        if (CurrentState == nextState)
        {
            return;
        }

        CurrentState = nextState;
        OnStateChanged?.Invoke(this, CurrentState);
    }

    private void EnsureColliderDefaults()
    {
        CapsuleCollider colliderComponent = GetComponent<CapsuleCollider>();
        colliderComponent.center = new Vector3(0f, 0.58f, 0f);
        colliderComponent.height = 1.16f;
        colliderComponent.radius = 0.24f;
    }

    private void FlashDamageColor()
    {
        if (_renderer == null)
        {
            return;
        }

        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        _renderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", new Color(0.92f, 0.34f, 0.34f));
        propertyBlock.SetColor("_Color", new Color(0.92f, 0.34f, 0.34f));
        _renderer.SetPropertyBlock(propertyBlock);
    }

    private IEnumerator FadeOutAndDestroy()
    {
        if (_renderer != null)
        {
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", new Color(0.22f, 0.06f, 0.06f));
            propertyBlock.SetColor("_Color", new Color(0.22f, 0.06f, 0.06f));
            _renderer.SetPropertyBlock(propertyBlock);
        }

        yield return new WaitForSeconds(0.45f);
        Destroy(gameObject);
    }
}
