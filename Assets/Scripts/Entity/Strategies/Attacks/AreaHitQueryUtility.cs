using UnityEngine;

public static class AreaHitQueryUtility
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
    private const float CENTER_OVERLAP_SQR_EPSILON = 0.000001f;

    public static int OverlapCircleNonAlloc(
        Vector2 center,
        float radius,
        Collider2D[] results,
        LayerMask layerMask)
    {
        if (results == null || radius <= 0f)
        {
            return 0;
        }

        return Physics2D.OverlapCircleNonAlloc(center, radius, results, layerMask);
    }

    public static int OverlapFacingSemicircleNonAlloc(
        Vector2 center,
        float radius,
        Vector2 facingDirection,
        Collider2D[] results,
        LayerMask layerMask)
    {
        int hitCount = OverlapCircleNonAlloc(center, radius, results, layerMask);
        if (hitCount == 0)
        {
            return 0;
        }

        if (facingDirection.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
        {
            return hitCount;
        }

        Vector2 normalizedDirection = facingDirection.normalized;
        int acceptedCount = 0;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = results[i];
            if (!IsColliderInFacingSemicircle(hitCollider, center, normalizedDirection))
            {
                continue;
            }

            results[acceptedCount] = hitCollider;
            acceptedCount++;
        }

        for (int i = acceptedCount; i < hitCount; i++)
        {
            results[i] = null;
        }

        return acceptedCount;
    }

    public static bool IsColliderInFacingSemicircle(
        Collider2D targetCollider,
        Vector2 center,
        Vector2 normalizedFacingDirection)
    {
        if (targetCollider == null)
        {
            return false;
        }

        if (normalizedFacingDirection.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
        {
            return true;
        }

        Vector2 closestPoint = targetCollider.ClosestPoint(center);
        Vector2 centerToTarget = closestPoint - center;
        if (centerToTarget.sqrMagnitude <= CENTER_OVERLAP_SQR_EPSILON)
        {
            return true;
        }

        return Vector2.Dot(normalizedFacingDirection, centerToTarget) >= 0f;
    }

    public static int OverlapForwardBoxNonAlloc(
        Vector2 nearCenter,
        float length,
        float width,
        Vector2 facingDirection,
        Collider2D[] results,
        LayerMask layerMask)
    {
        if (results == null || length <= 0f || width <= 0f)
        {
            return 0;
        }

        Vector2 direction = ResolveHorizontalDirection(facingDirection);
        Vector2 boxCenter = nearCenter + direction * (length * 0.5f);
        Vector2 size = new(length, width);
        return Physics2D.OverlapBoxNonAlloc(boxCenter, size, 0f, results, layerMask);
    }

    private static Vector2 ResolveHorizontalDirection(Vector2 facingDirection)
    {
        if (facingDirection.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
        {
            return Vector2.right;
        }

        return facingDirection.x < 0f ? Vector2.left : Vector2.right;
    }
}
