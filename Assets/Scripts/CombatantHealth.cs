using UnityEngine;

public class CombatantHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    public event System.Action<CombatantHealth> OnHealthChanged;
    public event System.Action<CombatantHealth, int> OnDamaged;
    public event System.Action<CombatantHealth> OnDied;

    private void Awake()
    {
        RestoreFull();
    }

    public void Configure(int health)
    {
        maxHealth = Mathf.Max(1, health);
        RestoreFull();
    }

    public void RestoreFull()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
        GameStateHub.MarkRuntimeStateDirty();
        OnHealthChanged?.Invoke(this);
    }

    public void RestoreTo(int currentHealth)
    {
        CurrentHealth = Mathf.Clamp(currentHealth, 1, maxHealth);
        GameStateHub.MarkRuntimeStateDirty();
        OnHealthChanged?.Invoke(this);
    }

    public bool TakeDamage(int amount)
    {
        if (IsDead)
        {
            return false;
        }

        int damage = Mathf.Max(1, amount);
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        GameStateHub.MarkRuntimeStateDirty();
        OnHealthChanged?.Invoke(this);
        OnDamaged?.Invoke(this, damage);

        if (CurrentHealth > 0)
        {
            return false;
        }

        OnDied?.Invoke(this);
        return true;
    }
}
