using System.Collections.Generic;

public sealed class RunContentHistory
{
    private ContentHistoryState state = new();

    public ContentHistoryState State => state;

    public void Reset()
    {
        state = new ContentHistoryState();
    }

    public int GetRollCount(ContentRollScope scope, string entryId)
    {
        return state.GetRollCount(scope.ToHistoryScope(), entryId);
    }

    public int GetPickCount(ContentRollScope scope, string entryId)
    {
        return state.GetPickCount(scope.ToHistoryScope(), entryId);
    }

    public bool WasPreviouslyRolled(ContentRollScope scope, string entryId)
    {
        return state.WasPreviouslyRolled(scope.ToHistoryScope(), entryId);
    }

    public bool WasPreviouslyOffered(ContentRollScope scope, string entryId)
    {
        return state.WasPreviouslyOffered(scope.ToHistoryScope(), entryId);
    }

    public void RecordRoll(ContentRollScope scope, ContentRollOutcome outcome)
    {
        state.RecordRoll(scope.ToHistoryScope(), ToLegacyItems(outcome));
    }

    public void RecordPick(ContentRollScope scope, ContentRollSelection selection)
    {
        state.RecordPick(scope.ToHistoryScope(), selection.ToLegacyItem());
    }

    public void RecordPick(ContentRollScope scope, ContentRollItem item)
    {
        state.RecordPick(scope.ToHistoryScope(), item);
    }

    private static IReadOnlyList<ContentRollItem> ToLegacyItems(ContentRollOutcome outcome)
    {
        if (outcome == null || outcome.Selections.Count == 0)
        {
            return System.Array.Empty<ContentRollItem>();
        }

        List<ContentRollItem> items = new(outcome.Selections.Count);
        for (int i = 0; i < outcome.Selections.Count; i++)
        {
            items.Add(outcome.Selections[i].ToLegacyItem());
        }

        return items;
    }
}

public static class RunContentHistoryRuntime
{
    private static RunContentHistory current;

    public static RunContentHistory Current
    {
        get
        {
            current ??= new RunContentHistory();
            return current;
        }
    }

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        current = null;
    }

    public static RunContentHistory BeginRun()
    {
        current = new RunContentHistory();
        return current;
    }

    public static void SetCurrent(RunContentHistory history)
    {
        current = history ?? new RunContentHistory();
    }
}
