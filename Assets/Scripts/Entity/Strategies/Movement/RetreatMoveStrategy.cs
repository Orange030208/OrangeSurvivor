using System;
using UnityEngine;

public sealed class RetreatMoveStrategy : IMoveStrategy
{
    private readonly Enemy owner;
    private readonly IMovable movable;
    private readonly float safeDistance;
    private readonly float retreatStepDistance;

    public RetreatMoveStrategy(
        Enemy owner,
        IMovable movable,
        RetreatMoveData data)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.movable = movable ?? throw new ArgumentNullException(nameof(movable));
        safeDistance = Mathf.Max(0f, data.safeDistance);
        retreatStepDistance = Mathf.Max(0f, data.retreatStepDistance);
    }

    public void ExecuteMove(Entity target)
    {
        if (target == null)
        {
            movable.StopMoving();
            return;
        }

        float currentDistance = Vector2.Distance(owner.Center, target.Center);
        if (currentDistance >= safeDistance)
        {
            movable.StopMoving();
            return;
        }

        Vector2 retreatDirection = (owner.Center - target.Center).normalized;
        movable.MoveTo(owner.Center + retreatDirection * retreatStepDistance);
    }
}
