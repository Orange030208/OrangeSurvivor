using System;
using System.Collections.Generic;

public sealed class WaveDirectorRuntimeSession
{
    private readonly StageDirectorProfileSO profile;
    private readonly WaveDirectiveResolver resolver;
    private readonly IEnemySpawnExecutor enemySpawnExecutor;
    private readonly Dictionary<Enemy, SpawnedEnemyHandle> activeEnemies = new();

    private WaveSpawnDirector director;
    private ResolvedWaveDirective currentDirective;
    private SpawnPositionResolver defaultSpawnResolver;
    private float tickAccumulator;

    public WaveDirectorRuntimeSession(
        StageDirectorProfileSO profile,
        WaveDirectiveResolver resolver,
        IEnemySpawnExecutor enemySpawnExecutor)
    {
        this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
        this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        this.enemySpawnExecutor = enemySpawnExecutor ?? throw new ArgumentNullException(nameof(enemySpawnExecutor));
    }

    public ResolvedWaveDirective CurrentDirective => currentDirective;
    public WaveSpawnDirector CurrentDirector => director;

    public void BeginWave(int waveIndex)
    {
        currentDirective = resolver.Resolve(profile, waveIndex);
        director = new WaveSpawnDirector(currentDirective);
        defaultSpawnResolver = SpawnPositionResolver.FromDefinition(currentDirective.DefaultSpawnLocation);
        tickAccumulator = 0f;
        activeEnemies.Clear();
    }

    public void Advance(
        float previousTime,
        float currentTime,
        WaveDirectorExecutionContext context)
    {
        if (director == null || currentDirective == null)
        {
            return;
        }

        ExecuteCommands(director.CollectReadyBeatCommands(previousTime, currentTime), currentTime, context);

        tickAccumulator += Math.Max(0f, currentTime - previousTime);
        int catchUpTicks = 0;
        while (tickAccumulator >= profile.DirectorTickInterval &&
               catchUpTicks < profile.MaxCatchUpTicksPerFrame)
        {
            tickAccumulator -= profile.DirectorTickInterval;
            catchUpTicks++;

            if (!director.TryCreateTickCommand(currentTime, out EnemySpawnCommand command))
            {
                continue;
            }

            ExecuteCommand(command, currentTime, context, markBeatTriggered: false);
        }
    }

    public void NotifyEnemyUnregistered(Enemy enemy)
    {
        if (enemy == null || director == null)
        {
            return;
        }

        if (!activeEnemies.TryGetValue(enemy, out SpawnedEnemyHandle handle))
        {
            return;
        }

        activeEnemies.Remove(enemy);
        director.NotifyEnemyRemoved(handle.EntryId, handle.Role, handle.UnitCost);
    }

    public bool HasNextWave(int currentWaveIndex)
    {
        return resolver.HasNextWave(profile, currentWaveIndex);
    }

    public int GetDisplayTotalWaves(int currentWaveIndex)
    {
        return resolver.GetDisplayTotalWaves(profile, currentWaveIndex);
    }

    public int GetProgressionTotalWaves()
    {
        return resolver.GetProgressionTotalWaves(profile);
    }

    private void ExecuteCommands(
        IReadOnlyList<EnemySpawnCommand> commands,
        float currentTime,
        WaveDirectorExecutionContext context)
    {
        if (commands == null || commands.Count == 0)
        {
            return;
        }

        Dictionary<string, int> beatSpawnCounts = new(StringComparer.Ordinal);
        for (int i = 0; i < commands.Count; i++)
        {
            EnemySpawnCommand command = commands[i];
            int spawnedCount = ExecuteCommand(command, currentTime, context, markBeatTriggered: false);
            if (spawnedCount <= 0 || !command.IsScriptedBeat)
            {
                continue;
            }

            beatSpawnCounts[command.BeatId] = beatSpawnCounts.TryGetValue(command.BeatId, out int count)
                ? count + spawnedCount
                : spawnedCount;
        }

        foreach (KeyValuePair<string, int> pair in beatSpawnCounts)
        {
            if (pair.Value > 0)
            {
                director.MarkBeatTriggered(pair.Key);
            }
        }
    }

    private int ExecuteCommand(
        EnemySpawnCommand command,
        float currentTime,
        WaveDirectorExecutionContext context,
        bool markBeatTriggered)
    {
        if (command == null)
        {
            return 0;
        }

        int spawnedCount = enemySpawnExecutor.Execute(
            command,
            context,
            defaultSpawnResolver,
            handle => activeEnemies[handle.Enemy] = handle);
        if (spawnedCount <= 0)
        {
            return 0;
        }

        director.CommitSpawn(command, currentTime, spawnedCount);
        if (markBeatTriggered && command.IsScriptedBeat)
        {
            director.MarkBeatTriggered(command.BeatId);
        }

        return spawnedCount;
    }
}
