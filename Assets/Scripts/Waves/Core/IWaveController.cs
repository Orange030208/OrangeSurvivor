public interface IWaveController
{
    WaveRuntimeState CurrentState { get; }
    bool HasCurrentWave { get; }
    bool HasMoreWaves { get; }
    WaveHudViewData CreateHudViewData();
    WaveRuntimeViewData CreateRuntimeViewData();
    void StartFirstWave();
    void StartNextWave();
    void StopCurrentWave();
    void ResumeCurrentWave();
    void ResetWaves();
}
