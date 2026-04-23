using UnityEngine;

public sealed class ChaseMove : MoveBase
{
    private const float MIN_STOP_DISTANCE = 0f;

    private float stopDistance;

    public void SetStopDistance(float value)
    {
        stopDistance = Mathf.Max(MIN_STOP_DISTANCE, value);
    }

    public override void Tick(Entity owner, Entity target, float deltaTime, float desiredDistance)
    {
        ClearMoveDirection();

        if (!CanMove || owner == null || target == null)
        {
            return;
        }

        Vector2 currentPosition = owner.Transform.position;
        Vector2 targetPosition = target.Transform.position;
        Vector2 offset = targetPosition - currentPosition;
        float distance = offset.magnitude;
        float runtimeStopDistance = Mathf.Max(MIN_STOP_DISTANCE, stopDistance);

        if (distance <= runtimeStopDistance || distance <= Mathf.Epsilon)
        {
            return;
        }

        Vector2 direction = offset / distance;
        float moveDistance = Mathf.Min(CurrentMoveSpeed * deltaTime, distance - runtimeStopDistance);
        ApplyMoveDirection(direction);
        owner.Transform.position = currentPosition + direction * moveDistance;
    }
}
