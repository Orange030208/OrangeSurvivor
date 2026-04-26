using System;
using UnityEngine;

public abstract class EntityComponentBase : MonoBehaviour, IComparable<EntityComponentBase>
{
    public static class PriorityPreset
    {
        public static int NoRely = 0;
        public static int RelyOthers = 10;
        public static int Latest = 100;
    }

    public abstract Entity Owner { get; }

    public virtual int Priority => PriorityPreset.NoRely;

    public virtual void Tick(float deltaTime)
    {

    }

    public virtual void FixedTick(float deltaTime)
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
