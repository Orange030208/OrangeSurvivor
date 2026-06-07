using UnityEngine;

public readonly struct RewardCardRollOption : IHasContentTier
{
    public RewardCardRollOption(RewardCardSO card, ContentRollItem rollItem, int pickCount)
    {
        Card = card;
        RollItem = rollItem;
        PickCount = Mathf.Max(0, pickCount);
    }

    public RewardCardSO Card { get; }
    public ContentRollItem RollItem { get; }
    public string EntryId => RollItem.EntryId;
    public int PickCount { get; }
    public int MaxPickCount => RollItem.Entry != null ? RollItem.Entry.MaxPickCount : 0;
    public bool HasPickLimit => MaxPickCount > 0;
    public ContentTier Tier => Card != null ? Card.Tier : ContentTier.Common;

    public RewardCardOptionViewData CreateViewData()
    {
        return Card != null
            ? Card.CreateOptionViewData(PickCount, MaxPickCount)
            : default;
    }
}
