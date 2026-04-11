using System;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class Weapon : MonoBehaviour
{
    [field: SerializeField] public WeaponDataSO WeaponData { get; private set; }
    [SerializeField] protected float attackDelay;
    protected float attackTimer;
    [SerializeField] protected float damage;
    [SerializeField] protected float aimLerp;
    [SerializeField] protected LayerMask enemyLayerMask;
    [SerializeField] protected float range;
    [SerializeField] protected Animator _animator;

    public int Level { get; private set; }

    [Header("暴击")]
    protected float criticalChance;
    protected float criticalMultiplier;

    protected PropertiesManager propertiesManager;

    protected virtual void Awake()
    {
        propertiesManager = GetComponentInParent<PropertiesManager>();
    }

    protected virtual void OnEnable()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged += UpdateStatus;
            propertiesManager.OnPropertyChanged += OnPropertyChanged;
        }
    }

    protected virtual void OnDisable()
    {
        if (propertiesManager != null)
        {
            propertiesManager.OnAllPropertiesChanged -= UpdateStatus;
            propertiesManager.OnPropertyChanged -= OnPropertyChanged;
        }
    }

    protected virtual void Start()
    {
        UpdateStatus();
    }

    private void OnPropertyChanged(PropType propType, float newValue)
    {
        if (propType == PropType.Attack ||
            propType == PropType.AttackSpeed ||
            propType == PropType.CriticalChance ||
            propType == PropType.CriticalPercent ||
            propType == PropType.Range)
        {
            UpdateStatus();
        }
    }

    protected Enemy GetClosestEnemy()
    {
        Enemy closestEnemy = null;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, range, enemyLayerMask);

        if (colliders.Length <= 0)
        {
            return null;
        }

        float minDistance = range;
        for (int i = 0; i < colliders.Length; i++)
        {
            Enemy enemyChecked = colliders[i].GetComponent<Enemy>();

            float distanceToEnemy = Vector2.Distance(transform.position, enemyChecked.transform.position);

            if (distanceToEnemy < minDistance)
            {
                closestEnemy = enemyChecked;
                minDistance = distanceToEnemy;
            }
        }

        return closestEnemy;
    }

    protected float GetDamage(out bool isCriticalHit)
    {
        isCriticalHit = false;

        if (Random.value <= criticalChance)
        {
            isCriticalHit = true;
            return damage * criticalMultiplier;
        }

        return damage;
    }

    protected virtual void ConfigureProps()
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

        damage = weaponAttack + playerAttack;
        float finalAttackSpeed = Mathf.Max(weaponAttackSpeed * playerAttackSpeedMultiplier, 0.01f);
        attackDelay = 1f / finalAttackSpeed;
        criticalChance = Mathf.Clamp01(weaponCriticalChance + playerCriticalChance);
        criticalMultiplier = Mathf.Max(1f, weaponCriticalMultiplier + playerCriticalBonus);
        range = Mathf.Max(0.1f, weaponRange + playerRange);
    }

    public virtual void UpdateStatus()
    {
        ConfigureProps();
    }

    public void UpgradeTo(int targetLevel)
    {
        Level = targetLevel;
        ConfigureProps();
        UpdateStatus();
    }
}
