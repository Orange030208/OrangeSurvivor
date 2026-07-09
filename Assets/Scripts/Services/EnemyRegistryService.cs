using System;
using Orange.GameServices;

[Serializable]
public sealed class EnemyRegistryService : GameService, IEnemyRegistry
{
    private readonly EnemyRegistryRuntime runtimeRegistry = new();

    public int AliveEnemyCount => runtimeRegistry.AliveEnemyCount;
    public int AliveBossCount => runtimeRegistry.AliveBossCount;

    protected override void RegisterContracts(GameServiceRegistry registry)
    {
        registry.Register<IEnemyRegistry>(this);
    }

    protected override void OnAttach()
    {
        runtimeRegistry.StartListening();
    }

    protected override void OnDispose()
    {
        runtimeRegistry.Dispose();
    }

    public void DefeatAllTrackedEnemies()
    {
        runtimeRegistry.DefeatAllTrackedEnemies();
    }

    public Enemy[] CreateAliveEnemySnapshot()
    {
        return runtimeRegistry.CreateAliveEnemySnapshot();
    }

    public void CancelPendingEnemySpawns()
    {
        runtimeRegistry.CancelPendingEnemySpawns();
    }

    public void ClearTracking()
    {
        runtimeRegistry.ClearTracking();
    }
}
