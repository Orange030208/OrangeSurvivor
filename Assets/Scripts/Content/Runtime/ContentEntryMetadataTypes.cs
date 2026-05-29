using System;
using UnityEngine;

[Serializable]
public sealed class WaveSpawnMetadata : ContentEntryMetadata
{
    [SerializeField] private WaveEnemyTag tags = WaveEnemyTag.Normal;

    public WaveSpawnMetadata()
    {
    }

    public WaveSpawnMetadata(WaveEnemyTag tags)
    {
        this.tags = tags == WaveEnemyTag.None ? WaveEnemyTag.Normal : tags;
    }

    public WaveEnemyTag Tags => tags == WaveEnemyTag.None ? WaveEnemyTag.Normal : tags;

    public void ConfigureTags(WaveEnemyTag tags)
    {
        this.tags = tags == WaveEnemyTag.None ? WaveEnemyTag.Normal : tags;
    }
}

[Serializable]
public sealed class WeaponLevelRollMetadata : ContentEntryMetadata
{
    [SerializeField] private int minLevel = WeaponLevelHelper.MinLevel;
    [SerializeField] private int maxLevel = WeaponLevelHelper.MaxLevel;

    public WeaponLevelRollMetadata()
    {
    }

    public WeaponLevelRollMetadata(int minLevel, int maxLevel)
    {
        ConfigureRange(minLevel, maxLevel);
    }

    public int MinLevel => WeaponLevelHelper.ClampLevel(minLevel);
    public int MaxLevel => Mathf.Max(MinLevel, WeaponLevelHelper.ClampLevel(maxLevel));

    public void ConfigureRange(int minLevel, int maxLevel)
    {
        this.minLevel = WeaponLevelHelper.ClampLevel(minLevel);
        this.maxLevel = Mathf.Max(this.minLevel, WeaponLevelHelper.ClampLevel(maxLevel));
    }
}

[Serializable]
public sealed class ShopPricingMetadata : ContentEntryMetadata
{
    private const float MIN_PRICE_MULTIPLIER = 0.01f;

    [SerializeField, Min(MIN_PRICE_MULTIPLIER)] private float priceMultiplier = 1f;

    public ShopPricingMetadata()
    {
    }

    public ShopPricingMetadata(float priceMultiplier)
    {
        ConfigurePriceMultiplier(priceMultiplier);
    }

    public float PriceMultiplier => Mathf.Max(MIN_PRICE_MULTIPLIER, priceMultiplier);

    public void ConfigurePriceMultiplier(float priceMultiplier)
    {
        this.priceMultiplier = Mathf.Max(MIN_PRICE_MULTIPLIER, priceMultiplier);
    }
}

[Serializable]
public sealed class QualityMetadata : ContentEntryMetadata
{
    private const int MinTierValue = (int)ContentTier.Common;
    private const int MaxTierValue = (int)ContentTier.Legendary;

    [SerializeField] private int qualityValue;

    public QualityMetadata()
    {
    }

    public QualityMetadata(int qualityValue)
    {
        this.qualityValue = NormalizeQualityValue(qualityValue);
    }

    public QualityMetadata(ContentTier tier)
    {
        qualityValue = ContentTierResolver.ToQualityValue(tier);
    }

    public int QualityValue => qualityValue;
    public ContentTier Tier => ContentTierResolver.FromQualityValue(qualityValue);

    public void ConfigureQualityValue(int qualityValue)
    {
        this.qualityValue = NormalizeQualityValue(qualityValue);
    }

    public void ConfigureTier(ContentTier tier)
    {
        qualityValue = ContentTierResolver.ToQualityValue(tier);
    }

    private static int NormalizeQualityValue(int qualityValue)
    {
        return Mathf.Clamp(qualityValue, MinTierValue, MaxTierValue);
    }
}

[Serializable]
public sealed class DropQuantityMetadata : ContentEntryMetadata
{
    [SerializeField, Min(1)] private int quantity = 1;

    public DropQuantityMetadata()
    {
    }

    public DropQuantityMetadata(int quantity)
    {
        ConfigureQuantity(quantity);
    }

    public int Quantity => Mathf.Max(1, quantity);

    public void ConfigureQuantity(int quantity)
    {
        this.quantity = Mathf.Max(1, quantity);
    }
}
