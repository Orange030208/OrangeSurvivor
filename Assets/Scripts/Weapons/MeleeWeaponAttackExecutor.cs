using System.Collections.Generic;
using UnityEngine;

public sealed class MeleeWeaponAttackExecutor
{
    private readonly Transform hitOrigin;
    private readonly BoxCollider2D hitCollider;
    private readonly LayerMask enemyLayerMask;

    public MeleeWeaponAttackExecutor(Transform hitOrigin, BoxCollider2D hitCollider, LayerMask enemyLayerMask)
    {
        this.hitOrigin = hitOrigin;
        this.hitCollider = hitCollider;
        this.enemyLayerMask = enemyLayerMask;
    }

    public void ExecuteAttack(in WeaponAttackContext context, HashSet<HealthComponent> hitTargets)
    {
        if (hitOrigin == null || hitCollider == null || hitTargets == null)
        {
            return;
        }

        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            hitOrigin.position,
            hitCollider.size,
            hitOrigin.eulerAngles.z,
            enemyLayerMask);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].TryGetComponent(out HealthComponent healthComponent))
            {
                continue;
            }

            if (hitTargets.Contains(healthComponent))
            {
                continue;
            }

            hitTargets.Add(healthComponent);
            healthComponent.TakeDamage(context.Hit.ToDamageInfo(healthComponent.transform.position));
        }
    }
}
