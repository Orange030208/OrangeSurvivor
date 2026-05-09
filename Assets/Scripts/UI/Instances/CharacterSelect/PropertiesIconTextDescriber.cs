using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 使用独立 Image 与 TMP 行组件渲染属性列表，替代 TMP 富文本 sprite 标签方案。
/// </summary>
public sealed class PropertiesIconTextDescriber : Describer
{
    [Tooltip("属性行实例的父节点，通常挂 VerticalLayoutGroup 或同类布局组件。")]
    [SerializeField] private Transform contentRoot;
    [Tooltip("单行属性 UI 预制体。需要包含 PropContainer 所需的 Image、属性名 TMP 与数值 TMP。")]
    [SerializeField] private PropContainer propContainerPrefab;

    private readonly List<PropContainer> propContainers = new List<PropContainer>();

    private void Awake()
    {
        contentRoot.Clear();
        ValidateConfiguration();
    }

    public override void Display(IDescribable describable)
    {
        if (describable == null)
        {
            Clear();
            return;
        }

        Display(describable.GetExtraInfos());
    }

    private void Display(IEnumerable<DescriptorInfo> descriptorInfos)
    {
        if (descriptorInfos == null)
        {
            Clear();
            return;
        }

        int displayIndex = 0;
        foreach (DescriptorInfo descriptorInfo in descriptorInfos)
        {
            PropContainer container = GetOrCreateContainer(displayIndex);
            PropPresentationEntry presentation = ResolvePresentation(descriptorInfo.label);
            // PropContainer 使用 rawValue 决定数值颜色；解析失败时回落到 0，避免描述文本影响渲染流程。
            float rawValue = ParseRawValue(descriptorInfo.value);

            container.gameObject.SetActive(true);
            container.Configure(
                presentation.Icon,
                presentation.ChineseName,
                descriptorInfo.value,
                rawValue);

            displayIndex++;
        }

        HideUnusedContainers(displayIndex);
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

    private static float ParseRawValue(string valueText)
    {
        if (string.IsNullOrWhiteSpace(valueText))
        {
            return 0f;
        }

        string normalized = valueText.Trim().Replace("%", string.Empty);
        return float.TryParse(normalized, out float value) ? value : 0f;
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
}
