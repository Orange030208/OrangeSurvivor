using System;
using System.Collections.Generic;
using UnityEngine;

public interface IFeatureEffect
{
    FeatureContext Context { get; set; }
    void OnInstall();
    void OnUninstall();
    void OnUpdate(float deltaTime);
}

[Serializable]
public abstract class FeatureEffectBase : IFeatureEffect,IHitModifier,IDescribable
{
    public FeatureContext Context { get; set; }

    public virtual void OnInstall(){}
    public virtual void OnUninstall(){}

    public virtual void OnUpdate(float deltaTime)
    {
    }

    //命中管线参与能力默认关闭，仅需要影响命中结算的 feature 重写以下成员
    //TODO:后续添加编辑器条件渲染
    public virtual bool CanModifyHit => false;
    
    [SerializeField] protected HitModifierTiming hitModifierTiming = HitModifierTiming.Deal;
    
    public virtual int HitPriority => int.MaxValue;

    public HitModifierTiming HitModifierTiming => hitModifierTiming;

    public virtual void ModifyHit(HitContext hitContext)
    {
    }

    public virtual string Title { get; set; }
    public virtual Sprite Icon { get; set; }
    public virtual string Description { get; set; }
    public virtual IEnumerable<DescriptorInfo> GetExtraInfos()
    {
        return new List<DescriptorInfo>();
    }
}

