using System.Collections.Generic;
using System.Text;

public static class InventoryItemCatalog
{
    public readonly struct ItemViewData
    {
        public ItemViewData(string itemId, string displayName, string description, string category)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Description = description;
            Category = category;
        }

        public string ItemId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string Category { get; }
    }

    private readonly struct ItemDefinition
    {
        public ItemDefinition(string displayName, string description, string category)
        {
            DisplayName = displayName;
            Description = description;
            Category = category;
        }

        public string DisplayName { get; }
        public string Description { get; }
        public string Category { get; }
    }

    private static readonly Dictionary<string, ItemDefinition> s_Definitions = new Dictionary<string, ItemDefinition>();

    public static void RegisterDefinition(string itemId, string displayName, string description, string category)
    {
        string normalizedItemId = NormalizeItemId(itemId);
        if (string.IsNullOrEmpty(normalizedItemId))
        {
            return;
        }

        s_Definitions[normalizedItemId] = new ItemDefinition(
            string.IsNullOrWhiteSpace(displayName) ? FormatDisplayName(normalizedItemId) : displayName.Trim(),
            string.IsNullOrWhiteSpace(description) ? "这件物品还没有留下更多记录。" : description.Trim(),
            string.IsNullOrWhiteSpace(category) ? "杂项" : category.Trim());
    }

    public static ItemViewData Resolve(string itemId)
    {
        string normalizedItemId = NormalizeItemId(itemId);
        if (string.IsNullOrEmpty(normalizedItemId))
        {
            return new ItemViewData(string.Empty, "未命名物品", "这件物品没有可用数据。", "杂项");
        }

        if (s_Definitions.TryGetValue(normalizedItemId, out ItemDefinition definition))
        {
            return new ItemViewData(
                normalizedItemId,
                definition.DisplayName,
                definition.Description,
                definition.Category);
        }

        return new ItemViewData(
            normalizedItemId,
            FormatDisplayName(normalizedItemId),
            "你已经拿到了这件物品，但它还没有编入背包说明。",
            "杂项");
    }

    public static List<ItemViewData> GetCollectedItems()
    {
        string[] snapshot = ChapterState.GetCollectedItemsSnapshot();
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
