using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskDescription("Locks a direction and applies repeated laser damage in a capsule area.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class MechaStoneLaserCast : MechaStoneTaskBase
{
    private const int LASER_HIT_BUFFER_SIZE = 16;
    private const float MIN_RESOLVED_LASER_LENGTH = 0.05f;

    private readonly Collider2D[] hitBuffer = new Collider2D[LASER_HIT_BUFFER_SIZE];

    private float startTime;
    private float nextDamageTime;
    private Vector2 lockedDirection;
    private bool aimLocked;
    private bool laserFired;
    private bool cooldownCommitted;
    private Entity executionTarget;
    private GolemMechaStoneLaserVisual laserVisual;

    public override void OnStart()
    {
        base.OnStart();
        startTime = Time.time;
        nextDamageTime = float.PositiveInfinity;
        aimLocked = false;
        laserFired = false;
        cooldownCommitted = false;
        executionTarget = TargetEntity;
        if (!HasContext)
        {
            lockedDirection = Vector2.right;
            return;
        }

        lockedDirection = ResolveDirectionToTarget(executionTarget);
        StopMoving();
        FaceTarget();
        AudioSfxBridge.RequestPlay(AudioSfxKey.GolemMechaStoneBossLaser);
        BeginBossAction(BossData.LaserAction);
        EnsureLaserVisual();
        laserVisual?.PlayStart();
    }

    public override TaskStatus OnUpdate()
    {
        if (!RefreshContext())
        {
            return TaskStatus.Failure;
        }

        StopMoving();
        if (Animatable == null)
        {
            return TaskStatus.Failure;
        }

        TickBossAction(Time.deltaTime);
        if (!ActionRunner.Progress.IsPlaying)
        {
            return TaskStatus.Running;
        }

        float normalizedTime = ActionRunner.Progress.NormalizedTime;
        if (!laserFired && normalizedTime < BossData.LaserFireStartNormalizedTime)
        {
            UpdatePreFireDirection(normalizedTime);
            return TaskStatus.Running;
        }

        if (!laserFired)
        {
            lockedDirection = ResolveSafeDirection(lockedDirection);
            FacingController?.FaceDirection(lockedDirection);
            laserFired = true;
        }

        UpdateActiveDirection();
        UpdateLaserVisual();
        if (laserVisual != null && !laserVisual.IsCoreLineVisible)
        {
            return TaskStatus.Running;
        }

        if (float.IsPositiveInfinity(nextDamageTime))
        {
            startTime = Time.time;
            nextDamageTime = Time.time;
        }

        if (Time.time >= nextDamageTime)
        {
            DealLaserDamage();
            nextDamageTime = Time.time + BossData.LaserDamageInterval;
        }

        if (Time.time - startTime < BossData.LaserDuration)
        {
            return TaskStatus.Running;
        }

        CommitCooldown();
        ActionRunner.MarkCommitted();
        ActionRunner.MarkComplete();
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
        if (OwnerEnemy == null || AttackController == null || AttributeManager == null)
        {
            return;
        }

        Vector2 direction = ResolveSafeDirection(lockedDirection);
        Vector2 laserOrigin = ResolveLaserOrigin();
        float laserLength = BossData.LaserLength;
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

            float damage = PropValueUtility.ClampNonNegative(
                AttributeManager.GetAttributeValue(PropType.Attack) * BossData.LaserDamageMultiplier);
            Vector2 knockbackDirection = hitEntity.Center - OwnerEnemy.Center;
            Vector2 hitPoint = hitEntity.GetClosestPointTo(laserOrigin);
            HitService.Apply(new HitRequest(
                OwnerEnemy,
                hitEntity,
                HitSpec.EnemyHitSpec(damage),
                hitPoint,
                knockbackDirection,
                HitSourceKind.Direct,
                laserOrigin));
        }
    }

    private void CommitCooldown()
    {
        if (cooldownCommitted || AttackController == null)
        {
            return;
        }

        AttackController.CommitSkillCooldown(GolemMechaStoneBossSO.LASER_ACTION_ID, BossData.LaserCooldown);
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

    private void UpdateActiveDirection()
    {
        float turnSpeed = BossData.LaserTurnSpeedDegrees;
        if (turnSpeed <= 0f)
        {
            return;
        }

        Vector2 targetDirection = ResolveDirectionToTarget(executionTarget);
        lockedDirection = RotateDirectionTowards(
            ResolveSafeDirection(lockedDirection),
            targetDirection,
            turnSpeed * Time.deltaTime);
        FacingController?.FaceDirection(lockedDirection);
    }

    private void UpdatePreFireDirection(float normalizedTime)
    {
        if (!aimLocked && normalizedTime < BossData.LaserAimLockNormalizedTime)
        {
            lockedDirection = ResolveDirectionToTarget(executionTarget);
            FacingController?.FaceTarget(executionTarget);
            return;
        }

        aimLocked = true;
        lockedDirection = ResolveSafeDirection(lockedDirection);
        FacingController?.FaceDirection(lockedDirection);
    }

    private static Vector2 RotateDirectionTowards(Vector2 currentDirection, Vector2 targetDirection, float maxDegreesDelta)
    {
        if (targetDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return currentDirection;
        }

        float currentAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        float resultAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, Mathf.Max(0f, maxDegreesDelta));
        float radians = resultAngle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
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

        Transform laserParent = BossBrain != null ? BossBrain.LaserOriginTransform : OwnerEnemy.transform;
        laserVisual = Object.Instantiate(visualPrefab, laserParent);
        Transform visualTransform = laserVisual.transform;
        visualTransform.localPosition = Vector3.zero;
        visualTransform.localRotation = Quaternion.identity;
        visualTransform.localScale = Vector3.one;
        laserVisual.Hide();
    }

    private void UpdateLaserVisual()
    {
        if (laserVisual == null || OwnerEnemy == null || BossData == null)
        {
            return;
        }

        Vector2 direction = ResolveSafeDirection(lockedDirection);
        Vector3 localStartPosition = Vector3.zero;
        Vector3 localEndPosition = ResolveLocalLaserEndPosition(direction, BossData.LaserLength);
        laserVisual.ShowCore(
            localStartPosition,
            localEndPosition);
    }

    private Vector3 ResolveLocalLaserEndPosition(Vector2 worldDirection, float laserLength)
    {
        Transform laserOriginTransform = BossBrain != null ? BossBrain.LaserOriginTransform : null;
        Vector3 worldOffset = (Vector3)(worldDirection * laserLength);
        if (laserOriginTransform == null)
        {
            return worldOffset;
        }

        return laserOriginTransform.InverseTransformVector(worldOffset);
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
