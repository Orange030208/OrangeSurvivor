using System;
using System.Collections.Generic;

public sealed class ContentFactSet
{
    private readonly Dictionary<string, object> values = new(StringComparer.Ordinal);

    public static ContentFactSet Empty => new ContentFactSet();

    public ContentFactSet Set<T>(string key, T value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            values[key.Trim()] = value;
        }

        return this;
    }

    public bool TryGet<T>(string key, out T value)
    {
        if (!string.IsNullOrWhiteSpace(key) &&
            values.TryGetValue(key.Trim(), out object rawValue) &&
            rawValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    public T GetOrDefault<T>(string key, T defaultValue = default)
    {
        return TryGet(key, out T value) ? value : defaultValue;
    }
}

public static class ContentFactKeys
{
    public const string Player = "player";
    public const string Source = "source";
    public const string PropertiesManager = "properties_manager";
    public const string WeaponsHolder = "weapons_holder";
    public const string CharacterData = "character_data";
    public const string ProgressionSnapshot = "progression_snapshot";
    public const string WaveSpawn = "wave_spawn";
    public const string WaveId = "wave_id";
    public const string WaveTrackId = "wave_track_id";
    public const string WaveProgressPercent = "wave_progress_percent";
    public const string ShopRefreshCount = "shop_refresh_count";
    public const string ShopRerollCount = "shop_reroll_count";
}
