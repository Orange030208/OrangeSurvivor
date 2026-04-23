using UnityEngine;

public sealed class KeepDistanceMove : MoveBase
{
    private float desiredDistance;
    private float tolerance = 0.5f;

    public void SetDesiredDistance(float value)
    {
        desiredDistance = Mathf.Max(0f, value);
    }

    public void SetTolerance(float value)
    {
        tolerance = Mathf.Max(0f, value);
    }

    public override void Tick(Entity owner, Entity target, float deltaTime, float ignoredDesiredDistance)
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
        float minDistance = Mathf.Max(0f, desiredDistance - tolerance);
        float maxDistance = desiredDistance + tolerance;

        if (distance < minDistance && distance > Mathf.Epsilon)
        {
            Vector2 direction = -offset.normalized;
            ApplyMoveDirection(direction);
            owner.Transform.position += (Vector3)(direction * CurrentMoveSpeed * deltaTime);
            return;
        }

        if (distance > maxDistance)
        {
            Vector2 direction = offset.normalized;
            float moveDistance = Mathf.Min(CurrentMoveSpeed * deltaTime, distance - desiredDistance);
            ApplyMoveDirection(direction);
            owner.Transform.position = currentPosition + direction * moveDistance;
        }
    }
}
