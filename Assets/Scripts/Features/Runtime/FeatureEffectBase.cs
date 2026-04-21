using System;
using UnityEngine;

public interface IRuntimeFeatureEffect
{
    string RuntimeFeatureId { get; set; }
    FeatureContext Context { get; set; }
    void OnInstall();
    void OnUninstall();
    void OnUpdate(float deltaTime);
}

[HideInFeatureMenu]
[Serializable]
public abstract class FeatureEffectBase : IRuntimeFeatureEffect,IHitModifier
{
    [HideInInspector]
    [SerializeField] private string runtimeFeatureId;

    public string RuntimeFeatureId
    {
        get => runtimeFeatureId;
        set => runtimeFeatureId = value;
    }

    public FeatureContext Context { get; set; }

    public abstract string FeatureDescription { get; }
    
    public virtual void OnInstall(){}
    public virtual void OnUninstall(){}

    public virtual void OnUpdate(float deltaTime)
    {
    }

    //命中管线参与能力默认关闭，仅需要影响命中结算的 feature 重写以下成员
    public virtual bool CanModifyHit => false;
    public virtual int HitPriority => int.MaxValue;

    public virtual HitModifierTiming HitModifierTiming => HitModifierTiming.Deal;

    public virtual void ModifyHit(HitContext hitContext)
    {
    }
}

