using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(Rigidbody2D))]
public class KnockbackComponent : EntityComponentBase
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    [Header("Config")]
    [SerializeField] private KnockbackReceiverConfigSO receiverConfig;

    private Entity owner;
    private HealthComponent healthComponent;
    private Rigidbody2D rb;
    private IMovable movable;
    private Vector2 knockbackDirection;
    private float knockbackVelocity;
    private float knockbackTimer;
    private bool isKnockbackActive;
    private bool movementLockedByKnockback;

    public override Entity Owner => owner;
    public override int Priority => PriorityPreset.Latest;

    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        healthComponent = owner.GetComponent<HealthComponent>();
        rb = owner.GetComponent<Rigidbody2D>();
        movable = owner.MoveComponent;
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

        EndKnockback(false);
    }

    public override void OnFixedTick(float deltaTime)
    {
        if (!isKnockbackActive || receiverConfig == null || rb == null)
        {
            return;
        }

        knockbackTimer -= deltaTime;
        if (knockbackTimer <= 0f)
        {
            EndKnockback(true);
            return;
        }

        float normalizedTime = 1f - Mathf.Clamp01(knockbackTimer / receiverConfig.Duration);
        float curveMultiplier = receiverConfig.VelocityCurve != null
            ? Mathf.Max(0f, receiverConfig.VelocityCurve.Evaluate(normalizedTime))
            : 1f;
        rb.velocity = knockbackDirection * (knockbackVelocity * curveMultiplier);
    }

    private void OnDamaged(HitResult result)
    {
        if (result.Target != owner ||
            result.IsCancelled ||
            result.IsDodged ||
            result.IsBlocked ||
            result.FinalDamage <= 0f ||
            result.KnockbackForce <= 0f ||
            receiverConfig == null)
        {
            return;
        }

        Vector2 direction = ResolveKnockbackDirection(result);
        if (direction.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
        {
            return;
        }

        StartKnockback(direction.normalized, result.KnockbackForce);
    }

    private void StartKnockback(Vector2 direction, float force)
    {
        knockbackDirection = direction;
        knockbackVelocity = Mathf.Min(force * receiverConfig.ForceMultiplier, receiverConfig.MaxVelocity);
        knockbackTimer = receiverConfig.Duration;
        isKnockbackActive = true;

        if (receiverConfig.DisableMovementWhileKnockback && movable != null)
        {
            LockMovement();
        }
    }

    private void EndKnockback(bool stopRigidbody)
    {
        if (!isKnockbackActive && !movementLockedByKnockback)
        {
            return;
        }

        isKnockbackActive = false;
        knockbackTimer = 0f;
        knockbackVelocity = 0f;
        knockbackDirection = Vector2.zero;

        if (stopRigidbody && rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        UnlockMovement();
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

    private void LockMovement()
    {
        if (movementLockedByKnockback)
        {
            return;
        }

        if (movable is IMovementLockable lockable)
        {
            lockable.AddMovementLock(this);
        }
        else
        {
            movable.DisableMovement();
        }

        movementLockedByKnockback = true;
    }

    private void UnlockMovement()
    {
        if (!movementLockedByKnockback || movable == null)
        {
            return;
        }

        if (movable is IMovementLockable lockable)
        {
            lockable.RemoveMovementLock(this);
        }
        else
        {
            movable.EnableMovement();
        }

        movementLockedByKnockback = false;
    }

    private void ResetRuntimeState()
    {
        knockbackDirection = Vector2.zero;
        knockbackVelocity = 0f;
        knockbackTimer = 0f;
        isKnockbackActive = false;
        movementLockedByKnockback = false;
    }
}
