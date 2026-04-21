using System;
using System.Collections;
using UnityEngine;

public sealed class ProjectileEnemyAttackStrategy : IEnemyAttackStrategy
{
    private readonly ProjectileAttackDefinitionSO attackDefinition;
    private float attackTimer;
    private int activeBurstId = -1;

    public ProjectileEnemyAttackStrategy(ProjectileAttackDefinitionSO attackDefinition)
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
        ExecuteAttackByMode(context);
        return true;
    }

    private void ExecuteAttackByMode(EnemyAttackContext context)
    {
        switch (attackDefinition.AttackMode)
        {
            case EnemyProjectileAttackMode.Spread:
                ExecuteSpread(context);
                break;
            case EnemyProjectileAttackMode.Burst:
                ExecuteBurst(context);
                break;
            case EnemyProjectileAttackMode.Nova:
                ExecuteNova(context);
                break;
            default:
                context.Attacker.ExecuteProjectileAttack(context.Target, attackDefinition);
                break;
        }
    }

    private void ExecuteSpread(EnemyAttackContext context)
    {
        int spreadCount = attackDefinition.PatternConfig.SpreadCount;
        if (spreadCount <= 1)
        {
            context.Attacker.ExecuteProjectileAttack(context.Target, attackDefinition);
            return;
        }

        float spreadAngle = attackDefinition.PatternConfig.SpreadAngle;
        float step = spreadCount > 1 ? (spreadAngle * 2f) / (spreadCount - 1) : 0f;
        for (int i = 0; i < spreadCount; i++)
        {
            float angle = -spreadAngle + (step * i);
            context.Attacker.ExecuteProjectileAttack(context.Target, attackDefinition, angle, 0, attackDefinition.AttackMode, attackDefinition.PatternConfig);
        }
    }

    private void ExecuteBurst(EnemyAttackContext context)
    {
        if (activeBurstId >= 0)
        {
            return;
        }

        activeBurstId++;
        context.Attacker.StartCoroutine(BurstRoutine(context, activeBurstId));
    }

    private IEnumerator BurstRoutine(EnemyAttackContext context, int burstId)
    {
        int burstCount = attackDefinition.PatternConfig.BurstCount;
        float burstInterval = attackDefinition.PatternConfig.BurstInterval;

        for (int i = 0; i < burstCount; i++)
        {
            while (!GameSimulation.IsRunning)
            {
                yield return null;
            }

            if (context.Target == null)
            {
                break;
            }

            context.Attacker.ExecuteProjectileAttack(context.Target, attackDefinition, 0f, burstId, attackDefinition.AttackMode, attackDefinition.PatternConfig);
            if (i < burstCount - 1)
            {
                yield return new WaitForSeconds(burstInterval);
            }
        }

        activeBurstId = -1;
    }

    private void ExecuteNova(EnemyAttackContext context)
    {
        int novaCount = attackDefinition.PatternConfig.NovaCount;
        for (int i = 0; i < novaCount; i++)
        {
            float angle = 360f / novaCount * i;
            context.Attacker.ExecuteProjectileAttack(context.Target, attackDefinition, angle, 0, attackDefinition.AttackMode, attackDefinition.PatternConfig, true);
        }
    }
}
