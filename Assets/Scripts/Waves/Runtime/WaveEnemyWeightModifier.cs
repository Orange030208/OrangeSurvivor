using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
/// <summary>
/// 刷怪权重 Modifier 直接作用于 WaveSpawn ContentPool 候选，不再维护旧的波次敌人候选列表。
/// </summary>
public sealed class WaveEnemyWeightModifier : FeatureBase, IContentPoolModifier
{
    [SerializeField] private int priority;
    [SerializeField] private int minWaveNumber = 1;
    [SerializeField] private int maxWaveNumber;
    [SerializeField] private string waveId;
    [SerializeField] private EnemySO targetEnemyDefinition;
    [SerializeField] private WaveEnemyTag targetTags = WaveEnemyTag.Special;
    [SerializeField] private float weightMultiplier = 1f;
    [SerializeField] private float additionalWeight;

    public int Priority => priority;

    public override string Description
    {
        get
        {
            string targetName = targetEnemyDefinition != null ? targetEnemyDefinition.name : targetTags.ToString();
            return $"调整{targetName}的刷怪权重。";
        }
    }

    public override void OnInstall()
    {
        ContentPoolModifierRegistry.Register(this);
    }

    public override void OnUninstall()
    {
        ContentPoolModifierRegistry.Unregister(this);
    }

    public bool AffectsContext(ContentRollContext context)
    {
        return context != null &&
               string.Equals(context.ScopeId, ContentPoolScopeIds.WaveSpawn, StringComparison.Ordinal);
    }

    public void ModifyCandidates(ContentRollContext context, List<ContentPoolCandidate> candidates)
    {
        if (candidates == null || !AffectsContext(context) || !AffectsWaveContext(context))
        {
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            ContentPoolCandidate candidate = candidates[i];
            if (candidate == null || candidate.Content == null)
            {
                continue;
            }

            if (targetEnemyDefinition != null && candidate.Content != targetEnemyDefinition)
            {
                continue;
            }

            WaveEnemyTag candidateTags = ResolveCandidateTags(candidate);
            if (targetEnemyDefinition == null &&
                !MatchesTags(candidateTags, targetTags))
            {
                continue;
            }

            candidate.Weight = Mathf.Max(0f, candidate.Weight * Mathf.Max(0f, weightMultiplier) + additionalWeight);
        }
    }

    private static WaveEnemyTag ResolveCandidateTags(ContentPoolCandidate candidate)
    {
        if (candidate != null && candidate.TryGetMetadata(out WaveSpawnMetadata metadata))
        {
            return metadata.Tags;
        }

        return WaveEnemyTag.Normal;
    }

    private bool AffectsWaveContext(ContentRollContext context)
    {
        int currentWave = context != null ? context.CurrentWaveNumber : 1;

        if (currentWave < Mathf.Max(1, minWaveNumber))
        {
            return false;
        }

        if (maxWaveNumber > 0 && currentWave > maxWaveNumber)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(waveId))
        {
            return true;
        }

        return context != null &&
               string.Equals(waveId, context.WaveId, StringComparison.Ordinal);
    }

    private static bool MatchesTags(WaveEnemyTag sourceTags, WaveEnemyTag requiredTags)
    {
        return requiredTags == WaveEnemyTag.None || (sourceTags & requiredTags) != 0;
    }
}
