using System;
using System.Collections.Generic;

[Serializable]
public abstract class ContentEntryMetadata
{
    public virtual ContentEntryMetadata Clone()
    {
        return (ContentEntryMetadata)MemberwiseClone();
    }
}

public static class ContentMetadataUtility
{
    public static List<ContentEntryMetadata> CloneMetadata(IReadOnlyList<ContentEntryMetadata> source)
    {
        List<ContentEntryMetadata> clone = new();
        if (source == null)
        {
            return clone;
        }

        for (int i = 0; i < source.Count; i++)
        {
            ContentEntryMetadata metadata = source[i];
            if (metadata != null)
            {
                clone.Add(metadata.Clone());
            }
        }

        return clone;
    }

    public static bool TryGetMetadata<T>(IReadOnlyList<ContentEntryMetadata> source, out T value)
        where T : ContentEntryMetadata
    {
        if (source != null)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] is T typed)
                {
                    value = typed;
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    public static T GetOrCreateMetadata<T>(List<ContentEntryMetadata> source)
        where T : ContentEntryMetadata, new()
    {
        if (TryGetMetadata(source, out T value))
        {
            return value;
        }

        source ??= new List<ContentEntryMetadata>();
        value = new T();
        source.Add(value);
        return value;
    }
}
