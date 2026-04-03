using System;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class Weapon:MonoBehaviour,IPlayerStatusDependency
{
    [field:SerializeField] public WeaponDataSO WeaponData { get;private set; }
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
    protected float criticalPercent;

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

        int rand = Random.Range(0, 101);
        if (rand <= criticalChance)
        {
            isCriticalHit = true;
            return damage * (criticalPercent / 100);
        }
        
        return damage;
    }

    protected void ConfigureProperties()
    {
        //TODO:不要每次都构建字典
        float multiplier = 1 + (float)Level / 6;
        damage = WeaponData.GetPropValue(PropType.Attack) * multiplier;
        attackDelay = 1f/(WeaponData.GetPropValue(PropType.AttackSpeed) * multiplier);
        criticalChance = (int)(WeaponData.GetPropValue(PropType.CriticalChance) * multiplier);
        criticalPercent = WeaponData.GetPropValue(PropType.CriticalPercent) * multiplier;

        //只有远程武器加射程
        if (WeaponData.WeaponPrefab.GetType() == typeof(RangeWeapon))
        {
            range = WeaponData.GetPropValue(PropType.Range) * multiplier;
        }
    }
    
    public abstract void UpdateStatus(PropertiesManager propertiesManager);

    public void UpgradeTo(int targetLevel)
    {
        Level = targetLevel;
        ConfigureProperties();
    }
}