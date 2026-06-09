public sealed class ShopContentRoller
{
    private readonly IContentPoolService service;

    public ShopContentRoller(IContentPoolService service = null)
    {
        this.service = service ?? new ContentPoolServiceV2();
    }

    public ContentRollItem RollItem(
        ContentPoolSO pool,
        Player player,
        int shopRefreshCount,
        int shopRerollCount,
        RunContentHistory history)
    {
        if (pool == null)
        {
            return default;
        }

        ContentRollScope scope = CreateScope(pool, player);
        ContentRollContext legacyContext = new(
            ContentPoolScopeIds.Shop,
            player,
            progressionSnapshot: RunProgressionRuntime.CurrentSnapshot,
            historyScope: scope.ToHistoryScope(),
            history: history != null ? history.State : null,
            shopRefreshCount: shopRefreshCount,
            shopRerollCount: shopRerollCount);
        ContentFactSet facts = new ContentFactSet()
            .Set(ContentFactKeys.Player, player)
            .Set(ContentFactKeys.ProgressionSnapshot, RunProgressionRuntime.CurrentSnapshot)
            .Set(ContentFactKeys.ShopRefreshCount, shopRefreshCount)
            .Set(ContentFactKeys.ShopRerollCount, shopRerollCount);
        ContentRollRequest request = LegacyContentPoolAdapter.CreateRequest(
            pool,
            legacyContext,
            scope,
            1,
            entry => entry.Content is ItemDataSO,
            history,
            facts);
        ContentRollOutcome outcome = service.Roll(request);
        return outcome.HasAny ? outcome.Selections[0].ToLegacyItem() : default;
    }

    public void RecordPick(ContentPoolSO pool, Player player, RunContentHistory history, ContentRollItem item)
    {
        if (history == null || item.Content == null)
        {
            return;
        }

        history.RecordPick(CreateScope(pool, player), item);
    }

    public ContentRollScope CreateScope(ContentPoolSO pool, Player player)
    {
        string poolId = pool != null ? pool.name : ContentPoolScopeIds.Shop;
        string ownerId = player != null ? player.GetInstanceID().ToString() : string.Empty;
        return new ContentRollScope(ContentPoolScopeIds.Shop, poolId, ownerId);
    }
}
