using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statText;

    private Color _filledIconColor = Color.white;
    private Color _emptyIconColor = new Color(1f, 1f, 1f, 0.08f);

    public void Configure(Image slotIcon, TMP_Text slotNameText, TMP_Text slotStatText, Color filledIconColor, Color emptyIconColor)
    {
        iconImage = slotIcon;
        nameText = slotNameText;
        statText = slotStatText;
        _filledIconColor = filledIconColor;
        _emptyIconColor = emptyIconColor;
        Refresh(PlayerCombat.LocalInstance != null ? PlayerCombat.LocalInstance.EquippedWeapon : null);
    }

    private void OnEnable()
    {
        PlayerCombat.OnLocalInstanceChanged += HandleLocalPlayerChanged;
        PlayerCombat.OnWeaponEquipped += HandleWeaponEquipped;
        Refresh(PlayerCombat.LocalInstance != null ? PlayerCombat.LocalInstance.EquippedWeapon : null);
    }

    private void OnDisable()
    {
        PlayerCombat.OnLocalInstanceChanged -= HandleLocalPlayerChanged;
        PlayerCombat.OnWeaponEquipped -= HandleWeaponEquipped;
    }

    private void HandleLocalPlayerChanged(PlayerCombat playerCombat)
    {
        Refresh(playerCombat != null ? playerCombat.EquippedWeapon : null);
    }

    private void HandleWeaponEquipped(WeaponData weapon)
    {
        Refresh(weapon);
    }

    private void Refresh(WeaponData weapon)
    {
        if (nameText != null)
        {
            nameText.text = weapon != null ? weapon.WeaponName : "当前武器：徒手";
        }

        if (statText != null)
        {
            statText.text = weapon != null
                ? $"攻击力 {weapon.AttackPower}    范围 {weapon.AttackRange:0.0}"
                : "未装备可记录武器";
        }

        if (iconImage == null)
        {
            return;
        }

        if (weapon != null && weapon.Icon != null)
        {
            UiFactory.ApplySprite(iconImage, weapon.Icon, Image.Type.Simple, true);
            iconImage.color = _filledIconColor;
            return;
        }

        UiFactory.ApplySprite(iconImage, null);
        iconImage.color = _emptyIconColor;
    }
}
