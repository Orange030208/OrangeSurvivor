using System;
using UnityEngine;

public sealed class OrbitTargetEnemyMovementStrategy : IEnemyMovementStrategy
{
    private readonly float orbitRadius;
    private readonly float radiusTolerance;
    private readonly bool clockwise;

    public OrbitTargetEnemyMovementStrategy(float orbitRadius, float radiusTolerance, bool clockwise)
    {
        this.orbitRadius = Mathf.Max(0f, orbitRadius);
        this.radiusTolerance = Mathf.Max(0f, radiusTolerance);
        this.clockwise = clockwise;
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

        Vector2 currentPosition = context.Owner.Transform.position;
        Vector2 targetPosition = context.Target.Transform.position;
        Vector2 toTarget = targetPosition - currentPosition;
        float distance = toTarget.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            context.Movement.Stop();
            return;
        }

        float minRadius = Mathf.Max(0f, orbitRadius - radiusTolerance);
        float maxRadius = orbitRadius + radiusTolerance;
        if (distance < minRadius)
        {
            context.Movement.MoveInDirection(-toTarget.normalized, context.DeltaTime);
            return;
        }

        if (distance > maxRadius)
        {
            context.Movement.MoveTowardsPosition(targetPosition, context.DeltaTime, orbitRadius);
            return;
        }

        Vector2 radialDirection = toTarget.normalized;
        Vector2 tangentDirection = clockwise
            ? new Vector2(radialDirection.y, -radialDirection.x)
            : new Vector2(-radialDirection.y, radialDirection.x);

        context.Movement.MoveInDirection(tangentDirection, context.DeltaTime);
    }
}
