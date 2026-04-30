public interface IEntityAttackExecutor
{
    bool CanUse(EnemyAttackDefinitionSO attackDefinition);
    bool IsInAttackRange(EnemyAttackDefinitionSO attackDefinition, Entity target);
    bool TryUse(EnemyAttackDefinitionSO attackDefinition, Entity target);
    void ResetAttackCooldown(EnemyAttackDefinitionSO attackDefinition);
}
