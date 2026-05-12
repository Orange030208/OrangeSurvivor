public sealed class BossDefeatedWaveCompletionRule : IWaveCompletionRule
{
    private int bossSpawnedCount;
    private int bossDefeatedCount;

    public bool ShowsCountdownTimer => false;
    public bool PlaysCountdownWarning => false;

    public WaveCompletionDecision OnTimerElapsed(WaveCompletionContext context)
    {
        if (bossSpawnedCount <= 0)
        {
            string diagnosticError = $"{nameof(BossDefeatedWaveCompletionRule)}: Wave {context.WaveNumber} requires boss defeat but no boss was spawned. Completing by timer fallback.";
            return WaveCompletionDecision.CompleteWithStoppedTimer(diagnosticError);
        }

        return WaveCompletionDecision.StopWithoutCompletion;
    }

    public WaveCompletionDecision OnEnemyRegistered(EnemyRole role, WaveCompletionContext context)
    {
        if (role != EnemyRole.Boss)
        {
            return WaveCompletionDecision.None;
        }

        bossSpawnedCount++;
        return WaveCompletionDecision.None;
    }

    public WaveCompletionDecision OnEnemyDied(EnemyRole role, WaveCompletionContext context)
    {
        if (role != EnemyRole.Boss || bossSpawnedCount <= 0)
        {
            return WaveCompletionDecision.None;
        }

        bossDefeatedCount++;
        return bossDefeatedCount >= bossSpawnedCount
            ? WaveCompletionDecision.Complete
            : WaveCompletionDecision.None;
    }
}
