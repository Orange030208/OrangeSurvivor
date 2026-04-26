using UnityEngine;

public interface IAttackable
{
    void TryAttack(Entity target);
    bool IsInAttackRange(Entity target);

    void ResetAttackTimer();
    float AttackInterval { get;}
    bool CanAttack { get; }
    LayerMask AttackLayer { get; set; }
}