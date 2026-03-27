using System;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : Weapon
{
    enum State
    {
        Idle,
        Attack
    }

    private State _state;

    [SerializeField] private Transform hitDetectionTransform;
    [SerializeField] private BoxCollider2D hitCollider;
    [SerializeField] private float hitDetectionRadius;
    private List<Enemy> damagedEnemies = new List<Enemy>();

    private void Start()
    {
        _state = State.Idle;
    }

    private void Update()
    {
        switch (_state)
        {
            case State.Idle:
                AutoAim();
                break;
            case State.Attack:
                Attacking();
                break;
        }
    }

    [NaughtyAttributes.Button]
    private void StartAttack()
    {
        _animator.Play("Attack");
        _state = State.Attack;
        damagedEnemies.Clear();

        _animator.speed = 1f / attackDelay;
    }

    private void Attacking()
    {
        Attack();
    }

    private void StopAttack()
    {
        _state = State.Idle;
        damagedEnemies.Clear();
    }

    private void AutoAim()
    {
        Enemy closestEnemy = GetClosestEnemy();

        Vector2 targetUpVector = Vector3.up;

        if (closestEnemy != null)
        {
            targetUpVector = (closestEnemy.transform.position - transform.position).normalized;
            transform.up = targetUpVector;
            ManageAttack();
        }

        transform.up = Vector3.Lerp(transform.up, targetUpVector, aimLerp * Time.deltaTime);
        IncrementAttackTimer();
    }

    private void ManageAttack()
    {
        if (attackTimer >= attackDelay)
        {
            attackTimer = 0;
            StartAttack();
        }
    }

    private void IncrementAttackTimer()
    {
        attackTimer += Time.deltaTime;
    }

    private void Attack()
    {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(hitDetectionTransform.position, hitCollider.bounds.size,
            hitDetectionTransform.localEulerAngles.z, enemyLayerMask);

        for (int i = 0; i < colliders.Length; i++)
        {
            Enemy enemy = colliders[i].GetComponent<Enemy>();
            if (!damagedEnemies.Contains(enemy))
            {
                int finalDamage = GetDamage(out bool isCriticalHit);

                enemy.TakeDamage(new DamageInfo(finalDamage, enemy.transform.position, isCriticalHit));
                damagedEnemies.Add(enemy);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, range);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitDetectionTransform.position, hitDetectionRadius);
    }
}