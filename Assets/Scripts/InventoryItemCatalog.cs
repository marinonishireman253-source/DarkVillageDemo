using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class InventoryItemCatalog
{
    public readonly struct ItemViewData
    {
        public ItemViewData(string itemId, string displayName, string description, string category, Sprite icon, WeaponData weapon)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Description = description;
            Category = category;
            Icon = icon;
            Weapon = weapon;
        }

        public string ItemId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string Category { get; }
        public Sprite Icon { get; }
        public WeaponData Weapon { get; }
    }

    private readonly struct ItemDefinition
    {
        public ItemDefinition(string displayName, string description, string category, Sprite icon, WeaponData weapon)
        {
            DisplayName = displayName;
            Description = description;
            Category = category;
            Icon = icon;
            Weapon = weapon;
        }

        public string DisplayName { get; }
        public string Description { get; }
        public string Category { get; }
        public Sprite Icon { get; }
        public WeaponData Weapon { get; }
    }

    private static readonly Dictionary<string, ItemDefinition> s_Definitions = new Dictionary<string, ItemDefinition>();

    public static void RegisterDefinition(string itemId, string displayName, string description, string category, Sprite icon = null, WeaponData weapon = null)
    {
        string normalizedItemId = NormalizeItemId(itemId);
        if (string.IsNullOrEmpty(normalizedItemId))
        {
            return;
        }

        string resolvedDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? weapon != null ? weapon.WeaponName : FormatDisplayName(normalizedItemId)
            : displayName.Trim();
        string resolvedDescription = string.IsNullOrWhiteSpace(description)
            ? weapon != null ? weapon.Description : "这件物品还没有留下更多记录。"
            : description.Trim();
        string resolvedCategory = string.IsNullOrWhiteSpace(category)
            ? weapon != null ? "武器" : "杂项"
            : category.Trim();

        s_Definitions[normalizedItemId] = new ItemDefinition(
            resolvedDisplayName,
            resolvedDescription,
            resolvedCategory,
            icon != null ? icon : weapon != null ? weapon.Icon : null,
            weapon);
    }

    public static ItemViewData Resolve(string itemId)
    {
        string normalizedItemId = NormalizeItemId(itemId);
        if (string.IsNullOrEmpty(normalizedItemId))
        {
            return new ItemViewData(string.Empty, "未命名物品", "这件物品没有可用数据。", "杂项", null, null);
        }

        if (s_Definitions.TryGetValue(normalizedItemId, out ItemDefinition definition))
        {
            return new ItemViewData(
                normalizedItemId,
                definition.DisplayName,
                definition.Description,
                definition.Category,
                definition.Icon,
                definition.Weapon);
        }

        return new ItemViewData(
            normalizedItemId,
            FormatDisplayName(normalizedItemId),
            "你已经拿到了这件物品，但它还没有编入背包说明。",
            "杂项",
            null,
            null);
    }

    public static List<ItemViewData> GetCollectedItems()
    {
        string[] snapshot = GameStateHub.Instance != null
            ? GameStateHub.Instance.GetCollectedItemSnapshot()
            : System.Array.Empty<string>();
        List<ItemViewData> items = new List<ItemViewData>(snapshot.Length);
        for (int i = 0; i < snapshot.Length; i++)
        {
            items.Add(Resolve(snapshot[i]));
        }

        return items;
    }

    private static string NormalizeItemId(string itemId)
    {
        return string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
    }

    private static string FormatDisplayName(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return "未命名物品";
        }

        StringBuilder builder = new StringBuilder(itemId.Length);
        bool capitalizeNext = true;

        for (int i = 0; i < itemId.Length; i++)
        {
            char current = itemId[i];
            if (current == '_' || current == '-')
            {
                if (builder.Length > 0 && builder[builder.Length - 1] != ' ')
                {
                    builder.Append(' ');
                }

                capitalizeNext = true;
                continue;
            }

            builder.Append(capitalizeNext ? char.ToUpperInvariant(current) : current);
            capitalizeNext = false;
        }

        return builder.ToString().Trim();
    }
}
