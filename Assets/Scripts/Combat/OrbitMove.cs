using UnityEngine;

public sealed class OrbitMove : MoveBase
{
    private float orbitRadius = 3f;
    private float radiusTolerance = 0.35f;
    private bool clockwise = true;

    public void ApplyConfig(OrbitMoveConfigSO config, float fallbackOrbitRadius)
    {
        orbitRadius = config != null ? config.OrbitRadius : Mathf.Max(0f, fallbackOrbitRadius);
        radiusTolerance = config != null ? config.RadiusTolerance : 0.35f;
        clockwise = config != null && config.Clockwise;
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
