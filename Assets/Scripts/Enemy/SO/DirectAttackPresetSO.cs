using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Direct Attack Preset", menuName = "SO/Enemies/Attack/Direct Attack Preset", order = 20)]
public sealed class DirectAttackPresetSO : AttackPresetSO
{
    [Header("Attack")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private float attackFrequency = 1f;

    public override Type GetComponentType()
    {
        return typeof(DirectAttack);
    }

    public override void ApplyTo(AttackBase attack, EnemySO enemy)
    {
        if (attack is not DirectAttack directAttack)
        {
            throw new InvalidOperationException($"{nameof(DirectAttackPresetSO)} requires {nameof(DirectAttack)}.");
        }

        directAttack.SetDamage(damage);
        directAttack.SetAttackFrequency(attackFrequency);
    }
}
