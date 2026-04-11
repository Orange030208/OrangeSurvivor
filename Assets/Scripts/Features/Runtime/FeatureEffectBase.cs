using System;
using UnityEngine;

public interface IRuntimeFeatureEffect
{
    string RuntimeFeatureId { get; set; }
    void OnInstall(FeatureContext context);
    void OnUninstall(FeatureContext context);
    void OnUpdate(FeatureContext context, float deltaTime);
}

[HideInFeatureMenu]
[Serializable]
public abstract class FeatureEffectBase : IRuntimeFeatureEffect, IFeatureDefinition
{
    [HideInInspector]
    [SerializeField] private string runtimeFeatureId;

    public string RuntimeFeatureId
    {
        get => runtimeFeatureId;
        set => runtimeFeatureId = value;
    }

    public virtual string FeatureTitle => GetType().Name;
    public abstract string FeatureDescription { get; }
    public virtual FeatureCategory FeatureCategory => FeatureCategory.Passive;
    public virtual FeaturePolarity FeaturePolarity => FeaturePolarity.Positive;

    public abstract void OnInstall(FeatureContext context);
    public abstract void OnUninstall(FeatureContext context);

    public virtual void OnUpdate(FeatureContext context, float deltaTime)
    {
    }
}
