using System;
using UnityEngine;

[Serializable]
public sealed class EnemyRosterEntry
{
    [SerializeField] private string entryId;
    [SerializeField] private EnemySO enemy;
    [SerializeField] private SpawnRole role = SpawnRole.Melee;
    [SerializeField] private WaveEnemyTag tags = WaveEnemyTag.Normal;
    [SerializeField] private float cost = 1f;
    [SerializeField] private int minGroupSize = 1;
    [SerializeField] private int maxGroupSize = 1;
    [SerializeField] private float cooldownSeconds = 0f;
    [SerializeField] private int maxAlive;
    [SerializeField] private Vector2 activeTimeRange = new Vector2(0f, 100f);
    [SerializeField] private SpawnLocationDefinition spawnLocationOverride;

    public string EntryId => string.IsNullOrWhiteSpace(entryId) && enemy != null ? enemy.name : entryId;
    public EnemySO Enemy => enemy;
    public SpawnRole Role => role;
    public WaveEnemyTag Tags => tags == WaveEnemyTag.None ? WaveEnemyTag.Normal : tags;
    public float Cost => Mathf.Max(0f, cost);
    public int MinGroupSize => Mathf.Max(1, minGroupSize);
    public int MaxGroupSize => Mathf.Max(MinGroupSize, maxGroupSize);
    public float CooldownSeconds => Mathf.Max(0f, cooldownSeconds);
    public int MaxAlive => Mathf.Max(0, maxAlive);
    public Vector2 ActiveTimeRange => activeTimeRange;
    public SpawnLocationDefinition SpawnLocationOverride => spawnLocationOverride;
    public bool IsValid => enemy != null && Cost > 0f;

    public EnemyRosterEntry()
    {
    }

    public EnemyRosterEntry(
        string entryId,
        EnemySO enemy,
        SpawnRole role,
        WaveEnemyTag tags,
        float cost,
        int minGroupSize,
        int maxGroupSize,
        float cooldownSeconds,
        int maxAlive,
        Vector2 activeTimeRange,
        SpawnLocationDefinition spawnLocationOverride = null)
    {
        this.entryId = entryId;
        this.enemy = enemy;
        this.role = role;
        this.tags = tags;
        this.cost = cost;
        this.minGroupSize = minGroupSize;
        this.maxGroupSize = maxGroupSize;
        this.cooldownSeconds = cooldownSeconds;
        this.maxAlive = maxAlive;
        this.activeTimeRange = activeTimeRange;
        this.spawnLocationOverride = spawnLocationOverride;
        Validate();
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(entryId) && enemy != null)
        {
            entryId = enemy.name;
        }

        if (tags == WaveEnemyTag.None)
        {
            tags = WaveEnemyTag.Normal;
        }

        cost = Mathf.Max(0f, cost);
        minGroupSize = Mathf.Max(1, minGroupSize);
        maxGroupSize = Mathf.Max(minGroupSize, maxGroupSize);
        cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
        maxAlive = Mathf.Max(0, maxAlive);
        float start = Mathf.Clamp(activeTimeRange.x, 0f, 100f);
        float end = Mathf.Clamp(activeTimeRange.y, start, 100f);
        activeTimeRange = new Vector2(start, end);
        spawnLocationOverride?.Validate();
    }
}

[Serializable]
public struct SpawnRoleTarget
{
    [SerializeField] private SpawnRole role;
    [SerializeField] private float budgetShare;
    [SerializeField] private float minBudget;
    [SerializeField] private float maxBudget;
    [SerializeField] private int priority;

    public SpawnRole Role => role;
    public float BudgetShare => Mathf.Max(0f, budgetShare);
    public float MinBudget => Mathf.Max(0f, minBudget);
    public float MaxBudget => Mathf.Max(0f, maxBudget);
    public int Priority => priority;

