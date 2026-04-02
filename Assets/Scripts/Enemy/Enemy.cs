using System;
using DG.Tweening;
using UnityEngine;

public abstract class Enemy : Entity
{
    [Header("组件")]
    protected EnemyMovement _movement;
    [SerializeField] protected ParticleSystem passAwayParticles;
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected SpriteRenderer spawnIndicator;
    [SerializeField] protected Collider2D collider;
    
    [Header("生命值")]
    [SerializeField] protected float maxHealth;
    protected float health;
    
    protected Player _player;
    protected bool _hasSpawned = false;
    
    [SerializeField] protected float attackDetectionRadius;
    
    public static Action<DamageInfo> onDamageTaken;
    public static Action<DeadInfo> onDeath;

    public override Vector2 Center => transform.position;

    protected virtual void Start()
    {
        health = maxHealth;
        _player = FindObjectOfType<Player>();
        _movement = GetComponent<EnemyMovement>();
        if (_player == null)
        {
            Debug.LogError("Player not found");
        }

        StartSpawnSequence();
    }

    private void Update()
    {
        if (!spriteRenderer.enabled) return;
    }

    protected virtual bool CanAttack => spriteRenderer.enabled;

    protected void StartSpawnSequence()
    {
        SetRendersVisibility(false);
        
        transform.DOScale(1.2f, .3f).SetLoops(5, LoopType.Yoyo).SetEase(Ease.InOutSine)
            .OnComplete(OnSpawnSequenceCompleted);
    }
    
    private void OnSpawnSequenceCompleted()
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
    
    public void TakeDamage(DamageInfo damageInfo)
    {
        float realDamage = Math.Min(health, damageInfo.damage);
        health -= realDamage;
        onDamageTaken?.Invoke(damageInfo);

        if (health <= 0)
        {
            PassAway();
        }
    }
    
    private void PassAway()
    {
        passAwayParticles.transform.SetParent(null);
        passAwayParticles.Play();
        onDeath.Invoke(new DeadInfo(transform.position));
        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDetectionRadius);
    }
}