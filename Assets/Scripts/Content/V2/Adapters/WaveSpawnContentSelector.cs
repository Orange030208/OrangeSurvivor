public sealed class WaveSpawnContentSelector
{
    private readonly IContentPoolService service;

    public WaveSpawnContentSelector(IContentPoolService service = null)
    {
        this.service = service ?? new ContentPoolServiceV2();
    }

    public ContentRollItem? Select(
        ContentPoolSO pool,
        WaveSpawnModifierContext modifierContext,
        RunContentHistory history)
    {
        if (pool == null)
        {
            return null;
        }

        ContentRollScope scope = CreateScope(pool);
        ContentRollContext legacyContext = new(
            ContentPoolScopeIds.WaveSpawn,
            modifierContext.SpawnContext.Player,
            waveSpawn: modifierContext.SpawnContext,
            progressionSnapshot: modifierContext.SpawnContext.ProgressionSnapshot,
            historyScope: scope.ToHistoryScope(),
            history: history != null ? history.State : null,
            source: modifierContext.SpawnContext.SpawnAnchor,
            waveTrackId: modifierContext.Segment.TrackId,
            waveProgressPercent: modifierContext.SpawnContext.NormalizedProgress * 100f);
        ContentFactSet facts = new ContentFactSet()
            .Set(ContentFactKeys.Player, modifierContext.SpawnContext.Player)
            .Set(ContentFactKeys.Source, modifierContext.SpawnContext.SpawnAnchor)
            .Set(ContentFactKeys.WaveSpawn, modifierContext.SpawnContext)
            .Set(ContentFactKeys.ProgressionSnapshot, modifierContext.SpawnContext.ProgressionSnapshot)
            .Set(ContentFactKeys.WaveTrackId, modifierContext.Segment.TrackId)
            .Set(ContentFactKeys.WaveProgressPercent, modifierContext.SpawnContext.NormalizedProgress * 100f);
        ContentRollRequest request = LegacyContentPoolAdapter.CreateRequest(
            pool,
            legacyContext,
            scope,
            1,
            entry => entry.Content is EnemySO || entry.Content is WaveSpawnPackSO,
            history,
            facts);
        ContentRollOutcome outcome = service.Roll(request);
        return outcome.HasAny ? outcome.Selections[0].ToLegacyItem() : null;
    }

    public static ContentRollScope CreateScope(ContentPoolSO pool)
    {
        string poolId = pool != null ? pool.name : ContentPoolScopeIds.WaveSpawn;
        return new ContentRollScope(ContentPoolScopeIds.WaveSpawn, poolId);
    }
}
