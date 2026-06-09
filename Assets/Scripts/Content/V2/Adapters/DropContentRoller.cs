using System.Collections.Generic;

public sealed class DropContentRoller
{
    private readonly IContentPoolService service;

    public DropContentRoller(IContentPoolService service = null)
    {
        this.service = service ?? new ContentPoolServiceV2();
    }

    public ContentRollItem RollProduct(
        IReadOnlyList<ContentPoolEntry> entries,
        ContentRollContext legacyContext,
        ContentRollScope scope,
        RunContentHistory history)
    {
        ContentRollRequest request = LegacyContentPoolAdapter.CreateRequest(
            ContentPoolScopeIds.Drop,
            entries,
            legacyContext,
            scope,
            1,
            false,
            entry => entry.Content is CollectionSO || entry.Content is ContentPoolSO,
            history);
        ContentRollOutcome outcome = service.Roll(request);
        return outcome.HasAny ? outcome.Selections[0].ToLegacyItem() : default;
    }

    public ContentRollItem RollCollection(
        ContentPoolSO pool,
        ContentRollContext legacyContext,
        ContentRollScope scope,
        RunContentHistory history)
    {
        ContentRollRequest request = LegacyContentPoolAdapter.CreateRequest(
            pool,
            legacyContext,
            scope,
            1,
            entry => entry.Content is CollectionSO,
            history);
        ContentRollOutcome outcome = service.Roll(request);
        return outcome.HasAny ? outcome.Selections[0].ToLegacyItem() : default;
    }

    public static ContentRollScope CreateScope(ContentPoolSO pool)
    {
        string poolId = pool != null ? pool.name : ContentPoolScopeIds.Drop;
        return new ContentRollScope(ContentPoolScopeIds.Drop, poolId);
    }
}
