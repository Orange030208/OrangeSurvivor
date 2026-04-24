using System;
using UnityEngine;

public abstract class EnemyBrain : EntityBrain
{
    protected Entity target;
    protected Enemy owner;
    protected IMovable currentMovable;
    protected HealthComponent healthComponent;

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

        isBrainActive = true;
    }

    protected virtual void Update()
    {
        if (!ShouldUpdateBrain())
        {
            return;
        }

        OnDetermineState();
        OnBrainUpdate();
    }

    protected virtual void FixedUpdate()
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

    public override void SetTarget(Entity newTarget)
    {
        target = newTarget;
    }
}
