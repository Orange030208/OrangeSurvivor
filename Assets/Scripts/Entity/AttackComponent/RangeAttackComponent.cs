using System;
using UnityEngine;

public class RangeAttackComponent : EnemyAttackBase, IProjectileLauncher
{
    [SerializeField] private float attackRange;
    [SerializeField] private float attackInterval;
    [SerializeField] private float attackDamage;
    [SerializeField] private ProjectileDefinitionSO projectileDefinition;
    [SerializeField] private Transform firePoint;

    private float attackTimer;
    private Entity owner;

    public override float AttackInterval => attackInterval;
    public override bool CanAttack => attackTimer <= 0f;
    public override Entity Owner => owner;
    public ProjectileDefinitionSO ProjectileDefinition => projectileDefinition;
    public Transform FirePoint => firePoint != null ? firePoint : transform;
    public float AttackDamage => attackDamage;
    public float AttackRange => attackRange;

    public override void Initialize(Entity owner)
    {
        base.Initialize(owner);
        this.owner = owner;
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
        if (attackTimer > 0f)
        {
            attackTimer -= deltaTime;
        }
    }

    public override void TryAttack(Entity target)
    {
        if (!CanAttack || target == null)
        {
            return;
        }

        Vector2 aimDirection = (target.Center - owner.Center).normalized;
        FireProjectile(aimDirection);
        CommitAttackCooldown();
    }

    public bool TryAttackDirection(Vector2 direction)
    {
        if (!CanAttack || direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        FireProjectile(direction);
        CommitAttackCooldown();
        return true;
    }

    public void FireProjectile(Vector2 direction)
    {
        if (owner == null || projectileDefinition == null || direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector2 normalizedDirection = direction.normalized;
        Projectile projectile = ProjectileFactory.CreateProjectile(projectileDefinition, FirePoint.position, Quaternion.identity);
        LaunchProjectile(projectile, new ProjectileLaunchContext(
            this,
            owner,
            FirePoint.position,
            normalizedDirection,
            HitSpec.EnemyHitSpec(attackDamage),
            AttackLayer,
            projectileDefinition));
    }

    public override bool IsInAttackRange(Entity target)
    {
        if (target == null)
        {
            return false;
        }

        return Vector3.Distance(transform.position, target.Center) <= attackRange;
    }

    public void LaunchProjectile(IProjectile projectile, in ProjectileLaunchContext context)
    {
        projectile.Launch(context);
    }

    public void CommitAttackCooldown()
    {
        attackTimer = attackInterval;
    }

    public override void ResetAttackTimer()
    {
        attackTimer = 0f;
    }

    private void OnPropertyChanged(PropType propType, float _)
    {
        if (propType == PropType.Attack ||
            propType == PropType.AttackSpeed ||
            propType == PropType.Range)
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
        attackRange = propertiesManager.GetPropValue(PropType.Range);

        float attackSpeed = Mathf.Max(propertiesManager.GetPropValue(PropType.AttackSpeed), 0.01f);
        attackInterval = 1f / attackSpeed;
    }

    private void BindProperties()
    {
        UnbindProperties();
        propertiesManager.OnAllPropertiesChanged += OnAllPropertiesChanged;
        propertiesManager.OnPropertyChanged += OnPropertyChanged;
    }

    private void UnbindProperties()
    {
        propertiesManager.OnAllPropertiesChanged -= OnAllPropertiesChanged;
        propertiesManager.OnPropertyChanged -= OnPropertyChanged;
    }
}
