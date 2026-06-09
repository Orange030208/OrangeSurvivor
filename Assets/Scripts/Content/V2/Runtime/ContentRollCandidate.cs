using System.Collections.Generic;

public sealed class ContentRollCandidate
{
    private List<ContentEntryMetadata> metadata;

    public ContentRollCandidate(ContentPoolEntryDefinition entry, float weight)
    {
        Entry = entry;
        Weight = weight;
        metadata = ContentMetadataUtility.CloneMetadata(entry?.Metadata);
    }

    public ContentPoolEntryDefinition Entry { get; }
    public float Weight { get; set; }
    public bool IsRemoved { get; private set; }
    public UnityEngine.Object Content => Entry != null ? Entry.Content : null;
    public IReadOnlyList<ContentEntryMetadata> Metadata => metadata;

    public ContentRollSelection ToSelection()
    {
        return new ContentRollSelection(Entry, Content, Weight, metadata);
    }

    public void ReplaceMetadata(IReadOnlyList<ContentEntryMetadata> nextMetadata)
    {
        metadata = ContentMetadataUtility.CloneMetadata(nextMetadata);
    }

    public void Remove()
    {
        IsRemoved = true;
        Weight = 0f;
    }
}
