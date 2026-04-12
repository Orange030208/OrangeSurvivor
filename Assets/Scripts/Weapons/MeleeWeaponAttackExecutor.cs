using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 近战攻击执行器：
/// 在命中窗口打开期间，持续用 OverlapBox 检测敌人，
/// 并借助 hitTargets 防止同一窗口内重复命中同一目标。
/// </summary>
public sealed class MeleeWeaponAttackExecutor
{
    private readonly Transform hitOrigin;
    private readonly BoxCollider2D hitCollider;
    private readonly LayerMask enemyLayerMask;
    private readonly float innerCompensationRadius;

    public MeleeWeaponAttackExecutor(Transform hitOrigin, BoxCollider2D hitCollider, LayerMask enemyLayerMask, float innerCompensationRadius = 1.1f)
    {
        this.hitOrigin = hitOrigin;
        this.hitCollider = hitCollider;
        this.enemyLayerMask = enemyLayerMask;
        this.innerCompensationRadius = Mathf.Max(0.05f, innerCompensationRadius);
    }

    /// <summary>
    /// 检测命中盒覆盖到的所有敌人，并对本窗口内尚未命中过的目标结算伤害。
    /// </summary>
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

        ApplyDamage(colliders, context, hitTargets);

        // 有些大开大合的横扫动作会把命中盒摆在较外围，
        // 如果敌人已经钻到角色脸上，纯靠当前命中盒位置会显得武器突然打不到近身目标。
        // 这里补一个以内圈为中心的小范围兜底，让“贴脸敌人”至少不会因为动画外圈路径而完全漏判。
        Collider2D[] innerColliders = Physics2D.OverlapCircleAll(context.Weapon.transform.position, innerCompensationRadius, enemyLayerMask);
        ApplyDamage(innerColliders, context, hitTargets);
    }

    private static void ApplyDamage(Collider2D[] colliders, in WeaponAttackContext context, HashSet<HealthComponent> hitTargets)
    {
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
