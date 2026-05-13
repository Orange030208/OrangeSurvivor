using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class PrepareWaveEndRuntimeStep : IWaveEndStep
{
    private readonly Player player;
    private readonly EnemyRegistry enemyRegistry;

    public PrepareWaveEndRuntimeStep(Player player, EnemyRegistry enemyRegistry)
    {
        this.player = player;
        this.enemyRegistry = enemyRegistry;
    }

    public int WaveEndPriority => WaveEndPriorities.PrepareRuntime;

    public UniTask ExecuteWaveEndAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        enemyRegistry?.CancelPendingEnemySpawns();
        StopPlayerCombat();

        return UniTask.CompletedTask;
    }

    private void StopPlayerCombat()
    {
        if (player == null)
        {
            return;
        }

        player.MoveComponent?.StopMoving();
        if (player.MoveComponent is IMovementLockable movementLockable)
        {
            movementLockable.AddMovementLock(typeof(WaveEndPipeline));
        }

        if (player.TryGetComponent(out WeaponsHolder weaponsHolder))
        {
            weaponsHolder.DisableWeaponsForWaveCleanup();
        }
    }
}
