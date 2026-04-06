using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PropContainerManager : MonoSingletonBase<PropContainerManager>
{
    [SerializeField] private PropContainer propContainer;

    private void GenerateContainer(Dictionary<PropType, float> propDictionary, Transform parent)
    {
        // 清理现有的属性容器
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }

        List<PropContainer> propContainers = new List<PropContainer>();
        foreach (var prop in propDictionary)
        {
            PropContainer container = Instantiate(propContainer, parent);
            string formattedValue = prop.Value.ToString("F1");
            container.Configure(ResourcesManager.GetPropIcon(prop.Key), prop.Key.GetChineseName(), formattedValue);
            propContainers.Add(container);
        }

        DOVirtual.DelayedCall(Time.deltaTime * 2, () => ResizeTexts(propContainers));
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
}