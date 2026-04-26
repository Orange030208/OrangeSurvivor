using System;
using UnityEngine;

public abstract class EntityComponentBase : MonoBehaviour, IComparable<EntityComponentBase>,ILifecycle
{
    public static class PriorityPreset
    {
        public static int NoRely = 0;
        public static int RelyOthers = 10;
        public static int Latest = 100;
    }

    public abstract Entity Owner { get; }

    public virtual int Priority => PriorityPreset.NoRely;

    public virtual void OnTick(float deltaTime)
    {

    }

    public virtual void OnFixedTick(float deltaTime)
    {

    }

    public abstract void Initialize(Entity owner);

    public virtual void OnEnableComponent()
    {

    }

    public virtual void OnDisableComponent()
    {

    }

    public int CompareTo(EntityComponentBase other)
    {
        return Priority.CompareTo(other.Priority);
    }
}

public interface ILifecycle
{
    public int Priority { get; }

    public void OnTick(float deltaTime);

    public void OnFixedTick(float deltaTime);

    public void Initialize(Entity owner);

    public void OnEnableComponent();

    public void OnDisableComponent();
}
