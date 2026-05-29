using UnityEngine;

public interface IRewardCardPresentation
{
    string OptionId { get; }
    RewardOptionKind Kind { get; }
    RewardCardStyle Style { get; }
    string Title { get; }
    Sprite Icon { get; }
    string Description { get; }
    ContentTier Tier { get; }
    bool Interactable { get; }
}
