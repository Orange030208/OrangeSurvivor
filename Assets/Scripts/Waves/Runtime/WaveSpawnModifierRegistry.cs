using System;
using System.Collections.Generic;
using UnityEngine;

public static class WaveSpawnModifierRegistry
{
    private static readonly List<IWaveSpawnModifier> modifiers = new();
    private static readonly List<IWaveSpawnModifier> sortedModifiers = new();
    private static bool isDirty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        modifiers.Clear();
        sortedModifiers.Clear();
        isDirty = false;
    }

    public static IReadOnlyList<IWaveSpawnModifier> ActiveModifiers
    {
        get
        {
            RebuildSortedModifiersIfNeeded();
            return sortedModifiers;
        }
    }

    public static void Register(IWaveSpawnModifier modifier)
    {
        if (modifier == null || modifiers.Contains(modifier))
        {
            return;
        }

        modifiers.Add(modifier);
        isDirty = true;
    }

    public static void Unregister(IWaveSpawnModifier modifier)
    {
        if (modifier == null)
        {
            return;
        }

        if (modifiers.Remove(modifier))
        {
            isDirty = true;
        }
    }

    public static void NotifyWaveStarted(WaveSpawnContext context)
    {
        IReadOnlyList<IWaveSpawnModifier> activeModifiers = ActiveModifiers;
        for (int i = 0; i < activeModifiers.Count; i++)
        {
            activeModifiers[i].OnWaveStarted(context);
        }
    }

    public static void NotifyWaveEnded(WaveSpawnContext context)
    {
        IReadOnlyList<IWaveSpawnModifier> activeModifiers = ActiveModifiers;
        for (int i = 0; i < activeModifiers.Count; i++)
        {
            activeModifiers[i].OnWaveEnded(context);
        }
    }

    private static void RebuildSortedModifiersIfNeeded()
    {
        if (!isDirty)
        {
            return;
        }

        sortedModifiers.Clear();
        sortedModifiers.AddRange(modifiers);
        sortedModifiers.Sort(ComparePriority);
        isDirty = false;
    }

    private static int ComparePriority(IWaveSpawnModifier left, IWaveSpawnModifier right)
    {
        int leftPriority = left != null ? left.Priority : 0;
        int rightPriority = right != null ? right.Priority : 0;
        return leftPriority.CompareTo(rightPriority);
    }
}
