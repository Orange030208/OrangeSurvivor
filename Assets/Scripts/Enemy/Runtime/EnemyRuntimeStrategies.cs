using System;
using UnityEngine;

public interface IEnemyRuntimeMovementStrategy
{
    void ExecuteMove(Entity target);
}

public interface IEnemyRuntimeDetectionStrategy
{
    float ResolveRange();
    bool IsTargetInRange(Entity target);
}

public interface IEnemyRuntimeAttackStrategy
{
    string ActionId { get; }
    IEnemyRuntimeDetectionStrategy DetectionStrategy { get; }
    bool CanUse(Entity target);
    bool TryExecute(Entity target);
    void ResetCooldown();
}

public static class EnemyRuntimeStrategyFactory
{
    public static IEnemyRuntimeMovementStrategy CreateMovementStrategy(
        Enemy owner,
        IMovable movable,
        PropertiesManager propertiesManager,
        in EnemyMovementConfig config)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        if (movable == null)
        {
            throw new ArgumentNullException(nameof(movable));
        }

        if (propertiesManager == null)
        {
            throw new ArgumentNullException(nameof(propertiesManager));
        }

        return config.pattern switch
        {
            EnemyMovementPattern.None => new NoopEnemyRuntimeMovementStrategy(movable),
            EnemyMovementPattern.DirectChase => new DirectChaseEnemyRuntimeMovementStrategy(movable),
            EnemyMovementPattern.CircleKite => new CircleKiteEnemyRuntimeMovementStrategy(owner, movable, propertiesManager, config),
            EnemyMovementPattern.Retreat => new RetreatEnemyRuntimeMovementStrategy(owner, movable, config),
            _ => throw new ArgumentOutOfRangeException(nameof(config.pattern), config.pattern, "Unsupported enemy movement pattern."),
        };
    }

    public static IEnemyRuntimeDetectionStrategy CreateDistanceDetectionStrategy(
        Enemy owner,
        PropertiesManager propertiesManager,
        in EnemyAttackConfig config)
    {
        return new DistanceEnemyRuntimeDetectionStrategy(owner, propertiesManager, config);
    }

    public static IEnemyRuntimeDetectionStrategy CreateForwardCircleDetectionStrategy(
        Enemy owner,
        PropertiesManager propertiesManager,
        in EnemyAttackConfig config)
    {
        return new ForwardCircleEnemyRuntimeDetectionStrategy(owner, propertiesManager, config);
    }

    public static IEnemyRuntimeAttackStrategy CreateDirectDamageAttackStrategy(
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        in EnemyAttackConfig config,
        IEnemyRuntimeDetectionStrategy detectionStrategy)
    {
        return new DirectDamageEnemyRuntimeAttackStrategy(
            owner,
            attackController,
            propertiesManager,
            config,
            detectionStrategy);
    }

    public static IEnemyRuntimeAttackStrategy CreateProjectileAttackStrategy(
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        in EnemyAttackConfig config,
        IEnemyRuntimeDetectionStrategy detectionStrategy)
    {
        return new ProjectileEnemyRuntimeAttackStrategy(
            owner,
            attackController,
            propertiesManager,
            config,
            detectionStrategy);
    }
}

public abstract class EnemyRuntimeDetectionStrategyBase : IEnemyRuntimeDetectionStrategy
{
    protected readonly Enemy owner;
    protected readonly PropertiesManager propertiesManager;
    private readonly AttackRangeSource rangeSource;
    private readonly float fixedRange;
    private readonly float rangeMultiplier;

    protected EnemyRuntimeDetectionStrategyBase(
        Enemy owner,
        PropertiesManager propertiesManager,
        in EnemyAttackConfig config)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.propertiesManager = propertiesManager ?? throw new ArgumentNullException(nameof(propertiesManager));
        rangeSource = config.rangeSource;
        fixedRange = Mathf.Max(0f, config.fixedRange);
        rangeMultiplier = Mathf.Max(0f, config.rangeMultiplier);
    }

    public float ResolveRange()
    {
        float range = rangeSource switch
        {
            AttackRangeSource.FixedValue => fixedRange,
            AttackRangeSource.AttackRangeProp => propertiesManager.GetPropValue(PropType.AttackRange),
            AttackRangeSource.DetectionRangeProp => propertiesManager.GetPropValue(PropType.DetectionRange),
            AttackRangeSource.OwnerCollider => ResolveOwnerColliderRange(),
            _ => fixedRange
        };

        return Mathf.Max(0f, range * rangeMultiplier);
    }

    public abstract bool IsTargetInRange(Entity target);

    protected Vector2 ResolveDirectionTo(Entity target)
    {
        if (target != null)
        {
            Vector2 direction = target.Center - owner.Center;
            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                return direction.normalized;
            }
        }

        return owner.transform.localScale.x < 0f ? Vector2.left : Vector2.right;
    }

    private float ResolveOwnerColliderRange()
    {
        Collider2D ownerCollider = owner.EntityCollider;
        if (ownerCollider == null)
        {
            return 0f;
        }

        Bounds bounds = ownerCollider.bounds;
        return Mathf.Max(bounds.extents.x, bounds.extents.y);
    }
}

