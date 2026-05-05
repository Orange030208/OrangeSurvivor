using System;
using System.Collections.Generic;
using UnityEngine;

public interface IFeatureEffect
{
    FeatureContext Context { get; set; }
    string SourceId { get; set; }
    void OnInstall();
    void OnUninstall();
    void OnUpdate(float deltaTime);
}

[Serializable]
public abstract class FeatureEffectBase : IFeatureEffect,IHitModifier,IDescribable
{
    public FeatureContext Context { get; set; }
    public string SourceId { get; set; }

    public virtual void OnInstall(){}
    public virtual void OnUninstall(){}

    public virtual void OnUpdate(float deltaTime)
    {
    }

    //命中管线参与能力默认关闭，仅需要影响命中结算的 feature 重写以下成员
    //TODO:后续添加编辑器条件渲染
    public virtual bool CanModifyHit => false;
    
    [SerializeField] protected HitModifierTiming hitModifierTiming = HitModifierTiming.Deal;
    
    public virtual int HitPriority => HitModifierPriority.Parameter;

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

    public virtual FeatureEffectBase CreateRuntimeCopy()
    {
        Type type = GetType();
        FeatureEffectBase copy = Activator.CreateInstance(type) as FeatureEffectBase;
        if (copy == null)
        {
            return this;
        }

        CopySerializableFields(this, copy, type);
        return copy;
    }

    private static void CopySerializableFields(FeatureEffectBase source, FeatureEffectBase target, Type type)
    {
        while (type != null && type != typeof(object))
        {
            System.Reflection.FieldInfo[] fields = type.GetFields(
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.DeclaredOnly);
            for (int i = 0; i < fields.Length; i++)
            {
                System.Reflection.FieldInfo field = fields[i];
                if (field.IsInitOnly || field.IsStatic)
                {
                    continue;
                }

                if (!field.IsPublic
                    && !Attribute.IsDefined(field, typeof(SerializeField), true)
                    && !Attribute.IsDefined(field, typeof(SerializeReference), true))
                {
                    continue;
                }

                field.SetValue(target, field.GetValue(source));
            }

            type = type.BaseType;
        }
    }
}

