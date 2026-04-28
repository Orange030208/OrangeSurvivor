using UnityEngine;

public class MeleeAttackComponent : EnemyAttackBase
{
    [Header("攻击配置")][SerializeField] private float detectionRange;
    [SerializeField] private float attackRange;
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

    public override void OnTick(float deltaTime)
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

        return Vector3.Distance(transform.position, target.Center) <= detectionRange;
    }

    public override void TryAttack(Entity target)
    {
        if (!CanAttack || target == null)
        {
            return;
        }

        ApplyDamage(target);
        attackTimer = attackInterval;
    }

    public override void ResetAttackTimer()
    {
        attackTimer = 0f;
    }

    private void ApplyDamage(Entity target)
    {
        if (target == null || !IsWithinAttackRange(target))
        {
            return;
        }

        HitService.Apply(new HitRequest(owner, target, HitSpec.EnemyHitSpec(attackDamage), owner.Center, HitSourceKind.Direct, GetType().Name));
    }

    private bool IsWithinAttackRange(Entity target)
    {
        return Vector3.Distance(transform.position, target.Center) <= attackRange;
    }

    private void OnPropertyChanged(PropType propType, float _)
    {
        if (propType == PropType.Attack ||
            propType == PropType.AttackSpeed ||
            propType == PropType.DetectionRange ||
            propType == PropType.AttackRange)
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
        detectionRange = propertiesManager.GetPropValue(PropType.DetectionRange);
        attackRange = propertiesManager.GetPropValue(PropType.AttackRange);
        if (attackRange <= 0f)
        {
            attackRange = detectionRange;
        }

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
