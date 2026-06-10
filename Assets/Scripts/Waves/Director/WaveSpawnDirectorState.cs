using System.Collections.Generic;

public sealed class WaveSpawnDirectorState
{
    private readonly Dictionary<SpawnRole, float> spentBudgetByRole = new();
    private readonly Dictionary<SpawnRole, float> alivePressureByRole = new();
    private readonly Dictionary<SpawnRole, int> selectionCursorByRole = new();
    private readonly Dictionary<string, int> aliveCountByEntryId = new(System.StringComparer.Ordinal);
    private readonly Dictionary<string, float> lastSpawnTimeByEntryId = new(System.StringComparer.Ordinal);
    private readonly HashSet<string> triggeredBeatIds = new(System.StringComparer.Ordinal);

    public float TotalSpentBudget { get; set; }
    public float AlivePressure { get; set; }
    public SpawnReason LastSpawnReason { get; set; }
    public string LastSkipReason { get; set; }

    public float GetSpentBudget(SpawnRole role)
    {
        return spentBudgetByRole.TryGetValue(role, out float value) ? value : 0f;
    }

    public void AddSpentBudget(SpawnRole role, float value)
    {
        spentBudgetByRole[role] = GetSpentBudget(role) + value;
    }

    public float GetAlivePressure(SpawnRole role)
    {
        return alivePressureByRole.TryGetValue(role, out float value) ? value : 0f;
    }

    public void AddAlivePressure(SpawnRole role, float value)
    {
        alivePressureByRole[role] = UnityEngine.Mathf.Max(0f, GetAlivePressure(role) + value);
    }

    public int GetAliveCount(string entryId)
    {
        return aliveCountByEntryId.TryGetValue(entryId, out int count) ? count : 0;
    }

    public void AddAliveCount(string entryId, int count)
    {
        aliveCountByEntryId[entryId] = System.Math.Max(0, GetAliveCount(entryId) + count);
    }

    public float GetLastSpawnTime(string entryId)
    {
        return lastSpawnTimeByEntryId.TryGetValue(entryId, out float value) ? value : float.NegativeInfinity;
    }

    public void SetLastSpawnTime(string entryId, float time)
    {
        lastSpawnTimeByEntryId[entryId] = time;
    }

    public int GetSelectionCursor(SpawnRole role)
    {
        return selectionCursorByRole.TryGetValue(role, out int value) ? value : 0;
    }

    public void AdvanceSelectionCursor(SpawnRole role, int entryCount)
    {
        if (entryCount <= 0)
        {
            selectionCursorByRole[role] = 0;
            return;
        }

        selectionCursorByRole[role] = (GetSelectionCursor(role) + 1) % entryCount;
    }

    public bool HasTriggeredBeat(string beatId)
    {
        return !string.IsNullOrWhiteSpace(beatId) && triggeredBeatIds.Contains(beatId);
    }

    public void MarkBeatTriggered(string beatId)
    {
        if (!string.IsNullOrWhiteSpace(beatId))
        {
            triggeredBeatIds.Add(beatId);
        }
    }
}
