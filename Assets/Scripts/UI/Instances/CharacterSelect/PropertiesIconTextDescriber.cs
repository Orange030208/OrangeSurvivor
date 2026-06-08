using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 使用独立 Image 与 TMP 行组件渲染属性列表，替代 TMP 富文本 sprite 标签方案。
/// </summary>
public sealed class PropertiesIconTextDescriber : Describer
{
    [Tooltip("属性行实例的父节点，通常挂 VerticalLayoutGroup 或同类布局组件。")]
    [SerializeField] private Transform contentRoot;
    [Tooltip("单行属性界面预制体。需要包含 PropContainer 所需的 Image、属性名 TMP 与数值 TMP。")]
    [SerializeField] private PropContainer propContainerPrefab;

    private readonly List<PropContainer> propContainers = new List<PropContainer>();

    private void Awake()
    {
        ValidateConfiguration();
        ClearContentRoot();
    }

    public override void Display(InfoDocument document)
    {
        Display(document, compactRowsOnly: false);
    }

    public void Display(InfoDocument document, bool compactRowsOnly)
    {
        if (document == null)
        {
            Clear();
            return;
        }

        int displayIndex = 0;
        List<InfoItem> currentLine = new();

        if (document.Items != null)
        {
            for (int itemIndex = 0; itemIndex < document.Items.Count; itemIndex++)
            {
                InfoItem item = document.Items[itemIndex];
                if (item.Type == InfoItemType.LineBreak)
                {
                    FlushDocumentLine(currentLine, compactRowsOnly, ref displayIndex);
                    continue;
                }

                if (item.Type == InfoItemType.Spacer)
                {
                    FlushDocumentLine(currentLine, compactRowsOnly, ref displayIndex);
                    if (!compactRowsOnly)
                    {
                        RenderSpacerLine(GetOrCreateContainer(displayIndex++));
                    }

                    continue;
                }

                if (item.Type != InfoItemType.Image)
                {
                    currentLine.Add(item);
                }
            }
        }

        FlushDocumentLine(currentLine, compactRowsOnly, ref displayIndex);

        HideUnusedContainers(displayIndex);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot as RectTransform);
    }

    public void DisplayProperties(PropertiesManager propertiesManager)
    {
        if (propertiesManager == null)
        {
            Clear();
            return;
        }

        int displayIndex = 0;
        Array propTypes = Enum.GetValues(typeof(PropType));
        for (int i = 0; i < propTypes.Length; i++)
        {
            PropType propType = (PropType)propTypes.GetValue(i);
            RenderProperty(displayIndex++, propertiesManager, ResolvePresentation(propType));
        }

        HideUnusedContainers(displayIndex);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot as RectTransform);
    }

    private void FlushDocumentLine(List<InfoItem> lineItems, bool compactRowsOnly, ref int displayIndex)
    {
        if (lineItems == null || lineItems.Count == 0)
        {
            return;
        }

        if (TryBuildPropertyLine(lineItems, out InfoPropertyPresentation presentation, out string valueText, out InfoTone valueTone))
        {
            PropContainer propertyContainer = GetOrCreateContainer(displayIndex++);
            propertyContainer.gameObject.SetActive(true);
            propertyContainer.Configure(
                presentation.Icon,
                presentation.DisplayName,
                valueText,
                ResolveToneColor(valueTone));
            lineItems.Clear();
            return;
        }

        if (!compactRowsOnly)
        {
            PropContainer textContainer = GetOrCreateContainer(displayIndex++);
            string text = BuildLineText(lineItems);
            InfoTone tone = ResolveLineTone(lineItems);
            string label = ResolveLineLabel(lineItems);
            textContainer.gameObject.SetActive(true);
            textContainer.Configure(null, label, text, ResolveToneColor(tone));
        }

        lineItems.Clear();
    }

    private static void RenderSpacerLine(PropContainer container)
    {
        if (container == null)
        {
            return;
        }

        container.gameObject.SetActive(true);
        container.Configure(null, string.Empty, string.Empty, Color.clear);
    }

    private static bool TryBuildPropertyLine(
        IReadOnlyList<InfoItem> lineItems,
        out InfoPropertyPresentation presentation,
        out string valueText,
        out InfoTone valueTone)
    {
        presentation = default;
        valueText = string.Empty;
        valueTone = InfoTone.Neutral;

        if (lineItems == null)
        {
            return false;
        }

        bool hasProperty = false;
        for (int i = 0; i < lineItems.Count; i++)
        {
            InfoItem item = lineItems[i];
            if (item.Type != InfoItemType.Property)
            {
                continue;
            }

            hasProperty = item.Decoder.TryDecode(item.Content, out presentation);
            if (!hasProperty)
            {
                string fallback = item.Decoder.DecodeText(item.Content).Trim().TrimEnd(':');
                presentation = new InfoPropertyPresentation(item.Content, fallback, null);
                hasProperty = true;
            }

            break;
        }

        if (!hasProperty)
        {
            return false;
        }

        List<string> valueParts = new();
        for (int i = 0; i < lineItems.Count; i++)
        {
            InfoItem item = lineItems[i];
            if (item.Type == InfoItemType.Property)
            {
                continue;
            }

            string text = item.Decoder.DecodeText(item.Content);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (valueTone == InfoTone.Neutral && item.Tone != InfoTone.Neutral)
            {
                valueTone = item.Tone;
            }

            valueParts.Add(text.Trim());
        }

        valueText = string.Join(" ", valueParts);
        return true;
    }

    private static string BuildLineText(IReadOnlyList<InfoItem> lineItems)
    {
        if (lineItems == null)
        {
            return string.Empty;
        }

        List<string> parts = new();
        for (int i = 0; i < lineItems.Count; i++)
        {
            string text = lineItems[i].Decoder.DecodeText(lineItems[i].Content);
            if (!string.IsNullOrWhiteSpace(text))
            {
                parts.Add(text.Trim());
            }
        }

        return string.Join(" ", parts);
    }

    private static string ResolveLineLabel(IReadOnlyList<InfoItem> lineItems)
    {
        if (lineItems == null || lineItems.Count == 0)
        {
            return string.Empty;
        }

        return lineItems[0].Type == InfoItemType.TagText ? "标签" : string.Empty;
    }

    private static InfoTone ResolveLineTone(IReadOnlyList<InfoItem> lineItems)
    {
        if (lineItems == null)
        {
            return InfoTone.Neutral;
        }

        for (int i = 0; i < lineItems.Count; i++)
        {
            if (lineItems[i].Tone != InfoTone.Neutral)
            {
                return lineItems[i].Tone;
            }
        }

        return InfoTone.Neutral;
    }

    private static Color ResolveToneColor(InfoTone tone)
    {
        return tone switch
        {
            InfoTone.Positive => new Color32(79, 220, 111, 255),
            InfoTone.Negative => new Color32(236, 74, 74, 255),
            InfoTone.Warning => new Color32(255, 183, 77, 255),
            InfoTone.Emphasis => new Color32(91, 214, 255, 255),
            InfoTone.Disabled => new Color32(135, 145, 155, 255),
            _ => Color.white
        };
    }

    private PropContainer GetOrCreateContainer(int index)
    {
        while (propContainers.Count <= index)
        {
            // 行对象按需创建并复用，避免每次刷新属性面板都产生额外实例和 GC 压力。
            PropContainer instance = Instantiate(propContainerPrefab, contentRoot);
            propContainers.Add(instance);
        }

        return propContainers[index];
    }

    private void HideUnusedContainers(int startIndex)
    {
        for (int i = startIndex; i < propContainers.Count; i++)
        {
            propContainers[i].gameObject.SetActive(false);
        }
    }

    private void Clear()
    {
        HideUnusedContainers(0);
    }

    private void RenderProperty(
        int displayIndex,
        PropertiesManager propertiesManager,
        PropPresentationEntry presentation)
    {
        PropType propType = presentation.PropType;
        float rawValue = propertiesManager.GetPropValue(propType);
        PropContainer container = GetOrCreateContainer(displayIndex);
        container.gameObject.SetActive(true);
        container.Configure(
            presentation.Icon,
            ResolveDisplayName(presentation, propType),
            propType.FormatDisplayValue(rawValue),
            rawValue);
    }

    private PropPresentationEntry ResolvePresentation(PropType propType)
    {
        if (GameContentRuntime.TryGetPropPresentationEntry(propType, out PropPresentationEntry entry))
        {
            return entry;
        }

        return new PropPresentationEntry(propType, propType.ToString(), string.Empty, null);
    }

    private static string ResolveDisplayName(PropPresentationEntry presentation, PropType propType)
    {
        return string.IsNullOrWhiteSpace(presentation.ChineseName)
            ? propType.ToString()
            : presentation.ChineseName;
    }

    private void ValidateConfiguration()
    {
        if (contentRoot == null)
        {
            throw new MissingReferenceException($"{nameof(PropertiesIconTextDescriber)} '{name}' is missing content root.");
        }

        if (propContainerPrefab == null)
        {
            throw new MissingReferenceException($"{nameof(PropertiesIconTextDescriber)} '{name}' is missing prop container prefab.");
        }
    }

    private void ClearContentRoot()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }
    }
}
