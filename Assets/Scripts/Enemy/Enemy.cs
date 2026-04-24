using System;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(EnemyBrain))]
[RequireComponent(typeof(EnemyRuntimeBridge))]
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : Entity
{
    [Header("组件")]
    [SerializeField] private new Collider2D collider;
    [SerializeField] private EntityRenderer entityRenderer;

    private HealthComponent healthComponent;
    private EnemyRuntimeBridge runtimeBridge;
    private IMovable activeMovement;
    private Entity targetEntity;
    private EnemySO enemyData;
    private EnemyBrain brain;
    private Rigidbody2D rb;
    
    public override IMovable MoveComponent => activeMovement;
    public override Vector2 Center => transform.position + new Vector3(0f, collider != null ? collider.offset.y : 0f, 0f);
    public override EntityRenderer EntityRenderer => entityRenderer;
    public HealthComponent HealthComponent => healthComponent;
    public EnemyRole Role => enemyData.role;
    public Entity TargetEntity => targetEntity;
    public EnemySO EnemyData => enemyData;
    public EnemyBrain Brain => brain;
    public Rigidbody2D Rb => rb;

    private void Awake()
    {
        healthComponent = GetComponent<HealthComponent>();
        runtimeBridge = GetComponent<EnemyRuntimeBridge>();
        activeMovement =  GetComponent<IMovable>();
        brain = GetComponent<EnemyBrain>();
        rb =  GetComponent<Rigidbody2D>();
        
        if (entityRenderer == null)
        {
            entityRenderer = GetComponentInChildren<EntityRenderer>();
        }

    }

    private void Start()
    {
        if (enemyData == null)
        {
            throw new InvalidOperationException($"{nameof(Enemy)} must be configured by factory or spawner before runtime. Missing {nameof(EnemySO)}.");
        }

        if (targetEntity == null)
        {
            throw new InvalidOperationException($"{nameof(Enemy)} must receive an explicit target {nameof(Entity)} from factory or spawner before runtime.");
        }

        InitializeComponent();
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
