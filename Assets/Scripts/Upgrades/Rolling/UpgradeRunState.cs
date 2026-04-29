using System.Collections.Generic;

public class UpgradeRunState
{
    private readonly Dictionary<string, int> cardPickCounts = new();
    private readonly Dictionary<UpgradeCardTag, int> tagPickCounts = new();
    private readonly HashSet<string> previousOfferCardIds = new();

    public IEnumerable<string> PickedCardIds => cardPickCounts.Keys;

    public int GetPickCount(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return 0;
        }

        return cardPickCounts.GetValueOrDefault(cardId, 0);
    }

    public int GetTagPickCount(UpgradeCardTag tag)
    {
        return tagPickCounts.GetValueOrDefault(tag, 0);
    }

    public bool WasPreviouslyOffered(string cardId)
    {
        return !string.IsNullOrWhiteSpace(cardId) && previousOfferCardIds.Contains(cardId);
    }

    public bool WasPicked(string cardId)
    {
        return GetPickCount(cardId) > 0;
    }

    public void RecordOffer(IReadOnlyList<UpgradeCardSO> cards)
    {
        previousOfferCardIds.Clear();
        if (cards == null)
        {
            return;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            UpgradeCardSO card = cards[i];
            if (card == null || string.IsNullOrWhiteSpace(card.CardId))
            {
                continue;
            }

            previousOfferCardIds.Add(card.CardId);
        }
    }

    public void RecordPick(UpgradeCardSO card)
    {
        if (card == null || string.IsNullOrWhiteSpace(card.CardId))
        {
            return;
        }

        cardPickCounts[card.CardId] = GetPickCount(card.CardId) + 1;
        IReadOnlyList<UpgradeCardTag> tags = card.Tags;
        for (int i = 0; i < tags.Count; i++)
        {
            UpgradeCardTag tag = tags[i];
            tagPickCounts[tag] = GetTagPickCount(tag) + 1;
        }
    }
}
