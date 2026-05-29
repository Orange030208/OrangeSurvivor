public abstract class RewardSelectionOption : IHasContentTier
{
    protected RewardSelectionOption(string optionId, RewardOptionKind kind, IRewardCardPresentation presentation)
    {
        OptionId = optionId ?? string.Empty;
        Kind = kind;
        Presentation = presentation;
    }

    public string OptionId { get; }
    public RewardOptionKind Kind { get; }
    public IRewardCardPresentation Presentation { get; }
    public abstract ContentTier Tier { get; }
}

public sealed class UpgradeRewardSelectionOption : RewardSelectionOption
{
    public UpgradeRewardSelectionOption(UpgradeCardRollOption upgradeCardOption, IRewardCardPresentation presentation)
        : base(presentation?.OptionId, RewardOptionKind.UpgradeCard, presentation)
    {
        UpgradeCardOption = upgradeCardOption;
    }

    public UpgradeCardSO UpgradeCard => UpgradeCardOption.Card;
    public UpgradeCardRollOption UpgradeCardOption { get; }
    public override ContentTier Tier => UpgradeCardOption.Tier;
}

public sealed class WeaponRewardSelectionOption : RewardSelectionOption
{
    public WeaponRewardSelectionOption(
        string optionId,
        WeaponDataSO weaponData,
        int level,
        ContentRollItem rollItem,
        IRewardCardPresentation presentation)
        : base(optionId, RewardOptionKind.Weapon, presentation)
    {
        WeaponData = weaponData;
        Level = WeaponLevelHelper.ClampLevel(level);
        RollItem = rollItem;
    }

    public WeaponDataSO WeaponData { get; }
    public int Level { get; }
    public ContentRollItem RollItem { get; }
    public override ContentTier Tier => RollItem.TryGetTier(out ContentTier tier)
        ? tier
        : ContentTierResolver.FromWeaponLevel(Level);
}

public sealed class AccessoryRewardSelectionOption : RewardSelectionOption
{
    public AccessoryRewardSelectionOption(
        string optionId,
        AccessoryDataSO accessoryData,
        ContentRollItem rollItem,
        IRewardCardPresentation presentation)
        : base(optionId, RewardOptionKind.Accessory, presentation)
    {
        AccessoryData = accessoryData;
        RollItem = rollItem;
    }

    public AccessoryDataSO AccessoryData { get; }
    public ContentRollItem RollItem { get; }
    public override ContentTier Tier => RollItem.TryGetTier(out ContentTier tier)
        ? tier
        : AccessoryData != null ? ContentTierResolver.FromAccessoryRarity(AccessoryData.RarityGrade) : ContentTier.Common;
}
