using System;
using System.Collections.Generic;

public sealed class RewardContentRoller
{
    private readonly IContentPoolService service;

    public RewardContentRoller(IContentPoolService service = null)
    {
        this.service = service ?? new ContentPoolServiceV2();
    }

    public ContentRollResult Roll(
        ContentPoolSO pool,
        ContentRollContext legacyContext,
        int? rollCountOverride,
        Predicate<ContentPoolEntryDefinition> entryFilter,
        RunContentHistory history,
        ContentFactSet facts = null)
    {
        if (pool == null)
        {
            return new ContentRollResult(Array.Empty<ContentRollItem>());
        }

        ContentRollScope scope = new(
            legacyContext != null ? legacyContext.ScopeId : ContentPoolScopeIds.Generic,
            pool.name,
            ResolveOwnerId(legacyContext));
        ContentRollRequest request = LegacyContentPoolAdapter.CreateRequest(
            pool,
            legacyContext,
            scope,
            rollCountOverride,
            entryFilter,
            history,
            facts);
        return service.Roll(request).ToLegacyResult();
    }

    public void RecordPick(
        ContentPoolSO pool,
        string scopeId,
        Player player,
        RunContentHistory history,
        ContentRollItem item)
    {
        if (history == null || item.Content == null)
        {
            return;
        }

        string resolvedScopeId = ContentPoolScopeIds.Normalize(scopeId);
        string poolId = pool != null ? pool.name : resolvedScopeId;
        string ownerId = player != null ? player.GetInstanceID().ToString() : string.Empty;
        history.RecordPick(new ContentRollScope(resolvedScopeId, poolId, ownerId), item);
    }

    private static string ResolveOwnerId(ContentRollContext context)
    {
        return context?.Player != null ? context.Player.GetInstanceID().ToString() : string.Empty;
    }
}
