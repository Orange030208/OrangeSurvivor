using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一个可被 WaveSpawn 内容池抽中的刷怪组合。
/// 内容池只负责按权重选中该资产；本资产描述选中后要展开出的敌人请求。
/// </summary>
[CreateAssetMenu(fileName = "Wave Spawn Pack", menuName = ScriptableObjectMenuPaths.WAVE_SPAWN_PACK, order = 0)]
public sealed class WaveSpawnPackSO : ScriptableObject
{
    [SerializeField] private string packId;
    [SerializeField] private List<WaveSpawnPackEntry> entries = new();

    public string PackId => string.IsNullOrWhiteSpace(packId) ? name : packId;
    public IReadOnlyList<WaveSpawnPackEntry> Entries => entries;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(packId))
        {
            packId = name;
        }

        entries ??= new List<WaveSpawnPackEntry>();
    }

    public void InitializeRuntime(string packId, IReadOnlyList<WaveSpawnPackEntry> sourceEntries)
    {
        this.packId = string.IsNullOrWhiteSpace(packId) ? name : packId;
        entries = sourceEntries != null
            ? new List<WaveSpawnPackEntry>(sourceEntries)
            : new List<WaveSpawnPackEntry>();
    }
}
