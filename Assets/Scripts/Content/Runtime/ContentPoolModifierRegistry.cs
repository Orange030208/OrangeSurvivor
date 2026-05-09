using System.Collections.Generic;

public static class ContentPoolModifierRegistry
{
    private static readonly List<IContentPoolModifier> modifiers = new();
    private static readonly List<IContentPoolModifier> sortedModifiers = new();
    private static bool isDirty;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        modifiers.Clear();
        sortedModifiers.Clear();
        isDirty = false;
    }

    public static IReadOnlyList<IContentPoolModifier> ActiveModifiers
    {
        get
        {
            RebuildSortedModifiersIfNeeded();
            return sortedModifiers;
        }
    }

    public static void Register(IContentPoolModifier modifier)
    {
        if (modifier == null || modifiers.Contains(modifier))
        {
            return;
        }

        modifiers.Add(modifier);
        isDirty = true;
    }

    public static void Unregister(IContentPoolModifier modifier)
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

#if UNITY_EDITOR
    public static void ClearForTests()
    {
        modifiers.Clear();
        sortedModifiers.Clear();
        isDirty = false;
    }
#endif

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

    private static int ComparePriority(IContentPoolModifier left, IContentPoolModifier right)
    {
        int leftPriority = left != null ? left.Priority : 0;
        int rightPriority = right != null ? right.Priority : 0;
        return leftPriority.CompareTo(rightPriority);
    }
}
