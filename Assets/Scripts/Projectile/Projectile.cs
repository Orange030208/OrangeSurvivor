using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class Projectile : Entity, IProjectile, IWaveEndStep
{
    private const string DEFAULT_OBSTACLE_LAYER_NAME = "Wall";

    [Header("基础")]
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected LayerMask targetsLayerMask;
    [Tooltip("弹射物碰到这些阻挡层时会播放命中特效并销毁。未配置时默认使用项目中的 Wall 层。")]
    [SerializeField] protected LayerMask obstacleLayerMask;
    [Tooltip("未收到发射射程时的兜底最长飞行时间，仅用于换算兜底距离。正常武器发射按攻击距离销毁。")]
    [SerializeField] protected float maxLifetime = 5f;
    [SerializeField] protected int maxHitCount = 1;
    [SerializeField] protected Rigidbody2D rb;

    /// <summary>
    /// 防止刚生成就接触一群敌人
    /// </summary>
    private int currentHitCount = 0;
    private int currentMaxHitCount;
    private float traveledDistance;
    private Vector2 lastPosition;
    protected ProjectileLaunchContext launchContext;
    private float currentMoveSpeed;
    private float currentMaxLifetime;
    private float currentMaxTravelDistance;
    private float currentDamageMultiplier = 1f;
    private Vector3 baseLocalScale;
    private Quaternion baseRotation;
    private SpriteRenderer cachedSpriteRenderer;
    private Animator cachedAnimator;
    private bool isDespawning;
    private readonly HashSet<HealthComponent> hitTargets = new();
    public int WaveEndPriority => WaveEndPriorities.EntityCleanup;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentMoveSpeed = moveSpeed;
        currentMaxLifetime = maxLifetime;
        currentMaxTravelDistance = moveSpeed * maxLifetime;
        baseLocalScale = transform.localScale;
        baseRotation = transform.rotation;

        cachedSpriteRenderer = EntityRenderer != null ? EntityRenderer.SpriteRenderer : GetComponentInChildren<SpriteRenderer>();
        cachedAnimator = GetComponentInChildren<Animator>();
    }

    protected virtual void OnEnable()
    {
        traveledDistance = 0f;
        lastPosition = transform.position;
        currentHitCount = 0;
        currentMaxHitCount = Mathf.Max(1, maxHitCount);
        isDespawning = false;
        hitTargets.Clear();
    }

    protected virtual void Update()
    {
        if (isDespawning)
        {
            return;
        }

        Vector2 currentPosition = transform.position;
        traveledDistance += Vector2.Distance(lastPosition, currentPosition);
        lastPosition = currentPosition;

        if (traveledDistance >= currentMaxTravelDistance)
        {
            DestroyProjectile();
        }
    }

    public virtual void Launch(ProjectileLaunchContext context)
    {
        launchContext = context;
        targetsLayerMask = context.TargetLayerMask;
        currentMaxHitCount = Mathf.Max(1, maxHitCount + context.PierceCount);
        ApplyProjectileDefinition(context.ProjectileDefinition);
        transform.position = context.SpawnPosition;
        lastPosition = context.SpawnPosition;
        traveledDistance = 0f;
        currentMaxTravelDistance = ResolveMaxTravelDistance(context);
        ApplyFacing(context.Direction, context.ProjectileDefinition);
        Rigidbody2D runtimeRigidbody = ResolveRigidbody();
        runtimeRigidbody.simulated = true;
        runtimeRigidbody.velocity = context.Direction * currentMoveSpeed;
        OnLaunched(context);
    }

    public void PrepareForWaveEnd()
    {
        StopProjectileMotion();
        if (isDespawning)
        {
            return;
        }

        isDespawning = true;
    }

    public void PrepareForWaveCleanup()
    {
        PrepareForWaveEnd();
    }

    public async UniTask ExecuteWaveEndAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReleaseForWaveEnd();
        await WaitUntilDestroyedForWaveEndAsync(cancellationToken);
    }

    public void ReleaseForWaveEnd()
    {
        PrepareForWaveEnd();
        Destroy(gameObject);
    }

    public void ReleaseForWaveCleanup()
    {
        ReleaseForWaveEnd();
    }

    protected virtual void OnLaunched(ProjectileLaunchContext context)
    {
        SpawnLaunchEffect(context.ProjectileDefinition);
    }

    protected virtual void OnTriggerEnter2D(Collider2D collider)
    {
        if (isDespawning)
        {
            return;
        }

        if (TryHandleObstacleImpact(collider, ResolveImpactPosition(collider)))
        {
            return;
        }

        if (!IsInLayerMask(collider.gameObject.layer, targetsLayerMask) || currentHitCount >= currentMaxHitCount)
        {
            return;
        }

        if (!collider.TryGetComponent(out HealthComponent healthComponent))
        {
            return;
        }

        if (!hitTargets.Add(healthComponent))
        {
            return;
        }

        currentHitCount++;
        ApplyImpact(healthComponent);
        if (currentHitCount >= currentMaxHitCount)
        {
            DestroyProjectile();
        }
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDespawning)
        {
            return;
        }

        if (collision == null || collision.collider == null)
        {
            return;
        }

        TryHandleObstacleImpact(collision.collider, ResolveImpactPosition(collision));
    }

    protected virtual void ApplyImpact(HealthComponent healthComponent)
    {
        Entity target = healthComponent != null ? healthComponent.GetComponent<Entity>() : null;
        if (target == null)
        {
            return;
        }

        HitSpec hitSpec = new HitSpec(
            launchContext.HitSpec.BaseDamage * currentDamageMultiplier,
            launchContext.HitSpec.CritChance,
            launchContext.HitSpec.CritMultiplier,
            launchContext.HitSpec.KnockbackStrength);

        HitRequest request = new HitRequest(
            launchContext.Source,
            target,
            hitSpec,
            healthComponent.transform.position,
            launchContext.Direction,
            HitSourceKind.Projectile,
            sourcePosition: launchContext.SpawnPosition,
            sourceWeapon: launchContext.SourceWeapon);

        HitService.Apply(request);
        SpawnImpactEffect(transform.position, launchContext.ProjectileDefinition);
    }

    public void ApplyProjectileDefinition(ProjectileDefinitionSO projectileDefinition)
    {
        currentMoveSpeed = moveSpeed;
        currentMaxLifetime = maxLifetime;
        currentMaxTravelDistance = moveSpeed * maxLifetime;
        currentDamageMultiplier = 1f;
        transform.localScale = baseLocalScale;

        if (projectileDefinition == null)
        {
            return;
        }

        currentMoveSpeed *= projectileDefinition.SpeedMultiplier;
        currentMaxLifetime *= projectileDefinition.LifetimeMultiplier;
        currentMaxTravelDistance = currentMoveSpeed * currentMaxLifetime;
        currentDamageMultiplier *= projectileDefinition.DamageMultiplier;
        transform.localScale = baseLocalScale * projectileDefinition.ScaleMultiplier;
        ApplyPresentation(projectileDefinition);
    }

    private float ResolveMaxTravelDistance(ProjectileLaunchContext context)
    {
        if (context.MaxTravelDistance > 0f)
        {
            return context.MaxTravelDistance;
        }

        return Mathf.Max(0f, currentMoveSpeed * currentMaxLifetime);
    }

    private void ApplyPresentation(ProjectileDefinitionSO projectileDefinition)
    {
        if (cachedSpriteRenderer != null)
        {
            if (projectileDefinition.Sprite != null)
            {
                cachedSpriteRenderer.sprite = projectileDefinition.Sprite;
            }

            if (projectileDefinition.Material != null)
            {
                cachedSpriteRenderer.material = projectileDefinition.Material;
            }

            cachedSpriteRenderer.sortingOrder = projectileDefinition.SortingOrder;
        }

        if (cachedAnimator == null)
        {
            return;
        }

        cachedAnimator.runtimeAnimatorController = projectileDefinition.AnimatorController;
        if (!string.IsNullOrWhiteSpace(projectileDefinition.LaunchAnimationTrigger) &&
            cachedAnimator.HasParameter(projectileDefinition.LaunchAnimationTrigger, AnimatorControllerParameterType.Trigger))
        {
            cachedAnimator.SetTrigger(projectileDefinition.LaunchAnimationTrigger);
        }
    }

    private void ApplyFacing(Vector2 direction, ProjectileDefinitionSO projectileDefinition)
    {
        if (projectileDefinition == null)
        {
            transform.right = direction;
            return;
        }

        if (!projectileDefinition.UseDirectionFacing)
        {
            transform.rotation = baseRotation * Quaternion.Euler(0f, 0f, projectileDefinition.RotationOffset);
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + projectileDefinition.RotationOffset);
    }

    private void SpawnLaunchEffect(ProjectileDefinitionSO projectileDefinition)
    {
        if (projectileDefinition == null)
        {
            return;
        }

        RuntimeVfx.Spawn(projectileDefinition.LaunchVfxPrefab, transform.position, transform.rotation);
    }

    private void SpawnImpactEffect(Vector3 impactPosition, ProjectileDefinitionSO projectileDefinition)
    {
        if (projectileDefinition == null)
        {
            return;
        }

        RuntimeVfx.Spawn(projectileDefinition.ImpactVfxPrefab, impactPosition, transform.rotation);
    }

    private bool TryHandleObstacleImpact(Collider2D collider, Vector3 impactPosition)
    {
        if (isDespawning || collider == null || !IsInLayerMask(collider.gameObject.layer, ResolveObstacleLayerMask()))
        {
            return false;
        }

        SpawnImpactEffect(impactPosition, launchContext.ProjectileDefinition);
        DestroyProjectile();
        return true;
    }

    private Vector3 ResolveImpactPosition(Collider2D collider)
    {
        if (collider == null)
        {
            return transform.position;
        }

        Vector2 closestPoint = collider.ClosestPoint(transform.position);
        return closestPoint;
    }

    private Vector3 ResolveImpactPosition(Collision2D collision)
    {
        if (collision != null && collision.contactCount > 0)
        {
            return collision.GetContact(0).point;
        }

        return transform.position;
    }

    private LayerMask ResolveObstacleLayerMask()
    {
        if (obstacleLayerMask.value != 0)
        {
            return obstacleLayerMask;
        }

        return LayerMask.GetMask(DEFAULT_OBSTACLE_LAYER_NAME);
    }

    private void DestroyProjectile()
    {
        if (isDespawning)
        {
            return;
        }

        isDespawning = true;
        StopProjectileMotion();
        Destroy(gameObject);
    }

    private void StopProjectileMotion()
    {
        Rigidbody2D runtimeRigidbody = ResolveRigidbody();
        if (runtimeRigidbody != null)
        {
            runtimeRigidbody.velocity = Vector2.zero;
            runtimeRigidbody.angularVelocity = 0f;
            runtimeRigidbody.simulated = false;
        }

        if (EntityCollider != null)
        {
            EntityCollider.enabled = false;
        }
    }

    private Rigidbody2D ResolveRigidbody()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        return rb;
    }

    private async UniTask WaitUntilDestroyedForWaveEndAsync(CancellationToken cancellationToken)
    {
        while (this != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    protected virtual void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        maxLifetime = Mathf.Max(0f, maxLifetime);
        maxHitCount = Mathf.Max(1, maxHitCount);
    }
}

internal static class AnimatorExtensions
{
    public static bool HasParameter(this Animator animator, string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == parameterType && string.Equals(parameter.name, parameterName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
