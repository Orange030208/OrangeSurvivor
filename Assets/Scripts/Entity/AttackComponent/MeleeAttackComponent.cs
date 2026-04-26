using UnityEngine;

public class MeleeAttackComponent : EnemyAttackBase
{
    [Header("攻击配置")][SerializeField] private float attackRange;
    [SerializeField] private float attackInterval;
    [SerializeField] private float attackDamage;

    private Entity owner;
    private float attackTimer;

    public override Entity Owner => owner;
    public override bool CanAttack => attackTimer <= 0f;
    public override float AttackInterval => attackInterval;


    public override void Initialize(Entity owner)
    {
        base.Initialize(owner);
        this.owner = owner;
        RefreshProps();
    }

    public override void OnEnableComponent()
    {
        BindProperties();
    }

    public override void OnDisableComponent()
    {
        UnbindProperties();
    }

    public override void Tick(float deltaTime)
    {
        if (attackTimer > 0f)
        {
            attackTimer -= deltaTime;
        }
    }

    public override bool IsInAttackRange(Entity target)
    {
        if (target == null)
        {
            return false;
        }

        return Vector3.Distance(transform.position, target.Center) <= attackRange;
    }

    public override void TryAttack(Entity target)
    {
        if (!CanAttack || target == null)
        {
            return;
        }

        if (IsInAttackRange(target))
        {
            HealthComponent health = target.GetComponent<HealthComponent>();
            health.ApplyHitResult(HitService.Apply(new HitRequest(owner, target, HitSpec.EnemyHitSpec(attackDamage), owner.Center, HitSourceKind.Direct, GetType().Name)));
        }

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
            RefreshProps();
        }
    }

    private void OnAllPropertiesChanged()
    {
        RefreshProps();
    }

    private void RefreshProps()
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
