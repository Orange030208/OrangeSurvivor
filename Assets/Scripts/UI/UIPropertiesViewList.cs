using System.Collections.Generic;
using UnityEngine;

//TODO:后续使用对象池
public class UIPropertiesViewList : MonoBehaviour
{
    [Header("Prop管理")]
    [SerializeField] private PropContainer propContainerPrefab;
    [SerializeField] private Transform propContainersParent;

    private readonly List<PropContainer> propContainers = new();

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
        if (propContainersParent == null || propContainerPrefab == null)
        {
            return;
        }

        int count = propEntries?.Count ?? 0;
        EnsureContainerCount(count);

        for (int i = 0; i < count; i++)
        {
            PropEntry entry = propEntries[i];
            PropContainer container = propContainers[i];

            container.Configure(
                ResourcesManager.GetPropIcon(entry.propType),
                entry.propType.GetChineseName(),
                entry.value);
        }
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
