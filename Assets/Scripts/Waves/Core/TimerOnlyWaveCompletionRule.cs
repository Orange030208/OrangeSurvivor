public sealed class TimerOnlyWaveCompletionRule : IWaveCompletionRule
{
    public bool ShowsCountdownTimer => true;
    public bool PlaysCountdownWarning => true;

    public WaveCompletionDecision OnTimerElapsed(WaveCompletionContext context)
    {
        return WaveCompletionDecision.Complete;
    }

    public WaveCompletionDecision OnEnemyRegistered(EnemyRole role, WaveCompletionContext context)
    {
        return WaveCompletionDecision.None;
    }

    public WaveCompletionDecision OnEnemyDied(EnemyRole role, WaveCompletionContext context)
    {
        return WaveCompletionDecision.None;
    }
}
