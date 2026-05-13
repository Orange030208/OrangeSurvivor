using System.Collections.Generic;
using UnityEngine;

public class UpgradeCardRollService
{
    private readonly ContentPoolRollService contentPoolRollService = new();

    public List<UpgradeCardRollOption> RollOptions(ContentPoolSO pool, ContentRollContext rollContext)
    {
        List<UpgradeCardRollOption> options = new();
        if (pool == null)
        {
            Debug.LogError($"[{nameof(UpgradeCardRollService)}] Missing upgrade card {nameof(ContentPoolSO)}.");
            return options;
        }

        ContentRollResult result = contentPoolRollService.Roll(
            pool,
            rollContext,
            null,
            entry => entry.Content is UpgradeCardSO card &&
                     !string.IsNullOrWhiteSpace(card.CardId) &&
                     card.HasAnyEffect());

        AddOptions(options, result, entryId => ResolvePickCount(rollContext, entryId));
        return options;
    }

    private static void AddOptions(
        List<UpgradeCardRollOption> options,
        ContentRollResult result,
        System.Func<string, int> resolvePickCount)
    {
        for (int i = 0; i < result.Items.Count; i++)
        {
            ContentRollItem item = result.Items[i];
            UpgradeCardSO card = item.Content as UpgradeCardSO;
            if (card != null)
            {
                options.Add(new UpgradeCardRollOption(
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
