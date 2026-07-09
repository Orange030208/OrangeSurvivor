public interface IEnemyRegistry
{
    int AliveEnemyCount { get; }
    int AliveBossCount { get; }

    void DefeatAllTrackedEnemies();
    Enemy[] CreateAliveEnemySnapshot();
    void CancelPendingEnemySpawns();
    void ClearTracking();
}
