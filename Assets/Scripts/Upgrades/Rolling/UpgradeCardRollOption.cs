using UnityEngine;

public readonly struct UpgradeCardRollOption
{
    public UpgradeCardRollOption(UpgradeCardSO card, ContentRollItem rollItem, int pickCount)
    {
        Card = card;
        RollItem = rollItem;
        PickCount = Mathf.Max(0, pickCount);
    }

    public UpgradeCardSO Card { get; }
    public ContentRollItem RollItem { get; }
    public string EntryId => RollItem.EntryId;
    public int PickCount { get; }
    public int MaxPickCount => RollItem.Entry != null ? RollItem.Entry.MaxPickCount : 0;
    public bool HasPickLimit => MaxPickCount > 0;

    public UpgradeCardOptionViewData CreateViewData()
    {
        return Card != null
            ? Card.CreateOptionViewData(PickCount, MaxPickCount)
            : default;
    }
}
