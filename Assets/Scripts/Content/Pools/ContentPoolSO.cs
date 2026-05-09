using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Content Pool", menuName = ScriptableObjectMenuPaths.CONTENT_POOL, order = 0)]
public class ContentPoolSO : ScriptableObject
{
    private const int MIN_ROLL_COUNT = 1;

    [SerializeField] private ContentPoolPurpose purpose = ContentPoolPurpose.Generic;
    [SerializeField, Min(MIN_ROLL_COUNT)] private int defaultRollCount = 1;
    [SerializeField] private bool allowDuplicateResults;
    [SerializeField] private List<ContentPoolEntry> entries = new();

    public ContentPoolPurpose Purpose => purpose;
    public int DefaultRollCount => Mathf.Max(MIN_ROLL_COUNT, defaultRollCount);
    public bool AllowDuplicateResults => allowDuplicateResults;
    public IReadOnlyList<ContentPoolEntry> Entries => entries;

    private void OnValidate()
    {
        defaultRollCount = Mathf.Max(MIN_ROLL_COUNT, defaultRollCount);
        entries ??= new List<ContentPoolEntry>();
    }

    public void CollectFactDefinitions(List<FactDefinitionSO> results)
    {
        if (entries == null || results == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            entries[i]?.CollectFactDefinitions(results);
        }
    }

    public void Initialize(
        ContentPoolPurpose purpose,
        IReadOnlyList<ContentPoolEntry> sourceEntries,
        int rollCount,
        bool allowDuplicateResults)
    {
        this.purpose = purpose;
        defaultRollCount = Mathf.Max(MIN_ROLL_COUNT, rollCount);
        this.allowDuplicateResults = allowDuplicateResults;
        entries = sourceEntries != null
            ? new List<ContentPoolEntry>(sourceEntries)
            : new List<ContentPoolEntry>();
    }
}
