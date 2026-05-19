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
    protected IEntityFacingController facingController;
    protected HealthComponent healthComponent;
    protected PropertiesManager propertiesManager;

    protected bool isDead;
    protected bool isBrainActive;
    protected bool isSpawnLocked;

    public override Entity Owner => owner;

    protected override bool ShouldStartOnInitialize => false;

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
        facingController = this.owner.GetComponent<IEntityFacingController>();

        isBrainActive = false;
    }

    public override void OnTick(float deltaTime)
    {
        if (!ShouldUpdateBrain())
        {
            return;
        }

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
        return isBrainActive && !isDead && !isSpawnLocked;
    }

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

    public override void StartBrain()
    {
        isDead = false;
        isBrainActive = true;
        EnsureBrainStarted();
        enabled = true;
    }

    public void LockSpawn()
    {
        isSpawnLocked = true;
        currentMovable?.StopMoving();
    }

    public void UnlockSpawn()
    {
        isSpawnLocked = false;
        if (isBrainActive)
        {
            enabled = true;
        }
    }

    public virtual void SetTarget(Entity newTarget)
    {
        target = newTarget;
    }

    protected void FaceTarget()
    {
        facingController?.FaceTarget(target);
    }

    protected void FaceMoveDirection()
    {
        facingController?.FaceMoveDirection(currentMovable);
    }
}
