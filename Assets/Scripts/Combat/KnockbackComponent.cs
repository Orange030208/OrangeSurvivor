using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(Rigidbody2D))]
public class KnockbackComponent : EntityComponentBase
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
    private const float MIN_DISTANCE = 0.001f;
    private const float KNOCKBACK_DISTANCE_PER_STRENGTH = 0.1f;
    private const float COLLISION_SKIN_WIDTH = 0.01f;
    private const int CURVE_SAMPLE_COUNT = 16;
    private const int CAST_HIT_CAPACITY = 8;
    private const string WALL_LAYER_NAME = "Wall";

    [Header("Config")]
    [SerializeField] private KnockbackReceiverConfigSO receiverConfig;

    private Entity owner;
    private HealthComponent healthComponent;
    private Rigidbody2D rb;
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[CAST_HIT_CAPACITY];
    private ContactFilter2D collisionFilter;
    private Vector2 knockbackDirection;
    private float knockbackVelocity;
    private float knockbackElapsedTime;
    private float remainingDistance;
    private bool isKnockbackActive;
    private bool hasLoggedMissingWallLayer;

    public override Entity Owner => owner;
    public override int Priority => PriorityPreset.Latest;

    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        healthComponent = owner.GetComponent<HealthComponent>();
        rb = owner.GetComponent<Rigidbody2D>();
        RefreshCollisionFilter();
        ResetRuntimeState();
    }

    public override void OnEnableComponent()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDamaged += OnDamaged;
        }
    }

    public override void OnDisableComponent()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDamaged -= OnDamaged;
        }

        EndKnockback();
    }

    public override void OnFixedTick(float deltaTime)
    {
        if (!isKnockbackActive || receiverConfig == null || rb == null)
        {
            return;
        }

        float stepDistance = CalculateStepDistance(deltaTime);
        if (stepDistance > 0f)
        {
            float resolvedDistance = ResolveSafeStepDistance(stepDistance);
            if (resolvedDistance <= MIN_DISTANCE)
            {
                EndKnockback();
                return;
            }

            Vector2 targetPosition = rb.position + knockbackDirection * resolvedDistance;
            rb.MovePosition(targetPosition);
            remainingDistance = Mathf.Max(0f, remainingDistance - resolvedDistance);
            if (resolvedDistance < stepDistance)
            {
                EndKnockback();
                return;
            }
        }

        knockbackElapsedTime += deltaTime;
        if (remainingDistance <= MIN_DISTANCE || knockbackElapsedTime >= receiverConfig.Duration)
        {
            EndKnockback();
        }
    }

    private void OnDamaged(HitResult result)
    {
        if (result.Target != owner ||
            result.IsCancelled ||
            result.IsDodged ||
            result.IsBlocked ||
            result.FinalDamage <= 0f ||
            result.KnockbackStrength <= 0f ||
            receiverConfig == null)
        {
            return;
        }

        Vector2 direction = ResolveKnockbackDirection(result);
        if (direction.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
        {
            return;
        }

        StartKnockback(direction.normalized, result.KnockbackStrength);
    }

    private void StartKnockback(Vector2 direction, float strength)
    {
        float newKnockbackDistance = CalculateKnockbackDistance(strength);
        if (newKnockbackDistance <= MIN_DISTANCE)
        {
            return;
        }

        float newKnockbackVelocity = CalculateKnockbackVelocity(newKnockbackDistance);

        // Stronger hits replace the current displacement budget; weaker hits wait for the old knockback to finish.
        if (isKnockbackActive && newKnockbackDistance <= remainingDistance)
        {
            return;
        }

        knockbackDirection = direction;
        knockbackVelocity = newKnockbackVelocity;
        knockbackElapsedTime = 0f;
        remainingDistance = newKnockbackDistance;
        isKnockbackActive = true;
    }

    private void EndKnockback()
    {
        if (!isKnockbackActive)
        {
            return;
        }

        isKnockbackActive = false;
        knockbackElapsedTime = 0f;
        knockbackVelocity = 0f;
        remainingDistance = 0f;
        knockbackDirection = Vector2.zero;
    }

    private Vector2 ResolveKnockbackDirection(HitResult result)
    {
        if (result.HasKnockbackDirection)
        {
            return result.KnockbackDirection;
        }

        if (result.Source != null)
        {
            Vector2 sourceDirection = owner.Center - result.Source.Center;
            if (sourceDirection.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
            {
                return sourceDirection;
            }
        }

        Vector2 hitPointDirection = owner.Center - result.HitPoint;
        if (hitPointDirection.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
        {
            return hitPointDirection;
        }

        return rb != null && rb.velocity.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE
            ? -rb.velocity
            : Vector2.zero;
    }

    private float CalculateStepDistance(float deltaTime)
    {
        float normalizedTime = Mathf.Clamp01(knockbackElapsedTime / receiverConfig.Duration);
        float curveMultiplier = EvaluateVelocityCurve(normalizedTime);
        float stepDistance = knockbackVelocity * curveMultiplier * deltaTime;
        return Mathf.Min(stepDistance, remainingDistance);
    }

    private float ResolveSafeStepDistance(float stepDistance)
    {
        if (stepDistance <= MIN_DISTANCE)
        {
            return 0f;
        }

        float castDistance = stepDistance + COLLISION_SKIN_WIDTH;
        int hitCount = rb.Cast(knockbackDirection, collisionFilter, castHits, castDistance);
        if (hitCount <= 0)
        {
            return stepDistance;
        }

        float nearestDistance = castDistance;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = castHits[i];
            if (hit.collider == null || hit.rigidbody == rb)
            {
                continue;
            }

            nearestDistance = Mathf.Min(nearestDistance, hit.distance);
        }

        if (nearestDistance >= castDistance)
        {
            return stepDistance;
        }

        return Mathf.Max(0f, nearestDistance - COLLISION_SKIN_WIDTH);
    }

    private void RefreshCollisionFilter()
    {
        int wallMask = LayerMask.GetMask(WALL_LAYER_NAME);
        if (wallMask == 0 && !hasLoggedMissingWallLayer)
        {
            Debug.LogWarning($"[{nameof(KnockbackComponent)}] Layer '{WALL_LAYER_NAME}' is not configured. Knockback wall collision checks will be skipped.");
            hasLoggedMissingWallLayer = true;
        }

        collisionFilter = new ContactFilter2D();
        collisionFilter.SetLayerMask(wallMask);
        collisionFilter.useTriggers = false;
    }

    private void OnValidate()
    {
        RefreshCollisionFilter();
    }

    private float CalculateKnockbackDistance(float strength)
    {
        return Mathf.Max(0f, strength) * KNOCKBACK_DISTANCE_PER_STRENGTH * receiverConfig.DistanceMultiplier;
    }

    private float CalculateKnockbackVelocity(float distance)
    {
        float movementBudget = receiverConfig.Duration * EstimateVelocityCurveAverage();
        if (movementBudget <= MIN_DISTANCE)
        {
            return receiverConfig.MaxVelocity;
        }

        return Mathf.Min(distance / movementBudget, receiverConfig.MaxVelocity);
    }

    private float EvaluateVelocityCurve(float normalizedTime)
    {
        if (receiverConfig.VelocityCurve == null)
        {
            return 1f;
        }

        return Mathf.Max(0f, receiverConfig.VelocityCurve.Evaluate(normalizedTime));
    }

    private float EstimateVelocityCurveAverage()
    {
        if (receiverConfig.VelocityCurve == null)
        {
            return 1f;
        }

        float area = 0f;
        float previousValue = EvaluateVelocityCurve(0f);
        for (int i = 1; i <= CURVE_SAMPLE_COUNT; i++)
        {
            float time = i / (float)CURVE_SAMPLE_COUNT;
            float value = EvaluateVelocityCurve(time);
            area += (previousValue + value) * 0.5f / CURVE_SAMPLE_COUNT;
            previousValue = value;
        }

        return Mathf.Max(0f, area);
    }

    private void ResetRuntimeState()
    {
        knockbackDirection = Vector2.zero;
        knockbackVelocity = 0f;
        knockbackElapsedTime = 0f;
        remainingDistance = 0f;
        isKnockbackActive = false;
    }
}
