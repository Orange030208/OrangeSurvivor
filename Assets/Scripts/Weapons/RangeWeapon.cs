using System;
using UnityEngine;

public class RangeWeapon : Weapon
{
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private Bullet bulletPrefab;

    private void Update()
    {
        AutoAim();
    }

    private void AutoAim()
    {
        Enemy closestEnemy = GetClosestEnemy();

        Vector2 targetUpVector = Vector3.up;

        if (closestEnemy != null)
        {
            targetUpVector = (closestEnemy.transform.position - transform.position).normalized;
            transform.up = targetUpVector;
            ManageShooting();
            return;
        }

        transform.up = Vector3.Lerp(transform.up, targetUpVector, Time.deltaTime * aimLerp);
    }

    private void ManageShooting()
    {
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackDelay)
        {
            attackTimer = 0;
            Shoot();
        }
    }

    private void Shoot()
    {
        float finalDamage = GetDamage(out bool isCriticalHit);
        Bullet bullet = Instantiate(bulletPrefab, shootingPoint.position, Quaternion.identity);
        bullet.Shoot(transform.up, finalDamage, isCriticalHit);
    }

    //TODO:属性初始加载有问题，后续要修改
    public override void UpdateStatus(PropertiesManager propertiesManager)
    {
        ConfigureProperties();
        damage = propertiesManager.GetPropValue(PropType.Attack) + damage;
        attackDelay = attackDelay / (1 + propertiesManager.GetPropValue(PropType.AttackSpeed) / 100);

        criticalChance += propertiesManager.GetPropValue(PropType.CriticalChance);
        criticalPercent += propertiesManager.GetPropValue(PropType.CriticalPercent);
        
        range +=  propertiesManager.GetPropValue(PropType.Range);
    }
}