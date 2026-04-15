using UnityEngine;

/// <summary>
/// 玩家投射物基类。
/// 当前负责：
/// - 按发射上下文设置方向与速度；
/// - 处理寿命；
/// - 命中目标时结算伤害；
/// - 根据弹射物定义和变体覆盖值应用基础倍率。
/// 后续如果要做穿透、弹射、分裂、持续伤害等，可以在子类里扩展。
/// </summary>
[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected LayerMask targetsLayerMask;
    [SerializeField] protected float maxLifetime = 5f;

    [Header("Variant Overrides")]
    [Tooltip("按老的变体索引思路保留的速度覆盖位。当前若没有 ProjectileDefinitionSO 倍率，也仍可回退使用基础值。")]
    [SerializeField] private float[] variantSpeedOverrides;
    [Tooltip("按老的变体索引思路保留的生存时间覆盖位。")]
    [SerializeField] private float[] variantLifetimeOverrides;
    [Tooltip("按老的变体索引思路保留的伤害倍率覆盖位。")]
    [SerializeField] private float[] variantDamageMultipliers;

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

    /// <summary>
    /// 初始化一颗子弹，并根据弹射物定义应用基础速度/寿命/伤害倍率。
    /// </summary>
    public virtual void Launch(ProjectileLaunchContext context)
    {
        launchContext = context;
        ApplyProjectileDefinition(context.ProjectileDefinition);
        transform.position = context.SpawnPosition;
        transform.right = context.Direction;
        rb.velocity = context.Direction * currentMoveSpeed;
        OnLaunched(context);
    }

    /// <summary>
    /// 提供给子类的扩展点。
    /// 例如：根据 burstId 做不同特效，或根据 firingMode 改变拖尾表现。
    /// </summary>
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
        DamageInfo damageInfo = launchContext.Hit.ToDamageInfo(healthComponent.transform.position);
        damageInfo.damage = Mathf.Max(0f, damageInfo.damage * currentDamageMultiplier);
        healthComponent.TakeDamage(damageInfo);
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

    private float ResolveVariantValue(float[] values, int variantIndex, float fallback)
    {
        if (values == null || values.Length == 0)
        {
            return fallback;
        }

        int clampedIndex = Mathf.Clamp(variantIndex, 0, values.Length - 1);
        return values[clampedIndex] > 0f ? values[clampedIndex] : fallback;
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}
