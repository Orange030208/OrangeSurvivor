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
            new WeaponLevelDescribable(weaponData, clampedLevel),
            CardQualityResolver.FromWeaponLevel(clampedLevel),
            weaponData != null);
    }

    public EquipmentRewardCardPresentation CreateAccessory(AccessoryDataSO accessory)
    {
        string optionId = accessory != null ? accessory.AccessoryId : string.Empty;
        return new EquipmentRewardCardPresentation(
            RewardOptionKind.Accessory,
            optionId,
            accessory,
            accessory != null ? CardQualityResolver.FromAccessoryRarity(accessory.RarityGrade) : CardQuality.Common,
            accessory != null);
    }
}
