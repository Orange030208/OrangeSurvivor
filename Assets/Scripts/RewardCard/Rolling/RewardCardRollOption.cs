using UnityEngine;

public readonly struct RewardCardRollOption : IHasContentTier
{
    public RewardCardRollOption(RewardCardSO card, string entryId, int pickCount)
    {
        Card = card;
        EntryId = string.IsNullOrWhiteSpace(entryId)
            ? card != null ? card.Id : string.Empty
            : entryId;
        PickCount = Mathf.Max(0, pickCount);
    }

    public RewardCardSO Card { get; }
    public string EntryId { get; }
    public int PickCount { get; }
    public int MaxPickCount => RewardCardSO.UNLIMITED_PICK_COUNT;
    public bool HasPickLimit => false;
    public ContentTier Tier => Card != null ? Card.Tier : ContentTier.Common;

    public RewardCardOptionViewData CreateViewData()
    {
        return Card != null
            ? Card.CreateOptionViewData(PickCount, MaxPickCount)
            : default;
    }
}
