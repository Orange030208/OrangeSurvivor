using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Direct Attack Config", menuName = "SO/Enemies/Attack/Direct Attack Config", order = 20)]
public sealed class DirectAttackConfigSO : AttackConfigSO
{
    [SerializeField] [Min(0f)] private float damage = 1f;
    [SerializeField] [Min(0.01f)] private float attackFrequency = 1f;

    public float Damage => Mathf.Max(0f, damage);
    public float AttackFrequency => Mathf.Max(0.01f, attackFrequency);

    public override Type GetComponentType()
    {
        return typeof(DirectAttack);
    }

    public override void ApplyTo(AttackBase attack)
    {
        if (attack is not DirectAttack directAttack)
        {
            throw new InvalidOperationException($"{nameof(DirectAttackConfigSO)} requires {nameof(DirectAttack)}.");
        }

        directAttack.ApplyConfig(this);
    }
}
