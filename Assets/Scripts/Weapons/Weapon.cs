using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class Weapon : MonoBehaviour
{
    [field: SerializeField] public WeaponDataSO WeaponData { get; private set; }
    [SerializeField] protected float aimLerp = 12f;
    [SerializeField] protected LayerMask enemyLayerMask;
    [SerializeField] protected Animator animator;

    public int Level { get; private set; }
    public WeaponRuntimeStats RuntimeStats { get; private set; }
    public bool IsAttacking { get; protected set; }

    protected PropertiesManager propertiesManager;
    protected Enemy currentTarget;
    private float attackCooldownTimer;

    protected virtual void Awake()
    {
        propertiesManager = GetComponentInParent<PropertiesManager>();
    }

    protected virtual void OnEnable()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged += RefreshRuntimeStats;
            propertiesManager.OnPropertyChanged += OnPropertyChanged;
        }
    }

    protected virtual void OnDisable()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= RefreshRuntimeStats;
            propertiesManager.OnPropertyChanged -= OnPropertyChanged;
        }
    }

    protected virtual void Start()
    {
        RefreshRuntimeStats();
    }

    protected virtual void Update()
    {
        TickTargeting();
        TickWeapon(Time.deltaTime);
    }

    public void SetLevel(int targetLevel)
    {
        Level = Mathf.Max(1, targetLevel);
        RefreshRuntimeStats();
    }

    public virtual void RefreshRuntimeStats()
    {
        RuntimeStats = BuildRuntimeStats();
    }

    protected virtual void TickWeapon(float deltaTime)
    {
        attackCooldownTimer += deltaTime;

        if (currentTarget == null)
        {
            return;
        }

        if (!CanStartAttack())
        {
            return;
        }

        if (attackCooldownTimer < RuntimeStats.AttackInterval)
        {
            return;
        }

        attackCooldownTimer = 0f;
        BeginAttack(currentTarget);
    }

    protected virtual bool CanStartAttack()
    {
        return !IsAttacking;
    }

    protected abstract void BeginAttack(Enemy target);

    protected void CompleteAttackCycle()
    {
        IsAttacking = false;
    }

    protected Enemy GetCurrentTarget()
    {
        return currentTarget;
    }

    protected virtual void TickTargeting()
    {
        currentTarget = FindClosestEnemyInRange(RuntimeStats.Range);

        Vector2 targetUpVector = Vector2.up;
        if (currentTarget != null)
        {
            targetUpVector = ((Vector2)currentTarget.transform.position - (Vector2)transform.position).normalized;
        }

        if (targetUpVector.sqrMagnitude > 0.0001f)
        {
            transform.up = Vector3.Lerp(transform.up, targetUpVector, Time.deltaTime * aimLerp);
        }
    }

    protected Enemy FindClosestEnemyInRange(float searchRange)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, searchRange, enemyLayerMask);
        Enemy closestEnemy = null;
        float minDistance = searchRange;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].TryGetComponent(out Enemy enemyChecked))
            {
                continue;
            }

            float distanceToEnemy = Vector2.Distance(transform.position, enemyChecked.transform.position);
            if (distanceToEnemy < minDistance)
            {
                closestEnemy = enemyChecked;
                minDistance = distanceToEnemy;
            }
        }

        return closestEnemy;
    }

    protected ResolvedWeaponHit ResolveHit()
    {
        bool isCritical = Random.value <= RuntimeStats.CriticalChance;
        float damage = isCritical ? RuntimeStats.Damage * RuntimeStats.CriticalMultiplier : RuntimeStats.Damage;
        return new ResolvedWeaponHit(damage, isCritical);
    }

    protected WeaponAttackContext BuildAttackContext(Enemy target, Transform origin = null)
    {
        Transform sourceTransform = origin != null ? origin : transform;
        Vector2 aimDirection = target != null
            ? ((Vector2)target.transform.position - (Vector2)sourceTransform.position).normalized
            : (Vector2)transform.up;

        return new WeaponAttackContext(this, sourceTransform, target, aimDirection, RuntimeStats, ResolveHit());
    }

    protected virtual WeaponRuntimeStats BuildRuntimeStats()
    {
        var calculatedProps = WeaponPropsCalculator.GetProps(WeaponData, Level);

        float weaponAttack = calculatedProps[PropType.Attack];
        float weaponAttackSpeed = Mathf.Max(calculatedProps[PropType.AttackSpeed], 0.01f);
        float weaponCriticalChance = Mathf.Clamp01(calculatedProps[PropType.CriticalChance]);
        float weaponCriticalMultiplier = Mathf.Max(1f, calculatedProps[PropType.CriticalPercent]);
        float weaponRange = calculatedProps[PropType.Range];

        float playerAttack = propertiesManager != null ? propertiesManager.GetPropValue(PropType.Attack) : 0f;
        float playerAttackSpeedMultiplier = propertiesManager != null ? Mathf.Max(propertiesManager.GetPropValue(PropType.AttackSpeed), 0.01f) : 1f;
        float playerCriticalChance = propertiesManager != null ? propertiesManager.GetPropValue(PropType.CriticalChance) : 0f;
        float playerCriticalBonus = propertiesManager != null ? propertiesManager.GetPropValue(PropType.CriticalPercent) : 0f;
        float playerRange = propertiesManager != null ? propertiesManager.GetPropValue(PropType.Range) : 0f;

        float damage = weaponAttack + playerAttack;
        float finalAttackSpeed = Mathf.Max(weaponAttackSpeed * playerAttackSpeedMultiplier, 0.01f);
        float attackInterval = 1f / finalAttackSpeed;
        float criticalChance = Mathf.Clamp01(weaponCriticalChance + playerCriticalChance);
        float criticalMultiplier = Mathf.Max(1f, weaponCriticalMultiplier + playerCriticalBonus);
        float range = Mathf.Max(0.1f, weaponRange + playerRange);

        return new WeaponRuntimeStats(damage, attackInterval, range, criticalChance, criticalMultiplier);
    }

    private void OnPropertyChanged(PropType propType, float _)
    {
        if (propType == PropType.Attack ||
            propType == PropType.AttackSpeed ||
            propType == PropType.CriticalChance ||
            propType == PropType.CriticalPercent ||
            propType == PropType.Range)
        {
            RefreshRuntimeStats();
        }
    }
}
