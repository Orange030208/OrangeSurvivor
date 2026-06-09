using System.Collections.Generic;
using UnityEngine;

public class RewardCardRollService
{
    private readonly RewardContentRoller contentRoller = new();

    public List<RewardCardRollOption> RollOptions(
        ContentPoolSO pool,
        ContentRollContext rollContext,
        RunContentHistory history)
    {
        List<RewardCardRollOption> options = new();
        if (pool == null)
        {
            Debug.LogError($"[{nameof(RewardCardRollService)}] Missing upgrade card {nameof(ContentPoolSO)}.");
            return options;
        }

        ContentRollResult result = contentRoller.Roll(
            pool,
            rollContext,
            null,
            entry => entry.Content is RewardCardSO card &&
                     !string.IsNullOrWhiteSpace(card.Id) &&
                     card.HasAnyEffect(),
            history);

        AddOptions(options, result, entryId => ResolvePickCount(rollContext, entryId));
        return options;
    }

    private static void AddOptions(
        List<RewardCardRollOption> options,
        ContentRollResult result,
        System.Func<string, int> resolvePickCount)
    {
        for (int i = 0; i < result.Items.Count; i++)
        {
            ContentRollItem item = result.Items[i];
            RewardCardSO card = item.Content as RewardCardSO;
            if (card != null)
            {
                options.Add(new RewardCardRollOption(
                    card,
                    item,
                    resolvePickCount != null ? resolvePickCount(item.EntryId) : 0));
            }
        }
    }

    private static int ResolvePickCount(ContentRollContext rollContext, string entryId)
    {
        if (rollContext?.History != null)
        {
            return rollContext.History.GetPickCount(rollContext.HistoryScope, entryId);
        }

        return 0;
    }
}
