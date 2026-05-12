using UnityEngine;

/// <summary>
/// 局内推进状态服务。由波次系统驱动，向敌人、掉落、商店提供统一的局内系数。
/// </summary>
public sealed class RunProgressionService : IRunProgressionProvider
{
    private readonly RunProgressionProfileSO profile;
    private int currentWaveNumber = 1;
    private int totalWaves = 1;
    private float runElapsedSeconds;

    public RunProgressionService(RunProgressionProfileSO profile = null)
    {
        this.profile = profile != null ? profile : RunProgressionProfileSO.CreateRuntimeDefault();
    }

    public RunProgressionSnapshot CurrentSnapshot => profile.Evaluate(currentWaveNumber, totalWaves, runElapsedSeconds);

    public void Reset(int nextTotalWaves)
    {
        totalWaves = Mathf.Max(1, nextTotalWaves);
        currentWaveNumber = 1;
        runElapsedSeconds = 0f;
    }

    public void BeginWave(int waveNumber, int nextTotalWaves)
    {
        currentWaveNumber = Mathf.Max(1, waveNumber);
        totalWaves = Mathf.Max(1, nextTotalWaves);
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime > 0f)
        {
            runElapsedSeconds += deltaTime;
        }
    }

    public void SetRunTime(float elapsedSeconds)
    {
        runElapsedSeconds = Mathf.Max(0f, elapsedSeconds);
    }

    public void CompleteWave(int completedWaveNumber)
    {
        currentWaveNumber = Mathf.Max(1, completedWaveNumber);
    }

    public RunProgressionSnapshot CreateSnapshot()
    {
        return CurrentSnapshot;
    }

    public RunProgressionEnemyScale EvaluateEnemyScale(EnemySO enemyData)
    {
        return profile.EvaluateEnemyScale(CurrentSnapshot, enemyData);
    }

    public RunProgressionEnemyScale EvaluateEnemyScale(EnemySO enemyData, WaveEnemyTag enemyTags)
    {
        return profile.EvaluateEnemyScale(CurrentSnapshot, enemyData, enemyTags);
    }

    public RunProgressionProfileSO Profile => profile;
}
