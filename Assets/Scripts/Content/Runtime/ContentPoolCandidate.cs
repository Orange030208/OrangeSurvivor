public sealed class ContentPoolCandidate
{
    private const float MIN_PRICE_MULTIPLIER = 0.01f;

    public ContentPoolCandidate(ContentPoolEntry entry, float weight)
    {
        Entry = entry;
        Weight = weight;
        MinLevel = entry != null ? entry.MinLevel : 0;
        MaxLevel = entry != null ? entry.MaxLevel : 0;
        QualityValue = entry != null ? entry.QualityValue : 0;
        DomainFlags = entry != null ? entry.DomainFlags : 0;
        PriceMultiplier = entry != null ? entry.PriceMultiplier : 1f;
    }

    public ContentPoolEntry Entry { get; }
    public float Weight { get; set; }
    public int MinLevel { get; private set; }
    public int MaxLevel { get; private set; }
    public int QualityValue { get; private set; }
    public int DomainFlags { get; private set; }
    public float PriceMultiplier { get; private set; }
    public bool IsRemoved { get; private set; }
    public UnityEngine.Object Content => Entry != null ? Entry.Content : null;

    public void ConfigureLevelRange(int minLevel, int maxLevel)
    {
        MinLevel = UnityEngine.Mathf.Max(0, minLevel);
        MaxLevel = UnityEngine.Mathf.Max(MinLevel, maxLevel);
    }

    public void ConfigureQualityValue(int qualityValue)
    {
        QualityValue = qualityValue;
    }

    public void ConfigureDomainFlags(int domainFlags)
    {
        DomainFlags = domainFlags;
    }

    public void ConfigurePriceMultiplier(float priceMultiplier)
    {
        PriceMultiplier = UnityEngine.Mathf.Max(MIN_PRICE_MULTIPLIER, priceMultiplier);
    }

    public void Remove()
    {
        IsRemoved = true;
        Weight = 0f;
    }
}
