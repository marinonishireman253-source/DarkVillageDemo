using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CombatantHealth))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackCooldown = 0.45f;
    [SerializeField] private float attackRadius = 1.35f;
    [SerializeField] private float attackForwardOffset = 1.1f;

    [Header("Equipment")]
    [SerializeField] private WeaponData defaultWeapon;
    [SerializeField] private Vector3 weaponMountLocalPosition = new Vector3(0.28f, 1.02f, 0.16f);
    [SerializeField] private Vector3 weaponMountLocalEulerAngles = new Vector3(0f, 20f, -12f);
    [SerializeField] private Vector3 weaponMountLocalScale = Vector3.one;

    [Header("VFX")]
    [SerializeField] private Color slashColor = new Color(1f, 0.9f, 0.72f, 1f);
    [SerializeField] private Color hitBurstColor = new Color(1f, 0.55f, 0.35f, 1f);

    [Header("Vitals")]
    [SerializeField] private int maxHealth = 6;
    [SerializeField] private float deathReloadDelay = 1.25f;

    [Header("Hit Response")]
    [SerializeField] private float hitKnockbackForce = 4.4f;
    [SerializeField] private float hitControlLockDuration = 0.18f;

    public static PlayerCombat LocalInstance { get; private set; }
    public static event Action<PlayerCombat> OnLocalInstanceChanged;
    public static event Action<WeaponData> OnWeaponEquipped;

    public CombatantHealth Health { get; private set; }
    public float AttackCooldown => attackCooldown;
    public int AttackDamage => EquippedWeapon != null ? EquippedWeapon.AttackPower : attackDamage;
    public float AttackRange => EquippedWeapon != null ? EquippedWeapon.AttackRange : attackRadius;
    public WeaponData EquippedWeapon { get; private set; }
    public bool CanAttack => !UiStateCoordinator.BlocksPlayerActionsForMode(_currentUiMode) && !Health.IsDead && Time.time >= _nextAttackTime;
    public event Action OnAttackStarted;

    private float _nextAttackTime;
    private bool _reloadScheduled;
    private UiStateCoordinator.UiMode _currentUiMode = UiStateCoordinator.UiMode.Exploration;
    private UiStateCoordinator _stateCoordinator;
    private Transform _weaponMount;
    private GameObject _equippedWeaponVisual;
    private PlayerMover _playerMover;

    private void Awake()
    {
        _playerMover = GetComponent<PlayerMover>();
        Health = GetComponent<CombatantHealth>();
        Health.Configure(maxHealth);
        Health.OnDied += HandleDeath;
        EquippedWeapon = defaultWeapon;
        EnsureWeaponMount();
        SyncEquippedWeaponVisual();

        LocalInstance = this;
        OnLocalInstanceChanged?.Invoke(this);
    }

    private void OnEnable()
    {
        InventoryController.OnWeaponEquipRequested += HandleWeaponEquipRequested;
        UiStateCoordinator.OnInstanceChanged += HandleStateCoordinatorChanged;
        BindStateCoordinator(UiStateCoordinator.Instance);
        SyncEquippedWeaponVisual();
    }

    private void OnDisable()
    {
        InventoryController.OnWeaponEquipRequested -= HandleWeaponEquipRequested;
        UiStateCoordinator.OnInstanceChanged -= HandleStateCoordinatorChanged;
        BindStateCoordinator(null);
    }

    private void OnDestroy()
    {
        if (Health != null)
        {
            Health.OnDied -= HandleDeath;
        }

        if (LocalInstance == this)
        {
            LocalInstance = null;
            OnLocalInstanceChanged?.Invoke(null);
        }

        ClearEquippedWeaponVisual();
    }

    private void HandleStateCoordinatorChanged(UiStateCoordinator stateCoordinator)
    {
        BindStateCoordinator(stateCoordinator);
    }

    private void HandleUiModeChanged(UiStateCoordinator.UiMode mode)
    {
        _currentUiMode = mode;
    }

    private void HandleWeaponEquipRequested(WeaponData weapon)
    {
        EquipWeapon(weapon);
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

    private void Update()
    {
        if (!CanAttack || !WasAttackPressedThisFrame())
        {
            return;
        }

        TryAttack();
    }

    private void TryAttack()
    {
        _nextAttackTime = Time.time + attackCooldown;
        OnAttackStarted?.Invoke();
        CombatVfxFactory.SpawnSlash(transform, slashColor);

        Vector3 origin = transform.position + Vector3.up * 0.9f + transform.forward * attackForwardOffset;
        Collider[] hits = Physics.OverlapSphere(origin, AttackRange, ~0, QueryTriggerInteraction.Collide);

        SimpleEnemyController bestEnemy = null;
        float bestScore = float.NegativeInfinity;

        foreach (Collider hit in hits)
        {
            SimpleEnemyController enemy = hit.GetComponentInParent<SimpleEnemyController>();
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;

            if (toEnemy.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            float facing = Vector3.Dot(transform.forward, toEnemy.normalized);
            float score = facing - toEnemy.sqrMagnitude * 0.08f;

            if (score > bestScore)
            {
                bestScore = score;
                bestEnemy = enemy;
            }
        }

        if (bestEnemy == null)
        {
            return;
        }

        bestEnemy.TakeDamage(AttackDamage);
        CombatVfxFactory.SpawnHitBurst(bestEnemy.transform.position + Vector3.up * 0.95f, transform.forward, hitBurstColor);
    }

    public void EquipWeapon(WeaponData weapon)
    {
        WeaponData nextWeapon = weapon != null ? weapon : defaultWeapon;
        if (EquippedWeapon == nextWeapon)
        {
            return;
        }

        EquippedWeapon = nextWeapon;
        SyncEquippedWeaponVisual();
        OnWeaponEquipped?.Invoke(EquippedWeapon);
    }

    public void ReceiveHit(int damage, Vector3 sourcePosition)
    {
        if (Health == null || Health.IsDead)
        {
            return;
        }

        Health.TakeDamage(damage);
        ApplyHitKnockback(sourcePosition);
    }

    private void EnsureWeaponMount()
    {
        if (_weaponMount == null)
        {
            Transform existingMount = transform.Find("WeaponMount");
            if (existingMount != null)
            {
                _weaponMount = existingMount;
            }
            else
            {
                GameObject mount = new GameObject("WeaponMount");
                _weaponMount = mount.transform;
                _weaponMount.SetParent(transform, false);
            }
        }

        _weaponMount.localPosition = weaponMountLocalPosition;
        _weaponMount.localRotation = Quaternion.Euler(weaponMountLocalEulerAngles);
        _weaponMount.localScale = weaponMountLocalScale;
    }

    private void SyncEquippedWeaponVisual()
    {
        EnsureWeaponMount();
        ClearEquippedWeaponVisual();

        if (EquippedWeapon == null || EquippedWeapon.WeaponPrefab == null || _weaponMount == null)
        {
            return;
        }

        _equippedWeaponVisual = Instantiate(EquippedWeapon.WeaponPrefab, _weaponMount);
        _equippedWeaponVisual.name = $"{EquippedWeapon.WeaponName} Visual";
        _equippedWeaponVisual.transform.localPosition = Vector3.zero;
        _equippedWeaponVisual.transform.localRotation = Quaternion.identity;
        _equippedWeaponVisual.transform.localScale = Vector3.one;
    }

    private void ClearEquippedWeaponVisual()
    {
        if (_equippedWeaponVisual == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_equippedWeaponVisual);
        }
        else
        {
            DestroyImmediate(_equippedWeaponVisual);
        }

        _equippedWeaponVisual = null;
    }

    private void ApplyHitKnockback(Vector3 sourcePosition)
    {
        if (_playerMover == null)
        {
            _playerMover = GetComponent<PlayerMover>();
        }

        if (_playerMover == null)
        {
            return;
        }

        Vector3 knockbackDirection = transform.position - sourcePosition;
        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude <= 0.0001f)
        {
            knockbackDirection = -transform.forward;
            knockbackDirection.y = 0f;
        }

        if (knockbackDirection.sqrMagnitude <= 0.0001f)
        {
            knockbackDirection = Vector3.left;
        }

        _playerMover.AddImpulse(knockbackDirection.normalized * hitKnockbackForce);
        _playerMover.LockControls(hitControlLockDuration);
    }

    private bool WasAttackPressedThisFrame()
    {
        if (Keyboard.current != null
            && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.jKey.wasPressedThisFrame))
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        return Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
    }

    private void HandleDeath(CombatantHealth health)
    {
        if (_reloadScheduled)
        {
            return;
        }

        _reloadScheduled = true;
        StartCoroutine(ReloadAfterDeath());
    }

    private IEnumerator ReloadAfterDeath()
    {
        if (SimpleDialogueUI.Instance != null && !SimpleDialogueUI.IsOpen)
        {
            SimpleDialogueUI.Instance.Show("伊尔萨恩", "你被异化的残响压倒了。重新整理呼吸，再试一次。");
        }

        yield return new WaitForSeconds(deathReloadDelay);
        SceneLoader.ReloadCurrent();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 origin = transform.position + Vector3.up * 0.9f + transform.forward * attackForwardOffset;
        Gizmos.DrawWireSphere(origin, AttackRange);
    }
}
