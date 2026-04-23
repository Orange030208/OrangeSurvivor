using UnityEngine;

public sealed class OrbitMove : MoveBase
{
    private float orbitRadius = 3f;
    private float radiusTolerance = 0.35f;
    private bool clockwise = true;

    public void SetOrbitRadius(float value)
    {
        orbitRadius = Mathf.Max(0f, value);
    }

    public void SetRadiusTolerance(float value)
    {
        radiusTolerance = Mathf.Max(0f, value);
    }

    public void SetClockwise(bool value)
    {
        clockwise = value;
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
        Vector2 toTarget = targetPosition - currentPosition;
        float distance = toTarget.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return;
        }

        float minRadius = Mathf.Max(0f, orbitRadius - radiusTolerance);
        float maxRadius = orbitRadius + radiusTolerance;
        if (distance < minRadius)
        {
            Vector2 direction = -toTarget.normalized;
            ApplyMoveDirection(direction);
            owner.Transform.position += (Vector3)(direction * CurrentMoveSpeed * deltaTime);
            return;
        }

        if (distance > maxRadius)
        {
            Vector2 direction = toTarget.normalized;
            float moveDistance = Mathf.Min(CurrentMoveSpeed * deltaTime, distance - orbitRadius);
            ApplyMoveDirection(direction);
            owner.Transform.position = currentPosition + direction * moveDistance;
            return;
        }

        Vector2 radialDirection = toTarget.normalized;
        Vector2 tangentDirection = clockwise
            ? new Vector2(radialDirection.y, -radialDirection.x)
            : new Vector2(-radialDirection.y, radialDirection.x);

        ApplyMoveDirection(tangentDirection);
        owner.Transform.position += (Vector3)(tangentDirection * CurrentMoveSpeed * deltaTime);
    }
}
