using UnityEngine;

public sealed class FeatureContext
{
    public Entity OwnerEntity { get; }
    public Transform Transform => OwnerEntity.Transform;
    public PropertiesManager PropertiesManager { get; }
    public HealthComponent HealthComponent { get; }
    public WeaponsHolder WeaponsHolder { get; }
    public AccessoryManager AccessoryManager { get; }
    public PlayerLevel PlayerLevel { get; }

    public FeatureContext(Entity ownerEntity, PropertiesManager propertiesManager)
    {
        OwnerEntity = ownerEntity;
        PropertiesManager = propertiesManager;
        HealthComponent = ownerEntity != null ? ownerEntity.GetComponent<HealthComponent>() : null;
        WeaponsHolder = ownerEntity != null ? ownerEntity.GetComponent<WeaponsHolder>() : null;
        AccessoryManager = ownerEntity != null ? ownerEntity.GetComponent<AccessoryManager>() : null;
        PlayerLevel = ownerEntity != null ? ownerEntity.GetComponent<PlayerLevel>() : null;
    }

    public T GetComponent<T>() where T : Component
    {
        return OwnerEntity != null ? OwnerEntity.GetComponent<T>() : null;
    }

    public bool TryGetComponent<T>(out T component) where T : Component
    {
        if (OwnerEntity != null && OwnerEntity.TryGetComponent(out component))
        {
            return true;
        }

        component = null;
        return false;
    }
}
