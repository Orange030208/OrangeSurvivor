public interface IGameFlowController
{
    GameState CurrentGameState { get; }

    void RequestSimulationPause(string sourceId);
    void ReleaseSimulationPause(string sourceId);
    void StopCurrentWave();
    void ResumeCurrentWave();
}
