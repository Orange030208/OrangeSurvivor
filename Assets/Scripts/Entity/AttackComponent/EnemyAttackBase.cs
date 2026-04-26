using System;
using UnityEngine;

public abstract class EnemyAttackBase : EntityComponentBase, IAttackable
{
    protected PropertiesManager propertiesManager;
    public PropertiesManager PropertiesManager => propertiesManager;
    public abstract void TryAttack(Entity target);

    public abstract bool IsInAttackRange(Entity target);
    public abstract void ResetAttackTimer();
    public abstract float AttackInterval { get; }
    public abstract bool CanAttack { get; }
    public virtual LayerMask AttackLayer { get; set; }

    public override void Initialize(Entity owner)
    {
        AttackLayer = LayerMask.GetMask("Player");
        propertiesManager = owner.GetComponent<PropertiesManager>();
    }

    public override int Priority => EntityComponentBase.PriorityPreset.RelyOthers;
}
