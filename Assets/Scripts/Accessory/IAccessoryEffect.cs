using System;
using Survivors.Player;
using UnityEngine;

public interface IAccessoryEffect
{
    string EffectId { get; }
    void OnEquip(GameObject owner, PropertiesManager propertiesManager);
    void OnUnequip(GameObject owner, PropertiesManager propertiesManager);
    void OnUpdate(GameObject owner, PropertiesManager propertiesManager, float deltaTime);
}

[Serializable]
public abstract class AccessoryEffectBase : IAccessoryEffect
{
    [field: SerializeField] public string EffectId { get; protected set; }

    public abstract void OnEquip(GameObject owner, PropertiesManager propertiesManager);
    public abstract void OnUnequip(GameObject owner, PropertiesManager propertiesManager);

    public virtual void OnUpdate(GameObject owner, PropertiesManager propertiesManager, float deltaTime)
    {
    }
}