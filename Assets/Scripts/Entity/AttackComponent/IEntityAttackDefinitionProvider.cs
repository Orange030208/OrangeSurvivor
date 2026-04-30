using System.Collections.Generic;

public interface IEntityAttackDefinitionProvider
{
    IReadOnlyList<EnemyAttackDefinitionSO> AttackDefinitions { get; }
}
