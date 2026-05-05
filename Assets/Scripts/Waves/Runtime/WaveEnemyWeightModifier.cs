using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class WaveEnemyWeightModifier : WaveSpawnModifierEffect
{
    [SerializeField] private EnemySO targetEnemyDefinition;
    [SerializeField] private WaveEnemyTag targetTags = WaveEnemyTag.Special;
    [SerializeField] private float weightMultiplier = 1f;
    [SerializeField] private float additionalWeight;

    public override string Description
    {
        get
        {
            string targetName = targetEnemyDefinition != null ? targetEnemyDefinition.name : targetTags.ToString();
            return $"调整{targetName}的刷怪权重。";
        }
    }

    public override void ModifyEnemyCandidates(WaveSpawnModifierContext context, List<WaveEnemySpawnCandidate> candidates)
    {
        if (!AffectsWave(context.SpawnContext) || candidates == null)
        {
            return;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            WaveEnemySpawnCandidate candidate = candidates[i];
            if (candidate == null || candidate.EnemyDefinition == null)
            {
                continue;
            }

            if (targetEnemyDefinition != null && candidate.EnemyDefinition != targetEnemyDefinition)
            {
                continue;
            }

            if (targetEnemyDefinition == null && !MatchesTags(candidate.Tags, targetTags))
            {
                continue;
            }

            candidate.Weight = Mathf.Max(0f, candidate.Weight * Mathf.Max(0f, weightMultiplier) + additionalWeight);
        }
    }
}
