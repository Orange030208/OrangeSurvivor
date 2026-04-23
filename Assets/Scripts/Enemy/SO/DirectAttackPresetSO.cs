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
        throw new System.NotImplementedException();
    }

    public override void ApplyTo(AttackBase attack, EnemyDefinitionSO definition)
    {
        throw new System.NotImplementedException();
    }
}