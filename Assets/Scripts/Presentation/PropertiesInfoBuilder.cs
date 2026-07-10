using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct PropertiesInfoSource
{
    public PropertiesInfoSource(AttributeManager attributeManager, bool includeZeroValues = true)
    {
        AttributeManager = attributeManager;
        IncludeZeroValues = includeZeroValues;
    }

    public AttributeManager AttributeManager { get; }
    public bool IncludeZeroValues { get; }
}

public sealed class PropertiesInfoBuilder :
    IInfoDocumentBuilder<PropertiesInfoSource>,
    IInfoDocumentBuilder<AttributeManager>
{
    public InfoDocument Build(AttributeManager source)
    {
        return Build(new PropertiesInfoSource(source));
    }

    public InfoDocument Build(PropertiesInfoSource source)
    {
        if (source.AttributeManager == null)
        {
            return new InfoDocument(
                string.Empty,
                new[]
                {
                    InfoDocumentUtility.CreateTitle("属性"),
                    InfoDocumentUtility.CreateLineBreak(),
                    InfoDocumentUtility.CreateSectionHeader("角色属性"),
                    InfoDocumentUtility.CreateLineBreak(),
                    InfoDocumentUtility.CreateText("无法生成属性详情：AttributeManager 为空。", InfoTone.Warning),
                    InfoDocumentUtility.CreateLineBreak()
                });
        }

        Dictionary<PropType, int> values = source.AttributeManager.GetAllAttributeValues();
        List<InfoItem> items = new()
        {
            InfoDocumentUtility.CreateTitle("属性"),
            InfoDocumentUtility.CreateLineBreak(),
            InfoDocumentUtility.CreateSectionHeader("角色属性"),
            InfoDocumentUtility.CreateLineBreak()
        };

        Array propTypes = Enum.GetValues(typeof(PropType));
        for (int i = 0; i < propTypes.Length; i++)
        {
            PropType propType = (PropType)propTypes.GetValue(i);
            int value = values.TryGetValue(propType, out int resolvedValue)
                ? resolvedValue
                : AttributeManager.GetDefaultAttributeValue(propType);

            if (!source.IncludeZeroValues && Mathf.Approximately(value, 0f))
            {
                continue;
            }

            InfoDocumentUtility.AppendPropertyLine(
                items,
                propType.ToString(),
                propType.FormatDisplayValue(value),
                ResolveTone(value));
        }

        return new InfoDocument(
            "properties",
            items);
    }

    private static InfoTone ResolveTone(float value)
    {
        if (value > 0f)
        {
            return InfoTone.Positive;
        }

        if (value < 0f)
        {
            return InfoTone.Negative;
        }

        return InfoTone.Neutral;
    }
}
