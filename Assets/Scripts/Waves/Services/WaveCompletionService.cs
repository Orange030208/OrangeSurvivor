using UnityEngine;

public class WaveCompletionService
{
    private readonly EnemyRuntimeRegistry enemyRuntimeRegistry;

    public WaveCompletionService(EnemyRuntimeRegistry enemyRuntimeRegistry)
    {
        this.enemyRuntimeRegistry = enemyRuntimeRegistry;
    }

    public WaveCompletionCheckResult EvaluatePerFrame(WaveRuntimeState runtimeState, float currentWaveDuration)
    {
        return runtimeState.CompletionType switch
        {
            WaveCompletionType.ClearAllEnemies => enemyRuntimeRegistry != null && enemyRuntimeRegistry.AliveEnemyCount == 0
                ? WaveCompletionCheckResult.Complete(WaveCompletionReason.ClearedAllEnemies)
                : WaveCompletionCheckResult.Continue(),
            WaveCompletionType.BossDefeated => WaveCompletionCheckResult.Continue(),
            _ => runtimeState.Timer >= currentWaveDuration
                ? WaveCompletionCheckResult.Complete(WaveCompletionReason.DurationElapsed)
                : WaveCompletionCheckResult.Continue()
        };
    }

    public WaveCompletionCheckResult EvaluateEntityDeath(WaveRuntimeState runtimeState, EntityDiedEvent eventData)
    {
        if (runtimeState.CompletionType != WaveCompletionType.BossDefeated)
        {
            return WaveCompletionCheckResult.Continue();
        }

        if (eventData.Entity is Enemy enemy && enemy.Role == EnemyRole.Boss)
        {
            return WaveCompletionCheckResult.Complete(WaveCompletionReason.BossDefeated);
        }

        return WaveCompletionCheckResult.Continue();
    }
}
