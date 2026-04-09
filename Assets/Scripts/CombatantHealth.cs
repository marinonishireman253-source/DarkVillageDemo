using UnityEngine;

public class CombatantHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

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
        SaveSystem.MarkDirty();
    }

    public void RestoreTo(int currentHealth)
    {
        CurrentHealth = Mathf.Clamp(currentHealth, 1, maxHealth);
        SaveSystem.MarkDirty();
    }

    public bool TakeDamage(int amount)
    {
        if (IsDead)
        {
            return false;
        }

        int damage = Mathf.Max(1, amount);
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        SaveSystem.MarkDirty();
        OnDamaged?.Invoke(this, damage);

        if (CurrentHealth > 0)
        {
            return false;
        }

        OnDied?.Invoke(this);
        return true;
    }
}
