using System;
using UnityEngine;

public interface IGolemMechaStoneBossMovementStrategy
{
    void ExecuteMove(Entity target);
}

public interface IGolemMechaStoneBossDetectionStrategy
{
    float ResolveRange();
    bool IsTargetInRange(Entity target);
}

public interface IGolemMechaStoneBossAttackStrategy
{
    string ActionId { get; }
    IGolemMechaStoneBossDetectionStrategy DetectionStrategy { get; }
    bool CanUse(Entity target);
    bool TryExecute(Entity target);
    void ResetCooldown();
}

public static class GolemMechaStoneBossStrategyFactory
{
    public static IGolemMechaStoneBossMovementStrategy CreateChaseStrategy(
        Enemy owner,
        IMovable movable,
        GolemMechaStoneBossSO bossData)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        if (movable == null)
        {
            throw new ArgumentNullException(nameof(movable));
        }

        if (bossData == null)
        {
            throw new ArgumentNullException(nameof(bossData));
        }

        return bossData.ChaseMovementType switch
        {
            GolemMechaStoneBossChaseMovementType.DirectChase => new GolemMechaStoneBossDirectChaseMovementStrategy(movable),
            _ => throw new ArgumentOutOfRangeException(nameof(bossData.ChaseMovementType), bossData.ChaseMovementType, "Unsupported chase movement type."),
        };
    }

    public static IGolemMechaStoneBossDetectionStrategy CreateMeleeDetectionStrategy(
        Enemy owner,
        PropertiesManager propertiesManager,
        GolemMechaStoneBossSO bossData)
    {
        return new GolemMechaStoneBossForwardCircleDetectionStrategy(
            owner,
            propertiesManager,
            bossData.MeleeRangeSource,
            bossData.MeleeFixedRange,
            bossData.MeleeRangeMultiplier,
            bossData.MeleeForwardOffset);
    }

    public static IGolemMechaStoneBossDetectionStrategy CreateShootDetectionStrategy(
        Enemy owner,
        PropertiesManager propertiesManager,
        GolemMechaStoneBossSO bossData)
    {
        return new GolemMechaStoneBossDistanceDetectionStrategy(
            owner,
            propertiesManager,
            bossData.ShootRangeSource,
            bossData.ShootFixedRange,
            bossData.ShootRangeMultiplier);
    }

    public static IGolemMechaStoneBossAttackStrategy CreateMeleeAttackStrategy(
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        GolemMechaStoneBossSO bossData,
        IGolemMechaStoneBossDetectionStrategy detectionStrategy)
    {
        return new GolemMechaStoneBossDirectDamageAttackStrategy(
            GolemMechaStoneBossSO.MELEE_ACTION_ID,
            owner,
            attackController,
            propertiesManager,
            detectionStrategy,
            bossData.MeleeCooldown,
            bossData.MeleeDamageMultiplier,
            bossData.MeleeAttackSfxKey);
    }

    public static IGolemMechaStoneBossAttackStrategy CreateShootAttackStrategy(
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        GolemMechaStoneBossSO bossData,
        IGolemMechaStoneBossDetectionStrategy detectionStrategy)
    {
        return new GolemMechaStoneBossProjectileAttackStrategy(
            GolemMechaStoneBossSO.SHOOT_ACTION_ID,
            owner,
            attackController,
            propertiesManager,
            detectionStrategy,
            bossData.ShootCooldown,
            bossData.ShootDamageMultiplier,
            bossData.ShootAttackSfxKey,
            bossData.ShootProjectileDefinition);
    }
}

public sealed class GolemMechaStoneBossDirectChaseMovementStrategy : IGolemMechaStoneBossMovementStrategy
{
    private readonly IMovable movable;

    public GolemMechaStoneBossDirectChaseMovementStrategy(IMovable movable)
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

public abstract class GolemMechaStoneBossDetectionStrategyBase : IGolemMechaStoneBossDetectionStrategy
{
    protected readonly Enemy owner;
    protected readonly PropertiesManager propertiesManager;
    protected readonly AttackRangeSource rangeSource;
    protected readonly float fixedRange;
    protected readonly float rangeMultiplier;

