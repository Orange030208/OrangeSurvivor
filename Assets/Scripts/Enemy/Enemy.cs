using System;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(EnemyBrain))]
[RequireComponent(typeof(EnemyRuntimeBridge))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PropertiesManager))]
public class Enemy : Entity, IPropGroupProvider
{
    [Header("组件")]
    [SerializeField] private new Collider2D collider;

    private HealthComponent healthComponent;
    private PropertiesManager propertiesManager;
    private EnemyRuntimeBridge runtimeBridge;
    private IMovable activeMovement;
    private Entity targetEntity;
    private EnemySO enemyData;
    private EnemyBrain brain;
    private Rigidbody2D rb;

    public override IMovable MoveComponent => activeMovement;
    public override Vector2 Center => transform.position + new Vector3(0f, collider != null ? collider.offset.y : 0f, 0f);
    public HealthComponent HealthComponent => healthComponent;
    public EnemyRole Role => enemyData.role;
    public Entity TargetEntity => targetEntity;
    public EnemySO EnemyData => enemyData;
    public PropertiesManager PropertiesManager => propertiesManager;
    public EnemyBrain Brain => brain;
    public Rigidbody2D Rb => rb;
    public BasePropGroupSO BasePropsGroup => enemyData.BasePropsAsset;

    private void Awake()
    {
        InitComponentReferences();
    }

    private void Start()
    {
        InitializeComponent();
        OnEnableComponent();
    }

    private void Update()
    {
        Tick();
    }

    private void FixedUpdate()
    {
        FixedTick();
    }

    private void OnDisable()
    {
        OnDisableComponent();
    }

    private void InitComponentReferences()
    {
        healthComponent = GetComponent<HealthComponent>();
        propertiesManager = GetComponent<PropertiesManager>();
        runtimeBridge = GetComponent<EnemyRuntimeBridge>();
        activeMovement = GetComponent<IMovable>();
        brain = GetComponent<EnemyBrain>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Configure(EnemySO enemyData, Entity target)
    {
        if (enemyData == null)
        {
            throw new ArgumentNullException(nameof(enemyData), $"{nameof(Enemy)} requires a non-null {nameof(EnemySO)}.");
        }

        if (target == null)
        {
            throw new ArgumentNullException(nameof(target), $"{nameof(Enemy)} requires an explicit non-null {nameof(Entity)} target.");
        }

        this.enemyData = enemyData;
        targetEntity = target;
    }

    public void PassAwayAfterWave()
    {
        runtimeBridge?.PassAwayAfterWave();
    }
}
