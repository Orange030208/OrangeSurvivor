using UnityEngine;

public readonly struct UpgradeCardOptionSnapshot
{
    public readonly string CardId;
    public readonly string Title;
    public readonly Sprite Icon;
    public readonly string Description;
    public readonly UpgradeCardRarity Rarity;
    public readonly UpgradeCardTag[] Tags;
    public readonly int PickCount;
    public readonly int MaxPickCount;

    public UpgradeCardOptionSnapshot(
        string cardId,
        string title,
        Sprite icon,
        string description,
        UpgradeCardRarity rarity,
        UpgradeCardTag[] tags,
        int pickCount,
        int maxPickCount)
    {
        CardId = cardId;
        Title = title;
        Icon = icon;
        Description = description;
        Rarity = rarity;
        Tags = tags ?? System.Array.Empty<UpgradeCardTag>();
        PickCount = Mathf.Max(0, pickCount);
        MaxPickCount = Mathf.Max(1, maxPickCount);
    }
}
