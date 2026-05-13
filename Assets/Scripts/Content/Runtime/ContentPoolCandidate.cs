using System.Collections.Generic;

public sealed class ContentPoolCandidate
{
    private const float MIN_PRICE_MULTIPLIER = 0.01f;
    private readonly List<ContentEntryMetadata> metadata;

    public ContentPoolCandidate(ContentPoolEntry entry, float weight)
    {
        Entry = entry;
        Weight = weight;
        metadata = ContentMetadataUtility.CloneMetadata(entry?.Metadata);
    }

    public ContentPoolEntry Entry { get; }
    public float Weight { get; set; }
    public IReadOnlyList<ContentEntryMetadata> Metadata => metadata;
    public bool IsRemoved { get; private set; }
    public UnityEngine.Object Content => Entry != null ? Entry.Content : null;

    public bool TryGetMetadata<T>(out T value)
        where T : ContentEntryMetadata
    {
        return ContentMetadataUtility.TryGetMetadata(Metadata, out value);
    }

    public void ConfigureLevelRange(int minLevel, int maxLevel)
    {
        WeaponLevelRollMetadata levelMetadata = ContentMetadataUtility.GetOrCreateMetadata<WeaponLevelRollMetadata>(metadata);
        levelMetadata.ConfigureRange(minLevel, maxLevel);
    }

    public void ConfigureQualityValue(int qualityValue)
    {
        QualityMetadata qualityMetadata = ContentMetadataUtility.GetOrCreateMetadata<QualityMetadata>(metadata);
        qualityMetadata.ConfigureQualityValue(qualityValue);
    }

    public void ConfigureWaveSpawnTags(WaveEnemyTag tags)
    {
        WaveSpawnMetadata spawnMetadata = ContentMetadataUtility.GetOrCreateMetadata<WaveSpawnMetadata>(metadata);
        spawnMetadata.ConfigureTags(tags);
    }

    public void ConfigurePriceMultiplier(float priceMultiplier)
    {
        ShopPricingMetadata pricingMetadata = ContentMetadataUtility.GetOrCreateMetadata<ShopPricingMetadata>(metadata);
        pricingMetadata.ConfigurePriceMultiplier(UnityEngine.Mathf.Max(MIN_PRICE_MULTIPLIER, priceMultiplier));
    }

    public float GetPriceMultiplier()
    {
        return TryGetMetadata(out ShopPricingMetadata pricingMetadata)
            ? pricingMetadata.PriceMultiplier
            : 1f;
    }

    public void Remove()
    {
        IsRemoved = true;
        Weight = 0f;
    }
}
