public sealed class EquipmentRewardCardPresenter
{
    public EquipmentRewardCardPresentation CreateWeapon(WeaponDataSO weaponData, int level)
    {
        int clampedLevel = WeaponLevelHelper.ClampLevel(level);
        string optionId = weaponData != null && !string.IsNullOrWhiteSpace(weaponData.ItemName)
            ? $"{weaponData.ItemName}:{clampedLevel}"
            : string.Empty;
        return new EquipmentRewardCardPresentation(
            RewardOptionKind.Weapon,
            optionId,
            weaponData != null ? ItemDisplayHelper.GetWeaponDisplayName(weaponData.ItemName, clampedLevel) : string.Empty,
            weaponData != null ? weaponData.ItemIcon : null,
            weaponData != null ? weaponData.BuildDescriptionForLevel(clampedLevel) : string.Empty,
            ContentTierResolver.FromWeaponLevel(clampedLevel),
            weaponData != null);
    }

    public EquipmentRewardCardPresentation CreateAccessory(AccessoryDataSO accessory)
    {
        string optionId = accessory != null ? accessory.AccessoryId : string.Empty;
        return new EquipmentRewardCardPresentation(
            RewardOptionKind.Accessory,
            optionId,
            accessory != null ? accessory.ItemName : string.Empty,
            accessory != null ? accessory.ItemIcon : null,
            accessory != null ? accessory.Description : string.Empty,
            accessory != null ? ContentTierResolver.FromAccessoryRarity(accessory.RarityGrade) : ContentTier.Common,
            accessory != null);
    }
}
