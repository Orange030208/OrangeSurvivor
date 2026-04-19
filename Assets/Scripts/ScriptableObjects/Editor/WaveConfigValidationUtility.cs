#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

public static class WaveConfigValidationUtility
{
    [MenuItem("Tools/Waves/Validate Stage Definition")]
    public static void ValidateStageDefinition()
    {
        StageDefinitionSO stageDefinition = ResourcesManager.GetStageDefinition();
        if (stageDefinition == null)
        {
            Debug.LogError("[WaveConfigValidation] Missing Stage Definition asset at Resources/Data/Waves/Stage Definition.");
            return;
        }

        StringBuilder builder = new();
        bool hasError = false;
        WaveDefinitionSO[] waves = stageDefinition.Waves;
        if (waves == null || waves.Length == 0)
        {
            builder.AppendLine("StageDefinitionSO has no waves configured.");
            hasError = true;
        }
        else
        {
            for (int i = 0; i < waves.Length; i++)
            {
                WaveDefinitionSO wave = waves[i];
                if (wave == null)
                {
                    builder.AppendLine($"Wave index {i} is null.");
                    hasError = true;
                    continue;
                }

                ValidateWave(wave, i, builder, ref hasError);
            }
        }

        if (hasError)
        {
            Debug.LogError($"[WaveConfigValidation]\n{builder}");
            return;
        }

        Debug.Log($"[WaveConfigValidation] {stageDefinition.name} passed validation.");
    }

    private static void ValidateWave(WaveDefinitionSO wave, int waveIndex, StringBuilder builder, ref bool hasError)
    {
        if (wave.SpawnLocationPolicy == null)
        {
            builder.AppendLine($"Wave[{waveIndex}] {wave.name} is missing SpawnLocationPolicy.");
            hasError = true;
        }

        WaveSpawnPlan[] spawnPlans = wave.SpawnPlans;
        if (spawnPlans == null || spawnPlans.Length == 0)
        {
            builder.AppendLine($"Wave[{waveIndex}] {wave.name} has no spawn plans.");
            hasError = true;
            return;
        }

        bool containsBossEnemy = false;
        for (int planIndex = 0; planIndex < spawnPlans.Length; planIndex++)
        {
            WaveSpawnPlan spawnPlan = spawnPlans[planIndex];
            if (spawnPlan.EnemyDefinition == null)
            {
                builder.AppendLine($"Wave[{waveIndex}] {wave.name} spawn plan {planIndex} is missing enemy definition.");
                hasError = true;
                continue;
            }

            if (spawnPlan.EnemyDefinition.Role == EnemyRole.Boss)
            {
                containsBossEnemy = true;
            }

            if (spawnPlan.EnemyDefinition.AttackDefinition == null)
            {
                builder.AppendLine($"Wave[{waveIndex}] {wave.name} spawn plan {planIndex} enemy definition is missing attack definition.");
                hasError = true;
            }
        }

        ValidateCompletionType(wave, waveIndex, containsBossEnemy, builder, ref hasError);
        ValidateFlowAndReward(wave, waveIndex, builder, ref hasError);
    }

    private static void ValidateCompletionType(WaveDefinitionSO wave, int waveIndex, bool containsBossEnemy, StringBuilder builder, ref bool hasError)
    {
        if (wave.CompletionType != WaveCompletionType.BossDefeated)
        {
            return;
        }

        if (!containsBossEnemy)
        {
            builder.AppendLine($"Wave[{waveIndex}] {wave.name} uses BossDefeated but no boss enemy is configured in spawn plans.");
            hasError = true;
        }
    }

    private static void ValidateFlowAndReward(WaveDefinitionSO wave, int waveIndex, StringBuilder builder, ref bool hasError)
    {
        WaveFlowDefinitionSO flowDefinition = wave.FlowDefinition;
        WaveRewardDefinitionSO rewardDefinition = wave.RewardDefinition;
        if (flowDefinition == null)
        {
            return;
        }

        if (flowDefinition.SkipToNextWaveImmediately && flowDefinition.ShopMode == WaveShopMode.AlwaysEnterShop)
        {
            builder.AppendLine($"Wave[{waveIndex}] {wave.name} cannot skip immediately and always enter shop at the same time.");
            hasError = true;
        }

        if (flowDefinition.SkipToNextWaveImmediately && flowDefinition.TransitionMode == WaveTransitionMode.AlwaysEnterTransition)
        {
            builder.AppendLine($"Wave[{waveIndex}] {wave.name} cannot skip immediately and always enter transition at the same time.");
            hasError = true;
        }

        if (flowDefinition.ShopMode == WaveShopMode.NeverEnterShop && rewardDefinition != null && rewardDefinition.GrantShopEntry)
        {
            builder.AppendLine($"Wave[{waveIndex}] {wave.name} reward grants shop entry but flow definition forbids shop.");
        }
    }
}
#endif
