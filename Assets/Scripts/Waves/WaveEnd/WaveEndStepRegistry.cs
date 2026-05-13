using System.Collections.Generic;
using UnityEngine;

public static class WaveEndStepRegistry
{
    private static readonly List<IWaveEndStep> registeredSteps = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        registeredSteps.Clear();
    }

    public static void Register(IWaveEndStep step)
    {
        if (step == null || registeredSteps.Contains(step))
        {
            return;
        }

        registeredSteps.Add(step);
    }

    public static void Unregister(IWaveEndStep step)
    {
        if (step == null)
        {
            return;
        }

        registeredSteps.Remove(step);
    }

    public static IWaveEndStep[] MergeWithRegisteredSteps(IReadOnlyList<IWaveEndStep> defaultSteps)
    {
        int defaultCount = defaultSteps?.Count ?? 0;
        IWaveEndStep[] mergedSteps = new IWaveEndStep[defaultCount + registeredSteps.Count];
        for (int i = 0; i < defaultCount; i++)
        {
            mergedSteps[i] = defaultSteps[i];
        }

        for (int i = 0; i < registeredSteps.Count; i++)
        {
            mergedSteps[defaultCount + i] = registeredSteps[i];
        }

        return mergedSteps;
    }
}
