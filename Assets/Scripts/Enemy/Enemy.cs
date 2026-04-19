using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(HealthComponent), typeof(Movement), typeof(Attacker))]
public class Enemy : Entity
{
    [Header("组件")]
    [SerializeField] protected ParticleSystem passAwayParticles;
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected SpriteRenderer spawnIndicator;
    [SerializeField] protected new Collider2D collider;
    [SerializeField] private Transform attackOrigin;

    [Header("默认配置")]
    [SerializeField] private float maxHealth = 1f;
    [SerializeField] private EnemyRole role = EnemyRole.Normal;
    [SerializeField] private float attackDetectionRadius = 1f;

    [Header("受击反馈")]
    [SerializeField] private Color hitFlashColor = new(1f, 0.92f, 0.82f, 1f);
    [SerializeField] private Color criticalHitFlashColor = new(1f, 0.7f, 0.3f, 1f);
    [SerializeField] private float hitFlashDuration = 0.08f;
    [SerializeField] private float hitPunchScale = 0.08f;
    [SerializeField] private float criticalHitPunchScale = 0.16f;

    protected Player _player;
    protected HealthComponent healthComponent;
    protected Movement _movement;

    public override Vector2 Center => transform.position + new Vector3(0, collider.offset.y, 0);
    public EnemyRole Role => role;
    public float AttackDetectionRadius => attackDetectionRadius;
    public bool HasAttackController => attacker != null && attacker.HasAttackController;
    public bool CanExecuteAttack => spriteRenderer != null && spriteRenderer.enabled;

    private Tween hitFlashTween;
    private Tween hitPunchTween;
    private Vector3 defaultScale;
    private Color defaultSpriteColor = Color.white;
    private bool runtimeRegistered;
    private EnemyDefinitionSO runtimeDefinition;
    private AttackDefinitionSO runtimeAttackDefinition;
    private EnemyMovementDefinitionSO runtimeMovementDefinition;
    private Attacker attacker;
    private IEnemyMovementExecutor movementExecutor;

    protected virtual void Awake()
    {
        healthComponent = GetComponent<HealthComponent>();
        _movement = GetComponent<Movement>();
        attacker = GetComponent<Attacker>();
        defaultScale = transform.localScale;
        if (spriteRenderer != null)
        {
            defaultSpriteColor = spriteRenderer.color;
        }

        if (attackOrigin == null)
        {
            AttackOrigin attackOriginMarker = GetComponentInChildren<AttackOrigin>();
            attackOrigin = attackOriginMarker != null ? attackOriginMarker.transform : transform;
        }

        if (attacker != null)
        {
            attacker.Initialize(this, attackOrigin);
        }
    }

