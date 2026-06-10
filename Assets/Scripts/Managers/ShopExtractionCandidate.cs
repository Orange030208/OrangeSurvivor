using System;

public sealed class ShopExtractionCandidate : IHasContentTier
{
    private ShopExtractionCandidate(
        string entryId,
        ItemDataSO itemData,
        int level,
        ContentTier tier)
    {
        if (string.IsNullOrWhiteSpace(entryId))
        {
            throw new ArgumentException("Shop extraction candidate entry id cannot be null, empty, or whitespace.", nameof(entryId));
        }

        EntryId = entryId;
        ItemData = itemData ?? throw new ArgumentNullException(nameof(itemData));
        Level = WeaponLevelHelper.ClampLevel(level);
        Tier = ContentTierResolver.FromQualityValue((int)tier);
    }

    public string EntryId { get; }
    public ItemDataSO ItemData { get; }
    public int Level { get; }
    public ContentTier Tier { get; }

    public static ShopExtractionCandidate CreateAccessory(AccessoryDataSO accessory)
    {
        if (accessory == null)
        {
            return null;
        }

        string entryId = !string.IsNullOrWhiteSpace(accessory.AccessoryId)
            ? accessory.AccessoryId
            : accessory.name;
        return new ShopExtractionCandidate(
            entryId,
            accessory,
            WeaponLevelHelper.MinLevel,
            accessory.Tier);
    }

    public static ShopExtractionCandidate CreateWeapon(WeaponDataSO weapon, int level)
    {
        if (weapon == null)
        {
            return null;
        }

        int clampedLevel = WeaponLevelHelper.ClampLevel(level);
        return new ShopExtractionCandidate(
            $"{weapon.WeaponId}_Lv{clampedLevel}",
            weapon,
            clampedLevel,
            ContentTierResolver.FromWeaponLevel(clampedLevel));
    }
}
