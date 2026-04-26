using System;
using UnityEngine;

[RequireComponent(typeof(IAnimatable))]
[RequireComponent(typeof(IMovable))]
[RequireComponent(typeof(Enemy))]
public abstract class EnemyBrain : EntityBrain
{
    protected Entity target;
    protected Enemy owner;
    protected IMovable currentMovable;
    protected IAnimatable currentAnimatable;
    protected HealthComponent healthComponent;
    protected PropertiesManager propertiesManager;

    protected bool isDead;
    protected bool isBrainActive;

    public override Entity Owner => owner;

    protected override void OnInitialize(Entity owner)
    {
        this.owner = owner as Enemy;
        if (this.owner == null)
        {
            throw new ArgumentException($"{nameof(EnemyBrain)} requires an {nameof(Enemy)} owner.", nameof(owner));
        }

        target = this.owner.TargetEntity;
        currentMovable = this.owner.MoveComponent;
        healthComponent = this.owner.HealthComponent;
        propertiesManager = this.owner.PropertiesManager;
        currentAnimatable = this.owner.AnimComponent;

        isBrainActive = true;
    }

    public override void OnTick(float deltaTime)
    {
        if (!ShouldUpdateBrain())
        {
            return;
        }

        OnDetermineState();
        OnBrainUpdate();
    }

    public override void OnFixedTick(float deltaTime)
    {
        if (!ShouldUpdateBrain())
        {
            return;
        }

        OnBrainFixedUpdate();
    }

    protected virtual bool ShouldUpdateBrain()
    {
        return isBrainActive && !isDead && target != null;
    }

    protected abstract void OnDetermineState();

    protected virtual void OnBrainUpdate()
    {
    }

    protected virtual void OnBrainFixedUpdate()
    {
    }

    public override void StopBrain()
    {
        isBrainActive = false;
        enabled = false;
    }

    public virtual void SetTarget(Entity newTarget)
    {
        target = newTarget;
    }
}
