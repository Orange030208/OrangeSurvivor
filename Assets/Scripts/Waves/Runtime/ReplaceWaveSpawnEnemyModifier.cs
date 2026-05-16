using System;
using UnityEngine;

[Serializable]
public sealed class ReplaceWaveSpawnEnemyModifier : WaveSpawnModifier
{
    [SerializeField] private EnemySO sourceEnemyDefinition;
    [SerializeField] private WaveEnemyTag sourceTags = WaveEnemyTag.Normal;
    [SerializeField] private EnemySO replacementEnemyDefinition;
    [SerializeField] private WaveEnemyTag replacementTags = WaveEnemyTag.Special;
    [Range(0f, 1f)]
    [SerializeField] private float replacementChance = 1f;

    public override string Description
    {
        get
        {
            string targetName = replacementEnemyDefinition != null ? replacementEnemyDefinition.name : "指定特殊怪";
            return $"刷怪时有 {replacementChance:P0} 概率替换为{targetName}。";
        }
    }

    public override void ModifySpawnRequest(WaveSpawnModifierContext context, WaveSpawnRequest request)
    {
        if (!AffectsWave(context.SpawnContext) || request == null || replacementEnemyDefinition == null)
        {
            return;
        }

        if (sourceEnemyDefinition != null && request.EnemyDefinition != sourceEnemyDefinition)
        {
            return;
        }

        if (sourceEnemyDefinition == null && !MatchesTags(request.EnemyTags, sourceTags))
        {
            return;
        }

        if (context.SpawnContext.Roll01() > Mathf.Clamp01(replacementChance))
        {
            return;
        }

        request.EnemyDefinition = replacementEnemyDefinition;
        request.EnemyTags = replacementTags;
    }
}
