using UnityEngine;

public sealed class ProjectileAttackStrategy : AttackStrategyBase
{
    private readonly Transform firePointTransform;
    private readonly ProjectileDefinitionSO projectileDefinition;
    private readonly float attackRangeMultiplier;
    private bool hasWarnedMissingFirePoint;

    public ProjectileAttackStrategy(
        Enemy owner,
        EnemyAttackController attackController,
        AttributeManager AttributeManager,
        string actionId,
        float attackSpeedBenefitRatio,
        Transform firePointTransform,
        ProjectileDefinitionSO projectileDefinition,
        float attackRangeMultiplier = 1f)
        : base(owner, attackController, AttributeManager, actionId, attackSpeedBenefitRatio)
    {
        this.firePointTransform = firePointTransform;
        this.projectileDefinition = projectileDefinition;
        this.attackRangeMultiplier = Mathf.Max(0f, attackRangeMultiplier);
    }

    public override bool IsTargetInRange(Entity target)
    {
        return target != null &&
               target.IsColliderWithinRange(ResolveFirePointPosition(), ResolveAttackRange());
    }

    protected override bool ExecuteCore(Entity target)
    {
        if (projectileDefinition == null)
        {
            Debug.LogWarning($"{nameof(ProjectileAttackStrategy)} on {owner.name} is missing projectile definition.", owner);
            return false;
        }

        Vector3 firePointPosition = ResolveFirePointPosition();
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
            projectileDefinition,
            maxTravelDistance: ResolveAttackRange()));
        return true;
    }

    private float ResolveAttackRange()
    {
        return PropValueUtility.DistancePointsToEffectiveAttackRangeWorldUnits(
            AttributeManager.GetAttributeValue(PropType.AttackRange)) * attackRangeMultiplier;
    }

    private Vector3 ResolveFirePointPosition()
    {
        if (firePointTransform != null)
        {
            return firePointTransform.position;
        }

        if (!hasWarnedMissingFirePoint)
        {
            hasWarnedMissingFirePoint = true;
            Debug.LogWarning($"{nameof(ProjectileAttackStrategy)} on {owner.name} is missing fire point for {ActionId}. Falling back to owner center.", owner);
        }

        return owner.Center;
    }
}
