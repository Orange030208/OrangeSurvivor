using UnityEngine;

public sealed class RewardCardViewConfig : IHasContentTier
{
    public RewardCardViewConfig(
        string optionId,
        RewardOptionKind kind,
        ContentTier tier,
        string title,
        string description,
        Sprite icon,
        bool interactable)
    {
        OptionId = optionId ?? string.Empty;
        Kind = kind;
        Tier = tier;
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
        Icon = icon;
        Interactable = interactable;
    }

    public string OptionId { get; }
    public RewardOptionKind Kind { get; }
    public ContentTier Tier { get; }
    public string Title { get; }
    public string Description { get; }
    public Sprite Icon { get; }
    public bool Interactable { get; }
}

public static class RewardCardViewConfigFactory
{
    public static RewardCardViewConfig CreateUpgrade(RewardCardOptionViewData viewData, bool interactable)
    {
        return new RewardCardViewConfig(
            viewData.Id ?? string.Empty,
            RewardOptionKind.UpgradeCard,
            viewData.Tier,
            viewData.Title ?? string.Empty,
            viewData.Description ?? string.Empty,
            viewData.Icon,
            interactable);
    }

    public static RewardCardViewConfig CreateUpgrade(RewardCardSO card)
    {
        return CreateUpgrade(
            card != null
                ? card.CreateOptionViewData(0, RewardCardSO.UNLIMITED_PICK_COUNT)
                : default,
            card != null);
    }

    public static RewardCardViewConfig CreateWeapon(WeaponDataSO weaponData, int level, ContentTier tier)
    {
        int clampedLevel = WeaponLevelHelper.ClampLevel(level);
        return new RewardCardViewConfig(
            ResolveWeaponOptionId(weaponData, clampedLevel),
            RewardOptionKind.Weapon,
            tier,
            weaponData != null ? ItemDisplayHelper.GetWeaponDisplayName(weaponData.ItemName, clampedLevel) : string.Empty,
            weaponData != null ? weaponData.BuildDescriptionForLevel(clampedLevel) : string.Empty,
            weaponData != null ? weaponData.ItemIcon : null,
            weaponData != null);
    }

    public static RewardCardViewConfig CreateAccessory(AccessoryDataSO accessoryData, ContentTier tier)
    {
        return new RewardCardViewConfig(
            ResolveAccessoryOptionId(accessoryData),
            RewardOptionKind.Accessory,
            tier,
            accessoryData != null ? accessoryData.ItemName : string.Empty,
            accessoryData != null ? accessoryData.Description : string.Empty,
            accessoryData != null ? accessoryData.ItemIcon : null,
            accessoryData != null);
    }

    private static string ResolveUpgradeOptionId(RewardCardSO card)
    {
        if (card == null)
        {
            return string.Empty;
        }

        return !string.IsNullOrWhiteSpace(card.Id) ? card.Id : card.name;
    }

    private static string ResolveWeaponOptionId(WeaponDataSO weaponData, int level)
    {
        if (weaponData == null)
        {
            return string.Empty;
        }

        string baseId = !string.IsNullOrWhiteSpace(weaponData.ItemName)
            ? weaponData.ItemName
            : weaponData.name;
        return !string.IsNullOrWhiteSpace(baseId) ? $"{baseId}:{level}" : string.Empty;
    }

    private static string ResolveAccessoryOptionId(AccessoryDataSO accessoryData)
    {
        if (accessoryData == null)
        {
            return string.Empty;
        }

        return !string.IsNullOrWhiteSpace(accessoryData.AccessoryId)
            ? accessoryData.AccessoryId
            : accessoryData.name;
    }
}
