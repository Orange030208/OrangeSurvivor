using System;
using System.Collections.Generic;
using UnityEngine;

public enum InfoDocumentKind
{
    Unknown = 0,
    Weapon = 1,
    Accessory = 2,
    Buff = 3,
    UpgradeCard = 4,
    Properties = 5,
    General = 6
}

public enum InfoTone
{
    Neutral = 0,
    Positive = 1,
    Negative = 2,
    Warning = 3,
    Emphasis = 4,
    Disabled = 5
}

public sealed class InfoDocument
{
    public string Id { get; }
    public string Title { get; }
    public Sprite Icon { get; }
    public InfoDocumentKind Kind { get; }
    public IReadOnlyList<string> Tags { get; }
    public IReadOnlyList<InfoSection> Sections { get; }

    public InfoDocument(
        string id,
        string title,
        Sprite icon,
        InfoDocumentKind kind,
        IReadOnlyList<string> tags,
        IReadOnlyList<InfoSection> sections)
    {
        Id = id ?? string.Empty;
        Title = title ?? string.Empty;
        Icon = icon;
        Kind = kind;
        Tags = tags ?? Array.Empty<string>();
        Sections = sections ?? Array.Empty<InfoSection>();
    }
}

public sealed class InfoSection
{
    public string Title { get; }
    public IReadOnlyList<InfoLine> Lines { get; }

    public InfoSection(string title, IReadOnlyList<InfoLine> lines)
    {
        Title = title ?? string.Empty;
        Lines = lines ?? Array.Empty<InfoLine>();
    }
}

public sealed class InfoLine
{
    public string Label { get; }
    public IReadOnlyList<InfoLinePart> Parts { get; }
    public InfoTone Tone { get; }

    public InfoLine(string label, IReadOnlyList<InfoLinePart> parts, InfoTone tone = InfoTone.Neutral)
    {
        Label = label ?? string.Empty;
        Parts = parts ?? Array.Empty<InfoLinePart>();
        Tone = tone;
    }
}

public readonly struct InfoLinePart
{
    public InfoLinePart(string text, InfoTone tone = InfoTone.Neutral, bool isEmphasis = false)
    {
        Text = text ?? string.Empty;
        Tone = tone;
        IsEmphasis = isEmphasis;
    }

    public string Text { get; }
    public InfoTone Tone { get; }
    public bool IsEmphasis { get; }
}

public readonly struct InfoValueSpan
{
    public InfoValueSpan(string text, InfoTone tone = InfoTone.Neutral)
    {
        Text = text ?? string.Empty;
        Tone = tone;
    }

    public string Text { get; }
    public InfoTone Tone { get; }
}

public readonly struct InfoStatReference
{
    public InfoStatReference(PropType propType, float value)
    {
        PropType = propType;
        Value = value;
    }

    public PropType PropType { get; }
    public float Value { get; }
}

public static class InfoDocumentUtility
{
    public static string BuildText(IReadOnlyList<InfoSection> sections)
    {
        if (sections == null || sections.Count == 0)
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new();
        for (int i = 0; i < sections.Count; i++)
        {
            InfoSection section = sections[i];
            if (section == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(section.Title))
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.AppendLine(section.Title);
            }

            IReadOnlyList<InfoLine> lines = section.Lines;
            if (lines == null)
            {
                continue;
            }

            for (int j = 0; j < lines.Count; j++)
            {
                InfoLine line = lines[j];
                if (line == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(line.Label))
                {
                    builder.Append(line.Label);
                    builder.Append(": ");
                }

                builder.Append(BuildLineText(line.Parts));
                if (j < lines.Count - 1)
                {
                    builder.AppendLine();
                }
            }
        }

        return builder.ToString();
    }

    public static string BuildLineText(IReadOnlyList<InfoLinePart> parts)
    {
        if (parts == null || parts.Count == 0)
        {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new();
        for (int i = 0; i < parts.Count; i++)
        {
            builder.Append(parts[i].Text);
        }

        return builder.ToString();
    }

    public static string BuildLineText(params InfoLinePart[] parts)
    {
        return BuildLineText((IReadOnlyList<InfoLinePart>)parts);
    }

    public static InfoLinePart Text(string text, InfoTone tone = InfoTone.Neutral)
    {
        return new InfoLinePart(text, tone);
    }

    public static InfoLinePart Emphasis(string text, InfoTone tone = InfoTone.Emphasis)
    {
        return new InfoLinePart(text, tone, true);
    }

    public static InfoLine CreateSingleValueLine(string label, string value, InfoTone tone = InfoTone.Neutral)
    {
        return new InfoLine(label, new[] { Text(value, tone) }, tone);
    }

    public static InfoLine CreateFormulaLine(string label, params InfoLinePart[] parts)
    {
        return new InfoLine(label, parts, InfoTone.Neutral);
    }
}
