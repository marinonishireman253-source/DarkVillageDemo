using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CombatantHealth))]
[RequireComponent(typeof(CapsuleCollider))]
public class SimpleEnemyController : MonoBehaviour
{
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
    public bool IsAlive => Health != null && !Health.IsDead;
    public bool IsMoving => _isMoving;
    public bool IsEncounterEnabled => _encounterEnabled;
    public float LastAttackStartedAt => _lastAttackStartedAt;
    public float AttackAnimationDuration => attackAnimationDuration;

    public event System.Action<SimpleEnemyController> OnDefeated;

    private PlayerMover _player;
    private PlayerCombat _playerCombat;
    private Renderer _renderer;
    private float _nextAttackTime;
    private bool _isMoving;
    private float _lastAttackStartedAt = float.NegativeInfinity;
    private float _moveSpeedMultiplier = 1f;
    private float _attackRangeMultiplier = 1f;
    private float _attackCooldownMultiplier = 1f;
    private bool _encounterEnabled = true;
    private bool _attackInProgress;
    private bool _pendingAttackDamage;
    private float _attackDamageAt;
    private float _attackRecoverUntil;

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

    public void SetEncounterEnabled(bool enabled)
    {
        _encounterEnabled = enabled;

        if (!enabled)
        {
            _isMoving = false;
        }
    }

    private void Update()
    {
        if (!IsAlive || !_encounterEnabled || SimpleDialogueUI.IsOpen || InventoryController.IsOpen)
        {
            _isMoving = false;
            return;
        }

        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerMover>();
            _playerCombat = _player != null ? _player.GetComponent<PlayerCombat>() : null;
        }

        if (_player == null || _playerCombat == null || _playerCombat.Health.IsDead)
        {
            _isMoving = false;
            return;
        }

        Vector3 toPlayer = _player.transform.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude <= 0.001f)
        {
            _isMoving = false;
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        float distance = toPlayer.magnitude;
        float currentAttackRange = attackRange * _attackRangeMultiplier;

        if (_attackInProgress)
        {
            _isMoving = false;

            if (_pendingAttackDamage && Time.time >= _attackDamageAt)
            {
                _pendingAttackDamage = false;

                if (distance <= currentAttackRange + 0.08f)
                {
                    _playerCombat.Health.TakeDamage(contactDamage);
                }
            }

            if (Time.time < _attackRecoverUntil)
            {
                return;
            }

            _attackInProgress = false;
        }

        if (distance > currentAttackRange)
        {
            _isMoving = true;
            float currentMoveSpeed = moveSpeed * _moveSpeedMultiplier;
            transform.position += toPlayer.normalized * (currentMoveSpeed * Time.deltaTime);
            return;
        }

        _isMoving = false;

        if (Time.time < _nextAttackTime)
        {
            return;
        }

        StartAttack();
    }

    private void StartAttack()
    {
        float animationDuration = Mathf.Max(0.01f, attackAnimationDuration);
        float hitDelay = Mathf.Clamp(attackHitDelay, 0.01f, animationDuration);
        float recoveryDuration = Mathf.Max(0f, attackRecoveryDuration);

        _isMoving = false;
        _attackInProgress = true;
        _pendingAttackDamage = true;
        _lastAttackStartedAt = Time.time;
        _attackDamageAt = Time.time + hitDelay;
        _attackRecoverUntil = Time.time + animationDuration + recoveryDuration;
        _nextAttackTime = _attackRecoverUntil + attackCooldown * _attackCooldownMultiplier;
    }

    private void HandleDeath(CombatantHealth health)
    {
        foreach (Collider colliderComponent in GetComponentsInChildren<Collider>())
        {
            colliderComponent.enabled = false;
        }

        OnDefeated?.Invoke(this);
        StartCoroutine(FadeOutAndDestroy());
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
