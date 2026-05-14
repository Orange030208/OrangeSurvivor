public interface IRewardCardPresentation : IDescribable
{
    string OptionId { get; }
    RewardOptionKind Kind { get; }
    RewardCardStyle Style { get; }
    CardQuality Quality { get; }
    bool Interactable { get; }
}
