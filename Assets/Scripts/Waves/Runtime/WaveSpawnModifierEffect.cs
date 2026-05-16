using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class WaveSpawnModifier : FeatureBase, IWaveSpawnModifier
{
    [SerializeField] private int priority;
    [SerializeField] private int minWaveNumber = 1;
    [SerializeField] private int maxWaveNumber;
    [SerializeField] private string waveId;

    public virtual int Priority => priority;

    public override void OnInstall()
    {
        WaveSpawnModifierRegistry.Register(this);
        if (this is IContentPoolModifier contentPoolModifier)
        {
            ContentPoolModifierRegistry.Register(contentPoolModifier);
        }
    }

    public override void OnUninstall()
    {
        WaveSpawnModifierRegistry.Unregister(this);
        if (this is IContentPoolModifier contentPoolModifier)
        {
            ContentPoolModifierRegistry.Unregister(contentPoolModifier);
        }
    }

    public virtual void OnWaveStarted(WaveSpawnContext context)
    {
    }

    public virtual void OnWaveEnded(WaveSpawnContext context)
    {
    }

    public virtual void ModifySchedule(WaveSpawnModifierContext context, WaveSpawnSchedule schedule)
    {
    }

    public virtual void ModifySpawnRequest(WaveSpawnModifierContext context, WaveSpawnRequest request)
    {
    }

    public virtual void AppendSpawnRequests(WaveSpawnModifierContext context, List<WaveSpawnRequest> requests)
    {
    }

    protected bool AffectsWave(WaveSpawnContext context)
    {
        if (context.WaveNumber < Mathf.Max(1, minWaveNumber))
        {
            return false;
        }

        if (maxWaveNumber > 0 && context.WaveNumber > maxWaveNumber)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(waveId) || string.Equals(waveId, context.WaveId, System.StringComparison.Ordinal);
    }

    protected static bool MatchesTags(WaveEnemyTag sourceTags, WaveEnemyTag requiredTags)
    {
        return requiredTags == WaveEnemyTag.None || (sourceTags & requiredTags) != 0;
    }

    protected static bool MatchesTrack(WaveSegment segment, string requiredTrackId)
    {
        return string.IsNullOrWhiteSpace(requiredTrackId)
            || string.Equals(segment.TrackId, requiredTrackId, System.StringComparison.Ordinal);
    }
}
