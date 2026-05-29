using System;
using UnityEngine;

public interface IFeature
{
    FeatureContext Context { get; set; }
    string SourceId { get; set; }
    void OnInstall();
    void OnUninstall();
    void OnUpdate(float deltaTime);
}

[Serializable]
public abstract class FeatureBase : IFeature
{
    public FeatureContext Context { get; set; }
    public string SourceId { get; set; }

    public virtual void OnInstall(){}
    public virtual void OnUninstall(){}

    public virtual void OnUpdate(float deltaTime)
    {
    }

    public virtual string Title { get; set; }
    public virtual Sprite Icon { get; set; }
    public virtual string Description { get; set; }

    public virtual FeatureBase CreateRuntimeCopy()
    {
        Type type = GetType();
        FeatureBase copy = Activator.CreateInstance(type) as FeatureBase;
        if (copy == null)
        {
            return this;
        }

        CopySerializableFields(this, copy, type);
        return copy;
    }

    private static void CopySerializableFields(FeatureBase source, FeatureBase target, Type type)
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

[Serializable]
public abstract class HitModifierFeatureBase : FeatureBase, IHitModifier
{
    [SerializeField] protected HitModifierTiming hitModifierTiming = HitModifierTiming.Deal;

    public virtual int HitPriority => HitModifierPriority.Parameter;

    public HitModifierTiming HitModifierTiming => hitModifierTiming;

    public abstract void ModifyHit(HitContext hitContext);
}

