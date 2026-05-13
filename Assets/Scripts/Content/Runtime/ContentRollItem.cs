using System.Collections.Generic;

public readonly struct ContentRollItem
{
    public ContentRollItem(ContentPoolEntry entry, UnityEngine.Object content, float finalWeight)
    {
        Entry = entry;
        Content = content;
        FinalWeight = finalWeight;
        Metadata = ContentMetadataUtility.CloneMetadata(entry?.Metadata);
    }

    public ContentRollItem(ContentPoolCandidate candidate)
    {
        Entry = candidate != null ? candidate.Entry : null;
        Content = candidate != null ? candidate.Content : null;
        FinalWeight = candidate != null ? candidate.Weight : 0f;
        Metadata = ContentMetadataUtility.CloneMetadata(candidate?.Metadata);
    }

    public ContentPoolEntry Entry { get; }
    public UnityEngine.Object Content { get; }
    public float FinalWeight { get; }
    public string EntryId => Entry != null ? Entry.EntryId : Content != null ? Content.name : string.Empty;
    public IReadOnlyList<ContentEntryMetadata> Metadata { get; }

    public bool TryGetMetadata<T>(out T value)
        where T : ContentEntryMetadata
    {
        return ContentMetadataUtility.TryGetMetadata(Metadata, out value);
    }
}
