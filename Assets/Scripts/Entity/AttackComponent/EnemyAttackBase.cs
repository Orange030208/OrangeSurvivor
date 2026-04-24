using System;
using UnityEngine;

public abstract class EnemyAttackBase : EntityComponentBase,IAttackable
{
    public abstract void TryAttack(Entity target);

    public abstract bool IsInAttackRange(Entity target);
    public abstract void ResetAttackTimer();
    public abstract float AttackInterval { get; set; }
    public abstract bool CanAttack { get; }
    public virtual LayerMask AttackLayer { get; set; }

    public override void Initialize(Entity owner)
    {
        AttackLayer = LayerMask.GetMask("Player");
    }
}
