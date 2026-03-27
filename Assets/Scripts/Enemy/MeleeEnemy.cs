using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
public class MeleeEnemy : Enemy
{
    [Header("攻击")]
    [SerializeField] private int damage;
    [SerializeField] private float attackFrequency;
    private float attackDelay;
    private float attackTimer;
    
    protected virtual void Start()
    {
        base.Start();
        attackDelay = 1f / attackFrequency;
    }

    private void Update()
    {
        if (!CanAttack) return;
        if (attackTimer >= attackDelay)
        {
            TryAttack();
        }
        else
        {
            Wait();
        }
        
        _movement.FollowPlayer();
    }

    private void Wait()
    {
        attackTimer += Time.deltaTime;
    }
    
    private void TryAttack()
    {
        float distanceToPlayer = Vector2.Distance(_player.transform.position, transform.position);

        if (distanceToPlayer <= attackDetectionRadius)
        {
            Attack();
        }
    }
    
    private void Attack()
    {
        _player.TakeDamage(damage);
        attackTimer = 0;
    }
}