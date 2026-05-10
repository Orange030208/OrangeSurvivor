using System;
using UnityEngine;

public sealed class CircleKiteMoveStrategy : IMoveStrategy
{
    private readonly Enemy owner;
    private readonly IMovable movable;
    private readonly PropertiesManager propertiesManager;
    private readonly float circleSpeedRatio;
    private readonly float idealRangeRatio;

    public CircleKiteMoveStrategy(
        Enemy owner,
        IMovable movable,
        PropertiesManager propertiesManager,
        CircleKiteMoveData data)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.movable = movable ?? throw new ArgumentNullException(nameof(movable));
        this.propertiesManager = propertiesManager ?? throw new ArgumentNullException(nameof(propertiesManager));
        circleSpeedRatio = Mathf.Max(0f, data.circleSpeedRatio);
        idealRangeRatio = Mathf.Max(0f, data.idealRangeRatio);
    }

    public void ExecuteMove(Entity target)
    {
        if (target == null)
        {
            movable.StopMoving();
            return;
        }

        Vector2 targetDirection = (Vector2)target.Center - (Vector2)owner.Center;
        if (targetDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            movable.StopMoving();
            return;
        }

        targetDirection.Normalize();
        Vector2 circleDirection = new(-targetDirection.y, targetDirection.x);
        float detectionRange = PropValueUtility.DistancePointsToWorldUnits(propertiesManager.GetPropValue(PropType.DetectionRange));
        Vector2 targetPosition = (Vector2)target.Center
                                 - targetDirection * idealRangeRatio * detectionRange
                                 + circleDirection * Mathf.Sin(circleSpeedRatio * movable.Speed) * 2f;
        movable.MoveTo(targetPosition);
    }
}
