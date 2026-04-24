using UnityEngine;

public class MeleeAttackComponent : EnemyAttackBase
{
    [Header("攻击配置")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackInterval = 0.8f;
    [SerializeField] private float attackDamage = 20f;
    private Entity owner;
    public override Entity Owner => Owner;
    
    private float attackTimer;
    
    public override bool CanAttack => attackTimer <= 0;
    
    public override float AttackInterval
    {
        get => attackInterval;
        set => attackInterval = value;
    }

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

    public override bool IsInAttackRange(Entity target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.Center) <= attackRange;
    }
    
    public override void TryAttack(Entity target)
    {
        if (!CanAttack || target == null) return;

        // 近战伤害判定
        if (IsInAttackRange(target))
        {
            HealthComponent health = target.GetComponent<HealthComponent>();
            health.ApplyHitResult(HitService.Apply(new HitRequest(owner, target,HitSpec.EnemyHitSpec(attackDamage),owner.Center,HitSourceKind.Direct,GetType().Name)));
        }

        attackTimer = attackInterval;
    }
    
    public override void ResetAttackTimer()
    {
        attackTimer = 0;
    }
}