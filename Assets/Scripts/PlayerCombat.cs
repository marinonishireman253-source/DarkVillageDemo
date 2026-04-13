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

    [Header("VFX")]
    [SerializeField] private Color slashColor = new Color(1f, 0.9f, 0.72f, 1f);
    [SerializeField] private Color hitBurstColor = new Color(1f, 0.55f, 0.35f, 1f);

    [Header("Vitals")]
    [SerializeField] private int maxHealth = 6;
    [SerializeField] private float deathReloadDelay = 1.25f;

    public CombatantHealth Health { get; private set; }
    public float AttackCooldown => attackCooldown;
    public int AttackDamage => attackDamage;
    public bool CanAttack => !SimpleDialogueUI.IsOpen && !InventoryController.IsOpen && !AshParlorChoiceOverlay.IsVisible && !FloorSummaryPanel.IsVisible && !Health.IsDead && Time.time >= _nextAttackTime;
    public event Action OnAttackStarted;

    private float _nextAttackTime;
    private bool _reloadScheduled;

    private void Awake()
    {
        Health = GetComponent<CombatantHealth>();
        Health.Configure(maxHealth);
        Health.OnDied += HandleDeath;
    }

    private void OnDestroy()
    {
        if (Health != null)
        {
            Health.OnDied -= HandleDeath;
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
        Collider[] hits = Physics.OverlapSphere(origin, attackRadius, ~0, QueryTriggerInteraction.Collide);

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

        bestEnemy.TakeDamage(attackDamage);
        CombatVfxFactory.SpawnHitBurst(bestEnemy.transform.position + Vector3.up * 0.95f, transform.forward, hitBurstColor);
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
        Gizmos.DrawWireSphere(origin, attackRadius);
    }
}
