using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PropContainerManager : MonoSingletonBase<PropContainerManager>
{
    [SerializeField] private PropContainer propContainer;

    private void GenerateContainer(Dictionary<PropType, float> propDictionary, Transform parent)
    {
        GenerateContainerMap(propDictionary, parent);
    }

    private Dictionary<PropType, PropContainer> GenerateContainerMap(Dictionary<PropType, float> propDictionary, Transform parent)
    {
        parent.Clear();

        List<PropContainer> propContainers = new List<PropContainer>();
        Dictionary<PropType, PropContainer> containerMap = new Dictionary<PropType, PropContainer>(propDictionary.Count);

        foreach (var prop in propDictionary)
        {
            PropContainer container = Instantiate(propContainer, parent);
            string formattedValue = prop.Value.ToString("F1");
            container.Configure(ResourcesManager.GetPropIcon(prop.Key), prop.Key.GetChineseName(), formattedValue);
            propContainers.Add(container);
            containerMap[prop.Key] = container;
        }

        DOVirtual.DelayedCall(Time.deltaTime * 2, () => ResizeTexts(propContainers));
        return containerMap;
    }

    private void ResizeTexts(List<PropContainer> propContainers)
    {
        float minFontSize = 5000;
        for (int i = 0; i < propContainers.Count; i++)
        {
            PropContainer container = propContainers[i];
            float fontSize = container.GetFontSize();

            if (fontSize < minFontSize)
            {
                minFontSize = fontSize;
            }
        }

        foreach (PropContainer container in propContainers)
        {
            container.SetFontSize(minFontSize);
        }
    }

    public static void GeneratePropContainers(Dictionary<PropType, float> propDictionary, Transform parent)
    {
        Instance.GenerateContainer(propDictionary, parent);
    }

    public static Dictionary<PropType, PropContainer> GeneratePropContainersMap(Dictionary<PropType, float> propDictionary, Transform parent)
    {
        return Instance.GenerateContainerMap(propDictionary, parent);
    }
}
