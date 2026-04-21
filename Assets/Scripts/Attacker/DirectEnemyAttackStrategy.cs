using System;

public sealed class DirectEnemyAttackStrategy : IEnemyAttackStrategy
{
    private readonly AttackDefinitionSO attackDefinition;
    private float attackTimer;

    public DirectEnemyAttackStrategy(AttackDefinitionSO attackDefinition)
    {
        this.attackDefinition = attackDefinition ?? throw new ArgumentNullException(nameof(attackDefinition));
        attackTimer = this.attackDefinition.AttackInterval;
    }

    public bool Tick(EnemyAttackContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (!GameSimulation.IsRunning || context.Attacker == null || context.Target == null)
        {
            return false;
        }

        attackTimer += context.DeltaTime;
        if (attackTimer < attackDefinition.AttackInterval)
        {
            return false;
        }

        if (!context.Attacker.IsTargetInRange(context.Target, context.AttackDetectionRadius))
        {
            return false;
        }

        attackTimer = 0f;
        context.Attacker.ExecuteDirectAttack(context.Target, attackDefinition);
        return true;
    }
}