public sealed class DistanceEnemyRuntimeDetectionStrategy : EnemyRuntimeDetectionStrategyBase
{
    public DistanceEnemyRuntimeDetectionStrategy(
        Enemy owner,
        PropertiesManager propertiesManager,
        in EnemyAttackConfig config)
        : base(owner, propertiesManager, config)
    {
    }

    public override bool IsTargetInRange(Entity target)
    {
        if (target == null)
        {
            return false;
        }

        float range = ResolveRange();
        return target.IsColliderWithinRange(owner.Center, range);
    }
}

public sealed class ForwardCircleEnemyRuntimeDetectionStrategy : EnemyRuntimeDetectionStrategyBase
{
    private readonly float forwardOffset;

    public ForwardCircleEnemyRuntimeDetectionStrategy(
        Enemy owner,
        PropertiesManager propertiesManager,
        in EnemyAttackConfig config)
        : base(owner, propertiesManager, config)
    {
        forwardOffset = Mathf.Max(0f, config.forwardOffset);
    }

    public override bool IsTargetInRange(Entity target)
    {
        if (target == null)
        {
            return false;
        }

        Vector2 origin = owner.Center + ResolveDirectionTo(target) * forwardOffset;
        return target.IsColliderWithinRange(origin, ResolveRange());
    }
}

public abstract class EnemyRuntimeAttackStrategyBase : IEnemyRuntimeAttackStrategy
{
    protected readonly Enemy owner;
    protected readonly EnemyAttackController attackController;
    protected readonly PropertiesManager propertiesManager;
    protected readonly EnemyAttackConfig config;

    protected EnemyRuntimeAttackStrategyBase(
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        in EnemyAttackConfig config,
        IEnemyRuntimeDetectionStrategy detectionStrategy)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.attackController = attackController ?? throw new ArgumentNullException(nameof(attackController));
        this.propertiesManager = propertiesManager ?? throw new ArgumentNullException(nameof(propertiesManager));
        this.config = config;
        DetectionStrategy = detectionStrategy ?? throw new ArgumentNullException(nameof(detectionStrategy));
        ActionId = string.IsNullOrWhiteSpace(config.actionId)
            ? throw new ArgumentException("Action id cannot be null or whitespace.", nameof(config))
            : config.actionId;
    }

    public string ActionId { get; }
    public IEnemyRuntimeDetectionStrategy DetectionStrategy { get; }

    public bool CanUse(Entity target)
    {
        return target != null &&
               attackController.CanUseRuntimeAction(ActionId) &&
               DetectionStrategy.IsTargetInRange(target);
    }

    public bool TryExecute(Entity target)
    {
        if (!CanUse(target) || !ExecuteCore(target))
        {
            return false;
        }

        AudioSfxBridge.RequestPlay(config.attackSfxKey);
        attackController.CommitRuntimeCooldown(ActionId, Mathf.Max(0f, config.cooldown));
        return true;
    }

    public void ResetCooldown()
    {
        attackController.ResetRuntimeCooldown(ActionId);
    }

    protected float ResolveDamage()
    {
        return Mathf.Max(0f, propertiesManager.GetPropValue(PropType.Attack) * Mathf.Max(0f, config.damageMultiplier));
    }

    protected abstract bool ExecuteCore(Entity target);
}

public sealed class DirectDamageEnemyRuntimeAttackStrategy : EnemyRuntimeAttackStrategyBase
{
    public DirectDamageEnemyRuntimeAttackStrategy(
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        in EnemyAttackConfig config,
        IEnemyRuntimeDetectionStrategy detectionStrategy)
        : base(owner, attackController, propertiesManager, config, detectionStrategy)
    {
    }

    protected override bool ExecuteCore(Entity target)
    {
        Vector2 knockbackDirection = target.Center - owner.Center;
        Vector2 hitPoint = target.GetClosestPointTo(owner.Center);
        HitService.Apply(new HitRequest(
            owner,
            target,
            HitSpec.EnemyHitSpec(ResolveDamage()),
            hitPoint,
            knockbackDirection,
            HitSourceKind.Direct,
            ActionId,
            owner.Center));
        return true;
    }
}

