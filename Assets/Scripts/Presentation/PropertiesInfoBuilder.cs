using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct PropertiesInfoSource
{
    public PropertiesInfoSource(PropertiesManager propertiesManager, bool includeZeroValues = true)
    {
        PropertiesManager = propertiesManager;
        IncludeZeroValues = includeZeroValues;
    }

    public PropertiesManager PropertiesManager { get; }
    public bool IncludeZeroValues { get; }
}

public sealed class PropertiesInfoBuilder :
    IInfoDocumentBuilder<PropertiesInfoSource>,
    IInfoDocumentBuilder<PropertiesManager>
{
    public InfoDocument Build(PropertiesManager source)
    {
        return Build(new PropertiesInfoSource(source));
    }

    public InfoDocument Build(PropertiesInfoSource source)
    {
        if (source.PropertiesManager == null)
        {
            return new InfoDocument(
                string.Empty,
                new[]
                {
                    InfoDocumentUtility.CreateTitle("属性"),
                    InfoDocumentUtility.CreateLineBreak(),
                    InfoDocumentUtility.CreateSectionHeader("角色属性"),
                    InfoDocumentUtility.CreateLineBreak(),
                    InfoDocumentUtility.CreateText("无法生成属性详情：PropertiesManager 为空。", InfoTone.Warning),
                    InfoDocumentUtility.CreateLineBreak()
                });
        }

        Dictionary<PropType, float> values = source.PropertiesManager.GetAllPropValues();
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
            float value = values.TryGetValue(propType, out float resolvedValue)
                ? resolvedValue
                : PropertiesManager.GetDefaultValue(propType);

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
