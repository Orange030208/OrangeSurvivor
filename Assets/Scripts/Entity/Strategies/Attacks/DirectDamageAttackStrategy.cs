using System;
using UnityEngine;

public sealed class DirectDamageAttackStrategy : AttackStrategyBase
{
    private const int AREA_HIT_BUFFER_SIZE = 16;

    private readonly Collider2D[] areaHitBuffer = new Collider2D[AREA_HIT_BUFFER_SIZE];
    private readonly Transform attackPointTransform;
    private readonly float rangeMultiplier;
    private readonly GameObject hitVfxPrefab;
    private readonly DirectDamageHitShape hitShape;
    private readonly Func<Vector2> attackDirectionProvider;
    private readonly Func<Entity, Vector2> rangeDirectionProvider;
    private bool hasWarnedMissingAttackPoint;

    public DirectDamageAttackStrategy(
        Enemy owner,
        EnemyAttackController attackController,
        PropertiesManager propertiesManager,
        string actionId,
        float attackSpeedBenefitRatio,
        Transform attackPointTransform = null,
        float rangeMultiplier = 1f,
        GameObject hitVfxPrefab = null,
        DirectDamageHitShape hitShape = DirectDamageHitShape.Circle,
        Func<Vector2> attackDirectionProvider = null,
        Func<Entity, Vector2> rangeDirectionProvider = null)
        : base(owner, attackController, propertiesManager, actionId, attackSpeedBenefitRatio)
    {
        this.attackPointTransform = attackPointTransform;
        this.rangeMultiplier = Mathf.Max(0f, rangeMultiplier);
        this.hitVfxPrefab = hitVfxPrefab;
        this.hitShape = hitShape;
        this.attackDirectionProvider = attackDirectionProvider;
        this.rangeDirectionProvider = rangeDirectionProvider;
    }

    public override bool IsTargetInRange(Entity target)
    {
        if (target == null)
        {
            return false;
        }

        Vector2 attackCenter = ResolveAttackCenter();
        float attackRadius = ResolveAttackRadius();
        if (!target.IsColliderWithinRange(attackCenter, attackRadius))
        {
            return false;
        }

        return hitShape != DirectDamageHitShape.FacingSemicircle ||
               AreaHitQueryUtility.IsColliderInFacingSemicircle(
                   target.EntityCollider,
                   attackCenter,
                   ResolveRangeDirection(target));
    }

    protected override bool ExecuteCore(Entity target)
    {
        ExecuteCommittedAreaAttack();
        return true;
    }

    private void ExecuteCommittedAreaAttack()
    {
        Vector2 attackCenter = ResolveAttackCenter();
        float attackRadius = ResolveAttackRadius();
        RuntimeVfx.Spawn(hitVfxPrefab, attackCenter, Quaternion.identity);

        int hitCount = QueryAreaHits(attackCenter, attackRadius);

        for (int i = 0; i < hitCount; i++)
        {
            Entity hitEntity = ResolveEntity(areaHitBuffer[i]);
            if (hitEntity == null || hitEntity == owner)
            {
                continue;
            }

            Vector2 hitPoint = hitEntity.GetClosestPointTo(attackCenter);
            Vector2 knockbackDirection = hitEntity.Center - owner.Center;
            HitService.Apply(new HitRequest(
                owner,
                hitEntity,
                HitSpec.EnemyHitSpec(ResolveDamage()),
                hitPoint,
                knockbackDirection,
                HitSourceKind.Direct,
                owner.Center));
        }
    }

    private int QueryAreaHits(Vector2 attackCenter, float attackRadius)
    {
        return hitShape == DirectDamageHitShape.FacingSemicircle
            ? AreaHitQueryUtility.OverlapFacingSemicircleNonAlloc(
                attackCenter,
                attackRadius,
                ResolveAttackDirection(),
                areaHitBuffer,
                attackController.AttackLayer)
            : AreaHitQueryUtility.OverlapCircleNonAlloc(
                attackCenter,
                attackRadius,
                areaHitBuffer,
                attackController.AttackLayer);
    }

    private Vector2 ResolveAttackDirection()
    {
        return attackDirectionProvider != null ? attackDirectionProvider.Invoke() : Vector2.right;
    }

    private Vector2 ResolveRangeDirection(Entity target)
    {
        return rangeDirectionProvider != null ? rangeDirectionProvider.Invoke(target) : ResolveAttackDirection();
    }

    private float ResolveAttackRadius()
    {
        return PropValueUtility.DistancePointsToEffectiveAttackRangeWorldUnits(
            propertiesManager.GetPropValue(PropType.AttackRange)) * rangeMultiplier;
    }

    private Vector2 ResolveAttackCenter()
    {
        if (attackPointTransform != null)
        {
            return attackPointTransform.position;
        }

        if (!hasWarnedMissingAttackPoint)
        {
            hasWarnedMissingAttackPoint = true;
            Debug.LogWarning($"{nameof(DirectDamageAttackStrategy)} on {owner.name} is missing attack point for {ActionId}. Falling back to owner center.", owner);
        }

        return owner.Center;
    }

    private static Entity ResolveEntity(Collider2D hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        Entity entity = hitCollider.GetComponent<Entity>();
        return entity != null ? entity : hitCollider.GetComponentInParent<Entity>();
    }
}
