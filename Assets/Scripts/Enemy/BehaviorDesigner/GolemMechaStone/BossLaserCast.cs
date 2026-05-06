using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskDescription("Locks a direction and applies repeated laser damage in a capsule area.")]
[TaskCategory("Survivors/Enemy/Golem Mecha Stone")]
public sealed class BossLaserCast : GolemMechaStoneBossTaskBase
{
    private const string LASER_HIT_SOURCE_ID = "GolemMechaStoneBoss_Laser";
    private const int LASER_HIT_BUFFER_SIZE = 16;

    private readonly Collider2D[] hitBuffer = new Collider2D[LASER_HIT_BUFFER_SIZE];

    private float startTime;
    private float nextDamageTime;
    private Vector2 lockedDirection;
    private bool directionLocked;
    private bool cooldownCommitted;
    private Entity executionTarget;
    private LineRenderer laserLineRenderer;
    private Material laserMaterial;

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
        Vector2 center = laserOrigin + direction * (BossData.LaserRange * 0.5f);
        Vector2 size = new Vector2(BossData.LaserRange, BossData.LaserWidth);
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
            HitService.Apply(new HitRequest(
                OwnerEnemy,
                hitEntity,
                HitSpec.EnemyHitSpec(damage),
                hitEntity.Center,
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

        return ResolveSafeDirection(target.Center - OwnerEnemy.Center);
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
        if (AttackController != null)
        {
            return AttackController.FirePoint.position;
        }

        return OwnerEnemy != null ? OwnerEnemy.Center : Vector2.zero;
    }

    private void EnsureLaserVisual()
    {
        if (OwnerEnemy == null || laserLineRenderer != null)
        {
            return;
        }

        GameObject laserVisualObject = new("Golem Mecha Stone Laser Visual");
        laserVisualObject.transform.SetParent(OwnerEnemy.transform, false);
        laserLineRenderer = laserVisualObject.AddComponent<LineRenderer>();
        laserLineRenderer.useWorldSpace = true;
        laserLineRenderer.positionCount = 2;
        laserLineRenderer.textureMode = LineTextureMode.Stretch;
        laserLineRenderer.alignment = LineAlignment.View;
        laserLineRenderer.numCapVertices = 8;
        laserLineRenderer.numCornerVertices = 4;
        laserLineRenderer.sortingOrder = BossData.LaserSortingOrder;
        laserMaterial = new Material(Shader.Find("Sprites/Default"));
        laserLineRenderer.material = laserMaterial;
    }

    private void UpdateLaserVisual(bool active)
    {
        if (laserLineRenderer == null || OwnerEnemy == null || BossData == null)
        {
            return;
        }

        Vector2 direction = ResolveSafeDirection(lockedDirection);
        Vector3 startPosition = ResolveLaserOrigin();
        Vector3 endPosition = startPosition + (Vector3)(direction * BossData.LaserRange);
        Color color = active ? BossData.LaserActiveColor : BossData.LaserWindupColor;
        float width = active ? BossData.LaserActiveVisualWidth : BossData.LaserWindupVisualWidth;

        laserLineRenderer.startWidth = width;
        laserLineRenderer.endWidth = width;
        laserLineRenderer.startColor = color;
        laserLineRenderer.endColor = color;
        laserLineRenderer.SetPosition(0, startPosition);
        laserLineRenderer.SetPosition(1, endPosition);
    }

    private void ClearLaserVisual()
    {
        if (laserLineRenderer != null)
        {
            Object.Destroy(laserLineRenderer.gameObject);
            laserLineRenderer = null;
        }

        if (laserMaterial != null)
        {
            Object.Destroy(laserMaterial);
            laserMaterial = null;
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
