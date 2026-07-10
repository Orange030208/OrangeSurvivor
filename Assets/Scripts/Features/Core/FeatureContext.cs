using UnityEngine;

public sealed class FeatureContext
{
    private FeatureHost featureHost;
    private CurrencyWallet currencyWallet;

    public Entity OwnerEntity { get; }
    public Transform Transform => OwnerEntity.Transform;
    public AttributeManager AttributeManager { get; }
    public HealthComponent HealthComponent { get; }
    public FeatureHost FeatureHost => featureHost != null
        ? featureHost
        : featureHost = OwnerEntity != null ? OwnerEntity.GetComponent<FeatureHost>() : null;
    public CurrencyWallet CurrencyWallet => currencyWallet != null
        ? currencyWallet
        : currencyWallet = OwnerEntity != null ? OwnerEntity.GetComponent<CurrencyWallet>() : null;

    public FeatureContext(Entity ownerEntity, AttributeManager attributeManager)
    {
        OwnerEntity = ownerEntity;
        AttributeManager = attributeManager;
        HealthComponent = ownerEntity != null ? ownerEntity.GetComponent<HealthComponent>() : null;
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
