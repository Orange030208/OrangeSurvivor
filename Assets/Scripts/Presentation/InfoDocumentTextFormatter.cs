using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class InfoDocumentTextFormatter
{
    public static string ToPlainText(InfoDocument document, bool includeHeader = false)
    {
        return Format(document, includeHeader, richText: false);
    }

    public static string ToRichText(InfoDocument document, bool includeHeader = false)
    {
        return Format(document, includeHeader, richText: true);
    }

    private static string Format(InfoDocument document, bool includeHeader, bool richText)
    {
        if (document == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        if (includeHeader)
        {
            AppendHeader(builder, document);
        }

        AppendSections(builder, document.Sections, richText);
        return builder.ToString().TrimEnd();
    }

    private static void AppendHeader(StringBuilder builder, InfoDocument document)
    {
        if (!string.IsNullOrWhiteSpace(document.Title))
        {
            builder.AppendLine(document.Title);
        }

        if (document.Tags != null && document.Tags.Count > 0)
        {
            builder.AppendLine(string.Join(" / ", document.Tags));
        }

        if (builder.Length > 0)
        {
            builder.AppendLine();
        }
    }

    private static void AppendSections(StringBuilder builder, IReadOnlyList<InfoSection> sections, bool richText)
    {
        if (sections == null)
        {
            return;
        }

        for (int i = 0; i < sections.Count; i++)
        {
            InfoSection section = sections[i];
            if (section == null || section.Lines == null || section.Lines.Count == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(section.Title))
            {
                builder.AppendLine(richText ? WrapTone(section.Title, InfoTone.Emphasis) : section.Title);
            }

            for (int lineIndex = 0; lineIndex < section.Lines.Count; lineIndex++)
            {
                string lineText = FormatLine(section.Lines[lineIndex], richText);
                if (string.IsNullOrWhiteSpace(lineText))
                {
                    continue;
                }

                builder.AppendLine(lineText);
            }
        }
    }

    private static string FormatLine(InfoLine line, bool richText)
    {
        if (line == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        if (!string.IsNullOrWhiteSpace(line.Label))
        {
            builder.Append(line.Label);
            builder.Append(": ");
        }

        if (line.Parts != null)
        {
            for (int i = 0; i < line.Parts.Count; i++)
            {
                InfoLinePart part = line.Parts[i];
                builder.Append(richText ? WrapTone(part.Text, part.Tone) : part.Text);
            }
        }

        string result = builder.ToString();
        return richText && line.Tone != InfoTone.Neutral
            ? WrapTone(result, line.Tone)
            : result;
    }

    private static string WrapTone(string text, InfoTone tone)
    {
        if (string.IsNullOrEmpty(text) || tone == InfoTone.Neutral)
        {
            return text ?? string.Empty;
        }

        string hex = ColorUtility.ToHtmlStringRGB(GetToneColor(tone));
        return $"<color=#{hex}>{text}</color>";
    }

    private static Color32 GetToneColor(InfoTone tone)
    {
        return tone switch
        {
            InfoTone.Positive => new Color32(79, 220, 111, 255),
            InfoTone.Negative => new Color32(236, 74, 74, 255),
            InfoTone.Warning => new Color32(255, 183, 77, 255),
            InfoTone.Emphasis => new Color32(91, 214, 255, 255),
            InfoTone.Disabled => new Color32(135, 145, 155, 255),
            _ => new Color32(235, 239, 245, 255)
        };
    }
}
