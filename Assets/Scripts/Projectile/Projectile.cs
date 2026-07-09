using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 弹射物根协调器：
/// - 对外保持 IProjectile 发射入口；
/// - 对内把移动、命中、生命周期交给 prefab 上的显式模块；
/// - 统一处理表现覆盖、朝向、碰撞分发和销毁。
/// </summary>
[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class Projectile : Entity, IProjectile, IWaveEndStep
{
    private const string DEFAULT_OBSTACLE_LAYER_NAME = "Wall";

    [Header("模块")]
    [Tooltip("负责弹体移动，例如直线、追踪或回旋。")]
    [SerializeField] private ProjectileMovementBehaviour movementBehaviour;
    [Tooltip("负责弹体命中语义，例如直接伤害或范围爆炸。")]
    [SerializeField] private ProjectileImpactBehaviour impactBehaviour;
    [Tooltip("负责弹体何时结束，例如最远距离或兜底时间。")]
    [SerializeField] private ProjectileLifetimeBehaviour lifetimeBehaviour;

    [Header("碰撞")]
    [Tooltip("弹射物碰到这些阻挡层时会交给命中模块处理。未配置时默认使用项目中的 Wall 层。")]
    [SerializeField] private LayerMask obstacleLayerMask;

    [Header("运行时引用")]
    [SerializeField] private Rigidbody2D rb;

    private ProjectileLaunchContext launchContext;
    private ProjectileRuntimeContext runtimeContext;
    private Vector3 baseLocalScale;
    private Quaternion baseRotation;
    private SpriteRenderer cachedSpriteRenderer;
    private Animator cachedAnimator;
    private bool isDespawning;

    public int WaveEndPriority => WaveEndPriorities.EntityCleanup;

    protected virtual void Awake()
    {
        ResolveRequiredComponents();
        baseLocalScale = transform.localScale;
        baseRotation = transform.rotation;
        cachedSpriteRenderer = EntityRenderer != null ? EntityRenderer.SpriteRenderer : GetComponentInChildren<SpriteRenderer>();
        cachedAnimator = GetComponentInChildren<Animator>();
    }

    protected virtual void OnEnable()
    {
        isDespawning = false;
    }

    protected virtual void Update()
    {
        if (isDespawning)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        movementBehaviour.Tick(deltaTime);

        ProjectileLifetimeResult lifetimeResult = lifetimeBehaviour.Tick(deltaTime);
        if (!lifetimeResult.IsExpired)
        {
            return;
        }

        ProcessImpactResult(impactBehaviour.HandleLifetimeExpired(lifetimeResult.ImpactPosition));
    }

    public virtual void Launch(ProjectileLaunchContext context)
    {
        launchContext = context;
        ApplyProjectileDefinition(context.ProjectileDefinition);
        transform.position = context.SpawnPosition;
        ApplyFacing(context.Direction, context.ProjectileDefinition);

        ResolveRequiredComponents();
        runtimeContext = new ProjectileRuntimeContext(
            this,
            context,
            context.ProjectileDefinition,
            transform,
            ResolveRigidbody(),
            EntityCollider,
            ResolveObstacleLayerMask());

        if (EntityCollider != null)
        {
            EntityCollider.enabled = true;
        }

        movementBehaviour.Initialize(runtimeContext);
        impactBehaviour.Initialize(runtimeContext);
        lifetimeBehaviour.Initialize(runtimeContext);
        impactBehaviour.ResetState();
        lifetimeBehaviour.ResetState();
        movementBehaviour.Launch();
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
        if (isDespawning || collider == null)
        {
            return;
        }

        Vector2 impactPosition = ResolveImpactPosition(collider);
        if (TryHandleObstacleImpact(collider, impactPosition))
        {
            return;
        }

        if (!IsInLayerMask(collider.gameObject.layer, launchContext.TargetLayerMask) ||
            !collider.TryGetComponent(out HealthComponent healthComponent))
        {
            return;
        }

        ProjectileContact contact = new(
            ProjectileContactKind.Target,
            collider,
            healthComponent,
            impactPosition);
        ProcessImpactResult(impactBehaviour.HandleTargetContact(contact));
    }

    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDespawning || collision == null || collision.collider == null)
        {
            return;
        }

        TryHandleObstacleImpact(collision.collider, ResolveImpactPosition(collision));
    }

    public void ApplyProjectileDefinition(ProjectileDefinitionSO projectileDefinition)
    {
        transform.localScale = baseLocalScale;

        if (projectileDefinition == null)
        {
            return;
        }

        transform.localScale = baseLocalScale * projectileDefinition.ScaleMultiplier;
        ApplyPresentation(projectileDefinition);
    }

    private void ProcessImpactResult(ProjectileImpactResult result)
    {
        if (result.SpawnDefaultImpactVfx)
        {
            SpawnImpactEffect(result.ImpactPosition, launchContext.ProjectileDefinition);
        }

        if (result.ShouldDespawn)
        {
            DestroyProjectile();
        }
    }

    private bool TryHandleObstacleImpact(Collider2D collider, Vector2 impactPosition)
    {
        if (!IsInLayerMask(collider.gameObject.layer, ResolveObstacleLayerMask()))
        {
            return false;
        }

        ProjectileContact contact = new(
            ProjectileContactKind.Obstacle,
            collider,
            null,
            impactPosition);
        ProcessImpactResult(impactBehaviour.HandleObstacleContact(contact));
        return true;
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

    private Vector2 ResolveImpactPosition(Collider2D collider)
    {
        if (collider == null)
        {
            return transform.position;
        }

        return collider.ClosestPoint(transform.position);
    }

    private Vector2 ResolveImpactPosition(Collision2D collision)
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
        if (movementBehaviour != null)
        {
            movementBehaviour.Stop();
        }
        else
        {
            Rigidbody2D runtimeRigidbody = ResolveRigidbody();
            if (runtimeRigidbody != null)
            {
                runtimeRigidbody.velocity = Vector2.zero;
                runtimeRigidbody.angularVelocity = 0f;
                runtimeRigidbody.simulated = false;
            }
        }

        if (EntityCollider != null)
        {
            EntityCollider.enabled = false;
        }
    }

    private void ResolveRequiredComponents()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (movementBehaviour == null)
        {
            movementBehaviour = GetComponent<ProjectileMovementBehaviour>();
        }

        if (impactBehaviour == null)
        {
            impactBehaviour = GetComponent<ProjectileImpactBehaviour>();
        }

        if (lifetimeBehaviour == null)
        {
            lifetimeBehaviour = GetComponent<ProjectileLifetimeBehaviour>();
        }

        if (movementBehaviour == null || impactBehaviour == null || lifetimeBehaviour == null)
        {
            throw new MissingComponentException(
                $"{nameof(Projectile)} '{name}' requires {nameof(ProjectileMovementBehaviour)}, " +
                $"{nameof(ProjectileImpactBehaviour)} and {nameof(ProjectileLifetimeBehaviour)} components.");
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
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (movementBehaviour == null)
        {
            movementBehaviour = GetComponent<ProjectileMovementBehaviour>();
        }

        if (impactBehaviour == null)
        {
            impactBehaviour = GetComponent<ProjectileImpactBehaviour>();
        }

        if (lifetimeBehaviour == null)
        {
            lifetimeBehaviour = GetComponent<ProjectileLifetimeBehaviour>();
        }
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
