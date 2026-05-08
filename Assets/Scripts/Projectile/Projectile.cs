using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class Projectile : Entity, IProjectile
{
    private const string DEFAULT_OBSTACLE_LAYER_NAME = "Wall";

    [Header("Base")]
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected LayerMask targetsLayerMask;
    [Tooltip("弹射物碰到这些阻挡层时会播放命中特效并销毁。未配置时默认使用项目中的 Wall 层。")]
    [SerializeField] protected LayerMask obstacleLayerMask;
    [SerializeField] protected float maxLifetime = 5f;
    [SerializeField] protected int maxHitCount = 1;
    [SerializeField] protected Rigidbody2D rb;

    /// <summary>
    /// 防止刚生成就接触一群敌人
    /// </summary>
    private int currentHitCount = 0;
    private int currentMaxHitCount;
    private float lifetimeTimer;
    protected ProjectileLaunchContext launchContext;
    private float currentMoveSpeed;
    private float currentMaxLifetime;
    private float currentDamageMultiplier = 1f;
    private Vector3 baseLocalScale;
    private Quaternion baseRotation;
    private SpriteRenderer cachedSpriteRenderer;
    private Animator cachedAnimator;
    private bool isDespawning;
    private readonly HashSet<HealthComponent> hitTargets = new();

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentMoveSpeed = moveSpeed;
        currentMaxLifetime = maxLifetime;
        baseLocalScale = transform.localScale;
        baseRotation = transform.rotation;

        cachedSpriteRenderer = EntityRenderer != null ? EntityRenderer.SpriteRenderer : GetComponentInChildren<SpriteRenderer>();
        cachedAnimator = GetComponentInChildren<Animator>();
    }

    protected virtual void OnEnable()
    {
        lifetimeTimer = 0f;
        currentHitCount = 0;
        currentMaxHitCount = Mathf.Max(1, maxHitCount);
        isDespawning = false;
        hitTargets.Clear();
    }

    protected virtual void Update()
    {
        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= currentMaxLifetime)
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
        ApplyFacing(context.Direction, context.ProjectileDefinition);
        rb.velocity = context.Direction * currentMoveSpeed;
        OnLaunched(context);
    }

    protected virtual void OnLaunched(ProjectileLaunchContext context)
    {
        SpawnLaunchEffect(context.ProjectileDefinition);
    }

    protected virtual void OnTriggerEnter2D(Collider2D collider)
    {
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
        currentDamageMultiplier = 1f;
        transform.localScale = baseLocalScale;

        if (projectileDefinition == null)
        {
            return;
        }

        currentMoveSpeed *= projectileDefinition.SpeedMultiplier;
        currentMaxLifetime *= projectileDefinition.LifetimeMultiplier;
        currentDamageMultiplier *= projectileDefinition.DamageMultiplier;
        transform.localScale = baseLocalScale * projectileDefinition.ScaleMultiplier;
        ApplyPresentation(projectileDefinition);
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
        Destroy(gameObject);
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