    protected virtual void OnEnable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDied += PassAway;
        }

        GameEventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
        RegisterRuntime();
    }

    protected virtual void OnDisable()
    {
        UnregisterRuntime();

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
        if (_player == null)
        {
            _player = FindObjectOfType<Player>();
        }

        if (_player == null)
        {
            Debug.LogError("Player not found");
        }

        ApplyResolvedStats();
        BuildMovementExecutorIfNeeded();
        ConfigureAttackerIfNeeded();
        StartSpawnSequence();
    }

    protected virtual void Update()
    {
        bool hasAttacked = attacker != null && attacker.Tick(Time.deltaTime);
        if (!hasAttacked)
        {
            TickMovement(Time.deltaTime);
        }

        UpdateFacing();
    }

    public void Configure(EnemyRuntimeSetup setup)
    {
        runtimeDefinition = setup.Definition;
        _player = setup.Player;
        ApplyResolvedStats();
        BuildMovementExecutorIfNeeded();
        ConfigureAttackerIfNeeded();
    }

    public void PassAway()
    {
        PassAwayAfterWave();
    }

    public void PassAwayAfterWave()
    {
        if (passAwayParticles != null)
        {
            passAwayParticles.transform.SetParent(null);
            passAwayParticles.Play();
        }

        Destroy(gameObject);
    }

    private void ApplyResolvedStats()
    {
        float resolvedMaxHealth = runtimeDefinition != null ? runtimeDefinition.MaxHealth : maxHealth;
        role = runtimeDefinition != null ? runtimeDefinition.Role : role;
        attackDetectionRadius = runtimeDefinition != null ? runtimeDefinition.AttackDetectionRadius : attackDetectionRadius;
        runtimeMovementDefinition = runtimeDefinition != null ? runtimeDefinition.MovementDefinition : null;
        runtimeAttackDefinition = runtimeDefinition != null ? runtimeDefinition.AttackDefinition : null;

        if (healthComponent != null)
        {
            healthComponent.Initialize(resolvedMaxHealth);
        }

        if (_movement != null)
        {
            float resolvedMoveSpeed = runtimeDefinition != null ? runtimeDefinition.MoveSpeed : _movement.MoveSpeed;
            _movement.SetMoveSpeed(resolvedMoveSpeed);
        }
    }

    private void BuildMovementExecutorIfNeeded()
    {
        if (runtimeMovementDefinition == null)
        {
            movementExecutor = null;
            return;
        }

        movementExecutor = EnemyMovementExecutorFactory.Create(new EnemyMovementExecutorBuildContext(this, runtimeMovementDefinition));
    }

    private void ConfigureAttackerIfNeeded()
    {
        if (attacker == null)
        {
            return;
        }

        if (runtimeAttackDefinition == null)
        {
            throw new InvalidOperationException($"{nameof(Enemy)} requires {nameof(AttackDefinitionSO)} from {nameof(EnemyDefinitionSO)} before configuring {nameof(Attacker)}.");
        }

        attacker.Configure(_player, runtimeAttackDefinition, attackDetectionRadius);
    }

    private void TickMovement(float deltaTime)
    {
        if (_movement == null || movementExecutor == null)
        {
            return;
        }

        EnemyMovementContext context = new EnemyMovementContext(this, _player, attackDetectionRadius, deltaTime);
        movementExecutor.Execute(_movement, context);
    }

    private void RegisterRuntime()
    {
        if (runtimeRegistered)
        {
            return;
        }

        GameEventBus.Publish(new EnemyRuntimeRegisteredEvent(this, role));
        runtimeRegistered = true;
    }

    private void UnregisterRuntime()
    {
        if (!runtimeRegistered)
        {
            return;
        }

        GameEventBus.Publish(new EnemyRuntimeUnregisteredEvent(this, role));
        runtimeRegistered = false;
    }

    private void StartSpawnSequence()
    {
        SetRendersVisibility(false);
        if (collider != null)
        {
            collider.enabled = false;
        }

        transform.DOScale(1.2f, 0.3f).SetLoops(5, LoopType.Yoyo).SetEase(Ease.InOutSine)
            .OnComplete(OnSpawnSequenceCompleted);
    }

    private void OnSpawnSequenceCompleted()
    {
        SetRendersVisibility(true);
        if (collider != null)
        {
            collider.enabled = true;
        }
    }

    private void SetRendersVisibility(bool visible)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = visible;
        }

        if (spawnIndicator != null)
        {
            spawnIndicator.enabled = !visible;
        }
    }

    private void OnEntityDamaged(EntityDamagedEvent eventData)
    {
        if (eventData.Entity != this || spriteRenderer == null)
        {
            return;
        }

        FlashOnHit(eventData.HitResult.IsCritical);
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null || _player == null)
        {
            return;
        }

        Vector2 direction = _player.Center - Center;
        if (Mathf.Abs(direction.x) <= Mathf.Epsilon)
        {
            return;
        }

        spriteRenderer.flipX = direction.x < 0f;
    }

    private void FlashOnHit(bool isCritical)
    {
        hitFlashTween?.Kill();
        hitPunchTween?.Kill();

        spriteRenderer.color = isCritical ? criticalHitFlashColor : hitFlashColor;
        transform.localScale = defaultScale;

        hitPunchTween = transform.DOPunchScale(
            Vector3.one * (isCritical ? criticalHitPunchScale : hitPunchScale),
            hitFlashDuration,
            vibrato: 1,
            elasticity: 0.5f);

        hitFlashTween = spriteRenderer.DOColor(defaultSpriteColor, hitFlashDuration)
            .SetEase(Ease.OutQuad)
            .OnKill(() =>
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = defaultSpriteColor;
                }

                transform.localScale = defaultScale;
            });
    }
}
