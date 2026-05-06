using System;
using UnityEngine;

public interface IMoveStrategy
{
    void ExecuteMove(Entity target);
}

public interface IRangeDetectionStrategy
{
    float ResolveRange();
    bool IsTargetInRange(Entity target);
}

public interface IAttackStrategy
{
    string ActionId { get; }
    IRangeDetectionStrategy DetectionStrategy { get; }
    bool CanUse(Entity target);
    bool TryExecute(Entity target);
    bool TryExecuteCommitted(Entity target);
    void ResetCooldown();
}

public abstract class RangeDetectionStrategyBase : IRangeDetectionStrategy
{
    protected readonly Enemy owner;
    protected readonly PropertiesManager propertiesManager;

    private readonly AttackRangeSource rangeSource;
    private readonly float fixedRange;
    private readonly float rangeMultiplier;

    protected RangeDetectionStrategyBase(
        Enemy owner,
        PropertiesManager propertiesManager,
        AttackRangeSource rangeSource,
        float fixedRange,
        float rangeMultiplier)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.propertiesManager = propertiesManager ?? throw new ArgumentNullException(nameof(propertiesManager));
        this.rangeSource = rangeSource;
        this.fixedRange = Mathf.Max(0f, fixedRange);
        this.rangeMultiplier = Mathf.Max(0f, rangeMultiplier);
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

public sealed class DistanceRangeDetectionStrategy : RangeDetectionStrategyBase
{
    public DistanceRangeDetectionStrategy(
        Enemy owner,
        PropertiesManager propertiesManager,
        RangeDetectionData data)
        : base(owner, propertiesManager, data.rangeSource, data.fixedRange, data.rangeMultiplier)
    {
    }

    public DistanceRangeDetectionStrategy(
        Enemy owner,
        PropertiesManager propertiesManager,
        AttackRangeSource rangeSource,
        float fixedRange,
        float rangeMultiplier)
        : base(owner, propertiesManager, rangeSource, fixedRange, rangeMultiplier)
    {
    }

    public override bool IsTargetInRange(Entity target)
    {
        if (target == null)
        {
            return false;
        }

        return target.IsColliderWithinRange(owner.Center, ResolveRange());
    }
}

public sealed class ForwardCircleRangeDetectionStrategy : RangeDetectionStrategyBase
{
    private readonly float forwardOffset;

    public ForwardCircleRangeDetectionStrategy(
        Enemy owner,
        PropertiesManager propertiesManager,
        ForwardCircleDetectionData data)
        : base(owner, propertiesManager, data.rangeSource, data.fixedRange, data.rangeMultiplier)
    {
        forwardOffset = Mathf.Max(0f, data.forwardOffset);
    }

    public ForwardCircleRangeDetectionStrategy(
        Enemy owner,
        PropertiesManager propertiesManager,
        AttackRangeSource rangeSource,
        float fixedRange,
        float rangeMultiplier,
        float forwardOffset)
        : base(owner, propertiesManager, rangeSource, fixedRange, rangeMultiplier)
    {
        this.forwardOffset = Mathf.Max(0f, forwardOffset);
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

public abstract class AttackStrategyBase : IAttackStrategy
{
    protected readonly Enemy owner;
    protected readonly EnemyAttackController attackController;
    protected readonly PropertiesManager propertiesManager;
    protected readonly float damageMultiplier;
    protected readonly AudioSfxKey attackSfxKey;

    private readonly float cooldown;

    protected AttackStrategyBase(
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        AttackTimingData timingData,
        IRangeDetectionStrategy detectionStrategy)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.attackController = attackController ?? throw new ArgumentNullException(nameof(attackController));
        this.propertiesManager = propertiesManager ?? throw new ArgumentNullException(nameof(propertiesManager));
        DetectionStrategy = detectionStrategy ?? throw new ArgumentNullException(nameof(detectionStrategy));
        ActionId = string.IsNullOrWhiteSpace(timingData.actionId)
            ? throw new ArgumentException("Action id cannot be null or whitespace.", nameof(timingData))
            : timingData.actionId;
        cooldown = Mathf.Max(0f, timingData.cooldown);
        damageMultiplier = Mathf.Max(0f, timingData.damageMultiplier);
        attackSfxKey = timingData.attackSfxKey;
    }

    public string ActionId { get; }
    public IRangeDetectionStrategy DetectionStrategy { get; }

    public bool CanUse(Entity target)
    {
        return target != null &&
               attackController.CanUseRuntimeAction(ActionId) &&
               DetectionStrategy.IsTargetInRange(target);
    }

    public bool TryExecute(Entity target)
    {
        if (!CanUse(target))
        {
            return false;
        }

        return TryExecuteAfterEntry(target);
    }

    public bool TryExecuteCommitted(Entity target)
    {
        if (target == null || !attackController.CanUseRuntimeAction(ActionId))
        {
            return false;
        }

        return TryExecuteAfterEntry(target);
    }

    public void ResetCooldown()
    {
        attackController.ResetRuntimeCooldown(ActionId);
    }

    protected float ResolveDamage()
    {
        return Mathf.Max(0f, propertiesManager.GetPropValue(PropType.Attack) * damageMultiplier);
    }

    protected abstract bool ExecuteCore(Entity target);

    private bool TryExecuteAfterEntry(Entity target)
    {
        if (!ExecuteCore(target))
        {
            return false;
        }

        AudioSfxBridge.RequestPlay(attackSfxKey);
        attackController.CommitRuntimeCooldown(ActionId, cooldown);
        return true;
    }
}

public sealed class DirectDamageAttackStrategy : AttackStrategyBase
{
    public DirectDamageAttackStrategy(
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        AttackTimingData timingData,
        IRangeDetectionStrategy detectionStrategy)
        : base(owner, attackController, propertiesManager, timingData, detectionStrategy)
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

public sealed class ProjectileAttackStrategy : AttackStrategyBase
{
    private readonly ProjectileDefinitionSO projectileDefinition;

