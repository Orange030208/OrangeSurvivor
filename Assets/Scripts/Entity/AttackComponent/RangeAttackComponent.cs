using UnityEngine;
using UnityEngine.Tilemaps;

public class RangeAttackComponent : EnemyAttackBase,IProjectileLauncher
{
    [Header("攻击配置")]
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private ProjectileDefinitionSO projectileDefinition;
    [SerializeField] private Transform firePoint;
    
    private float attackTimer;
    private Entity owner;

    public override float AttackInterval
    {
        get => attackInterval;
        set => attackInterval = value;
    }

    public override bool CanAttack => attackTimer <= 0;

    public override Entity Owner => owner;

    public override void Initialize(Entity owner)
    {
        base.Initialize(owner);
        this.owner = owner;
    }

    private void Update()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }
    }
    
    public override void TryAttack(Entity target)
    {
        if (!CanAttack || target == null) return;
        
        Vector2 aimDirection = (target.Center - owner.Center).normalized;

        Projectile projectile = ProjectileFactory.CreateProjectile(projectileDefinition, firePoint.position, Quaternion.identity);
        // 生成投射物
        LaunchProjectile(projectile, new ProjectileLaunchContext(
            this,
            owner,
            firePoint.position,
            aimDirection,
            HitSpec.EnemyHitSpec(attackDamage),
            AttackLayer,
            projectileDefinition));

        // 重置冷却
        attackTimer = attackInterval;
    }

    public override bool IsInAttackRange(Entity target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.Center) <= attackRange;
    }

    public void LaunchProjectile(IProjectile projectile, in ProjectileLaunchContext context)
    {
        projectile.Launch(context);
    }

    public override void ResetAttackTimer()
    {
        attackTimer = 0;
    }
}