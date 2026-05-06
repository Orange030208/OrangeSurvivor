using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskDescription("Locks a direction and applies repeated laser damage in a capsule area.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class MechaStoneLaserCast : MechaStoneTaskBase
{
    private const string LASER_HIT_SOURCE_ID = "GolemMechaStoneBoss_Laser";
    private const int LASER_HIT_BUFFER_SIZE = 16;
    private const int LASER_TRACE_BUFFER_SIZE = 16;
    private const float MIN_RESOLVED_LASER_LENGTH = 0.05f;

    private readonly Collider2D[] hitBuffer = new Collider2D[LASER_HIT_BUFFER_SIZE];
    private readonly RaycastHit2D[] traceBuffer = new RaycastHit2D[LASER_TRACE_BUFFER_SIZE];

    private float startTime;
    private float nextDamageTime;
    private Vector2 lockedDirection;
    private bool directionLocked;
    private bool cooldownCommitted;
    private Entity executionTarget;
    private GolemMechaStoneLaserVisual laserVisual;

    public override void OnStart()
    {
        base.OnStart();
        startTime = Time.time;
        nextDamageTime = float.PositiveInfinity;
        directionLocked = false;
        cooldownCommitted = false;
        executionTarget = TargetEntity;
        if (!HasContext)
        {
            lockedDirection = Vector2.right;
            return;
        }

        AcquireActionLock();
        lockedDirection = ResolveDirectionToTarget(executionTarget);
        StopMoving();
        FaceTarget();
        Animatable?.PlayState(BossAnimationConfig.LaserCastHash);
        EnsureLaserVisual();
        UpdateLaserVisual(false);
    }

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext())
        {
            return TaskStatus.Failure;
        }

        StopMoving();
        float elapsedTime = Time.time - startTime;
        float lockTime = Mathf.Max(0f, BossData.LaserWindupDuration - BossData.LaserDirectionLockLeadTime);

        if (!directionLocked && elapsedTime < lockTime)
        {
            lockedDirection = ResolveDirectionToTarget(executionTarget);
            FacingController?.FaceTarget(executionTarget);
        }
        else if (!directionLocked)
        {
            directionLocked = true;
            lockedDirection = ResolveSafeDirection(lockedDirection);
            FacingController?.FaceDirection(lockedDirection);
        }

        if (elapsedTime < BossData.LaserWindupDuration)
        {
            UpdateLaserVisual(false);
            return TaskStatus.Running;
        }

        UpdateLaserVisual(true);
        if (float.IsPositiveInfinity(nextDamageTime))
        {
            nextDamageTime = Time.time;
        }

        if (Time.time >= nextDamageTime)
        {
            DealLaserDamage();
            nextDamageTime = Time.time + BossData.LaserDamageInterval;
        }

        float finishTime = BossData.LaserWindupDuration + BossData.LaserDuration;
        if (elapsedTime < finishTime)
        {
            return TaskStatus.Running;
        }

        CommitCooldown();
        return TaskStatus.Success;
    }

    public override void OnFixedUpdate()
    {
        StopMoving();
    }

    public override void OnEnd()
    {
        StopMoving();
        ClearLaserVisual();
        ReleaseActionLock();
    }

    public override void OnReset()
    {
        base.OnReset();
        ClearLaserVisual();
    }

    private void DealLaserDamage()
    {
        if (OwnerEnemy == null || AttackController == null || PropertiesManager == null)
        {
            return;
        }

        Vector2 direction = ResolveSafeDirection(lockedDirection);
        Vector2 laserOrigin = ResolveLaserOrigin();
        float laserLength = ResolveLaserLength(laserOrigin, direction);
        if (laserLength < MIN_RESOLVED_LASER_LENGTH)
        {
            return;
        }

        Vector2 center = laserOrigin + direction * (laserLength * 0.5f);
        Vector2 size = new Vector2(Mathf.Max(laserLength, BossData.LaserWidth), BossData.LaserWidth);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        int hitCount = Physics2D.OverlapCapsuleNonAlloc(
            center,
            size,
            CapsuleDirection2D.Horizontal,
            angle,
            hitBuffer,
            AttackController.AttackLayer);

        for (int i = 0; i < hitCount; i++)
        {
            Entity hitEntity = ResolveEntity(hitBuffer[i]);
            if (hitEntity == null || hitEntity == OwnerEnemy)
            {
                continue;
            }

            float damage = Mathf.Max(0f, PropertiesManager.GetPropValue(PropType.Attack) * BossData.LaserDamageMultiplier);
            Vector2 knockbackDirection = hitEntity.Center - OwnerEnemy.Center;
            Vector2 hitPoint = hitEntity.GetClosestPointTo(laserOrigin);
            HitService.Apply(new HitRequest(
                OwnerEnemy,
                hitEntity,
                HitSpec.EnemyHitSpec(damage),
                hitPoint,
                knockbackDirection,
                HitSourceKind.Direct,
                LASER_HIT_SOURCE_ID,
                laserOrigin));
        }
    }

    private void CommitCooldown()
    {
        if (cooldownCommitted || AttackController == null)
        {
            return;
        }

        AttackController.CommitRuntimeCooldown(GolemMechaStoneBossSO.LASER_ACTION_ID, BossData.LaserCooldown);
        cooldownCommitted = true;
    }

    private Vector2 ResolveDirectionToTarget(Entity target)
    {
        if (OwnerEnemy == null || target == null)
        {
            return ResolveSafeDirection(lockedDirection);
        }

        Vector2 laserOrigin = ResolveLaserOrigin();
        Vector2 targetPoint = target.GetClosestPointTo(laserOrigin);
        return ResolveSafeDirection(targetPoint - laserOrigin);
    }

    private Vector2 ResolveSafeDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > Mathf.Epsilon)
        {
            return direction.normalized;
        }

        if (OwnerEnemy != null && OwnerEnemy.transform.localScale.x < 0f)
        {
            return Vector2.left;
        }

        return Vector2.right;
    }

    private Vector2 ResolveLaserOrigin()
    {
        Transform laserOriginTransform = BossBrain != null ? BossBrain.LaserOriginTransform : null;
        if (laserOriginTransform != null)
        {
            return laserOriginTransform.position;
        }

        return OwnerEnemy != null ? OwnerEnemy.Center : Vector2.zero;
    }

    private float ResolveLaserLength(Vector2 origin, Vector2 direction)
    {
        if (AttackController == null || BossData == null)
        {
            return 0f;
        }

        float radius = BossData.LaserWidth * 0.5f;
        int hitCount = Physics2D.CircleCastNonAlloc(
            origin,
            radius,
            direction,
            traceBuffer,
            Mathf.Infinity,
            AttackController.AttackLayer);

        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = traceBuffer[i];
            Entity hitEntity = ResolveEntity(hit.collider);
            if (hitEntity == null || hitEntity == OwnerEnemy)
            {
                continue;
            }

            nearestDistance = Mathf.Min(nearestDistance, Mathf.Max(0f, hit.distance));
        }

        return float.IsPositiveInfinity(nearestDistance)
            ? BossData.LaserNoHitVisualLength
            : Mathf.Max(nearestDistance, MIN_RESOLVED_LASER_LENGTH);
    }

    private void EnsureLaserVisual()
    {
        if (OwnerEnemy == null || laserVisual != null)
        {
            return;
        }

        GolemMechaStoneLaserVisual visualPrefab = BossData.LaserVisualPrefab;
        if (visualPrefab == null)
        {
            Debug.LogWarning($"{nameof(MechaStoneLaserCast)} on {OwnerEnemy.name} is missing {nameof(GolemMechaStoneBossSO.LaserVisualPrefab)}.", OwnerEnemy);
            return;
        }

        laserVisual = Object.Instantiate(visualPrefab, OwnerEnemy.transform);
        laserVisual.Hide();
    }

    private void UpdateLaserVisual(bool active)
    {
        if (laserVisual == null || OwnerEnemy == null || BossData == null)
        {
            return;
        }

        Vector2 direction = ResolveSafeDirection(lockedDirection);
        Vector3 startPosition = ResolveLaserOrigin();
        float laserLength = ResolveLaserLength(startPosition, direction);
        Vector3 endPosition = startPosition + (Vector3)(direction * laserLength);
        Color color = active ? BossData.LaserActiveColor : BossData.LaserWindupColor;
        laserVisual.Show(
            startPosition,
            endPosition,
            color,
            active ? BossData.LaserActiveVisualWidth : BossData.LaserWindupVisualWidth,
            BossData.LaserCoreColor,
            BossData.LaserCoreVisualWidth,
            active,
            BossData.LaserSortingOrder);
    }

    private void ClearLaserVisual()
    {
        if (laserVisual != null)
        {
            Object.Destroy(laserVisual.gameObject);
            laserVisual = null;
        }
    }

    private Entity ResolveEntity(Collider2D hitCollider)
    {
        if (hitCollider == null)
        {
            return null;
        }

        Entity entity = hitCollider.GetComponent<Entity>();
        return entity != null ? entity : hitCollider.GetComponentInParent<Entity>();
    }
}
