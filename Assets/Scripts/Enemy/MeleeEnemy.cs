using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
public class MeleeEnemy : Enemy
{
    [Header("攻击")]
    [SerializeField] private int damage;
    [SerializeField] private float attackFrequency;
    private float attackDelay;
    private float attackTimer;
    
    protected override void Start()
    {
        base.Start();
        attackDelay = 1f / attackFrequency;
    }

    private void Update()
    {
        if (!GameSimulation.IsRunning)
        {
            return;
        }

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
        HealthComponent healthComponent = _player.GetComponent<HealthComponent>();
        if (healthComponent != null && healthComponent.OwnerEntity != null)
        {
            HitService.Apply(new HitRequest(
                this,
                healthComponent.OwnerEntity,
                new HitSpec(damage, 0f, 1f),
                healthComponent.transform.position,
                HitSourceKind.Direct,
                GetType().Name));
        }

        attackTimer = 0f;
    }

    public override bool IsMoving { get; }
    public override Vector2 CurrentFacingDirection { get; }
}