    public ProjectileAttackStrategy(
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        AttackTimingData timingData,
        IRangeDetectionStrategy detectionStrategy,
        ProjectileDefinitionSO projectileDefinition)
        : base(owner, attackController, propertiesManager, timingData, detectionStrategy)
    {
        this.projectileDefinition = projectileDefinition;
    }

    protected override bool ExecuteCore(Entity target)
    {
        if (projectileDefinition == null)
        {
            Debug.LogWarning($"{nameof(ProjectileAttackStrategy)} on {owner.name} is missing projectile definition.", owner);
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
            projectileDefinition,
            firePointPosition,
            Quaternion.identity);

        attackController.LaunchProjectile(projectile, new ProjectileLaunchContext(
            attackController,
            owner,
            firePointPosition,
            direction.normalized,
            HitSpec.EnemyHitSpec(ResolveDamage()),
            attackController.AttackLayer,
            projectileDefinition));
        return true;
    }
}

public sealed class MechaStoneDirectDamageAttackStrategy : AttackStrategyBase
{
    public MechaStoneDirectDamageAttackStrategy(
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        AttackTimingData timingData,
        IRangeDetectionStrategy detectionStrategy)
        : base(owner, attackController, propertiesManager, timingData, detectionStrategy)
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

public sealed class MechaStoneProjectileAttackStrategy : AttackStrategyBase
{
    private readonly ProjectileDefinitionSO projectileDefinition;

    public MechaStoneProjectileAttackStrategy(
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        AttackTimingData timingData,
        IRangeDetectionStrategy detectionStrategy,
        ProjectileDefinitionSO projectileDefinition)
        : base(owner, attackController, propertiesManager, timingData, detectionStrategy)
    {
        this.projectileDefinition = projectileDefinition;
    }

    protected override bool ExecuteCore(Entity target)
    {
        if (projectileDefinition == null)
        {
            Debug.LogWarning($"{nameof(MechaStoneProjectileAttackStrategy)} on {owner.name} is missing projectile definition.", owner);
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
            projectileDefinition,
            firePointPosition,
            Quaternion.identity);

        attackController.LaunchProjectile(projectile, new ProjectileLaunchContext(
            attackController,
            owner,
            firePointPosition,
            direction.normalized,
            HitSpec.EnemyHitSpec(ResolveDamage()),
            attackController.AttackLayer,
            projectileDefinition));
        return true;
    }
}

public sealed class DirectChaseMoveStrategy : IMoveStrategy
{
    private readonly IMovable movable;

    public DirectChaseMoveStrategy(IMovable movable)
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

public sealed class CircleKiteMoveStrategy : IMoveStrategy
{
    private readonly Enemy owner;
    private readonly IMovable movable;
    private readonly PropertiesManager propertiesManager;
    private readonly float circleSpeedRatio;
    private readonly float idealRangeRatio;

    public CircleKiteMoveStrategy(
        Enemy owner,
        IMovable movable,
        PropertiesManager propertiesManager,
        CircleKiteMoveData data)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.movable = movable ?? throw new ArgumentNullException(nameof(movable));
        this.propertiesManager = propertiesManager ?? throw new ArgumentNullException(nameof(propertiesManager));
        circleSpeedRatio = Mathf.Max(0f, data.circleSpeedRatio);
        idealRangeRatio = Mathf.Max(0f, data.idealRangeRatio);
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

public sealed class RetreatMoveStrategy : IMoveStrategy
{
    private readonly Enemy owner;
    private readonly IMovable movable;
    private readonly float safeDistance;
    private readonly float retreatStepDistance;

    public RetreatMoveStrategy(
        Enemy owner,
        IMovable movable,
        RetreatMoveData data)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.movable = movable ?? throw new ArgumentNullException(nameof(movable));
        safeDistance = Mathf.Max(0f, data.safeDistance);
        retreatStepDistance = Mathf.Max(0f, data.retreatStepDistance);
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
