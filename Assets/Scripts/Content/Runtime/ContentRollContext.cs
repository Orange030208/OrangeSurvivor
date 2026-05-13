using System.Collections.Generic;
using UnityEngine;

public sealed class ContentRollContext
{
    public ContentRollContext(
        string scopeId,
        Player player = null,
        WaveSpawnContext? waveSpawn = null,
        RunProgressionSnapshot? progressionSnapshot = null,
        ContentHistoryScope? historyScope = null,
        ContentHistoryState history = null,
        IReadOnlyList<ContentPoolEntry> selectedEntries = null,
        Entity source = null,
        PropertiesManager propertiesManager = null,
        WeaponsHolder weaponsHolder = null,
        CharacterDataSO characterData = null,
        string waveId = null,
        string waveTrackId = null,
        float? waveProgressPercent = null,
        int shopRefreshCount = 0,
        int shopRerollCount = 0)
    {
        ScopeId = ContentPoolScopeIds.Normalize(scopeId);
        Source = source;
        Player = player != null ? player : source as Player;
        WaveSpawn = waveSpawn;
        ProgressionSnapshot = progressionSnapshot ?? RunProgressionRuntime.CurrentSnapshot;
        HistoryScope = historyScope ?? new ContentHistoryScope(ScopeId);
        History = history;
        SelectedEntries = selectedEntries ?? System.Array.Empty<ContentPoolEntry>();
        PropertiesManager = propertiesManager;
        WeaponsHolder = weaponsHolder;
        CharacterData = characterData != null
            ? characterData
            : Player != null ? Player.CharacterData : null;
        WaveId = !string.IsNullOrWhiteSpace(waveId)
            ? waveId
            : waveSpawn.HasValue ? waveSpawn.Value.WaveId : string.Empty;
        WaveTrackId = waveTrackId ?? string.Empty;
        WaveProgressPercent = waveProgressPercent ?? ResolveWaveProgressPercent(waveSpawn);
        ShopRefreshCount = UnityEngine.Mathf.Max(0, shopRefreshCount);
        ShopRerollCount = UnityEngine.Mathf.Max(0, shopRerollCount);
    }

    public string ScopeId { get; }
    public Entity Source { get; }
    public Player Player { get; }
    public WaveSpawnContext? WaveSpawn { get; }
    public RunProgressionSnapshot ProgressionSnapshot { get; }
    public ContentHistoryScope HistoryScope { get; }
    public ContentHistoryState History { get; }
    public IReadOnlyList<ContentPoolEntry> SelectedEntries { get; }
    public PropertiesManager PropertiesManager { get; }
    public WeaponsHolder WeaponsHolder { get; }
    public CharacterDataSO CharacterData { get; }
    public string WaveId { get; }
    public string WaveTrackId { get; }
    public float WaveProgressPercent { get; }
    public int ShopRefreshCount { get; }
    public int ShopRerollCount { get; }
    public int CurrentWaveNumber => WaveSpawn.HasValue
        ? UnityEngine.Mathf.Max(1, WaveSpawn.Value.WaveNumber)
        : UnityEngine.Mathf.Max(1, ProgressionSnapshot.WaveNumber);

    public int GetRollCount(string entryId)
    {
        return History != null ? History.GetRollCount(HistoryScope, entryId) : 0;
    }

    public int GetPickCount(string entryId)
    {
        return History != null ? History.GetPickCount(HistoryScope, entryId) : 0;
    }

    public bool WasPreviouslyRolled(string entryId)
    {
        return History != null && History.WasPreviouslyRolled(HistoryScope, entryId);
    }

    public float GetPropertyValue(PropType propType)
    {
        PropertiesManager propertiesManager = ResolvePropertiesManager();
        return propertiesManager != null ? propertiesManager.GetPropValue(propType) : 0f;
    }

    public int GetOwnedWeaponCount()
    {
        WeaponsHolder weaponsHolder = ResolveWeaponsHolder();
        return weaponsHolder != null ? weaponsHolder.EquippedWeapons.Count : 0;
    }

    public int GetOwnedWeaponTagCount(WeaponTag tag)
    {
        WeaponsHolder weaponsHolder = ResolveWeaponsHolder();
        if (weaponsHolder == null)
        {
            return 0;
        }

        int count = 0;
        IReadOnlyList<EquippedWeaponInfo> equippedWeapons = weaponsHolder.EquippedWeapons;
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            WeaponDataSO weaponData = equippedWeapons[i].WeaponData;
            if (weaponData != null && weaponData.HasTag(tag))
            {
                count++;
            }
        }

        return count;
    }

    public bool HasOwnedWeapon(WeaponDataSO targetWeapon)
    {
        if (targetWeapon == null)
        {
            return false;
        }

        WeaponsHolder weaponsHolder = ResolveWeaponsHolder();
        if (weaponsHolder == null)
        {
            return false;
        }

        IReadOnlyList<EquippedWeaponInfo> equippedWeapons = weaponsHolder.EquippedWeapons;
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            if (IsSameWeapon(equippedWeapons[i].WeaponData, targetWeapon))
            {
                return true;
            }
        }

        return false;
    }

    public void RecordRoll(ContentRollResult result)
    {
        History?.RecordRoll(HistoryScope, result?.Items);
    }

    public ContentRollContext WithSelectedEntries(IReadOnlyList<ContentPoolEntry> selectedEntries)
    {
        return new ContentRollContext(
            ScopeId,
            Player,
            WaveSpawn,
            ProgressionSnapshot,
            HistoryScope,
            History,
            selectedEntries,
            Source,
            PropertiesManager,
            WeaponsHolder,
            CharacterData,
            WaveId,
            WaveTrackId,
            WaveProgressPercent,
            ShopRefreshCount,
            ShopRerollCount);
    }

    private static float ResolveWaveProgressPercent(WaveSpawnContext? waveSpawn)
    {
        return waveSpawn.HasValue
            ? UnityEngine.Mathf.Clamp01(waveSpawn.Value.NormalizedProgress) * 100f
            : 0f;
    }

    private PropertiesManager ResolvePropertiesManager()
    {
        if (PropertiesManager != null)
        {
            return PropertiesManager;
        }

        if (Player != null && Player.TryGetComponent(out PropertiesManager playerProperties))
        {
            return playerProperties;
        }

        return Source != null && Source.TryGetComponent(out PropertiesManager sourceProperties)
            ? sourceProperties
            : null;
    }

    private WeaponsHolder ResolveWeaponsHolder()
    {
        if (WeaponsHolder != null)
        {
            return WeaponsHolder;
        }

        if (Player != null && Player.TryGetComponent(out WeaponsHolder playerWeapons))
        {
            return playerWeapons;
        }

        return Source != null && Source.TryGetComponent(out WeaponsHolder sourceWeapons)
            ? sourceWeapons
            : null;
    }

    private static bool IsSameWeapon(WeaponDataSO left, WeaponDataSO right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (left == right)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(left.ItemName) &&
               string.Equals(left.ItemName, right.ItemName, System.StringComparison.Ordinal);
    }
}
