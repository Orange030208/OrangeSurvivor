using System.Collections.Generic;
using Orange.Extraction;
using UnityEngine;

public sealed class UpgradeRewardSelectionHandler : IRewardSelectionHandler
{
    private const int OPTION_COUNT = 4;

    private readonly RewardCardApplyService applyService = new();

    public RewardSelectionReason Reason => RewardSelectionReason.Upgrade;

    public bool ShouldCreateSelection(RewardSelectionHandlerContext context, bool hasProcessedSelection)
    {
        return context?.PlayerLevel != null && context.PlayerLevel.UnspentUpgradePoints > 0;
    }

    public RewardSelectionRound CreateSelection(RewardSelectionHandlerContext context)
    {
        if (context == null || context.TierWeightProfile == null)
        {
            Debug.LogError($"[{nameof(UpgradeRewardSelectionHandler)}] Missing {nameof(ContentTierWeightProfileSO)}.", context?.LogContext);
            return EmptyRound();
        }

        if (context.RewardCards == null || context.RewardCards.Count == 0)
        {
            Debug.LogError($"[{nameof(UpgradeRewardSelectionHandler)}] Missing reward card candidates.", context.LogContext);
            return EmptyRound();
        }

        WeightedExtractionPool<RewardCardSO, RewardSelectionHandlerContext> pool = new();
        pool.AddWeightModifier(
            new ContentTierLuckWeightModifier<RewardCardSO, RewardSelectionHandlerContext>(
                context.TierWeightProfile,
                card => card != null ? card.Tier : ContentTier.Common,
                handlerContext => handlerContext != null ? handlerContext.Luck : 0f));

        for (int i = 0; i < context.RewardCards.Count; i++)
        {
            RewardCardSO card = context.RewardCards[i];
            if (card == null || string.IsNullOrWhiteSpace(card.Id) || !card.HasAnyEffect())
            {
                continue;
            }

            pool.AddEntry(card.Id, card, context.TierWeightProfile.GetWeight(card.Tier));
        }

        IReadOnlyList<ExtractionResult<RewardCardSO>> results = pool.DrawManyUnique(context, OPTION_COUNT);
        RewardSelectionOption[] options = new RewardSelectionOption[results.Count];
        for (int i = 0; i < results.Count; i++)
        {
            RewardCardRollOption rollOption = new(results[i].Item, results[i].EntryId, 0);
            RewardCardOptionViewData viewData = rollOption.CreateViewData();
            RewardCardViewConfig viewConfig = RewardCardViewConfigFactory.CreateUpgrade(viewData, rollOption.Card != null);
            options[i] = new UpgradeRewardSelectionOption(rollOption, viewConfig);
        }

        if (options.Length == 0)
        {
            Debug.LogWarning("[UpgradeRewardSelectionHandler] No upgrade cards could be rolled for upgrade reward.", context.LogContext);
        }

        return new RewardSelectionRound("选择升级奖励", "选择 1 张升级卡。", options);
    }

    public bool ApplySelection(RewardSelectionOption option, RewardSelectionHandlerContext context)
    {
        if (option is not UpgradeRewardSelectionOption selectedOption)
        {
            return false;
        }

        RewardCardSO selectedCard = selectedOption.UpgradeCard;
        if (!applyService.Apply(selectedCard, context.Player))
        {
            Debug.LogWarning($"[UpgradeRewardSelectionHandler] Failed to apply upgrade card {selectedCard?.name}.", context.LogContext);
            return false;
        }

        context.PlayerLevel?.ConsumeUpgradePoint();
        return true;
    }

    private static RewardSelectionRound EmptyRound()
    {
        return new RewardSelectionRound("选择升级奖励", "选择 1 张升级卡。", System.Array.Empty<RewardSelectionOption>());
    }
}
