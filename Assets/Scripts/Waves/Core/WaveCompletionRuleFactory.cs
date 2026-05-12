public static class WaveCompletionRuleFactory
{
    public static IWaveCompletionRule Create(WaveCompletionMode completionMode)
    {
        return completionMode switch
        {
            WaveCompletionMode.TimerOnly => new TimerOnlyWaveCompletionRule(),
            WaveCompletionMode.BossDefeated => new BossDefeatedWaveCompletionRule(),
            _ => new TimerOnlyWaveCompletionRule()
        };
    }
}
