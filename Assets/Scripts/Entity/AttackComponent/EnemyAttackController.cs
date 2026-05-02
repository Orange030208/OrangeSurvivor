using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackController : EntityComponentBase, IProjectileLauncher, IEntityAttackExecutor
{
    /// <summary>
    /// 单个攻击定义的运行时冷却槽。
    /// 槽位不描述攻击行为本身，只负责记录“这份 EnemyAttackDefinitionSO 还要冷却多久”。
    /// </summary>
    [Serializable]
    private sealed class AttackSlot
    {
        [SerializeField] private EnemyAttackDefinitionSO attackDefinition;

        private float cooldownTimer;

        public AttackSlot(EnemyAttackDefinitionSO attackDefinition)
        {
            this.attackDefinition = attackDefinition;
        }

        public EnemyAttackDefinitionSO AttackDefinition => attackDefinition;
        public bool CanAttack => cooldownTimer <= 0f;
        public float CooldownTimer => cooldownTimer;

        public void Tick(float deltaTime)
        {
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= deltaTime;
            }
        }

        public void CommitCooldown()
        {
            cooldownTimer = attackDefinition != null ? attackDefinition.Cooldown : 0f;
        }

        public void ResetCooldown()
        {
            cooldownTimer = 0f;
        }
    }

    [Header("Attack Definitions")]
    [Tooltip("运行时冷却槽。通常不需要在 Prefab 上手动填，Controller 会从 Enemy 提供的 AttackDefinitions 注册槽。")]
    [SerializeField] private List<AttackSlot> attackSlots = new();

    [Header("Runtime References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Collider2D ownerCollider;

    private readonly Dictionary<EnemyAttackDefinitionSO, AttackSlot> attackSlotLookup = new();
    private LayerMask attackLayer;
    private Entity owner;
    private IEntityAttackDefinitionProvider attackDefinitionProvider;
    private PropertiesManager propertiesManager;
    private float attackDamage;
    private float attackInterval;

    public override Entity Owner => owner;
    public float AttackInterval => attackInterval;
    public LayerMask AttackLayer
    {
        get => attackLayer;
        set => attackLayer = value;
    }

    public Transform FirePoint => firePoint != null ? firePoint : transform;
    public override int Priority => EntityComponentBase.PriorityPreset.RelyOthers;

    public override void Initialize(Entity owner)
    {
        this.owner = owner;
        attackLayer = LayerMask.GetMask("Player");
        attackDefinitionProvider = owner as IEntityAttackDefinitionProvider;
        propertiesManager = owner.GetComponent<PropertiesManager>();
        if (propertiesManager == null)
        {
            throw new MissingComponentException($"{nameof(EnemyAttackController)} requires a {nameof(PropertiesManager)} on {owner.name}.");
        }

        if (ownerCollider == null)
        {
            ownerCollider = owner.GetComponent<Collider2D>();
        }

        RebuildLookup();
        RefreshRuntimeStats();
    }

    public override void OnEnableComponent()
    {
        BindProperties();
    }

    public override void OnDisableComponent()
    {
        UnbindProperties();
    }

    public override void OnTick(float deltaTime)
    {
        // 所有运行时自动补出的槽都会被加入 attackSlots，
        // 因此这里统一驱动显式槽与自动槽的冷却计时。
        for (int i = 0; i < attackSlots.Count; i++)
        {
            attackSlots[i]?.Tick(deltaTime);
        }
    }

    public bool IsInAttackRange(EnemyAttackDefinitionSO attackDefinition, Entity target)
    {
        if (attackDefinition == null || attackDefinition.IsNoAttack || target == null)
        {
            return false;
        }

        float range = ResolveRange(attackDefinition);
        if (attackDefinition.HitShape != null)
        {
            AttackHitContext context = CreateHitContext(attackDefinition, target, range);
            return attackDefinition.HitShape.Contains(context);
        }

        return Vector2.Distance(owner.Center, target.Center) <= range;
    }

    public bool TryUse(EnemyAttackDefinitionSO attackDefinition, Entity target)
    {
        if (attackDefinition == null || target == null || !CanUse(attackDefinition))
        {
            return false;
        }

        bool executed = attackDefinition.ExecutionKind switch
        {
            EnemyAttackExecutionKind.DirectDamage => TryApplyDirectDamage(attackDefinition, target),
            EnemyAttackExecutionKind.Projectile => TryFireProjectile(attackDefinition, target),
            EnemyAttackExecutionKind.None => false,
            _ => false
        };

        if (!executed)
        {
            return false;
        }

        CommitCooldown(attackDefinition);
        return true;
    }

    public bool CanUse(EnemyAttackDefinitionSO attackDefinition)
    {
        // None 是显式“这个状态不攻击”的定义，保留配置可见性，但不会进入出手流程。
        return attackDefinition != null && !attackDefinition.IsNoAttack && GetOrCreateSlot(attackDefinition).CanAttack;
    }

    public bool TryUseDirectDamageOverride(
        EnemyAttackDefinitionSO attackDefinition,
        Entity target,
        AttackHitShapeSO hitShapeOverride,
        bool commitCooldown)
    {
        if (attackDefinition == null || target == null || attackDefinition.IsNoAttack)
        {
            return false;
        }

        if (attackDefinition.ExecutionKind != EnemyAttackExecutionKind.DirectDamage)
        {
            Debug.LogWarning($"{nameof(EnemyAttackController)} on {name} cannot use {attackDefinition.DisplayName} as direct damage: execution kind is {attackDefinition.ExecutionKind}.", this);
            return false;
        }

        if (!IsInAttackRange(attackDefinition, target, hitShapeOverride))
        {
            return false;
        }

        ApplyDirectDamage(attackDefinition, target);

        if (commitCooldown)
        {
            CommitCooldown(attackDefinition);
        }

        return true;
    }

    private bool IsInAttackRange(
        EnemyAttackDefinitionSO attackDefinition,
        Entity target,
        AttackHitShapeSO hitShapeOverride)
    {
        if (attackDefinition == null || attackDefinition.IsNoAttack || target == null)
        {
            return false;
        }

        float range = ResolveRange(attackDefinition);
        AttackHitShapeSO hitShape = hitShapeOverride != null ? hitShapeOverride : attackDefinition.HitShape;
        if (hitShape != null)
        {
            AttackHitContext context = CreateHitContext(attackDefinition, target, range);
            return hitShape.Contains(context);
        }

        return Vector2.Distance(owner.Center, target.Center) <= range;
    }

    public void CommitCooldown(EnemyAttackDefinitionSO attackDefinition)
    {
        if (attackDefinition != null)
        {
            AttackSlot slot = GetOrCreateSlot(attackDefinition);
            slot.CommitCooldown();
            attackInterval = attackDefinition.Cooldown;
        }
    }

    public void ResetAllAttackCooldowns()
    {
        for (int i = 0; i < attackSlots.Count; i++)
        {
            attackSlots[i]?.ResetCooldown();
        }
    }

    public void ResetAttackCooldown(EnemyAttackDefinitionSO attackDefinition)
    {
        if (attackDefinition != null)
        {
            GetOrCreateSlot(attackDefinition).ResetCooldown();
        }
    }

    public void LaunchProjectile(IProjectile projectile, in ProjectileLaunchContext context)
    {
        projectile.Launch(context);
    }

    private bool TryApplyDirectDamage(EnemyAttackDefinitionSO attackDefinition, Entity target)
    {
        if (!IsInAttackRange(attackDefinition, target))
        {
            return false;
        }

        ApplyDirectDamage(attackDefinition, target);
        return true;
    }

    private void ApplyDirectDamage(EnemyAttackDefinitionSO attackDefinition, Entity target)
    {
        float damage = Mathf.Max(0f, attackDamage * attackDefinition.DamageMultiplier);
        Vector2 knockbackDirection = target.Center - owner.Center;
        HitService.Apply(new HitRequest(
            owner,
            target,
            HitSpec.EnemyHitSpec(damage),
            target.Center,
            knockbackDirection,
            HitSourceKind.Direct,
            attackDefinition.Id,
            sourcePosition: owner.Center));
    }

    private bool TryFireProjectile(EnemyAttackDefinitionSO attackDefinition, Entity target)
    {
        if (attackDefinition.ProjectileDefinition == null)
        {
            Debug.LogWarning($"{nameof(EnemyAttackController)} on {name} cannot fire {attackDefinition.DisplayName}: missing projectile definition.", this);
            return false;
        }

        Vector2 direction = target.Center - owner.Center;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        Projectile projectile = ProjectileFactory.CreateProjectile(
            attackDefinition.ProjectileDefinition,
            FirePoint.position,
            Quaternion.identity);

        float damage = Mathf.Max(0f, attackDamage * attackDefinition.DamageMultiplier);
        LaunchProjectile(projectile, new ProjectileLaunchContext(
            this,
            owner,
            FirePoint.position,
            direction.normalized,
            HitSpec.EnemyHitSpec(damage),
            AttackLayer,
            attackDefinition.ProjectileDefinition));

        return true;
    }

    private AttackHitContext CreateHitContext(EnemyAttackDefinitionSO attackDefinition, Entity target, float range)
    {
        Vector2 direction = target != null ? target.Center - owner.Center : Vector2.right;
        return new AttackHitContext(
            owner,
            target,
            owner.Center,
            direction,
            range,
            AttackLayer,
            ownerCollider);
    }

    private float ResolveRange(EnemyAttackDefinitionSO attackDefinition)
    {
        float range = attackDefinition.RangeSource switch
        {
            AttackRangeSource.FixedValue => attackDefinition.FixedRange,
            AttackRangeSource.AttackRangeProp => propertiesManager.GetPropValue(PropType.AttackRange),
            AttackRangeSource.DetectionRangeProp => propertiesManager.GetPropValue(PropType.DetectionRange),
            AttackRangeSource.OwnerCollider => ResolveOwnerColliderRange(),
            _ => attackDefinition.FixedRange
        };

        return Mathf.Max(0f, range * attackDefinition.RangeMultiplier);
    }

    private float ResolveOwnerColliderRange()
    {
        if (ownerCollider == null)
        {
            return 0f;
        }

        Bounds bounds = ownerCollider.bounds;
        return Mathf.Max(bounds.extents.x, bounds.extents.y);
    }

    private void RebuildLookup()
    {
        attackSlotLookup.Clear();

        // 先登记 Inspector 中显式配置的槽。它们只是冷却槽，不再表达“默认攻击”。
        for (int i = 0; i < attackSlots.Count; i++)
        {
            AttackSlot slot = attackSlots[i];
            if (slot?.AttackDefinition == null)
            {
                continue;
            }

            if (!attackSlotLookup.ContainsKey(slot.AttackDefinition))
            {
                attackSlotLookup.Add(slot.AttackDefinition, slot);
            }
        }

        if (attackDefinitionProvider == null)
        {
            return;
        }

        IReadOnlyList<EnemyAttackDefinitionSO> attackDefinitions = attackDefinitionProvider.AttackDefinitions;
        for (int i = 0; i < attackDefinitions.Count; i++)
        {
            EnemyAttackDefinitionSO attackDefinition = attackDefinitions[i];
            if (attackDefinition != null && !attackSlotLookup.ContainsKey(attackDefinition))
            {
                RegisterRuntimeSlot(attackDefinition);
            }
        }
    }

    private AttackSlot GetOrCreateSlot(EnemyAttackDefinitionSO attackDefinition)
    {
        if (attackSlotLookup.TryGetValue(attackDefinition, out AttackSlot slot))
        {
            return slot;
        }

        return RegisterRuntimeSlot(attackDefinition);
    }

    private AttackSlot RegisterRuntimeSlot(EnemyAttackDefinitionSO attackDefinition)
    {
        AttackSlot slot = new AttackSlot(attackDefinition);
        attackSlotLookup.Add(attackDefinition, slot);
        attackSlots.Add(slot);
        return slot;
    }

    private void OnPropertyChanged(PropType propType, float _)
    {
        if (propType == PropType.Attack || propType == PropType.AttackSpeed)
        {
            RefreshRuntimeStats();
        }
    }

    private void OnAllPropertiesChanged()
    {
        RefreshRuntimeStats();
    }

    private void RefreshRuntimeStats()
    {
        attackDamage = propertiesManager.GetPropValue(PropType.Attack);
        float attackSpeed = Mathf.Max(propertiesManager.GetPropValue(PropType.AttackSpeed), 0.01f);
        attackInterval = attackSlots.Count > 0 && attackSlots[0]?.AttackDefinition != null
            ? attackSlots[0].AttackDefinition.Cooldown
            : 1f / attackSpeed;
    }

    private void BindProperties()
    {
        UnbindProperties();
        propertiesManager.OnAllPropertiesChanged += OnAllPropertiesChanged;
        propertiesManager.OnPropertyChanged += OnPropertyChanged;
    }

    private void UnbindProperties()
    {
        if (propertiesManager == null)
        {
            return;
        }

        propertiesManager.OnAllPropertiesChanged -= OnAllPropertiesChanged;
        propertiesManager.OnPropertyChanged -= OnPropertyChanged;
    }
}
