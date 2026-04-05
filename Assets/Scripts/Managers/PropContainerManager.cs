using System.Collections.Generic;
using UnityEngine;

public class PropContainerManager : MonoSingletonBase<PropContainerManager>
{
    [SerializeField] private PropContainer propContainer;

    private void GenerateContainer(Dictionary<PropType,float> propDictionary,Transform parent)
    {
        foreach (var prop in propDictionary)
        {
            PropContainer container = Instantiate(propContainer, parent);
            container.Configure(ResourcesManager.GetPropIcon(prop.Key), prop.Key.GetChineseName(), prop.Value.ToString());
        }
    }

    public static void GeneratePropContainers(Dictionary<PropType, float> propDictionary, Transform parent)
    {
        Instance.GenerateContainer(propDictionary, parent);
    }
}