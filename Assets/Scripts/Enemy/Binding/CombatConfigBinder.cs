using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CombatConfigBinder : ConfiguredComponentBinder
{
    public void Apply(EnemyDefinitionSO definition, Enemy enemy)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (enemy == null)
        {
            throw new ArgumentNullException(nameof(enemy));
        }

        MoveBase activeMove = ResolveConfiguredComponent<MoveBase, MoveConfigSO>(
            definition.MoveConfig,
            move => move.enabled = false,
            config => config.GetComponentType(),
            (config, move) => config.ApplyTo(move, definition.MoveSpeed, definition.AttackDetectionRadius));

        AttackBase activeAttack = ResolveConfiguredComponent<AttackBase, AttackConfigSO>(
            definition.AttackConfig,
            attack => attack.enabled = false,
            config => config.GetComponentType(),
            (config, attack) => config.ApplyTo(attack));

        enemy.SetCombatLoadout(activeMove, activeAttack);
    }
}
