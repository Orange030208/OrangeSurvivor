using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 将 PropEntry 转换为标准展示文档。
/// 第一阶段同时覆盖属性列表与说明文本两种输出。
/// </summary>
public sealed class PropEntryDisplayBuilder : IDisplayDocumentBuilder<PropEntry>, IDisplayDocumentBuilder<IReadOnlyList<PropEntry>>
{
    public DisplayDocument Build(PropEntry source, DisplayContext context = null)
    {
        return Build(new[] { source }, context);
    }

    public DisplayDocument Build(IReadOnlyList<PropEntry> source, DisplayContext context = null)
    {
        context ??= DisplayContext.Default;

        IReadOnlyList<PropEntry> entries = source ?? Array.Empty<PropEntry>();
        List<StatItem> statItems = new(entries.Count);
        List<TextLineItem> textItems = new(entries.Count);

        for (int i = 0; i < entries.Count; i++)
        {
            PropEntry entry = entries[i];
            statItems.Add(BuildStatItem(entry));

            string description = entry.GetAutoDescription();
            if (!string.IsNullOrWhiteSpace(description))
            {
                textItems.Add(new TextLineItem
                {
                    Text = description,
                    StyleKey = "default"
                });
            }
        }

        List<DisplayBlock> blocks = new(2)
        {
            new StatListBlock
            {
                BlockId = "stats",
                Header = context.IsCompact ? null : "属性",
                Order = 0,
                Items = statItems
            }
        };

        if (textItems.Count > 0)
        {
            blocks.Add(new TextListBlock
            {
                BlockId = "descriptions",
                Header = context.IsCompact ? null : "说明",
                Order = 100,
                Items = textItems
            });
        }

        return new DisplayDocument
        {
            Id = BuildDocumentId(entries),
            Title = entries.Count == 1 ? entries[0].GetDisplayName() : null,
            Blocks = blocks
        };
    }

    public StatListBlock BuildStatBlock(IReadOnlyList<PropEntry> entries, DisplayContext context = null)
    {
        DisplayDocument document = Build(entries, context);
        return document.GetBlock<StatListBlock>();
    }

    public TextListBlock BuildTextBlock(IReadOnlyList<PropEntry> entries, DisplayContext context = null)
    {
        DisplayDocument document = Build(entries, context);
        return document.GetBlock<TextListBlock>();
    }

    private static StatItem BuildStatItem(PropEntry entry)
    {
        return new StatItem
        {
            Key = entry.GetDisplayName(),
            Value = entry.GetDisplayValueText(),
            Icon = ResourcesManager.GetPropIcon(entry.propType),
            NumericValue = entry.value,
            StyleKey = entry.value > 0f ? "positive" : entry.value < 0f ? "negative" : "neutral"
        };
    }

    private static string BuildDocumentId(IReadOnlyList<PropEntry> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            return "prop_entries_empty";
        }

        if (entries.Count == 1)
        {
            PropEntry entry = entries[0];
            return $"prop_{entry.propType}_{entry.modifierType}";
        }

        return $"prop_entries_{entries.Count}";
    }
}
