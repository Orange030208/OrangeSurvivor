using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class WaveBossSpawnConfigurationTests
{
    private const string StagePath = "Assets/GameContent/Waves/Data/Stage Definition.asset";
    private const string WaveSpawnPoolPath = "Assets/GameContent/Waves/Pools/Wave Spawn Pool.asset";
    private const string BossEnemyPath = "Assets/GameContent/Enemies/Data/Golem Mecha Stone/MechaStoneBossEnemy.asset";
    private const string BossTrackId = "Boss Gate";

    [Test]
    public void RuntimeWaveMappingPreservesBossCompletionMode()
    {
        StageDefinitionSO stage = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(StagePath);
        Assert.NotNull(stage);

        Wave[] waves = WaveDefinitionMapper.ToRuntimeWaves(stage);

        Assert.That(waves, Has.Length.GreaterThanOrEqualTo(20));
        Assert.AreEqual(WaveCompletionMode.BossDefeated, waves[9].CompletionMode);
        Assert.AreEqual(WaveCompletionMode.BossDefeated, waves[14].CompletionMode);
        Assert.AreEqual(WaveCompletionMode.BossDefeated, waves[19].CompletionMode);
        Assert.AreEqual(WaveCompletionMode.TimerOnly, waves[0].CompletionMode);
    }

    [TestCase(10)]
    [TestCase(15)]
    [TestCase(20)]
    public void BossGateRollsMechaStoneBossOnConfiguredWaves(int waveNumber)
    {
        ContentPoolSO pool = LoadRequired<ContentPoolSO>(WaveSpawnPoolPath);
        EnemySO bossEnemy = LoadRequired<EnemySO>(BossEnemyPath);
        ContentRollContext context = CreateWaveSpawnRollContext(waveNumber, BossTrackId);

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, context, 1, entry => entry.Content is EnemySO || entry.Content is WaveSpawnPackSO);

        Assert.IsTrue(result.HasAny);
        Assert.AreSame(bossEnemy, result.Items[0].Content);
        Assert.IsTrue(result.Items[0].TryGetMetadata(out WaveSpawnMetadata metadata));
        Assert.AreEqual(WaveEnemyTag.BossLike, metadata.Tags);
    }

    [TestCase(10)]
    [TestCase(15)]
    [TestCase(20)]
    public void BossGateUsesFullWaveTriggerWindow(int waveNumber)
    {
        WaveDefinitionSO waveDefinition = LoadRequired<WaveDefinitionSO>(WaveDefinitionPath(waveNumber));
        WaveSpawnPlan bossGate = FindRequiredSpawnPlan(waveDefinition, BossTrackId);

        Assert.AreEqual(WaveSpawnTriggerMode.OnceOnEnter, bossGate.TriggerMode);
        Assert.AreEqual(1, bossGate.MaxSpawnBatches);
        Assert.AreEqual(0f, bossGate.NormalizedTimeRange.x);
        Assert.AreEqual(100f, bossGate.NormalizedTimeRange.y);
    }

    [TestCase(10, "Veteran Mix")]
    [TestCase(15, "High Density Mix")]
    [TestCase(20, "Finale Core")]
    public void NonBossTracksDoNotRollMechaStoneBoss(int waveNumber, string trackId)
    {
        ContentPoolSO pool = LoadRequired<ContentPoolSO>(WaveSpawnPoolPath);
        EnemySO bossEnemy = LoadRequired<EnemySO>(BossEnemyPath);
        ContentRollContext context = CreateWaveSpawnRollContext(waveNumber, trackId);

        ContentRollResult result = new ContentPoolRollService(new SystemContentRandom(1))
            .Roll(pool, context, 8, entry => entry.Content is EnemySO || entry.Content is WaveSpawnPackSO);

        Assert.IsTrue(result.HasAny);
        for (int i = 0; i < result.Items.Count; i++)
        {
            Assert.AreNotSame(bossEnemy, result.Items[i].Content);
        }
    }

    private static ContentRollContext CreateWaveSpawnRollContext(int waveNumber, string trackId)
    {
        return new ContentRollContext(
            ContentPoolScopeIds.WaveSpawn,
            progressionSnapshot: new RunProgressionSnapshot(waveNumber, 20, 0f, 0, 1f, 1f, 1f, 0),
            waveTrackId: trackId,
            waveProgressPercent: 0f);
    }

    private static string WaveDefinitionPath(int waveNumber)
    {
        return $"Assets/GameContent/Waves/Data/Wave Definition {waveNumber:00}.asset";
    }

    private static WaveSpawnPlan FindRequiredSpawnPlan(WaveDefinitionSO waveDefinition, string trackId)
    {
        WaveSpawnPlan[] spawnPlans = waveDefinition.SpawnPlans;
        Assert.NotNull(spawnPlans);
        for (int i = 0; i < spawnPlans.Length; i++)
        {
            WaveSpawnPlan spawnPlan = spawnPlans[i];
            if (spawnPlan.TrackId == trackId)
            {
                return spawnPlan;
            }
        }

        Assert.Fail($"Missing required wave spawn plan '{trackId}' on {waveDefinition.name}.");
        return default;
    }

    private static T LoadRequired<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.NotNull(asset, $"Missing required test asset at {path}.");
        return asset;
    }
}
