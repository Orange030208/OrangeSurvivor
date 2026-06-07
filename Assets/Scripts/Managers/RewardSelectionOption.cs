using System;

public abstract class RewardSelectionOption : IHasContentTier
{
    protected RewardSelectionOption(RewardCardViewConfig viewConfig)
    {
        ViewConfig = viewConfig ?? throw new ArgumentNullException(nameof(viewConfig));
    }

    public string OptionId => ViewConfig.OptionId;
    public RewardOptionKind Kind => ViewConfig.Kind;
    public RewardCardViewConfig ViewConfig { get; }
    public ContentTier Tier => ViewConfig.Tier;
}

public sealed class UpgradeRewardSelectionOption : RewardSelectionOption
{
    public UpgradeRewardSelectionOption(RewardCardRollOption rewardCardOption, RewardCardViewConfig viewConfig)
        : base(viewConfig)
    {
        RewardCardOption = rewardCardOption;
    }

    public RewardCardSO UpgradeCard => RewardCardOption.Card;
    public RewardCardRollOption RewardCardOption { get; }
}

public sealed class WeaponRewardSelectionOption : RewardSelectionOption
{
    public WeaponRewardSelectionOption(
        WeaponDataSO weaponData,
        int level,
        ContentRollItem rollItem,
        RewardCardViewConfig viewConfig)
        : base(viewConfig)
    {
        WeaponData = weaponData;
        Level = WeaponLevelHelper.ClampLevel(level);
        RollItem = rollItem;
    }

    public WeaponDataSO WeaponData { get; }
    public int Level { get; }
    public ContentRollItem RollItem { get; }
}

public sealed class AccessoryRewardSelectionOption : RewardSelectionOption
{
    public AccessoryRewardSelectionOption(
        AccessoryDataSO accessoryData,
        ContentRollItem rollItem,
        RewardCardViewConfig viewConfig)
        : base(viewConfig)
    {
        AccessoryData = accessoryData;
        RollItem = rollItem;
    }

    public AccessoryDataSO AccessoryData { get; }
    public ContentRollItem RollItem { get; }
}
