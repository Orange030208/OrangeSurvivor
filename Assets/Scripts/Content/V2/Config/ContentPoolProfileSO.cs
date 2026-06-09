using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Content Pool Profile",
    menuName = ScriptableObjectMenuPaths.SYSTEMS_ROOT + "Content/V2 Content Pool Profile",
    order = 0)]
public sealed class ContentPoolProfileSO : ScriptableObject
{
    private const int MIN_ROLL_COUNT = 1;

    [SerializeField] private string poolId;
    [SerializeField] private ContentPoolKind kind = ContentPoolKind.Generic;
    [SerializeField, Min(MIN_ROLL_COUNT)] private int defaultRollCount = 1;
    [SerializeField] private bool allowDuplicateResults;
    [SerializeField] private List<ContentPoolEntryDefinition> entries = new();

    public string PoolId => string.IsNullOrWhiteSpace(poolId) ? name : poolId.Trim();
    public ContentPoolKind Kind => kind;
    public int DefaultRollCount => Mathf.Max(MIN_ROLL_COUNT, defaultRollCount);
    public bool AllowDuplicateResults => allowDuplicateResults;
    public IReadOnlyList<ContentPoolEntryDefinition> Entries => entries != null
        ? entries
        : System.Array.Empty<ContentPoolEntryDefinition>();

    private void OnValidate()
    {
        defaultRollCount = Mathf.Max(MIN_ROLL_COUNT, defaultRollCount);
        entries ??= new List<ContentPoolEntryDefinition>();
    }

    public void Initialize(
        string poolId,
        ContentPoolKind kind,
        IReadOnlyList<ContentPoolEntryDefinition> sourceEntries,
        int defaultRollCount,
        bool allowDuplicateResults)
    {
        this.poolId = string.IsNullOrWhiteSpace(poolId) ? name : poolId.Trim();
        this.kind = kind;
        this.defaultRollCount = Mathf.Max(MIN_ROLL_COUNT, defaultRollCount);
        this.allowDuplicateResults = allowDuplicateResults;
        entries = sourceEntries != null
            ? new List<ContentPoolEntryDefinition>(sourceEntries)
            : new List<ContentPoolEntryDefinition>();
    }
}
