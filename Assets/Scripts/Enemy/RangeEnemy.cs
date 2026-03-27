using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(RangeEnemyAttack))]
public class RangeEnemy:MonoBehaviour
{
    private Player _player;
    private EnemyMovement _movement;
    [SerializeField] private ParticleSystem passAwayParticles;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer spawnIndicator;
    [SerializeField] private Collider2D collider;
    private RangeEnemyAttack _attacker;
    private bool _hasSpawned = false;
    
    [Header("攻击")]
    [SerializeField] private int damage;
    [SerializeField] private float attackFrequency;
    [SerializeField] private float attackDetectionRadius;
    private float attackDelay;
    private float attackTimer;

    public static Action<int,Vector2> onDamageTaken;
    
    [Header("生命值")]
    private int health;
    [SerializeField] private int maxHealth;
    
    private void Start()
    {
        health = maxHealth;
        
        _player = FindObjectOfType<Player>();

        _attacker = GetComponent<RangeEnemyAttack>();

        _movement = GetComponent<EnemyMovement>();
        
        _attacker.SetTarget(_player);

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
        if (!spriteRenderer.enabled) return;

        ManageAttack();
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

    private void SpawnSequenceComplete()
    {
        SetRendersVisibility(true);
        _hasSpawned = true;
        collider.enabled = true;
        _movement.SetTarget(_player);
    }

    private void SetRendersVisibility(bool visible)
    {
        spriteRenderer.enabled = visible;
        spawnIndicator.enabled = !visible;
    }
    
    private void TryAttack()
    {
        _attacker.AutoAim();
    }
    
    public void TakeDamage(int damage)
    {
        int realDamage = Math.Min(health, damage);
        health -= realDamage;
        onDamageTaken?.Invoke(realDamage,transform.position);

        if (health <= 0)
        {
            PassAway();
        }
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