public sealed class ProjectileEnemyRuntimeAttackStrategy : EnemyRuntimeAttackStrategyBase
{
    public ProjectileEnemyRuntimeAttackStrategy(
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        in EnemyAttackConfig config,
        IEnemyRuntimeDetectionStrategy detectionStrategy)
        : base(owner, attackController, propertiesManager, config, detectionStrategy)
    {
    }

    protected override bool ExecuteCore(Entity target)
    {
        if (config.projectileDefinition == null)
        {
            Debug.LogWarning($"{nameof(ProjectileEnemyRuntimeAttackStrategy)} on {owner.name} is missing projectile definition.", owner);
            return false;
        }

        Vector3 firePointPosition = attackController.FirePoint.position;
        Vector2 targetPoint = target.GetClosestPointTo(firePointPosition);
        Vector2 direction = targetPoint - (Vector2)firePointPosition;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = target.Center - owner.Center;
        }

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        Projectile projectile = ProjectileFactory.CreateProjectile(
            config.projectileDefinition,
            firePointPosition,
            Quaternion.identity);

        attackController.LaunchProjectile(projectile, new ProjectileLaunchContext(
            attackController,
            owner,
            firePointPosition,
            direction.normalized,
            HitSpec.EnemyHitSpec(ResolveDamage()),
            attackController.AttackLayer,
            config.projectileDefinition));
        return true;
    }
}

public sealed class NoopEnemyRuntimeMovementStrategy : IEnemyRuntimeMovementStrategy
{
    private readonly IMovable movable;

    public NoopEnemyRuntimeMovementStrategy(IMovable movable)
    {
        this.movable = movable ?? throw new ArgumentNullException(nameof(movable));
    }

    public void ExecuteMove(Entity target)
    {
        movable.StopMoving();
    }
}

public sealed class DirectChaseEnemyRuntimeMovementStrategy : IEnemyRuntimeMovementStrategy
{
    private readonly IMovable movable;

    public DirectChaseEnemyRuntimeMovementStrategy(IMovable movable)
    {
        this.movable = movable ?? throw new ArgumentNullException(nameof(movable));
    }

    public void ExecuteMove(Entity target)
    {
        if (target == null)
        {
            movable.StopMoving();
            return;
        }

        movable.MoveTo(target.Center);
    }
}

public sealed class CircleKiteEnemyRuntimeMovementStrategy : IEnemyRuntimeMovementStrategy
{
    private readonly Enemy owner;
    private readonly IMovable movable;
    private readonly PropertiesManager propertiesManager;
    private readonly float circleSpeedRatio;
    private readonly float idealRangeRatio;

    public CircleKiteEnemyRuntimeMovementStrategy(
        Enemy owner,
        IMovable movable,
        PropertiesManager propertiesManager,
        in EnemyMovementConfig config)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.movable = movable ?? throw new ArgumentNullException(nameof(movable));
        this.propertiesManager = propertiesManager ?? throw new ArgumentNullException(nameof(propertiesManager));
        circleSpeedRatio = Mathf.Max(0f, config.circleSpeedRatio);
        idealRangeRatio = Mathf.Max(0f, config.idealRangeRatio);
    }

    public void ExecuteMove(Entity target)
    {
        if (target == null)
        {
            movable.StopMoving();
            return;
        }

        Vector2 targetDirection = (Vector2)target.Center - (Vector2)owner.Center;
        if (targetDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            movable.StopMoving();
            return;
        }

        targetDirection.Normalize();
        Vector2 circleDirection = new(-targetDirection.y, targetDirection.x);
        float detectionRange = propertiesManager.GetPropValue(PropType.DetectionRange);
        Vector2 targetPosition = (Vector2)target.Center
                                 - targetDirection * idealRangeRatio * detectionRange
                                 + circleDirection * Mathf.Sin(circleSpeedRatio * movable.Speed) * 2f;
        movable.MoveTo(targetPosition);
    }
}

public sealed class RetreatEnemyRuntimeMovementStrategy : IEnemyRuntimeMovementStrategy
{
    private readonly Enemy owner;
    private readonly IMovable movable;
    private readonly float safeDistance;
    private readonly float retreatStepDistance;

    public RetreatEnemyRuntimeMovementStrategy(
        Enemy owner,
        IMovable movable,
        in EnemyMovementConfig config)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.movable = movable ?? throw new ArgumentNullException(nameof(movable));
        safeDistance = Mathf.Max(0f, config.safeDistance);
        retreatStepDistance = Mathf.Max(0f, config.retreatStepDistance);
    }

    public void ExecuteMove(Entity target)
    {
        if (target == null)
        {
            movable.StopMoving();
            return;
        }

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
