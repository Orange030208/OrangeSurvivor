using System;
using UnityEngine;

/// <summary>
/// 玩家投射物基类。
/// 当前负责：
/// - 按发射上下文设置方向与速度；
/// - 处理寿命；
/// - 命中目标时发起统一 hit 流程；
/// - 根据弹射物定义应用基础倍率。
/// 后续如果要做穿透、弹射、分裂、持续伤害等，可以在子类里扩展。
/// </summary>
[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class Projectile : Entity, IProjectile
{
    [Header("Base")]
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected LayerMask targetsLayerMask;
    [SerializeField] protected float maxLifetime = 5f;

    private Rigidbody2D rb;
    private float lifetimeTimer;
    protected ProjectileLaunchContext launchContext;
    private float currentMoveSpeed;
    private float currentMaxLifetime;
    private float currentDamageMultiplier = 1f;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentMoveSpeed = moveSpeed;
        currentMaxLifetime = maxLifetime;
    }

    protected virtual void OnEnable()
    {
        lifetimeTimer = 0f;
    }

    protected virtual void Update()
    {
        if (!GameSimulation.IsRunning)
        {
            return;
        }

        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= currentMaxLifetime)
        {
            Destroy(gameObject);
        }
    }

    public virtual void Launch(ProjectileLaunchContext context)
    {
        launchContext = context;
        targetsLayerMask = context.TargetLayerMask;
        ApplyProjectileDefinition(context.ProjectileDefinition);
        transform.position = context.SpawnPosition;
        transform.right = context.Direction;
        rb.velocity = context.Direction * currentMoveSpeed;
    }

    protected virtual void OnLaunched(ProjectileLaunchContext context)
    {
    }

    protected virtual void OnTriggerEnter2D(Collider2D collider)
    {
        if (!IsInLayerMask(collider.gameObject.layer, targetsLayerMask))
        {
            return;
        }

        if (!collider.TryGetComponent(out HealthComponent healthComponent))
        {
            return;
        }

        ApplyImpact(healthComponent);
        Destroy(gameObject);
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
            launchContext.HitSpec.CritMultiplier);

        HitRequest request = new HitRequest(
            launchContext.Source,
            target,
            hitSpec,
            healthComponent.transform.position,
            HitSourceKind.Projectile,
            GetType().Name);

        IProjectileLauncher projectileLauncher = launchContext.Launcher;

        HitService.Apply(request);
    }

    private void ApplyProjectileDefinition(ProjectileDefinitionSO projectileDefinition)
    {
        currentMoveSpeed = moveSpeed;
        currentMaxLifetime = maxLifetime;
        currentDamageMultiplier = 1f;

        if (projectileDefinition == null)
        {
            return;
        }

        currentMoveSpeed *= projectileDefinition.SpeedMultiplier;
        currentMaxLifetime *= projectileDefinition.LifetimeMultiplier;
        currentDamageMultiplier *= projectileDefinition.DamageMultiplier;
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}
