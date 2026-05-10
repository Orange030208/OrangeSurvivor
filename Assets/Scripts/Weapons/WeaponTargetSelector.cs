using UnityEngine;

public sealed class WeaponTargetSelector
{
    private const float STABLE_LOCK_RANGE_MULTIPLIER = 1.15f;
    private const float STABLE_LOCK_RANGE_PADDING = 0.15f;
    private const float STABLE_LOCK_SWITCH_SCORE_RATIO = 0.8f;
    private const float CURRENT_TARGET_SCORE_MULTIPLIER = 0.75f;
    private const float ANGLE_SCORE_WEIGHT = 0.35f;
    private const float MIN_AIM_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    public Entity SelectTarget(
        Entity currentTarget,
        Vector2 searchOrigin,
        Vector2 aimDirection,
        float searchRange,
        LayerMask targetLayerMask,
        WeaponTargetingMode targetingMode)
    {
        float clampedRange = Mathf.Max(0f, searchRange);
        return targetingMode == WeaponTargetingMode.StableLock
            ? SelectStableTarget(currentTarget, searchOrigin, aimDirection, clampedRange, targetLayerMask)
            : SelectDynamicClosestTarget(searchOrigin, clampedRange, targetLayerMask);
    }

    private Entity SelectDynamicClosestTarget(Vector2 searchOrigin, float searchRange, LayerMask targetLayerMask)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(searchOrigin, searchRange, targetLayerMask);
        Entity closestTarget = null;
        float bestDistance = searchRange;
        int bestInstanceId = int.MaxValue;

        for (int i = 0; i < colliders.Length; i++)
        {
            Entity candidate = ResolveValidTarget(colliders[i]);
            if (candidate == null)
            {
                continue;
            }

            float distance = candidate.DistanceToCollider(searchOrigin);
            int candidateId = candidate.GetInstanceID();
            if (distance < bestDistance || (Mathf.Approximately(distance, bestDistance) && candidateId < bestInstanceId))
            {
                closestTarget = candidate;
                bestDistance = distance;
                bestInstanceId = candidateId;
            }
        }

        return closestTarget;
    }

    private Entity SelectStableTarget(
        Entity currentTarget,
        Vector2 searchOrigin,
        Vector2 aimDirection,
        float searchRange,
        LayerMask targetLayerMask)
    {
        bool canKeepCurrentTarget = CanKeepCurrentTarget(currentTarget, searchOrigin, searchRange);
        float keepThresholdScore = canKeepCurrentTarget
            ? CalculateScore(currentTarget, searchOrigin, aimDirection, true)
            : float.PositiveInfinity;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(searchOrigin, searchRange, targetLayerMask);
        Entity bestTarget = null;
        float bestScore = float.PositiveInfinity;
        int bestInstanceId = int.MaxValue;

        for (int i = 0; i < colliders.Length; i++)
        {
            Entity candidate = ResolveValidTarget(colliders[i]);
            if (candidate == null)
            {
                continue;
            }

            float score = CalculateScore(candidate, searchOrigin, aimDirection, candidate == currentTarget);
            int candidateId = candidate.GetInstanceID();
            if (score < bestScore || (Mathf.Approximately(score, bestScore) && candidateId < bestInstanceId))
            {
                bestTarget = candidate;
                bestScore = score;
                bestInstanceId = candidateId;
            }
        }

        if (bestTarget == null)
        {
            return canKeepCurrentTarget ? currentTarget : null;
        }

        if (!canKeepCurrentTarget || bestTarget == currentTarget)
        {
            return bestTarget;
        }

        return bestScore <= keepThresholdScore * STABLE_LOCK_SWITCH_SCORE_RATIO
            ? bestTarget
            : currentTarget;
    }

    private static bool CanKeepCurrentTarget(Entity target, Vector2 searchOrigin, float searchRange)
    {
        if (!IsValidTarget(target))
        {
            return false;
        }

        float keepRange = searchRange * STABLE_LOCK_RANGE_MULTIPLIER + STABLE_LOCK_RANGE_PADDING;
        return target.DistanceToCollider(searchOrigin) <= keepRange;
    }

    private static float CalculateScore(Entity target, Vector2 searchOrigin, Vector2 aimDirection, bool isCurrentTarget)
    {
        float distance = target.DistanceToCollider(searchOrigin);
        float anglePenalty = 0f;
        if (aimDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
        {
            Vector2 targetPoint = target.GetClosestPointTo(searchOrigin);
            Vector2 targetDirection = targetPoint - searchOrigin;
            if (targetDirection.sqrMagnitude > MIN_AIM_DIRECTION_SQR_MAGNITUDE)
            {
                anglePenalty = Vector2.Angle(aimDirection.normalized, targetDirection.normalized) / 180f;
            }
        }

        float score = distance + anglePenalty * ANGLE_SCORE_WEIGHT;
        return isCurrentTarget ? score * CURRENT_TARGET_SCORE_MULTIPLIER : score;
    }

    private static Entity ResolveValidTarget(Collider2D collider)
    {
        if (collider == null || !collider.TryGetComponent(out Entity entity))
        {
            return null;
        }

        return IsValidTarget(entity) ? entity : null;
    }

    private static bool IsValidTarget(Entity entity)
    {
        if (entity == null || !entity.IsRuntimeEnabled)
        {
            return false;
        }

        return entity.TryGetComponent(out HealthComponent healthComponent) &&
               healthComponent.CurrentHealth > 0f;
    }
}
