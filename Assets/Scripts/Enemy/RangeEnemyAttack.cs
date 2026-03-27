using System;
using UnityEngine;

public class RangeEnemyAttack : MonoBehaviour
{
    [SerializeField] private Transform shootingPoint;
    [SerializeField] private EnemyBullet bulletPrefab;
    private Player _target;

    [SerializeField]private int damage;
    [SerializeField] private float attackFrequency;
    private float attackDelay;
    private float attackTimer;

    private void Start()
    {
        attackDelay = 1 / attackFrequency;
        attackTimer = attackDelay;
    }

    public void SetTarget(Player target)
    {
        _target = target;
    }

    public void AutoAim()
    {
        ManageShoot();
    }

    private void ManageShoot()
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
        Vector2 direction = (_target.Center - (Vector2)shootingPoint.position).normalized;
        
        EnemyBullet enemyBullet = Instantiate(bulletPrefab,shootingPoint.position, Quaternion.identity);
        enemyBullet.Shoot(direction,damage,false);
    }
}