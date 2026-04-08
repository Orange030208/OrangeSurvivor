using System.Collections.Generic;
using UnityEngine;

public class PropertiesDisplay : MonoBehaviour
{
    [Header("属性来源")]
    [SerializeField] private PropertiesManager propertiesManager;

    [Header("容器父物体")]
    [SerializeField] private Transform propContainersParent;

    private Dictionary<PropType, PropContainer> propContainerMap = new();

    private void OnEnable()
    {
        Bind(FindObjectOfType<PropertiesManager>());
        Subscribe();
        BuildAllContainers();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void Bind(PropertiesManager manager)
    {
        if (propertiesManager == manager)
        {
            RefreshAllContainers();
            return;
        }

        Unsubscribe();
        propertiesManager = manager;
        Subscribe();
        BuildAllContainers();
    }

    private void Subscribe()
    {
        if (propertiesManager == null)
        {
            return;
        }

        propertiesManager.OnPropertyChanged += OnPropertyChanged;
        propertiesManager.OnAllPropertiesChanged += OnAllPropertiesChanged;
    }

    private void Unsubscribe()
    {
        if (propertiesManager == null)
        {
            return;
        }

        propertiesManager.OnPropertyChanged -= OnPropertyChanged;
        propertiesManager.OnAllPropertiesChanged -= OnAllPropertiesChanged;
    }

    private void BuildAllContainers()
    {
        if (propertiesManager == null || propContainersParent == null)
        {
            return;
        }

        Dictionary<PropType, float> allProps = propertiesManager.GetAllPropValues();
        propContainerMap = PropContainerManager.GeneratePropContainersMap(allProps, propContainersParent);
    }

    private void RefreshAllContainers()
    {
        if (propertiesManager == null)
        {
            return;
        }

        Dictionary<PropType, float> allProps = propertiesManager.GetAllPropValues();
        if (propContainerMap == null || propContainerMap.Count != allProps.Count)
        {
            BuildAllContainers();
            return;
        }

        foreach (var prop in allProps)
        {
            if (!propContainerMap.TryGetValue(prop.Key, out PropContainer container))
            {
                BuildAllContainers();
                return;
            }

            container.SetValue(prop.Value);
        }
    }

    private void OnPropertyChanged(PropType propType, float value)
    {
        if (propContainerMap != null && propContainerMap.TryGetValue(propType, out PropContainer container))
        {
            container.SetValue(value);
            return;
        }

        BuildAllContainers();
    }

    private void OnAllPropertiesChanged()
    {
        RefreshAllContainers();
    }
}