    public SpawnRoleTarget(SpawnRole role, float budgetShare, float minBudget = 0f, float maxBudget = 0f, int priority = 0)
    {
        this.role = role;
        this.budgetShare = budgetShare;
        this.minBudget = minBudget;
        this.maxBudget = maxBudget;
        this.priority = priority;
        Validate();
    }

    public void Validate()
    {
        budgetShare = Mathf.Max(0f, budgetShare);
        minBudget = Mathf.Max(0f, minBudget);
        maxBudget = Mathf.Max(0f, maxBudget);
    }
}

[Serializable]
public sealed class EnemySpawnCommandTemplate
{
    [SerializeField] private string commandId;
    [SerializeField] private EnemySO enemy;
    [SerializeField] private SpawnRole role = SpawnRole.Melee;
    [SerializeField] private WaveEnemyTag tags = WaveEnemyTag.Normal;
    [SerializeField] private int count = 1;
    [SerializeField] private float cost = 1f;
    [SerializeField] private SpawnLocationDefinition spawnLocationOverride;

    public string CommandId => string.IsNullOrWhiteSpace(commandId) && enemy != null ? enemy.name : commandId;
    public EnemySO Enemy => enemy;
    public SpawnRole Role => role;
    public WaveEnemyTag Tags => tags == WaveEnemyTag.None ? WaveEnemyTag.Normal : tags;
    public int Count => Mathf.Max(1, count);
    public float Cost => Mathf.Max(0f, cost);
    public SpawnLocationDefinition SpawnLocationOverride => spawnLocationOverride;
    public bool IsValid => enemy != null && Count > 0 && Cost > 0f;

    public EnemySpawnCommandTemplate()
    {
    }

    public EnemySpawnCommandTemplate(
        string commandId,
        EnemySO enemy,
        SpawnRole role,
        WaveEnemyTag tags,
        int count,
        float cost,
        SpawnLocationDefinition spawnLocationOverride = null)
    {
        this.commandId = commandId;
        this.enemy = enemy;
        this.role = role;
        this.tags = tags;
        this.count = count;
        this.cost = cost;
        this.spawnLocationOverride = spawnLocationOverride;
        Validate();
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(commandId) && enemy != null)
        {
            commandId = enemy.name;
        }

        if (tags == WaveEnemyTag.None)
        {
            tags = WaveEnemyTag.Normal;
        }

        count = Mathf.Max(1, count);
        cost = Mathf.Max(0f, cost);
        spawnLocationOverride?.Validate();
    }
}

[Serializable]
public sealed class ScriptedSpawnBeat
{
    [SerializeField] private string beatId;
    [SerializeField] private bool useNormalizedTriggerTime;
    [SerializeField] private float triggerTime;
    [SerializeField] private float normalizedTriggerTime;
    [SerializeField] private bool ignoreBudget;
    [SerializeField] private bool allowWhenPressureCapped;
    [SerializeField] private EnemySpawnCommandTemplate[] commands = Array.Empty<EnemySpawnCommandTemplate>();

    public string BeatId => beatId;
    public bool UseNormalizedTriggerTime => useNormalizedTriggerTime;
    public float TriggerTime => Mathf.Max(0f, triggerTime);
    public float NormalizedTriggerTime => Mathf.Clamp(normalizedTriggerTime, 0f, 1f);
    public bool IgnoreBudget => ignoreBudget;
    public bool AllowWhenPressureCapped => allowWhenPressureCapped;
    public EnemySpawnCommandTemplate[] Commands => commands ?? Array.Empty<EnemySpawnCommandTemplate>();

    public ScriptedSpawnBeat()
    {
    }

    public ScriptedSpawnBeat(
        string beatId,
        float triggerTime,
        bool useNormalizedTriggerTime,
        bool ignoreBudget,
        bool allowWhenPressureCapped,
        EnemySpawnCommandTemplate[] commands)
    {
        this.beatId = beatId;
        this.triggerTime = triggerTime;
        this.useNormalizedTriggerTime = useNormalizedTriggerTime;
        normalizedTriggerTime = triggerTime;
        this.ignoreBudget = ignoreBudget;
        this.allowWhenPressureCapped = allowWhenPressureCapped;
        this.commands = commands ?? Array.Empty<EnemySpawnCommandTemplate>();
        Validate(beatId);
    }

