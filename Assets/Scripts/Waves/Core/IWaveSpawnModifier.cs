using System.Collections.Generic;

public interface IWaveSpawnModifier
{
    int Priority { get; }
    void OnWaveStarted(WaveSpawnContext context);
    void OnWaveEnded(WaveSpawnContext context);
    void ModifySchedule(WaveSpawnModifierContext context, WaveSpawnSchedule schedule);
    void ModifyEnemyCandidates(WaveSpawnModifierContext context, List<WaveEnemySpawnCandidate> candidates);
    void ModifySpawnRequest(WaveSpawnModifierContext context, WaveSpawnRequest request);
    void AppendSpawnRequests(WaveSpawnModifierContext context, List<WaveSpawnRequest> requests);
}
