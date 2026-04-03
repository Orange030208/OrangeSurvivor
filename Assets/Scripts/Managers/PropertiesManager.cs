using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PropertiesManager:MonoSingletonBase<PropertiesManager>
{
    [SerializeField] private CharacterDataSO basePropsData;

    private Dictionary<PropType, float> baseProps = new();
    private readonly Dictionary<PropType, float> addens = new();

    private void Awake()
    {
        baseProps = basePropsData.GetBaseProps();

        foreach (var prop in baseProps)
        {
            addens[prop.Key] = 0;
        }
    }


    private void Start()
    {
        UpdateStatus();
    }

    public void AddProp(PropType propType,float value)
    {
        if (addens.ContainsKey(propType))
        {
            addens[propType] += value;
        }
        else
        {
            Debug.LogError($"没有找到{propType.ToString()}属性");
        }

        UpdateStatus();
    }


    public void UpdateStatus()
    {
        IEnumerable<IPlayerStatusDependency> dependencies = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPlayerStatusDependency>();
        foreach (IPlayerStatusDependency dependency in dependencies)
        {
            dependency.UpdateStatus(this);
        }
    }

    public float GetPropValue(PropType propType)
    {
        return baseProps[propType] + addens[propType];
    }
}
