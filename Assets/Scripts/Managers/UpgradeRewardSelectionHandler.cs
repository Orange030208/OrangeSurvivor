using System.Collections.Generic;
using UnityEngine;

public sealed class UpgradeRewardSelectionHandler : IRewardSelectionHandler
{
    private const int OPTION_COUNT = 4;

    private readonly RewardCardRollService rollService = new();
    private readonly RewardCardApplyService applyService = new();

    public RewardSelectionReason Reason => RewardSelectionReason.Upgrade;

    public bool ShouldCreateSelection(RewardSelectionHandlerContext context, bool hasProcessedSelection)
    {
        return context?.PlayerLevel != null && context.PlayerLevel.UnspentUpgradePoints > 0;
    }

    public RewardSelectionRound CreateSelection(RewardSelectionHandlerContext context)
    {
        ContentPoolSO pool = ResolveUpgradeCardPool(context);
        if (pool == null)
        {
            return EmptyRound();
        }

        ContentRollContext rollContext = new(
            ContentPoolScopeIds.UpgradeCard,
            context.Player,
            progressionSnapshot: RunProgressionRuntime.CurrentSnapshot,
            historyScope: context.CreateHistoryScope(pool, ContentPoolScopeIds.UpgradeCard),
            history: context.ContentHistoryState);
        List<RewardCardRollOption> rollOptions = rollService.RollOptions(pool, rollContext);
        int count = Mathf.Min(OPTION_COUNT, rollOptions.Count);
        RewardSelectionOption[] options = new RewardSelectionOption[count];

        for (int i = 0; i < count; i++)
        {
            RewardCardRollOption rollOption = rollOptions[i];
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

        ContentPoolSO pool = ResolveUpgradeCardPool(context);
        context.ContentHistoryState.RecordPick(
            context.CreateHistoryScope(pool, ContentPoolScopeIds.UpgradeCard),
            selectedOption.RewardCardOption.RollItem);
        context.PlayerLevel?.ConsumeUpgradePoint();
        return true;
    }

    private static RewardSelectionRound EmptyRound()
    {
        return new RewardSelectionRound("选择升级奖励", "选择 1 张升级卡。", System.Array.Empty<RewardSelectionOption>());
    }

    private static ContentPoolSO ResolveUpgradeCardPool(RewardSelectionHandlerContext context)
    {
        if (context.UpgradeCardPool != null)
        {
            return context.UpgradeCardPool;
        }

        if (GameContentRuntime.TryGetProvider(out IGameContentProvider provider) && provider.UpgradeCardPool != null)
        {
            return provider.UpgradeCardPool;
        }

        Debug.LogError($"[UpgradeRewardSelectionHandler] Missing upgrade card content pool in scene or {nameof(GameContentCatalogSO)}.", context.LogContext);
        return null;
    }
}
