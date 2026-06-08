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
        AppendItems(builder, document.Items, includeHeader, richText);
        return builder.ToString().TrimEnd();
    }

    private static void AppendItems(StringBuilder builder, IReadOnlyList<InfoItem> items, bool includeHeader, bool richText)
    {
        if (items == null)
        {
            return;
        }

        bool hasWrittenContent = false;
        bool lineHasContent = false;
        for (int i = 0; i < items.Count; i++)
        {
            InfoItem item = items[i];
            if (ShouldSkipTextExportItem(item, includeHeader))
            {
                continue;
            }

            if (item.Type == InfoItemType.LineBreak)
            {
                if (lineHasContent)
                {
                    builder.AppendLine();
                    lineHasContent = false;
                }

                continue;
            }

            if (item.Type == InfoItemType.Spacer)
            {
                if (hasWrittenContent)
                {
                    if (lineHasContent)
                    {
                        builder.AppendLine();
                    }

                    builder.AppendLine();
                    lineHasContent = false;
                }

                continue;
            }

            string text = FormatItem(item, richText);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            builder.Append(text);
            hasWrittenContent = true;
            lineHasContent = true;
        }
    }

    private static bool ShouldSkipTextExportItem(InfoItem item, bool includeHeader)
    {
        return item.Type switch
        {
            InfoItemType.Image => true,
            InfoItemType.Title => !includeHeader,
            InfoItemType.TagText => !includeHeader,
            _ => false
        };
    }

    public static string FormatItem(InfoItem item, bool richText)
    {
        string content = item.Type switch
        {
            InfoItemType.LineBreak => string.Empty,
            InfoItemType.Spacer => string.Empty,
            InfoItemType.Image => string.Empty,
            _ => item.Decoder.DecodeText(item.Content)
        };

        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        return richText ? WrapTone(content, item.Tone) : content;
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