    public void Validate(string fallbackId)
    {
        if (string.IsNullOrWhiteSpace(beatId))
        {
            beatId = fallbackId;
        }

        triggerTime = Mathf.Max(0f, triggerTime);
        normalizedTriggerTime = Mathf.Clamp01(normalizedTriggerTime);
        commands ??= Array.Empty<EnemySpawnCommandTemplate>();
        for (int i = 0; i < commands.Length; i++)
        {
            commands[i]?.Validate();
        }
    }

    public float ResolveTriggerTimeSeconds(float waveDuration)
    {
        return useNormalizedTriggerTime
            ? Mathf.Clamp01(normalizedTriggerTime) * Mathf.Max(0f, waveDuration)
            : Mathf.Max(0f, triggerTime);
    }
}

[Serializable]
public sealed class EndlessRosterUnlockRule
{
    [SerializeField] private int unlockEndlessWaveNumber = 1;
    [SerializeField] private EnemyRosterEntry[] additionalRosterEntries = Array.Empty<EnemyRosterEntry>();

    public int UnlockEndlessWaveNumber => Mathf.Max(1, unlockEndlessWaveNumber);
    public EnemyRosterEntry[] AdditionalRosterEntries => additionalRosterEntries ?? Array.Empty<EnemyRosterEntry>();

    public EndlessRosterUnlockRule()
    {
    }

    public EndlessRosterUnlockRule(int unlockEndlessWaveNumber, EnemyRosterEntry[] additionalRosterEntries)
    {
        this.unlockEndlessWaveNumber = unlockEndlessWaveNumber;
        this.additionalRosterEntries = additionalRosterEntries ?? Array.Empty<EnemyRosterEntry>();
        Validate();
    }

    public void Validate()
    {
        unlockEndlessWaveNumber = Mathf.Max(1, unlockEndlessWaveNumber);
        additionalRosterEntries ??= Array.Empty<EnemyRosterEntry>();
        for (int i = 0; i < additionalRosterEntries.Length; i++)
        {
            additionalRosterEntries[i]?.Validate();
        }
    }
}

[Serializable]
public sealed class EndlessMilestoneBeat
{
    [SerializeField] private int firstWaveNumber = 1;
    [SerializeField] private int repeatInterval = 1;
    [SerializeField] private ScriptedSpawnBeat[] beats = Array.Empty<ScriptedSpawnBeat>();

    public int FirstWaveNumber => Mathf.Max(1, firstWaveNumber);
    public int RepeatInterval => Mathf.Max(1, repeatInterval);
    public ScriptedSpawnBeat[] Beats => beats ?? Array.Empty<ScriptedSpawnBeat>();

    public EndlessMilestoneBeat()
    {
    }

    public EndlessMilestoneBeat(int firstWaveNumber, int repeatInterval, ScriptedSpawnBeat[] beats)
    {
        this.firstWaveNumber = firstWaveNumber;
        this.repeatInterval = repeatInterval;
        this.beats = beats ?? Array.Empty<ScriptedSpawnBeat>();
        Validate();
    }

    public bool Matches(int endlessWaveNumber)
    {
        if (endlessWaveNumber < FirstWaveNumber)
        {
            return false;
        }

        return (endlessWaveNumber - FirstWaveNumber) % RepeatInterval == 0;
    }

    public void Validate()
    {
        firstWaveNumber = Mathf.Max(1, firstWaveNumber);
        repeatInterval = Mathf.Max(1, repeatInterval);
        beats ??= Array.Empty<ScriptedSpawnBeat>();
        for (int i = 0; i < beats.Length; i++)
        {
            beats[i]?.Validate($"MilestoneBeat_{firstWaveNumber}_{i}");
        }
    }
}
