using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(RangeEnemyAttack))]
public class RangeEnemy:Enemy
{
    private RangeEnemyAttack _attacker;
    
    protected override void Start()
    {
        base.Start();

        _attacker = GetComponent<RangeEnemyAttack>();
        _attacker.SetTarget(_player);
    }
    
    private void Update()
    {
        if (!CanAttack) return;
        ManageAttack();

        transform.localScale =
            _player.transform.position.x > transform.position.x ? Vector3.one : Vector3.one.With(x: -1);
    }

    private void ManageAttack()
    {
        float distanceToPlayer = Vector2.Distance(_player.transform.position, transform.position);

        if (distanceToPlayer > attackDetectionRadius)
        {
            _movement.FollowPlayer();
        }
        else
        {
            TryAttack();
        }
    }

    private void TryAttack()
    {
        _attacker.AutoAim();
    }
}