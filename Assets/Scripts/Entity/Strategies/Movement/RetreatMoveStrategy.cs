using System;
using UnityEngine;

public sealed class RetreatMoveStrategy : IMoveStrategy
{
    private readonly Enemy owner;
    private readonly IMovable movable;
    private readonly PropertiesManager propertiesManager;
    private readonly float safeDistanceRatio;
    private readonly float retreatStepDistanceRatio;

    public RetreatMoveStrategy(
        Enemy owner,
        IMovable movable,
        PropertiesManager propertiesManager,
        RetreatMoveData data)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.movable = movable ?? throw new ArgumentNullException(nameof(movable));
        this.propertiesManager = propertiesManager ?? throw new ArgumentNullException(nameof(propertiesManager));
        safeDistanceRatio = Mathf.Max(0f, data.safeDistanceRatio);
        retreatStepDistanceRatio = Mathf.Max(0f, data.retreatStepDistanceRatio);
    }

    public void ExecuteMove(Entity target)
    {
        if (target == null)
        {
            movable.StopMoving();
            return;
        }

        float detectionRange = PropValueUtility.DistancePointsToWorldUnits(propertiesManager.GetPropValue(PropType.DetectionRange));
        float safeDistance = detectionRange * safeDistanceRatio;
        float retreatStepDistance = detectionRange * retreatStepDistanceRatio;
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
