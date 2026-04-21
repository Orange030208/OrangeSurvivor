using System;
using System.Collections.Generic;
using UnityEngine;

//TODO:后续使用对象池
public class UIPropertiesViewList : MonoBehaviour
{
    [Header("Prop管理")]
    [SerializeField] private PropContainer propContainerPrefab;
    [SerializeField] private Transform propContainersParent;

    private readonly List<PropContainer> propContainers = new();
    private readonly PropEntryDisplayBuilder propEntryDisplayBuilder = new();

    private void Awake()
    {
        propContainersParent.Clear();
    }

    public void Render(StatListBlock block)
    {
        if (propContainersParent == null || propContainerPrefab == null)
        {
            return;
        }

        IReadOnlyList<StatItem> items = block != null ? block.Items : null;
        int count = items?.Count ?? 0;
        EnsureContainerCount(count);

        for (int i = 0; i < count; i++)
        {
            StatItem item = items[i];
            PropContainer container = propContainers[i];
            float rawValue = item != null && item.NumericValue.HasValue ? item.NumericValue.Value : 0f;

            container.Configure(
                item != null ? item.Icon : null,
                item != null ? item.Key : string.Empty,
                item != null ? item.Value : string.Empty,
                rawValue);
        }
    }

    public void Render(PropEntry[] propEntries)
    {
        if (propEntries == null)
        {
            HideAll();
            return;
        }

        Render((IReadOnlyList<PropEntry>)propEntries);
    }

    public void Render(IReadOnlyList<PropEntry> propEntries)
    {
        if (propEntries == null)
        {
            HideAll();
            return;
        }

        Render(propEntryDisplayBuilder.BuildStatBlock(propEntries));
    }

    public void HideAll()
    {
        EnsureContainerCount(0);
    }

    private void EnsureContainerCount(int targetCount)
    {
        while (propContainers.Count > targetCount)
        {
            int lastIndex = propContainers.Count - 1;
            PropContainer container = propContainers[lastIndex];
            propContainers.RemoveAt(lastIndex);

            if (container != null)
            {
                Destroy(container.gameObject);
            }
        }

        while (propContainers.Count < targetCount)
        {
            PropContainer newContainer = Instantiate(propContainerPrefab, propContainersParent);
            propContainers.Add(newContainer);
        }
    }
}
