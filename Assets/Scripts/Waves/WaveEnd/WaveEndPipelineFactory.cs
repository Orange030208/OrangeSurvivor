using System.Collections.Generic;
using UnityEngine;

public static class WaveEndPipelineFactory
{
    public static WaveEndPipeline CreateDefault()
    {
        return CreateDefault(null, null);
    }

    public static WaveEndPipeline CreateDefault(Player player, EnemyRegistry enemyRegistry)
    {
        List<IWaveEndStep> defaultSteps = new()
        {
            new PrepareWaveEndRuntimeStep(player, enemyRegistry)
        };

        AddActiveSceneSteps(defaultSteps, player);
        return new WaveEndPipeline(WaveEndStepRegistry.MergeWithRegisteredSteps(defaultSteps));
    }

    private static void AddActiveSceneSteps(List<IWaveEndStep> steps, Player player)
    {
        MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == player)
            {
                continue;
            }

            if (behaviour is IWaveEndStep waveEndStep)
            {
                steps.Add(waveEndStep);
            }
        }
    }
}
