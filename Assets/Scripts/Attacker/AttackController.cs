using System;
using UnityEngine;

public sealed class AttackController
{
    private readonly Func<bool> canAttackEvaluator;
    private readonly Func<bool> inRangeEvaluator;
    private readonly Func<AttackContext> attackContextBuilder;
    private readonly IAttackExecutor attackExecutor;
    private readonly float attackInterval;

    private float attackTimer;

    public AttackController(
        float attackInterval,
        Func<bool> canAttackEvaluator,
        Func<bool> inRangeEvaluator,
        Func<AttackContext> attackContextBuilder,
        IAttackExecutor attackExecutor)
    {
        if (canAttackEvaluator == null)
        {
            throw new ArgumentNullException(nameof(canAttackEvaluator), $"{nameof(AttackController)} requires {nameof(canAttackEvaluator)}.");
        }

        if (inRangeEvaluator == null)
        {
            throw new ArgumentNullException(nameof(inRangeEvaluator), $"{nameof(AttackController)} requires {nameof(inRangeEvaluator)}.");
        }

        if (attackContextBuilder == null)
        {
            throw new ArgumentNullException(nameof(attackContextBuilder), $"{nameof(AttackController)} requires {nameof(attackContextBuilder)}.");
        }

        this.attackExecutor = attackExecutor ?? throw new ArgumentNullException(nameof(attackExecutor), $"{nameof(AttackController)} requires {nameof(attackExecutor)}.");
        this.attackInterval = Mathf.Max(0.01f, attackInterval);
        this.canAttackEvaluator = canAttackEvaluator;
        this.inRangeEvaluator = inRangeEvaluator;
        this.attackContextBuilder = attackContextBuilder;
        attackTimer = this.attackInterval;
    }

    public bool Tick(float deltaTime)
    {
        if (!GameSimulation.IsRunning)
        {
            return false;
        }

        if (!canAttackEvaluator())
        {
            return false;
        }

        attackTimer += deltaTime;
        if (attackTimer < attackInterval)
        {
            return false;
        }

        if (!inRangeEvaluator())
        {
            return false;
        }

        attackTimer = 0f;
        AttackContext context = attackContextBuilder();
        attackExecutor.Execute(context);
        return true;
    }
}
