using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(IAnimatable))]
[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PropertiesManager))]
public class Enemy : Entity, IPropGroupProvider, IAnimationConfigProvider, IEntityAttackDefinitionProvider
{
    private IAnimatable animComponent;
    private HealthComponent healthComponent;
    private PropertiesManager propertiesManager;
    private IMovable activeMovement;
    private Entity targetEntity;
    private EnemySO enemyData;
    private EnemyBrain brain;
    private Rigidbody2D rb;
    private bool isRuntimeRegistered;

    public override IMovable MoveComponent => activeMovement;

    public IAnimatable AnimComponent => animComponent;
    public HealthComponent HealthComponent => healthComponent;
    public EnemyRole Role => enemyData != null ? enemyData.role : EnemyRole.Normal;
    public Entity TargetEntity => targetEntity;
    public EnemySO EnemyData => enemyData;
    public PropertiesManager PropertiesManager => propertiesManager;
    public EnemyBrain Brain => brain;
    public Rigidbody2D Rb => rb;
    public BasePropGroupSO BasePropsGroup => enemyData.BasePropsAsset;
    public EntityAnimationConfig AnimationConfig => enemyData.AnimConfig;

    public IReadOnlyList<EnemyAttackDefinitionSO> AttackDefinitions =>
        enemyData != null ? enemyData.GetAttackDefinitions() : Array.Empty<EnemyAttackDefinitionSO>();

    private void Awake()
    {
        InitComponentReferences();
    }

    private void Start()
    {
        InitializeComponent();
        EnableAllComponents();
    }

    private void Update()
    {
        TickAllComponents();
    }

    private void FixedUpdate()
    {
        FixedTickAllComponents();
    }

    private void OnDisable()
    {
        UnregisterRuntime();
        DisableAllComponents();
    }

    private void InitComponentReferences()
    {
        animComponent = GetComponent<IAnimatable>();
        healthComponent = GetComponent<HealthComponent>();
        propertiesManager = GetComponent<PropertiesManager>();
        activeMovement = GetComponent<IMovable>();
        brain = GetComponent<EnemyBrain>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Configure(EnemySO enemyData, Entity target)
    {
        if (enemyData == null)
        {
            throw new ArgumentNullException(nameof(enemyData),
                $"{nameof(Enemy)} requires a non-null {nameof(EnemySO)}.");
        }

        if (target == null)
        {
            throw new ArgumentNullException(nameof(target),
                $"{nameof(Enemy)} requires an explicit non-null {nameof(Entity)} target.");
        }

        UnregisterRuntime();

        this.enemyData = enemyData;
        targetEntity = target;
        RegisterRuntime();
    }

    public override void EnableRuntime()
    {
        base.EnableRuntime();
        brain?.StartBrain();
        activeMovement?.EnableMovement();

        EntityCollider.enabled = true;
    }

    public override void DisableRuntime()
    {
        base.DisableRuntime();
        brain?.StopBrain();
        activeMovement?.StopMoving();
        activeMovement?.DisableMovement();

        rb.velocity = Vector2.zero;

        EntityCollider.enabled = false;
    }

    public void DefeatSilently()
    {
        if (IsRuntimeEnabled)
        {
            DisableRuntime();
        }

        UnregisterRuntime();
        Destroy(gameObject);
    }

    private void RegisterRuntime()
    {
        if (isRuntimeRegistered)
        {
            return;
        }

        isRuntimeRegistered = true;
        GameEventBus.Publish(new EnemyRegisteredEvent(this, Role));
    }

    private void UnregisterRuntime()
    {
        if (!isRuntimeRegistered)
        {
            return;
        }

        isRuntimeRegistered = false;
        GameEventBus.Publish(new EnemyUnregisteredEvent(this, Role));
    }
}