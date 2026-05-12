public interface IWaveCompletionRule
{
    bool ShowsCountdownTimer { get; }
    bool PlaysCountdownWarning { get; }

    WaveCompletionDecision OnTimerElapsed(WaveCompletionContext context);
    WaveCompletionDecision OnEnemyRegistered(EnemyRole role, WaveCompletionContext context);
    WaveCompletionDecision OnEnemyDied(EnemyRole role, WaveCompletionContext context);
}
