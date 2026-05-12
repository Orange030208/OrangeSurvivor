using System;

public static class RunProgressionRuntime
{
    private static IRunProgressionProvider provider;

    public static RunProgressionSnapshot CurrentSnapshot =>
        provider != null ? provider.CurrentSnapshot : RunProgressionSnapshot.Default;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        provider = null;
    }

    public static void SetProvider(IRunProgressionProvider nextProvider)
    {
        provider = nextProvider ?? throw new ArgumentNullException(nameof(nextProvider));
    }

    public static void ClearProvider(IRunProgressionProvider expectedProvider)
    {
        if (provider == expectedProvider)
        {
            provider = null;
        }
    }

    public static bool TryGetProvider(out IRunProgressionProvider resolvedProvider)
    {
        resolvedProvider = provider;
        return resolvedProvider != null;
    }
}
