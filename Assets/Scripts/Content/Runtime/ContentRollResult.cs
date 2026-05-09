using System;
using System.Collections.Generic;

public sealed class ContentRollResult
{
    private readonly List<ContentRollItem> items;

    public ContentRollResult(IReadOnlyList<ContentRollItem> items)
    {
        this.items = items != null ? new List<ContentRollItem>(items) : new List<ContentRollItem>();
    }

    public IReadOnlyList<ContentRollItem> Items => items;
    public bool HasAny => items.Count > 0;

    public List<T> GetContents<T>() where T : UnityEngine.Object
    {
        List<T> results = new(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Content is T typed)
            {
                results.Add(typed);
            }
        }

        return results;
    }

    public T[] GetContentArray<T>() where T : UnityEngine.Object
    {
        return GetContents<T>().ToArray();
    }
}
