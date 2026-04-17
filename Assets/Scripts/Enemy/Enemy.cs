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

    [Header("受击反馈")]
    [SerializeField] private Color hitFlashColor = new(1f, 0.92f, 0.82f, 1f);
    [SerializeField] private Color criticalHitFlashColor = new(1f, 0.7f, 0.3f, 1f);
    [SerializeField] private float hitFlashDuration = 0.08f;
    [SerializeField] private float hitPunchScale = 0.08f;
    [SerializeField] private float criticalHitPunchScale = 0.16f;

    protected Player _player;
    protected HealthComponent healthComponent;

    [SerializeField] protected float attackDetectionRadius;

    public override Vector2 Center => transform.position + new Vector3(0, collider.offset.y, 0);

    private Tween hitFlashTween;
    private Tween hitPunchTween;
    private Vector3 defaultScale;
    private Color defaultSpriteColor = Color.white;

    protected virtual void Awake()
    {
        healthComponent = GetComponent<HealthComponent>();
        defaultScale = transform.localScale;
        if (spriteRenderer != null)
        {
            defaultSpriteColor = spriteRenderer.color;
        }
    }

    protected virtual void OnEnable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDied += PassAway;
        }

        GameEventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
    }

    protected virtual void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDied -= PassAway;
        }

        GameEventBus.Unsubscribe<EntityDamagedEvent>(OnEntityDamaged);
        hitFlashTween?.Kill();
        hitPunchTween?.Kill();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = defaultSpriteColor;
        }

        transform.localScale = defaultScale;
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
        PassAwayAfterWave();
    }

    public void PassAwayAfterWave()
    {
        passAwayParticles.transform.SetParent(null);
        passAwayParticles.Play();
        Destroy(gameObject);
    }

    private void OnEntityDamaged(EntityDamagedEvent eventData)
    {
        if (eventData.Entity != this || spriteRenderer == null)
        {
            return;
        }

        Color flashColor = eventData.HitResult.IsCritical ? criticalHitFlashColor : hitFlashColor;
        float punchStrength = eventData.HitResult.IsCritical ? criticalHitPunchScale : hitPunchScale;

        hitFlashTween?.Kill();
        hitPunchTween?.Kill();

        spriteRenderer.color = flashColor;
        hitFlashTween = spriteRenderer.DOColor(defaultSpriteColor, hitFlashDuration).SetEase(Ease.OutQuad);

        transform.localScale = defaultScale;
        hitPunchTween = transform.DOPunchScale(Vector3.one * punchStrength, hitFlashDuration * 1.6f, 8, 0.6f)
            .OnComplete(() => transform.localScale = defaultScale);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDetectionRadius);
    }
}
