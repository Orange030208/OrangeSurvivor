public readonly struct ContentRollItem
{
    public ContentRollItem(ContentPoolEntry entry, UnityEngine.Object content, float finalWeight)
    {
        Entry = entry;
        Content = content;
        FinalWeight = finalWeight;
        MinLevel = entry != null ? entry.MinLevel : 0;
        MaxLevel = entry != null ? entry.MaxLevel : 0;
        QualityValue = entry != null ? entry.QualityValue : 0;
        DomainFlags = entry != null ? entry.DomainFlags : 0;
        PriceMultiplier = entry != null ? entry.PriceMultiplier : 1f;
    }

    public ContentRollItem(ContentPoolCandidate candidate)
    {
        Entry = candidate != null ? candidate.Entry : null;
        Content = candidate != null ? candidate.Content : null;
        FinalWeight = candidate != null ? candidate.Weight : 0f;
        MinLevel = candidate != null ? candidate.MinLevel : 0;
        MaxLevel = candidate != null ? candidate.MaxLevel : 0;
        QualityValue = candidate != null ? candidate.QualityValue : 0;
        DomainFlags = candidate != null ? candidate.DomainFlags : 0;
        PriceMultiplier = candidate != null ? candidate.PriceMultiplier : 1f;
    }

    public ContentPoolEntry Entry { get; }
    public UnityEngine.Object Content { get; }
    public float FinalWeight { get; }
    public string EntryId => Entry != null ? Entry.EntryId : Content != null ? Content.name : string.Empty;
    public int MinLevel { get; }
    public int MaxLevel { get; }
    public int QualityValue { get; }
    public int DomainFlags { get; }
    public float PriceMultiplier { get; }
}
