using System;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    enum State
    {
        Idle,
        Attack
    }

    private State _state;

    [SerializeField] private Animator _animator;
    [SerializeField] private float range;
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float aimLerp;
    [SerializeField] private Transform hitDetectionTransform;
    [SerializeField] private BoxCollider2D hitCollider;
    [SerializeField] private float hitDetectionRadius;
    [SerializeField] private int damage = 1;
    private List<Enemy> damagedEnemies = new List<Enemy>();
    [SerializeField] private float attackDelay;
    private float attackTimer;

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
                enemy.TakeDamage(damage);
                damagedEnemies.Add(enemy);
            }
        }
    }

    private Enemy GetClosestEnemy()
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, range);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hitDetectionTransform.position, hitDetectionRadius);
    }
}