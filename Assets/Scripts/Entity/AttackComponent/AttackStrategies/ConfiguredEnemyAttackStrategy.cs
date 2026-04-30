using UnityEngine;

[CreateAssetMenu(fileName = "Configured Enemy Attack Strategy", menuName = ScriptableObjectMenuPaths.CONFIGURED_ENEMY_ATTACK_STRATEGY)]
public class ConfiguredEnemyAttackStrategy : AttackStrategyBase
{
    [SerializeField] private EnemyAttackDefinitionSO attackDefinition;

    public EnemyAttackDefinitionSO AttackDefinition => attackDefinition;

    public override bool CanExecute(IEntityAttackExecutor attackExecutor, Entity self, Entity target)
    {
        return attackExecutor != null &&
               attackDefinition != null &&
               attackExecutor.CanUse(attackDefinition) &&
               attackExecutor.IsInAttackRange(attackDefinition, target);
    }

    public override bool IsInAttackRange(IEntityAttackExecutor attackExecutor, Entity self, Entity target)
    {
        return attackExecutor != null &&
               attackDefinition != null &&
               attackExecutor.IsInAttackRange(attackDefinition, target);
    }

    public override void ExecuteAttack(IEntityAttackExecutor attackExecutor, Entity self, Entity target)
    {
        if (attackExecutor == null || attackDefinition == null)
        {
            return;
        }

        attackExecutor.TryUse(attackDefinition, target);
    }
}
