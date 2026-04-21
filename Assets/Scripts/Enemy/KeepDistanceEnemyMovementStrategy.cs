using System;
using UnityEngine;

public sealed class KeepDistanceEnemyMovementStrategy : IEnemyMovementStrategy
{
    private readonly float desiredDistance;
    private readonly float tolerance;

    public KeepDistanceEnemyMovementStrategy(float desiredDistance, float tolerance)
    {
        this.desiredDistance = Mathf.Max(0f, desiredDistance);
        this.tolerance = Mathf.Max(0f, tolerance);
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
        Vector2 offset = targetPosition - currentPosition;
        float distance = offset.magnitude;
        float minDistance = Mathf.Max(0f, desiredDistance - tolerance);
        float maxDistance = desiredDistance + tolerance;

        if (distance < minDistance && distance > Mathf.Epsilon)
        {
            context.Movement.MoveInDirection(-offset.normalized, context.DeltaTime);
            return;
        }

        if (distance > maxDistance)
        {
            context.Movement.MoveTowardsPosition(targetPosition, context.DeltaTime, desiredDistance);
            return;
        }

        context.Movement.Stop();
    }
}
