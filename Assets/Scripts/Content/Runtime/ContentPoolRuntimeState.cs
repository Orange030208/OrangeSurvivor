using System.Collections.Generic;

public sealed class ContentPoolRuntimeState
{
    private readonly Dictionary<string, int> rollCountsByEntryId = new(System.StringComparer.Ordinal);
    private readonly Dictionary<string, int> pickCountsByEntryId = new(System.StringComparer.Ordinal);
    private readonly HashSet<string> previousRollEntryIds = new(System.StringComparer.Ordinal);

    public int GetRollCount(string entryId)
    {
        return string.IsNullOrWhiteSpace(entryId) ? 0 : rollCountsByEntryId.GetValueOrDefault(entryId, 0);
    }

    public int GetPickCount(string entryId)
    {
        return string.IsNullOrWhiteSpace(entryId) ? 0 : pickCountsByEntryId.GetValueOrDefault(entryId, 0);
    }

    public bool WasPreviouslyRolled(string entryId)
    {
        return !string.IsNullOrWhiteSpace(entryId) && previousRollEntryIds.Contains(entryId);
    }

    public void RecordRoll(IReadOnlyList<ContentRollItem> items)
    {
        previousRollEntryIds.Clear();
        if (items == null)
        {
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            ContentRollItem item = items[i];
            string entryId = item.EntryId;
            if (string.IsNullOrWhiteSpace(entryId))
            {
                continue;
            }

            previousRollEntryIds.Add(entryId);
            rollCountsByEntryId[entryId] = GetRollCount(entryId) + 1;
        }
    }

    public void RecordPick(ContentRollItem item)
    {
        RecordPick(item.EntryId);
    }

    public void RecordPick(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
        {
            return;
        }

        pickCountsByEntryId[entryId] = GetPickCount(entryId) + 1;
    }
}
