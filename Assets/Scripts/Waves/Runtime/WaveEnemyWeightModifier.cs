using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
/// <summary>
/// 刷怪权重 Modifier 直接作用于 WaveSpawn ContentPool 候选，不再维护旧的波次敌人候选列表。
/// </summary>
public sealed class WaveEnemyWeightModifier : FeatureEffectBase, IContentPoolModifier
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

    public bool AffectsPurpose(ContentPoolPurpose purpose)
    {
        return purpose == ContentPoolPurpose.WaveSpawn;
    }

    public void ModifyCandidates(ContentPoolEvaluationContext context, List<ContentPoolCandidate> candidates)
    {
        if (candidates == null || !AffectsWaveFacts(context))
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

            // WaveSpawn 池用 DomainFlags 保存 WaveEnemyTag，避免候选抽取层反向依赖刷怪业务类型。
            WaveEnemyTag candidateTags = (WaveEnemyTag)candidate.DomainFlags;
            if (targetEnemyDefinition == null &&
                !MatchesTags(candidateTags == WaveEnemyTag.None ? WaveEnemyTag.Normal : candidateTags, targetTags))
            {
                continue;
            }

            candidate.Weight = Mathf.Max(0f, candidate.Weight * Mathf.Max(0f, weightMultiplier) + additionalWeight);
        }
    }

    private bool AffectsWaveFacts(ContentPoolEvaluationContext context)
    {
        // 波次范围从事实快照读取，保证 Modifier 与池条件使用同一份上下文。
        int currentWave = 1;
        if (context?.Facts != null &&
            context.Facts.TryGet(ContentFactIds.CurrentWave, out ContentFactValue waveValue) &&
            waveValue.TryGetNumber(out float waveNumber))
        {
            currentWave = Mathf.Max(1, Mathf.RoundToInt(waveNumber));
        }

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

        return context?.Facts != null &&
               context.Facts.TryGet(ContentFactIds.WaveId, out ContentFactValue waveIdValue) &&
               string.Equals(waveId, waveIdValue.StringValue, StringComparison.Ordinal);
    }

    private static bool MatchesTags(WaveEnemyTag sourceTags, WaveEnemyTag requiredTags)
    {
        return requiredTags == WaveEnemyTag.None || (sourceTags & requiredTags) != 0;
    }
}
