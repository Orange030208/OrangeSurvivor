using System.Collections.Generic;
using UnityEngine;

public class UpgradeCardRollService
{
    private readonly ContentPoolRollService contentPoolRollService = new();
    private readonly ContentPoolRuntimeState contentPoolRuntimeState = new();

    public List<UpgradeCardRollOption> RollOptions(ContentPoolSO pool, ContentFactSource factSource)
    {
        List<UpgradeCardRollOption> options = new();
        if (pool == null)
        {
            Debug.LogError($"[{nameof(UpgradeCardRollService)}] Missing upgrade card {nameof(ContentPoolSO)}.");
            return options;
        }

        factSource ??= new ContentFactSource();

        ContentRollResult result = contentPoolRollService.Roll(
            pool,
            factSource,
            contentPoolRuntimeState,
            null,
            entry => entry.Content is UpgradeCardSO card &&
                     !string.IsNullOrWhiteSpace(card.CardId) &&
                     card.HasAnyEffect());

        for (int i = 0; i < result.Items.Count; i++)
        {
            ContentRollItem item = result.Items[i];
            UpgradeCardSO card = item.Content as UpgradeCardSO;
            if (card != null)
            {
                options.Add(new UpgradeCardRollOption(
                    card,
                    item,
                    contentPoolRuntimeState.GetPickCount(item.EntryId)));
            }
        }

        return options;
    }

    public void RecordPick(UpgradeCardRollOption option)
    {
        if (string.IsNullOrWhiteSpace(option.EntryId))
        {
            return;
        }

        contentPoolRuntimeState.RecordPick(option.RollItem);
    }
}
