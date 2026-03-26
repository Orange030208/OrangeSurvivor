using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
public class Enemy : MonoBehaviour
{
    private Player _player;
    private EnemyMovement _movement;
    [SerializeField] private ParticleSystem passAwayParticles;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer spawnIndicator;
    private bool _hasSpawned = false;
    
    [Header("攻击")]
    [SerializeField] private int damage;
    [SerializeField] private float attackFrequency;
    [SerializeField] private float attackDetectionRadius;
    private float attackDelay;
    private float attackTimer;
    
    [Header("生命值")]
    private int health;
    [SerializeField] private int maxHealth;

    private void Start()
    {
        health = maxHealth;
        
        _player = FindObjectOfType<Player>();

        _movement = GetComponent<EnemyMovement>();

        if (_player == null)
        {
            Debug.LogError("Player not found");
        }

        SetRendersVisibility(false);
        
        transform.DOScale(1.2f, .3f).SetLoops(5, LoopType.Yoyo).SetEase(Ease.InOutSine)
            .OnComplete(SpawnSequenceComplete);
        
        attackDelay = 1f / attackFrequency;
        Debug.Log($"攻击延迟{attackDelay}");
    }

    private void Update()
    {
        if (attackTimer >= attackDelay)
        {
            TryAttack();
        }
        else
        {
            Wait();
        }
    }

    public void TakeDamage(int damage)
    {
        int realDamage = Math.Min(health, damage);
        health -= realDamage;

        if (health <= 0)
        {
            PassAway();
        }
    }

    private void SpawnSequenceComplete()
    {
        SetRendersVisibility(true);
        _hasSpawned = true;
        _movement.SetTarget(_player);
    }

    private void SetRendersVisibility(bool visible)
    {
        spriteRenderer.enabled = visible;
        spawnIndicator.enabled = !visible;
    }
    
    private void Wait()
    {
        attackTimer += Time.deltaTime;
    }
    
    private void TryAttack()
    {
        float distanceToPlayer = Vector2.Distance(_player.transform.position, transform.position);

        if (distanceToPlayer < attackDetectionRadius)
        {
            Attack();
        }
    }
    
    private void Attack()
    {
        _player.TakeDamage(damage);
        attackTimer = 0;
    }

    private void PassAway()
    {
        passAwayParticles.transform.SetParent(null);
        passAwayParticles.Play();

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDetectionRadius);
    }
}