using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class WaveEndPipeline
{
    private readonly List<IWaveEndStep> steps = new();

    public WaveEndPipeline(IEnumerable<IWaveEndStep> steps)
    {
        if (steps == null)
        {
            return;
        }

        foreach (IWaveEndStep step in steps)
        {
            if (step != null && !this.steps.Contains(step))
            {
                this.steps.Add(step);
            }
        }

        this.steps.Sort(CompareSteps);
    }

    public async UniTask RunAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < steps.Count;)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int priority = steps[i].WaveEndPriority;
            List<UniTask> priorityTasks = new();

            while (i < steps.Count && steps[i].WaveEndPriority == priority)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IWaveEndStep step = steps[i++];
                if (!IsDestroyedUnityObject(step))
                {
                    priorityTasks.Add(step.ExecuteWaveEndAsync(cancellationToken));
                }
            }

            if (priorityTasks.Count > 0)
            {
                await UniTask.WhenAll(priorityTasks);
            }
        }
    }

    private static int CompareSteps(IWaveEndStep left, IWaveEndStep right)
    {
        int priorityComparison = left.WaveEndPriority.CompareTo(right.WaveEndPriority);
        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        return string.CompareOrdinal(left.GetType().FullName, right.GetType().FullName);
    }

    private static bool IsDestroyedUnityObject(IWaveEndStep step)
    {
        return step is UnityEngine.Object unityObject && unityObject == null;
    }
}
