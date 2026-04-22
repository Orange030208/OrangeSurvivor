using UnityEngine;

public sealed class KeepDistanceMove : MoveBase
{
    private float desiredDistance;
    private float tolerance = 0.5f;

    public void ApplyConfig(KeepDistanceMoveConfigSO config, float fallbackDistance)
    {
        desiredDistance = config != null ? config.DesiredDistance : Mathf.Max(0f, fallbackDistance);
        tolerance = config != null ? config.Tolerance : 0.5f;
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
