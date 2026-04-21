using System;
using UnityEngine;

public sealed class DashApproachEnemyMovementStrategy : IEnemyMovementStrategy
{
    private readonly float dashSpeedMultiplier;
    private readonly float dashDuration;
    private readonly float dashCooldown;
    private readonly float dashTriggerDistance;
    private readonly float stopDistance;

    private float dashTimer;
    private float cooldownTimer;
    private bool isDashing;
    private float defaultMoveSpeed = -1f;

    public DashApproachEnemyMovementStrategy(float dashSpeedMultiplier, float dashDuration, float dashCooldown, float dashTriggerDistance, float stopDistance)
    {
        this.dashSpeedMultiplier = Mathf.Max(0f, dashSpeedMultiplier);
        this.dashDuration = Mathf.Max(0.01f, dashDuration);
        this.dashCooldown = Mathf.Max(0.01f, dashCooldown);
        this.dashTriggerDistance = Mathf.Max(0f, dashTriggerDistance);
        this.stopDistance = Mathf.Max(0f, stopDistance);
    }

    public void Tick(EnemyMovementContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Movement == null || context.Target == null)
        {
            return;
        }

        if (defaultMoveSpeed < 0f)
        {
            defaultMoveSpeed = context.Movement.MoveSpeed;
        }

        float distance = Vector2.Distance(context.Owner.Transform.position, context.Target.Transform.position);
        cooldownTimer = Mathf.Max(0f, cooldownTimer - context.DeltaTime);

        if (isDashing)
        {
            dashTimer -= context.DeltaTime;
            context.Movement.MoveTowardsPosition(context.Target.Transform.position, context.DeltaTime, stopDistance);
            if (dashTimer <= 0f)
            {
                context.Movement.SetMoveSpeed(defaultMoveSpeed);
                isDashing = false;
                cooldownTimer = dashCooldown;
            }

            return;
        }

        if (cooldownTimer <= 0f && distance <= dashTriggerDistance && distance > stopDistance)
        {
            context.Movement.SetMoveSpeed(defaultMoveSpeed * dashSpeedMultiplier);
            dashTimer = dashDuration;
            isDashing = true;
            context.Movement.MoveTowardsPosition(context.Target.Transform.position, context.DeltaTime, stopDistance);
            return;
        }

        context.Movement.SetMoveSpeed(defaultMoveSpeed);
        context.Movement.MoveTowardsPosition(context.Target.Transform.position, context.DeltaTime, stopDistance);
    }
}
