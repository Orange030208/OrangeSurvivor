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
        if (document == null)
        {
            Clear();
            return;
        }

        int displayIndex = 0;
        bool compactRowsOnly = document.Kind == InfoDocumentKind.Properties;
        if (!compactRowsOnly && !string.IsNullOrWhiteSpace(document.Title))
        {
            PropContainer titleContainer = GetOrCreateContainer(displayIndex++);
            titleContainer.gameObject.SetActive(true);
            titleContainer.Configure(null, document.Title, document.Title, 0f);
        }

        if (!compactRowsOnly && document.Tags != null && document.Tags.Count > 0)
        {
            PropContainer tagContainer = GetOrCreateContainer(displayIndex++);
            tagContainer.gameObject.SetActive(true);
            tagContainer.Configure(null, "标签", string.Join(" / ", document.Tags), 0f);
        }

        if (document.Sections != null)
        {
            for (int sectionIndex = 0; sectionIndex < document.Sections.Count; sectionIndex++)
            {
                InfoSection section = document.Sections[sectionIndex];
                if (section == null || section.Lines == null)
                {
                    continue;
                }

                if (!compactRowsOnly && !string.IsNullOrWhiteSpace(section.Title))
                {
                    PropContainer sectionTitleContainer = GetOrCreateContainer(displayIndex++);
                    sectionTitleContainer.gameObject.SetActive(true);
                    sectionTitleContainer.Configure(null, section.Title, section.Title, 0f);
                }

                for (int lineIndex = 0; lineIndex < section.Lines.Count; lineIndex++)
                {
                    InfoLine line = section.Lines[lineIndex];
                    if (line == null)
                    {
                        continue;
                    }

                    PropContainer container = GetOrCreateContainer(displayIndex++);
                    string lineLabel = string.IsNullOrWhiteSpace(line.Label) ? string.Empty : line.Label;
                    PropPresentationEntry presentation = ResolvePresentation(lineLabel);
                    container.gameObject.SetActive(true);
                    container.Configure(
                        presentation.Icon,
                        string.IsNullOrWhiteSpace(presentation.ChineseName) ? lineLabel : presentation.ChineseName,
                        InfoDocumentUtility.BuildLineText(line.Parts),
                        ResolveRawValue(line));
                }
            }
        }

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

    private static float ResolveRawValue(InfoLine line)
    {
        if (line == null || line.Parts == null)
        {
            return 0f;
        }

        for (int i = 0; i < line.Parts.Count; i++)
        {
            string text = line.Parts[i].Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            string normalized = text.Trim().Replace("%", string.Empty).Replace("s", string.Empty).Replace("格", string.Empty);
            if (float.TryParse(normalized, out float value))
            {
                return value;
            }
        }

        return 0f;
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

    private PropPresentationEntry ResolvePresentation(string propName)
    {
        if (GameContentRuntime.TryGetPropPresentationEntry(propName, out PropPresentationEntry entry))
        {
            return entry;
        }

        // 目录表漏配时仍显示原始 label，方便在 UI 上直接发现是哪项配置缺失。
        string fallbackName = string.IsNullOrWhiteSpace(propName) ? string.Empty : propName;
        return new PropPresentationEntry(default, fallbackName, string.Empty, null);
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
