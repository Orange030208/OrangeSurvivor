using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public abstract class Enemy : Entity
{
    [Header("组件")] protected EnemyMovement _movement;
    [SerializeField] protected ParticleSystem passAwayParticles;
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected SpriteRenderer spawnIndicator;
    [SerializeField] protected new Collider2D collider;

    [Header("生命值")]
    [SerializeField] protected float maxHealth = 1f;

    protected Player _player;
    protected HealthComponent healthComponent;

    [SerializeField] protected float attackDetectionRadius;

    public override Vector2 Center => transform.position;

    protected virtual void Awake()
    {
        healthComponent = GetComponent<HealthComponent>();
    }

    protected virtual void OnEnable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDied += PassAway;
        }
    }

    protected virtual void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDied -= PassAway;
        }
    }

    protected virtual void Start()
    {
        if (healthComponent != null)
        {
            healthComponent.Initialize(maxHealth);
        }

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
        collider.enabled = true;
        _movement.SetTarget(_player);
    }

    private void SetRendersVisibility(bool visible)
    {
        spriteRenderer.enabled = visible;
        spawnIndicator.enabled = !visible;
    }

    public void PassAway()
    {
        GameEventBus.Publish(new EntityDiedEvent(this, transform.position));
        PassAwayAfterWave();
    }

    public void PassAwayAfterWave()
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