    protected GolemMechaStoneBossDetectionStrategyBase(
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

public sealed class GolemMechaStoneBossDistanceDetectionStrategy : GolemMechaStoneBossDetectionStrategyBase
{
    public GolemMechaStoneBossDistanceDetectionStrategy(
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

        float range = ResolveRange();
        return (target.Center - owner.Center).sqrMagnitude <= range * range;
    }
}

public sealed class GolemMechaStoneBossForwardCircleDetectionStrategy : GolemMechaStoneBossDetectionStrategyBase
{
    private readonly float originForwardOffset;

    public GolemMechaStoneBossForwardCircleDetectionStrategy(
        Enemy owner,
        PropertiesManager propertiesManager,
        AttackRangeSource rangeSource,
        float fixedRange,
        float rangeMultiplier,
        float originForwardOffset)
        : base(owner, propertiesManager, rangeSource, fixedRange, rangeMultiplier)
    {
        this.originForwardOffset = Mathf.Max(0f, originForwardOffset);
    }

    public override bool IsTargetInRange(Entity target)
    {
        if (target == null)
        {
            return false;
        }

        Vector2 direction = ResolveDirectionTo(target);
        Vector2 origin = owner.Center + direction * originForwardOffset;
        return Vector2.Distance(origin, target.Center) <= ResolveRange();
    }
}

public abstract class GolemMechaStoneBossAttackStrategyBase : IGolemMechaStoneBossAttackStrategy
{
    protected readonly Enemy owner;
    protected readonly EnemyAttackController attackController;
    protected readonly PropertiesManager propertiesManager;
    protected readonly float cooldown;
    protected readonly float damageMultiplier;
    protected readonly AudioSfxKey attackSfxKey;

    protected GolemMechaStoneBossAttackStrategyBase(
        string actionId,
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        IGolemMechaStoneBossDetectionStrategy detectionStrategy,
        float cooldown,
        float damageMultiplier,
        AudioSfxKey attackSfxKey)
    {
        ActionId = string.IsNullOrWhiteSpace(actionId)
            ? throw new ArgumentException("Action id cannot be null or whitespace.", nameof(actionId))
            : actionId;
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.attackController = attackController ?? throw new ArgumentNullException(nameof(attackController));
        this.propertiesManager = propertiesManager ?? throw new ArgumentNullException(nameof(propertiesManager));
        DetectionStrategy = detectionStrategy ?? throw new ArgumentNullException(nameof(detectionStrategy));
        this.cooldown = Mathf.Max(0f, cooldown);
        this.damageMultiplier = Mathf.Max(0f, damageMultiplier);
        this.attackSfxKey = attackSfxKey;
    }

    public string ActionId { get; }
    public IGolemMechaStoneBossDetectionStrategy DetectionStrategy { get; }

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

        AudioSfxBridge.RequestPlay(attackSfxKey);
        attackController.CommitRuntimeCooldown(ActionId, cooldown);
        return true;
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
}

public sealed class GolemMechaStoneBossDirectDamageAttackStrategy : GolemMechaStoneBossAttackStrategyBase
{
    public GolemMechaStoneBossDirectDamageAttackStrategy(
        string actionId,
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        IGolemMechaStoneBossDetectionStrategy detectionStrategy,
        float cooldown,
        float damageMultiplier,
        AudioSfxKey attackSfxKey)
        : base(actionId, owner, attackController, propertiesManager, detectionStrategy, cooldown, damageMultiplier, attackSfxKey)
    {
    }

    protected override bool ExecuteCore(Entity target)
    {
        Vector2 knockbackDirection = target.Center - owner.Center;
        HitService.Apply(new HitRequest(
            owner,
            target,
            HitSpec.EnemyHitSpec(ResolveDamage()),
            target.Center,
            knockbackDirection,
            HitSourceKind.Direct,
            ActionId,
            owner.Center));
        return true;
    }
}

public sealed class GolemMechaStoneBossProjectileAttackStrategy : GolemMechaStoneBossAttackStrategyBase
{
    private readonly ProjectileDefinitionSO projectileDefinition;

    public GolemMechaStoneBossProjectileAttackStrategy(
        string actionId,
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        IGolemMechaStoneBossDetectionStrategy detectionStrategy,
        float cooldown,
        float damageMultiplier,
        AudioSfxKey attackSfxKey,
        ProjectileDefinitionSO projectileDefinition)
        : base(actionId, owner, attackController, propertiesManager, detectionStrategy, cooldown, damageMultiplier, attackSfxKey)
    {
        this.projectileDefinition = projectileDefinition;
    }

    protected override bool ExecuteCore(Entity target)
    {
        if (projectileDefinition == null)
        {
            Debug.LogWarning($"{nameof(GolemMechaStoneBossProjectileAttackStrategy)} on {owner.name} is missing projectile definition.", owner);
            return false;
        }

        Vector2 direction = target.Center - owner.Center;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        Vector3 firePointPosition = attackController.FirePoint.position;
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
