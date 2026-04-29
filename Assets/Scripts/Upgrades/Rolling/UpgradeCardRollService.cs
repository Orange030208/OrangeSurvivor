using System.Collections.Generic;
using UnityEngine;

public class UpgradeCardRollService
{
    private const int MAX_RARITY_ROLL_ATTEMPTS = 4;

    public List<UpgradeCardSO> RollOptions(UpgradeCardPoolSO pool, UpgradeRunState runState, int waveNumber)
    {
        return RollOptions(pool, new UpgradeCardOfferContext(runState, waveNumber, (WeaponsHolder)null));
    }

    public List<UpgradeCardSO> RollOptions(UpgradeCardPoolSO pool, UpgradeCardOfferContext context)
    {
        List<UpgradeCardSO> options = new();
        if (pool == null || pool.Cards == null || pool.Cards.Count == 0)
        {
            return options;
        }

        context ??= new UpgradeCardOfferContext(null, 1, (WeaponsHolder)null);
        int optionCount = pool.OptionCount;
        HashSet<string> selectedCardIds = new();
        int safetyLimit = Mathf.Max(optionCount * 12, 24);

        for (int attempt = 0; attempt < safetyLimit && options.Count < optionCount; attempt++)
        {
            UpgradeCardRarity targetRarity = RollRarity(pool.ResolveRarityWeights(context.WaveNumber));
            UpgradeCardSO selected = RollByRarity(pool, context, targetRarity, selectedCardIds);
            if (selected == null)
            {
                selected = RollAnyAvailable(pool, context, selectedCardIds);
            }

            if (selected == null)
            {
                break;
            }

            options.Add(selected);
            selectedCardIds.Add(selected.CardId);
        }

        context.RunState?.RecordOffer(options);
        return options;
    }

    private UpgradeCardRarity RollRarity(UpgradeRarityWeightByWave weights)
    {
        int totalWeight = weights.CommonWeight + weights.RareWeight + weights.EpicWeight + weights.LegendaryWeight;
        if (totalWeight <= 0)
        {
            return UpgradeCardRarity.Common;
        }

        int roll = Random.Range(0, totalWeight);
        if (roll < weights.CommonWeight)
        {
            return UpgradeCardRarity.Common;
        }

        roll -= weights.CommonWeight;
        if (roll < weights.RareWeight)
        {
            return UpgradeCardRarity.Rare;
        }

        roll -= weights.RareWeight;
        if (roll < weights.EpicWeight)
        {
            return UpgradeCardRarity.Epic;
        }

        return UpgradeCardRarity.Legendary;
    }

    private UpgradeCardSO RollByRarity(
        UpgradeCardPoolSO pool,
        UpgradeCardOfferContext context,
        UpgradeCardRarity rarity,
        HashSet<string> selectedCardIds)
    {
        for (int i = 0; i < MAX_RARITY_ROLL_ATTEMPTS; i++)
        {
            UpgradeCardSO selected = RollWeighted(pool, context, selectedCardIds, rarity);
            if (selected != null)
            {
                return selected;
            }
        }

        return null;
    }

    private UpgradeCardSO RollAnyAvailable(
        UpgradeCardPoolSO pool,
        UpgradeCardOfferContext context,
        HashSet<string> selectedCardIds)
    {
        return RollWeighted(pool, context, selectedCardIds, null);
    }

