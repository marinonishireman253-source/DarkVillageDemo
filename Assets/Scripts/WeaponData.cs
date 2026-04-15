using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "DarkVillage/Inventory/Weapon Data")]
public sealed class WeaponData : ScriptableObject
{
    [SerializeField] private string weaponId = "unarmed";
    [SerializeField] private string weaponName = "无名武器";
    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private int attackPower = 1;
    [SerializeField] private float attackRange = 1.35f;
    [TextArea(2, 4)]
    [SerializeField] private string description = "这把武器还没有留下更多记录。";

    public string WeaponId => string.IsNullOrWhiteSpace(weaponId) ? name : weaponId.Trim();
    public string WeaponName => string.IsNullOrWhiteSpace(weaponName) ? name : weaponName.Trim();
    public Sprite Icon => icon;
    public GameObject WeaponPrefab => weaponPrefab;
    public int AttackPower => Mathf.Max(1, attackPower);
    public float AttackRange => Mathf.Max(0.5f, attackRange);
    public string Description => string.IsNullOrWhiteSpace(description) ? "这把武器还没有留下更多记录。" : description.Trim();
}
