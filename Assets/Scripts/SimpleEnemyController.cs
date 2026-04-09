using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CombatantHealth))]
[RequireComponent(typeof(CapsuleCollider))]
public class SimpleEnemyController : MonoBehaviour
{
    [SerializeField] private string enemyName = "仪式回响";
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float moveSpeed = 2.7f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float attackRange = 1.15f;
    [SerializeField] private float attackCooldown = 1f;

    public string EnemyName => enemyName;
    public CombatantHealth Health { get; private set; }
    public bool IsAlive => Health != null && !Health.IsDead;

    public event System.Action<SimpleEnemyController> OnDefeated;

    private PlayerMover _player;
    private PlayerCombat _playerCombat;
    private Renderer _renderer;
    private float _nextAttackTime;

    private void Awake()
    {
        Health = GetComponent<CombatantHealth>();
        Health.Configure(maxHealth);
        Health.OnDied += HandleDeath;

        _renderer = GetComponentInChildren<Renderer>();
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

    private void Update()
    {
        if (!IsAlive || SimpleDialogueUI.IsOpen)
        {
            return;
        }

        if (_player == null)
        {
            _player = FindFirstObjectByType<PlayerMover>();
            _playerCombat = _player != null ? _player.GetComponent<PlayerCombat>() : null;
        }

        if (_player == null || _playerCombat == null || _playerCombat.Health.IsDead)
        {
            return;
        }

        Vector3 toPlayer = _player.transform.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        float distance = toPlayer.magnitude;
        if (distance > attackRange)
        {
            transform.position += toPlayer.normalized * (moveSpeed * Time.deltaTime);
            return;
        }

        if (Time.time < _nextAttackTime)
        {
            return;
        }

        _nextAttackTime = Time.time + attackCooldown;
        _playerCombat.Health.TakeDamage(contactDamage);
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
        colliderComponent.center = new Vector3(0f, 1f, 0f);
        colliderComponent.height = 2f;
        colliderComponent.radius = 0.38f;
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