    private UpgradeCardSO RollWeighted(
        UpgradeCardPoolSO pool,
        UpgradeCardOfferContext context,
        HashSet<string> selectedCardIds,
        UpgradeCardRarity? rarityFilter)
    {
        List<UpgradeCardSO> candidates = new();
        List<float> weights = new();
        float totalWeight = 0f;

        IReadOnlyList<UpgradeCardSO> cards = pool.Cards;
        for (int i = 0; i < cards.Count; i++)
        {
            UpgradeCardSO card = cards[i];
            if (!CanOffer(card, pool, context, selectedCardIds, rarityFilter))
            {
                continue;
            }

            float weight = CalculateWeight(card, pool, context.RunState);
            if (weight <= 0f)
            {
                continue;
            }

            candidates.Add(card);
            weights.Add(weight);
            totalWeight += weight;
        }

        if (candidates.Count == 0 || totalWeight <= 0f)
        {
            return null;
        }

        float roll = Random.value * totalWeight;
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0f)
            {
                return candidates[i];
            }
        }

        return candidates[^1];
    }

    private bool CanOffer(
        UpgradeCardSO card,
        UpgradeCardPoolSO pool,
        UpgradeCardOfferContext context,
        HashSet<string> selectedCardIds,
        UpgradeCardRarity? rarityFilter)
    {
        if (card == null || string.IsNullOrWhiteSpace(card.CardId) || !card.HasAnyEffect())
        {
            return false;
        }

        if (rarityFilter.HasValue && card.Rarity != rarityFilter.Value)
        {
            return false;
        }

        if (selectedCardIds != null && selectedCardIds.Contains(card.CardId))
        {
            return false;
        }

        UpgradeRunState runState = context?.RunState;
        int pickCount = runState != null ? runState.GetPickCount(card.CardId) : 0;
        if (pickCount >= card.MaxPickCount)
        {
            return false;
        }

        UpgradeCardOfferConditions conditions = card.OfferConditions;
        if (conditions != null && !conditions.AreSatisfied(context))
        {
            return false;
        }

        return !HasSelectedMutualExclusion(card, pool, selectedCardIds)
               && !HasPickedMutualExclusion(card, pool, runState);
    }

    private bool HasSelectedMutualExclusion(
        UpgradeCardSO card,
        UpgradeCardPoolSO pool,
        HashSet<string> selectedCardIds)
    {
        if (card == null || selectedCardIds == null || selectedCardIds.Count == 0)
        {
            return false;
        }

        foreach (string selectedCardId in selectedCardIds)
        {
            if (IsMutuallyExclusiveWithCardId(card, selectedCardId) ||
                AreMutuallyExclusive(card, FindCardById(pool, selectedCardId)))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasPickedMutualExclusion(UpgradeCardSO card, UpgradeCardPoolSO pool, UpgradeRunState runState)
    {
        if (card == null || runState == null)
        {
            return false;
        }

        foreach (string pickedCardId in runState.PickedCardIds)
        {
            if (IsMutuallyExclusiveWithCardId(card, pickedCardId) ||
                AreMutuallyExclusive(card, FindCardById(pool, pickedCardId)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AreMutuallyExclusive(UpgradeCardSO left, UpgradeCardSO right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return (left.OfferConditions != null && left.OfferConditions.IsMutuallyExclusiveWith(right.CardId)) ||
               (right.OfferConditions != null && right.OfferConditions.IsMutuallyExclusiveWith(left.CardId));
    }

    private static bool IsMutuallyExclusiveWithCardId(UpgradeCardSO card, string otherCardId)
    {
        return card != null &&
               card.OfferConditions != null &&
               card.OfferConditions.IsMutuallyExclusiveWith(otherCardId);
    }

    private static UpgradeCardSO FindCardById(UpgradeCardPoolSO pool, string cardId)
    {
        if (pool?.Cards == null || string.IsNullOrWhiteSpace(cardId))
        {
            return null;
        }

        IReadOnlyList<UpgradeCardSO> cards = pool.Cards;
        for (int i = 0; i < cards.Count; i++)
        {
            UpgradeCardSO card = cards[i];
            if (card != null && string.Equals(card.CardId, cardId, System.StringComparison.Ordinal))
            {
                return card;
            }
        }

        return null;
    }

    private float CalculateWeight(UpgradeCardSO card, UpgradeCardPoolSO pool, UpgradeRunState runState)
    {
        float weight = card.BaseWeight;
        if (runState == null)
        {
            return weight;
        }

        IReadOnlyList<UpgradeCardTag> tags = card.Tags;
        for (int i = 0; i < tags.Count; i++)
        {
            int tagPickCount = runState.GetTagPickCount(tags[i]);
            if (tagPickCount > 0)
            {
                weight *= 1f + pool.MatchingTagWeightBonus * tagPickCount;
            }
        }

        if (runState.WasPreviouslyOffered(card.CardId))
        {
            weight *= pool.PreviousOfferWeightMultiplier;
        }

        return weight;
    }
}
